using Riptide;
using Riptide.Utils;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Player : MonoBehaviour
{
    public static Dictionary<ushort, Player> list = new Dictionary<ushort, Player>();

    public ushort Id { get; private set; }

    public bool IsLocal { get; private set; }

    public short Ping { get; set; }
    public float Health { get; set; } = 100f;
    

    public PlayerController Controller { get; private set; }

    public PlayerInventory inventory;
    public Transform camProxy;
    [SerializeField] private Transform camTransform;
    [SerializeField] private Interpolator interpolator;

    [Header("Visuals")]
    [SerializeField] public Transform shootOrigin; // Assign the "Gun Barrel" or "Eye" transform
    [SerializeField] private GameObject tracerPrefab; // Drag your BulletTracer prefab here
    [SerializeField] private TMP_Text positionText;


    public GameObject bulletHolePrefab;

    public string username;

    public WeaponController WeaponController { get; private set; }


    public void Start()
    {
        WeaponController = GetComponentInChildren<WeaponController>();
        Controller = GetComponent<PlayerController>();
        if (inventory == null)
            inventory = GetComponent<PlayerInventory>();
    }

    private void OnDestroy()
    {
        list.Remove(Id);
    }


    public void Update()
    {
        Debug.DrawRay(camTransform.position, camTransform.forward * 100f, Color.green);
        if (positionText != null)
            positionText.text = $"Pos: {transform.position}\nID: {Id}\nUser: {username}\nPing: {Ping} ms";
        UpdatePing();
    }

    private void UpdatePing()
    {
        if (NetworkManager.Singleton == null || 
            NetworkManager.Singleton.Client == null || 
            !NetworkManager.Singleton.Client.IsConnected)
        {
            Ping = 0;
            return;
        }

        Ping = NetworkManager.Singleton.Client.RTT;
    }

    public void Move(ushort tick, bool didTeleport, Vector3 position)
    {
       
    interpolator.NewUpdate(tick, didTeleport, position);
       
       
    }
   public static void Spawn(ushort id, string username, Vector3 position)
{
    Debug.Log($"[Player.Spawn] Attempting to spawn player. ID: {id}, Username: {username}, Position: {position}");

    Player player;
    if (id == NetworkManager.Singleton.Client.Id)
    {
        Debug.Log($"[Player.Spawn] Spawning LOCAL player.");
        player = Instantiate(GameLogic.Singleton.LocalPlayerPrefab, position, Quaternion.identity).GetComponent<Player>();
        player.IsLocal = true;
        player.Controller = player.GetComponent<PlayerController>(); // Assign Controller for local player
    }
    else
    {
        Debug.Log($"[Player.Spawn] Spawning REMOTE player.");
        player = Instantiate(GameLogic.Singleton.PlayerPrefab, position, Quaternion.identity).GetComponent<Player>();
        player.IsLocal = false;
    }

    player.name = $"Player {id} ({(string.IsNullOrEmpty(username) ? "Guest" : username)})";
    player.Id = id;
    player.username = username;
    
    // Safety check for the camera to prevent NullReference errors
    var cameraComp = player.GetComponentInChildren<Camera>();
    if (cameraComp != null) player.camTransform = cameraComp.transform;

    list.Add(id, player);
    Debug.Log("Added Player to list" );
}


    public void HandleJail(Vector3 jailPosition)
    {
        Debug.Log($"[Client] Player {Id} has been jailed to {jailPosition}");

        // Call the ForceTeleport method on the controller to handle the move and reset the prediction state.
        // This is only necessary for the local player.
        if (Controller != null)
        {
            Controller.ForceTeleport(jailPosition);
        }
        else
        {
            // If this is a remote player (or controller is missing), just snap the position.
            transform.position = jailPosition;
        }
    }


    [MessageHandler((ushort)ServerToClientId.playerSpawned)]
    private static void SpawnPlayer(Message message)
    {
        Spawn(message.GetUShort(), message.GetString(), message.GetVector3());
    }

    [MessageHandler((ushort)ServerToClientId.PlayerMovement)]
    private static void HandlePlayerMovement(Message message)
    {
        ushort id = message.GetUShort();
        ushort tick = message.GetUShort();
        bool didTeleport = message.GetBool();
        Vector3 position = message.GetVector3();
        Vector3 forward = message.GetVector3();

        if (Player.list.TryGetValue(id, out Player player))
        {
            if (player.camProxy != null)
            {
                player.camProxy.forward = forward;
            }

            if (id == NetworkManager.Singleton.Client.Id)
            {
                // Note: The signature of OnServerStateReceived might need to be updated to accept all relevant data
                player.Controller.OnServerStateReceived(tick, didTeleport, position);
            }
            else
            {
                player.Move(tick, didTeleport, position);
            }
        }
        else
        {
            Debug.LogWarning($"[Player.HandlePlayerMovement] Received movement for unknown player ID: {id}");
        }
    }

    [MessageHandler((ushort)ServerToClientId.PlayerShot)]
    private static void HandlePlayerShot(Message message)
    {
        ushort shooterId = message.GetUShort();
        Vector3 shootPosition = message.GetVector3();
        Vector3 shootDirection = message.GetVector3();

        if(Player.list.TryGetValue(shooterId, out Player shooter))
        {
            // Here you would typically play shooting animations or effects
            Debug.Log($"[Client] Player {shooterId} shot from {shootPosition} in direction {shootDirection}");
            if(Physics.Raycast(shootPosition, shootDirection, out RaycastHit hitInfo, 100f))
            {
                shooter.WeaponController.currentGun.SpawnBulletHole(hitInfo);
            }
        }
    }

    [MessageHandler((ushort)ServerToClientId.PlayerDamage)]
    private static void HandlePlayerDamage(Message message)
    {
        ushort damagedPlayerId = message.GetUShort();
        float newHealth = message.GetFloat();
        if(Player.list.TryGetValue(damagedPlayerId, out Player damagedPlayer))
        {
            // Here you would typically update the player's health UI or play a damage animation
            Debug.Log($"[Client] Player {damagedPlayerId} took damage. New Health: {newHealth}");
            damagedPlayer.Health = newHealth;
        }
    }

    [MessageHandler((ushort)ServerToClientId.PlayerDied)]  
    private static void HandlePlayerDeath(Message message)
    {
        ushort deadPlayerId = message.GetUShort();
        if(Player.list.TryGetValue(deadPlayerId, out Player deadPlayer))
        {
            Debug.Log($"[Client] Player {deadPlayerId} has died.");
            // Here you would typically trigger a death animation or respawn logic
            Destroy(deadPlayer.gameObject);
        }
    }


    [MessageHandler((ushort)ServerToClientId.PlayerJailed)]
    private static void HandlePlayerJailed(Message message)
    {
        ushort jailedPlayerId = message.GetUShort();
        Vector3 jailPosition = message.GetVector3();
        if(Player.list.TryGetValue(jailedPlayerId, out Player jailedPlayer))
        {
            jailedPlayer.HandleJail(jailPosition);
            Debug.Log($"[Client] Player {jailedPlayerId} has been jailed.");
        }
    }

    
}
