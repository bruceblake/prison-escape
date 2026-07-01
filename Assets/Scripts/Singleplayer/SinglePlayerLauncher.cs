using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

public class SingleplayerLauncher : MonoBehaviour
{
    [Header("UI References")]
    public GameObject loadingScreenPanel;
    
    private Process localServerProcess;

    private void OnEnable()
    {
        NetworkManager.OnConnected += HandleConnectionSuccess;
    }

    private void OnDisable()
    {
        NetworkManager.OnConnected -= HandleConnectionSuccess;
    }

    public void LaunchSingleplayer()
    {
        Debug.Log("Booting local server for Singleplayer...");

        string serverPath = Path.Combine(Application.streamingAssetsPath, "Server", "3dgameserver.exe");

        ProcessStartInfo startInfo = new ProcessStartInfo(serverPath);
        
        // --- ADDED ARGUMENT HERE ---
        // This tells the server to only allow 1 player.
        startInfo.Arguments = "-maxPlayers 1"; 
        // ---------------------------

        startInfo.CreateNoWindow = false; 
        startInfo.WindowStyle = ProcessWindowStyle.Normal; // Changed to Normal so you can see the terminal for debugging
        startInfo.UseShellExecute = false;

        try
        {
            localServerProcess = Process.Start(startInfo);
            if (loadingScreenPanel != null) loadingScreenPanel.SetActive(true);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to start local server: {e.Message}");
            return;
        }

        Invoke(nameof(ConnectToLocalHost), 2.0f);
    }

    private void ConnectToLocalHost()
    {
        NetworkManager.Singleton.ConnectToIp("127.0.0.1");
    }

    private void HandleConnectionSuccess()
    {
        Debug.Log("Connected to local server. Waiting for map command...");
    }

    private void OnApplicationQuit()
    {
        if (localServerProcess != null && !localServerProcess.HasExited)
        {
            localServerProcess.Kill();
        }
    }
}