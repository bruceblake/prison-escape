using UnityEngine;
using TMPro;
using Riptide;

public class PingDisplay : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private TMP_Text pingText;

    [Header("Settings")]
    [SerializeField] private float updateInterval = 0.5f; // Update every 0.5s to stop text jitter

    private float timer;

    private void Update()
    {
        // 1. Safety Check: Are we actually connected?
        if (NetworkManager.Singleton == null || 
            NetworkManager.Singleton.Client == null || 
            !NetworkManager.Singleton.Client.IsConnected)
        {
            pingText.text = "N/A";
            pingText.color = Color.red;
            return;
        }

        // 2. Timer to prevent the text from updating 60 times a second
        timer += Time.deltaTime;
        if (timer >= updateInterval)
        {
            UpdatePingText();
            timer = 0f;
        }
    }

    private void UpdatePingText()
    {
        // GET THE PING
        int rtt = NetworkManager.Singleton.Client.RTT;

        pingText.text = $"{rtt} ms";

        // Color Coding
        if (rtt < 60) 
            pingText.color = Color.green;       // Great
        else if (rtt < 120) 
            pingText.color = Color.yellow;      // Okay
        else 
            pingText.color = Color.red;         // Bad
    }
}