using UnityEngine;
using Steamworks;
using Riptide;
using TMPro;
using UnityEngine.SceneManagement;

public class FindGame : MonoBehaviour
{
    [Header("Dev Testing")]
    public bool useSteam = true;
    public string devName = "DevPlayer";

    public GameObject mainMenuPanel;
    public GameObject lobbyPanel;

    public TMP_InputField usernameInputField;

    private void OnEnable()
    {
        NetworkManager.OnConnected += GoToLobby;
        NetworkManager.OnDisconnected += GoToMainMenu;
    }

    private void OnDisable()
    {
        NetworkManager.OnConnected -= GoToLobby;
        NetworkManager.OnDisconnected -= GoToMainMenu;
    }

    public void SendNameToServer()
    {
        string nameToSend = "";

        // 1. Priority: Steam Name
        if (useSteam && SteamManager.Initialized)
        {
            nameToSend = SteamFriends.GetPersonaName();
            Debug.Log($"[Client] Using Steam Name: {nameToSend}");
        }
        // 2. Fallback: Input Field (If user typed something)
        else if (usernameInputField != null && !string.IsNullOrEmpty(usernameInputField.text))
        {
            nameToSend = usernameInputField.text;
            Debug.Log($"[Client] Using Input Field Name: {nameToSend}");
        }
        // 3. Fallback: Random Dev Name (If empty)
        else
        {
            nameToSend = $"{devName} {Random.Range(1, 1000)}";
            Debug.Log($"[Client] Using Random Dev Name: {nameToSend}");
        }

        // Send to Server
        Message msg = Message.Create(MessageSendMode.Reliable, ClientToServerId.name);
        msg.AddString(nameToSend);
        NetworkManager.Singleton.Client.Send(msg);
    }

    public void GoToLobby()
    {
        // 1. Send Name immediately
        SendNameToServer();
        
        Debug.Log("Connected to server, switching to lobby panel.");
        
        // 2. Swap UI
        if(mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if(lobbyPanel != null) lobbyPanel.SetActive(true);
    }

    public void GoToMainMenu()
    {
        Debug.Log("Returning to main menu.");
        SceneManager.LoadScene("MainMenu");
        if(lobbyPanel != null) lobbyPanel.SetActive(false);
        if(mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    public void FindGameButton()
    {
        // Disable button to prevent double clicks?
        NetworkManager.Singleton.Connect();
    }
}