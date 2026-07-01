using UnityEngine;
using Riptide;
using UnityEngine.SceneManagement;

public class SceneLoaderCallback : MonoBehaviour
{
    private void Start()
    {
        // As soon as this runs, it means the scene is fully loaded on the client
        if(NetworkManager.Singleton == null)
        {
            Debug.LogWarning("[Client] Client is null, cannot send scene loaded message to server. Going to Main Menu");
            SceneManager.LoadScene("MainMenu");
            return;
        }
        else
        {
            Message msg = Message.Create(MessageSendMode.Reliable, ClientToServerId.ClientLoadedGame);
            NetworkManager.Singleton.Client.Send(msg);
            Debug.Log("[Client] Scene loaded. Telling server I am ready.");
        }
    
    }
}