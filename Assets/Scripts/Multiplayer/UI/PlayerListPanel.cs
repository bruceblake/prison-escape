using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class PlayerListPanel : MonoBehaviour
{
   [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject playerEntryPrefab;

    private readonly Dictionary<ushort, GameObject> playerEntries = new Dictionary<ushort, GameObject>();

    private void OnEnable()
    {
        RefreshPlayerList();
    }


    public void RefreshPlayerList()
    {
        foreach (var entry in playerEntries.Values)
            Destroy(entry);
        playerEntries.Clear();

        foreach (var player in Player.list.Values)
        {
            GameObject entry = Instantiate(playerEntryPrefab, contentParent);
            TextMeshProUGUI entryText = entry.GetComponentInChildren<TextMeshProUGUI>();
            if (entryText != null)
                entryText.text = $"ID: {player.Id} - {player.username} - Ping: {player.Ping} ms";
            playerEntries.Add(player.Id, entry);
        }
    }
}
