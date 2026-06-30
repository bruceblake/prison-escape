using UnityEngine;
using TMPro;
using Riptide;

public class GameClient : MonoBehaviour
{

    public enum PlayerRole
{
    None,
    Civilian,
    Shooter
}

public enum GameState
{
    Warmup,         // Counting down to role pick
    InProgress,     // Game is active
    Ending          // Game over
}
    public static GameClient Singleton { get; private set; }

    [Header("UI References")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private GameObject timerPanel;
    [SerializeField] private TMP_Text roleRevealText; // Assign a big text in center of screen
    [SerializeField] private GameObject rolePanel;    // Background panel for the text

    [SerializeField] private GameObject playerListPanel;


    public GameObject hitmarker;    
    [SerializeField] private TMP_Text ammoText;
    [SerializeField] private TMP_Text healthText;

    public TMP_Text pickupBombPartText;
    public TMP_Text craftBombText;
    public TMP_Text bombPartsText;

    private void Awake()
    {
        Singleton = this;
    }

    private void Start()
    {
        if (rolePanel) rolePanel.SetActive(false);
        if (timerText) timerText.text = "Waiting...";
    }


    public void Update()
    {
        if(Input.GetKeyDown(KeyCode.Tab))
        {
            playerListPanel.GetComponent<PlayerListPanel>().RefreshPlayerList();
            playerListPanel.SetActive(true);
        }   
        if(Input.GetKeyUp(KeyCode.Tab))
        {
            playerListPanel.SetActive(false);
        }
        
        ammoText.text = $"{Player.list[NetworkManager.Singleton.Client.Id].WeaponController.currentGun.bulletsLeft} / {Player.list[NetworkManager.Singleton.Client.Id].WeaponController.currentGun.bulletCapacity}";
        healthText.text = $"HP: {Player.list[NetworkManager.Singleton.Client.Id].Health.ToString("F0")}";
    }


    // --- Message Handlers ---

    [MessageHandler((ushort)ServerToClientId.GameTimer)]
    private static void HandleTimer(Message message)
    {
        float timeRemaining = message.GetFloat();
        int state = message.GetInt(); // If you sent state

        if (Singleton.timerText != null)
        {
            if (timeRemaining > 0)
                Singleton.timerText.text = $"Roles revealed in: {Mathf.CeilToInt(timeRemaining)}";
            else
            {
                Singleton.timerText.text = "";
                if (Singleton.timerPanel) Singleton.timerPanel.SetActive(false);
            }
        }
    }

    [MessageHandler((ushort)ServerToClientId.ReceiveRole)]
    private static void HandleRoleReceive(Message message)
    {
        // 1. Get the Role
        PlayerRole myRole = (PlayerRole)message.GetInt();

        // 2. Show UI
        if (Singleton.rolePanel) Singleton.rolePanel.SetActive(true);
        
        if (Singleton.roleRevealText)
        {
            if (myRole == PlayerRole.Shooter)
            {
                Singleton.roleRevealText.text = "YOU ARE THE SHOOTER";
                Singleton.roleRevealText.color = Color.red;
            }
            else
            {
                Singleton.roleRevealText.text = "YOU ARE A CIVILIAN";
                Singleton.roleRevealText.color = Color.green;
            }
        }

        // 3. Logic Hook: Enable weapons/abilities based on role
        // Example: LocalPlayerController.Singleton.SetRole(myRole);
        Debug.Log($"[Client] I have been assigned: {myRole}");
    }


}