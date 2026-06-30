using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Riptide;
using TMPro;
using System.Linq;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Singleton { get; private set; }

    public enum LobbyState
    {
        WaitingForPlayers,
        Voting,
        MatchCountdown,
        InMatch
    }

    public LobbyState CurrentState { get; private set; }

    // --- SYNC VARS ---
    private ushort stateEndTick; // The tick when the current timer ends
    // -----------------

    [System.Serializable]
    public struct MapAsset
    {
        public string mapName;   // Must match server spelling exactly
        public Sprite mapImage;
    }

    [Header("Map Assets")]
    public List<MapAsset> mapAssets; 

    [System.Serializable]
    public class MapUI
    {
        public string mapName;          
        public TMP_Text mapNameText;
        public TMP_Text voteCountText;
        public Sprite mapImage;
        public Button voteButton;
    }

    [Header("Map Voting UI")]
    public Transform mapRowContainer;   
    public MapUI[] maps;                

    [Header("Winning Map UI")]
    public GameObject winningMapPanel;  
    public Image winningMapImage;       
    public TMP_Text winningMapNameText; 

    [Header("Player List")]
    public Transform playerRowContainer;
    public GameObject playerEntryPrefab;

    [Header("General UI")]
    public TMP_Text statusText;
    public TMP_Text countdownText;

    private void Awake()
    {
        if (Singleton == null)
        {
             Singleton = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Singleton != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // UI Initialization
        if (mapRowContainer != null) mapRowContainer.gameObject.SetActive(true);
        if (winningMapPanel != null) winningMapPanel.SetActive(false);

        // Build maps[] from the children of mapRowContainer
        int childCount = mapRowContainer.childCount;
        maps = new MapUI[childCount];

        for (int i = 0; i < childCount; i++)
        {
            Transform child = mapRowContainer.GetChild(i);

            var button    = child.GetComponentInChildren<Button>();
            var nameText  = child.Find("MapNameText")?.GetComponent<TMP_Text>();
            var votesText = child.Find("VoteCountText")?.GetComponent<TMP_Text>();
            var image     = child.GetComponentInChildren<Image>();
            maps[i] = new MapUI
            {
                mapName      = "",       
                mapNameText  = nameText,
                voteCountText= votesText,
                voteButton   = button,
                mapImage     = image != null ? image.sprite : null
            };
        }

        // Reset UI Texts
        foreach (var map in maps)
        {
            if (map.mapNameText != null) map.mapNameText.text = "";
            if (map.voteCountText != null) map.voteCountText.text = "0";
        }

        if (statusText != null) statusText.text = "Connected. Waiting for players...";
        if (countdownText != null) countdownText.text = "";

        // Hook up button listeners
        for (int i = 0; i < maps.Length; i++)
        {
            int index = i;
            if (maps[i].voteButton != null)
            {
                maps[i].voteButton.onClick.RemoveAllListeners();
                maps[i].voteButton.onClick.AddListener(() => OnVoteMapButton(index));
            }
        }
    }

    private void FixedUpdate()
    {
        if (CurrentState == LobbyState.Voting || CurrentState == LobbyState.MatchCountdown)
        {
            ushort currentTick = NetworkManager.Singleton.ServerTick;
            int ticksRemaining = GetTickDifference(currentTick, stateEndTick);
            float secondsLeft = ticksRemaining * Time.fixedDeltaTime;
            if (secondsLeft < 0) secondsLeft = 0;

            if (countdownText != null)
            {
                string prefix = CurrentState == LobbyState.MatchCountdown ? "Match starting in" : "Voting ends in";
                countdownText.text = $"{prefix}: {Mathf.CeilToInt(secondsLeft)}s";
            }
        }
        else
        {
            if (countdownText != null && countdownText.text != "") 
                countdownText.text = "";
        }
    }

    private int GetTickDifference(ushort current, ushort target)
    {
        int diff = target - current;
        if (diff < -32768) diff += 65536;
        if (diff > 32768) diff -= 65536;
        return diff;
    }

    public void OnVoteMapButton(int mapIndex)
    {
        if (string.IsNullOrEmpty(maps[mapIndex].mapName)) return;

        var msg = Message.Create(MessageSendMode.Reliable, (ushort)ClientToServerId.MapVote);
        msg.AddInt(mapIndex);
        NetworkManager.Singleton.Client.Send(msg);
    }

    // --- FIX: Add HandleServerSceneLoad for NetworkManager ---
    public void HandleServerSceneLoad()
    {
        Debug.Log("[LobbyManager] Handling Server Scene Load. Hiding Lobby UI.");
        if (mapRowContainer != null) mapRowContainer.gameObject.SetActive(false);
        if (winningMapPanel != null) winningMapPanel.SetActive(false);
        if (countdownText != null) countdownText.text = "";
        if (statusText != null) statusText.text = "Loading Scene...";
    }

    public void ApplyMapOptionsFromServer(string[] mapNames)
    {
        if (winningMapPanel != null) winningMapPanel.SetActive(false);
        if (mapRowContainer != null) mapRowContainer.gameObject.SetActive(true);

        int count = Mathf.Min(mapNames.Length, maps.Length);

        for (int i = 0; i < count; i++)
        {
            maps[i].mapName = mapNames[i];
            if (maps[i].mapNameText != null) maps[i].mapNameText.text = mapNames[i];
            if (maps[i].voteCountText != null) maps[i].voteCountText.text = "0";
            if (maps[i].voteButton != null) maps[i].voteButton.gameObject.SetActive(true);
        }
    }

    public void SetLobbyState(LobbyState state, ushort endTick)
    {
        CurrentState = state;
        stateEndTick = endTick; 

        if (state == LobbyState.InMatch)
        {
            HandleServerSceneLoad();
        }

        if (statusText != null)
        {
            statusText.text = state switch
            {
                LobbyState.WaitingForPlayers => "Waiting for players...",
                LobbyState.Voting => "Voting on maps...",
                LobbyState.MatchCountdown => "Match starting soon...",
                LobbyState.InMatch => "In match",
                _ => statusText.text
            };
        }
    }

    public void SetLobbyRoster(Dictionary<ushort, string> names)
    {
       foreach(Transform child in playerRowContainer) Destroy(child.gameObject);
       foreach(var kvp in names)
       {
            GameObject obj = Instantiate(playerEntryPrefab, playerRowContainer);
            obj.GetComponentInChildren<TMP_Text>().text = kvp.Value;
       }
    }

    public void ShowWinningMapPanel(string mapName)
    {
        Sprite winnerSprite = null;
        var asset = mapAssets.FirstOrDefault(x => x.mapName == mapName);
        if (!string.IsNullOrEmpty(asset.mapName)) winnerSprite = asset.mapImage;

        if (mapRowContainer != null) mapRowContainer.gameObject.SetActive(false);
        if (winningMapPanel != null)
        {
            winningMapPanel.SetActive(true);
            if (winningMapNameText != null) winningMapNameText.text = mapName;
            if (winningMapImage != null && winnerSprite != null) winningMapImage.sprite = winnerSprite;
        }
    }

    public void UpdateMapVotes(string mapName, int count)
    {
        foreach (var map in maps)
        {
            if (map.mapName == mapName && map.voteCountText != null)
                map.voteCountText.text = count.ToString();
        }
    }
}