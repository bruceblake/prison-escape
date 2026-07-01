using Riptide;
using Riptide.Utils;
using System;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public enum ServerToClientId : ushort
{
    playerSpawned = 1,
    LobbyState,
    MapVoteUpdate,
    MatchStarting,
    MapOptions,
    Timer,
    PlayerJoinedLobby,
    LoadGameScene,
    ReceiveRole,
    GameTimer,
    PlayerMovement,
    Sync,
    PlayerShot,
    PlayerHealth,
    PlayerDied,
    PlayerJailed,
    PlayerDamage,
    InventoryUpdated,
    CraftingSuccess,
}

public enum ClientToServerId : ushort
{
    name = 1,
    MapVote,
    ClientLoadedGame,
    PlayerInput,
    PlayerShoot,
    WeaponChange,
    TryPickupItem,
    TryCraftItem,
}

public class NetworkManager : MonoBehaviour
{
    private static NetworkManager _singleton;

    public static NetworkManager Singleton
    {
        get => _singleton;
        private set
        {
            if (_singleton == null)
                _singleton = value;
            else if (_singleton != value)
            {
                Debug.Log($"{nameof(NetworkManager)} instance already exists, destroying duplicate!");
                Destroy(value);
            }
        }
    }

    public Client Client { get; private set; }

    public static ushort CurrentTick { get; private set; }

    private ushort _serverTick;
    public ushort ServerTick
    {
        get => _serverTick;
        private set
        {
            _serverTick = value;
            InterpolationTick = (ushort)(value - TicksBetweenPositionUpdates);
        }
    }

    public ushort InterpolationTick { get; private set; }
    private ushort _ticksBetweenPositionUpdates = 2;
    public ushort TicksBetweenPositionUpdates
    {
        get => _ticksBetweenPositionUpdates;
        set
        {
            _ticksBetweenPositionUpdates = value;
            InterpolationTick = (ushort)(ServerTick - value);
        }
    }

    public bool IsConnected = false;

    public static event Action OnConnected;
    public static event Action OnDisconnected;

    [SerializeField] private ushort port;
    [SerializeField] private string ip;
    [SerializeField] private ushort tickDivergenceTolerance = 1;
    [SerializeField] private TMP_Text statusText;

    private void Awake()
    {
        Singleton = this;
    }

    private void Start()
    {
        RiptideLogger.Initialize(Debug.Log, true);

        Client = new Client();
        Client.Connected += DidConnect;
        Client.ConnectionFailed += FailedToConnect;
        Client.ClientDisconnected += PlayerLeft;
        Client.Disconnected += DidDisconnect;

        ServerTick = 2;
        CurrentTick = 2;

        DontDestroyOnLoad(gameObject);
    }

    private void FixedUpdate()
    {
        Client.Update();
        
        if (ServerTick != 0)
        {
            ServerTick++;
            CurrentTick++; 
        }
    }

    private void OnApplicationQuit()
    {
        Client.Disconnect();
    }

    public void Connect()
    {
        statusText.text = "Connecting...";
        Client.Connect($"{ip}:{port}");
    }

    private void DidConnect(object sender, EventArgs e)
    {
        statusText.text = "Connected";
        IsConnected = true;
        OnConnected?.Invoke();
        Debug.Log("Connected to server");
    }

    private void FailedToConnect(object sender, EventArgs e)
    {
        statusText.text = "Failed to connect";
        OnDisconnected?.Invoke();
    }

    private void PlayerLeft(object sender, ClientDisconnectedEventArgs e)
    {
        if (Player.list.TryGetValue(e.Id, out Player player))
        {
            Destroy(player.gameObject);
        }
    }

    private void DidDisconnect(object sender, EventArgs e)
    {
        statusText.text = "Disconnected";
        OnDisconnected?.Invoke();
        foreach (Player player in Player.list.Values)
        {
            Destroy(player.gameObject);
        }
    }

    public void ConnectToIp(string overrideIp)
    {
        statusText.text = "Starting Local Session...";
        Client.Connect($"{overrideIp}:{port}");
    }

    private void SetTick(ushort serverTick)
    {
        if (Mathf.Abs(ServerTick - serverTick) > tickDivergenceTolerance)
        {
            ServerTick = serverTick;
        }
    }

    [MessageHandler((ushort)ServerToClientId.Sync)]
    public static void SyncTick(Message message)
    {
        ushort serverTick = message.GetUShort();
        NetworkManager.Singleton.SetTick(serverTick);
        CurrentTick = serverTick;
    }

    // --- NEW MESSAGE HANDLER ---
    [MessageHandler((ushort)ServerToClientId.LoadGameScene)]
    private static void HandleLoadScene(Message message)
    {
        string sceneName = message.GetString();
        Debug.Log($"[Client] Server commanded scene change to: {sceneName}");
        
        // Let the LobbyManager handle UI cleanup before we switch
        if (LobbyManager.Singleton != null)
        {
            LobbyManager.Singleton.HandleServerSceneLoad();
        }

        SceneManager.LoadScene(sceneName);
    }
}