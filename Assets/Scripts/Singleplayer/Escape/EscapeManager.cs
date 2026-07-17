using System;
using Prison;
using Prison.Career;
using UnityEngine;

/// <summary>
/// The win/lose keystone. Crossing the escape boundary completes the run — in a career world that
/// means CAUGHT AND TRANSFERRED up the ladder (never freedom until the top; see
/// docs/PrisonEscape/01 Game Design/Prison Career Ladder.md). Getting spotted inside an active
/// <see cref="RestrictedZone"/> sends the player to solitary confinement (stat penalty,
/// inventory confiscated, suspicion raised, day skipped to the next Morning Roll Call).
/// Spec: docs/PrisonEscape/02 Features/Escape Completion System.md
/// </summary>
public class EscapeManager : MonoBehaviour
{
    public enum EscapeState
    {
        Playing,
        InSolitary,
        Escaped,
    }

    private static EscapeManager _instance;
    public static EscapeManager Instance => _instance;

    [Header("Solitary Confinement")]
    [Tooltip("Spawn points inside the solitary cells. Auto-found under 'SolitaryBlock' if empty.")]
    public Transform[] solitarySpawnPoints;

    [Header("Runtime (Read Only)")]
    [SerializeField] private EscapeState state = EscapeState.Playing;
    [SerializeField] private int daysElapsed;
    [SerializeField] private int timesCaught;
    [SerializeField] private int itemsCrafted;

    private float _runStartRealtime;
    private PrisonEventType _lastDayCountEvent = (PrisonEventType)(-1);

    public EscapeState State => state;
    public int TimesCaught => timesCaught;

    public static EscapeManager EnsureInstance()
    {
        if (_instance != null) return _instance;
        var go = new GameObject("EscapeManager");
        return go.AddComponent<EscapeManager>();
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        _runStartRealtime = Time.realtimeSinceStartup;
    }

    private void Start()
    {
        if (PrisonTimeManager.Instance != null)
            PrisonTimeManager.Instance.OnEventChanged += OnScheduleEvent;
        CraftingSystem.OnItemCrafted += OnItemCrafted;

        PlayerStats.EnsureInstance();
        PrisonSuspicion.EnsureInstance();
        HudBootstrap.EnsureHud();
        PlayerVitalsHUD.EnsureInstance();
        CurrentLocationHUD.EnsureInstance();
        ObjectiveWaypointUI.EnsureInstance();

        if (solitarySpawnPoints == null || solitarySpawnPoints.Length == 0)
            AutoFindSolitarySpawns();
    }

    private void OnDestroy()
    {
        if (PrisonTimeManager.Instance != null)
            PrisonTimeManager.Instance.OnEventChanged -= OnScheduleEvent;
        CraftingSystem.OnItemCrafted -= OnItemCrafted;
        if (_instance == this)
            _instance = null;
    }

    private void OnScheduleEvent(PrisonEventType evt)
    {
        if (!PrisonEventExtensions.IsMorningLineUp(evt))
        {
            _lastDayCountEvent = (PrisonEventType)(-1);
            return;
        }
        if (_lastDayCountEvent == evt) return;
        _lastDayCountEvent = evt;
        daysElapsed++;
    }

    private void OnItemCrafted(CraftingRecipe recipe) => itemsCrafted++;

    private void AutoFindSolitarySpawns()
    {
        var block = GameObject.Find("SolitaryBlock");
        if (block == null) return;

        var spawns = new System.Collections.Generic.List<Transform>();
        foreach (Transform child in block.transform)
        {
            if (child.name.StartsWith("SolitarySpawn_"))
                spawns.Add(child);
        }
        spawns.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        solitarySpawnPoints = spawns.ToArray();
    }

    // ------------------------------------------------------------------
    // WIN
    // ------------------------------------------------------------------

    /// <summary>
    /// Called by <see cref="EscapeBoundary"/> when the player crosses outside the walls.
    /// Succeeding never means freedom below the top of the ladder — the career flow decides
    /// between transfer ceremony, career win, and the sandbox's plain escape screen.
    /// </summary>
    public void OnPlayerEscaped(PrisonerController player)
    {
        if (state == EscapeState.Escaped) return;
        state = EscapeState.Escaped;

        Debug.Log("[EscapeManager] Player crossed the boundary — completing the run.");
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        CareerTransferFlow.OnBoundaryCrossed(BuildRunStats());
    }

    /// <summary>Run stats snapshot for the ceremony screen (kept from the v1 end screen).</summary>
    public EscapeRunStats BuildRunStats()
    {
        float realSeconds = Time.realtimeSinceStartup - _runStartRealtime;
        var t = TimeSpan.FromSeconds(realSeconds);
        string playTime = t.Hours > 0 ? $"{t.Hours}h {t.Minutes}m {t.Seconds}s" : $"{t.Minutes}m {t.Seconds}s";

        string reputation = "OUTSIDER";
        var social = Prison.Social.SocialWorld.Instance;
        if (social != null && social.IsBuilt)
            reputation = social.GetReputationTier().ToString().ToUpperInvariant();

        return new EscapeRunStats
        {
            daysInside = Mathf.Max(1, daysElapsed),
            playTime = playTime,
            timesInSolitary = timesCaught,
            itemsCrafted = itemsCrafted,
            reputation = reputation,
        };
    }

    // ------------------------------------------------------------------
    // CAUGHT ESCAPING → SOLITARY
    // ------------------------------------------------------------------

    /// <summary>
    /// Guard spotted the player inside an active restricted zone: confiscate inventory
    /// (pillow stashes survive), apply stat penalties, raise suspicion, move to solitary,
    /// and skip the schedule to the next Morning Roll Call.
    /// </summary>
    public void OnCaughtEscaping(PrisonerController player, string guardName)
    {
        if (player == null || state != EscapeState.Playing) return;
        state = EscapeState.InSolitary;
        timesCaught++;

        Debug.Log($"[EscapeManager] Player caught escaping by {guardName} — solitary confinement (stay #{timesCaught}).");
        PrisonSecurityAlerts.RaiseSuspicion($"Escape attempt stopped by {guardName}.");
        CareerSession.ApplyCaughtEscapingPenalty();

        var stats = PlayerStats.EnsureInstance();
        float mhBefore = stats.MentalHealth;
        float phBefore = stats.PhysicalHealth;
        float strBefore = stats.Strength;
        stats.ApplySolitaryPenalty();

        PrisonSuspicion.EnsureInstance().RaiseSuspicion();

        var inventory = player.GetComponent<PlayerInventory>();
        if (inventory != null)
            inventory.ClearAllSlots();

        player.SetMovementBlocked(true);
        TeleportToSolitary(player);

        PrisonTimeManager.Instance?.SkipToEventType(PrisonEventType.MorningRollCall);

        SolitaryScreenUI.Show(mhBefore, stats.MentalHealth, phBefore, stats.PhysicalHealth, strBefore, stats.Strength, () =>
        {
            if (state == EscapeState.InSolitary)
                state = EscapeState.Playing;
            player.SetMovementBlocked(false);
        });
    }

    private void TeleportToSolitary(PrisonerController player)
    {
        if (solitarySpawnPoints == null || solitarySpawnPoints.Length == 0)
            AutoFindSolitarySpawns();

        if (solitarySpawnPoints == null || solitarySpawnPoints.Length == 0)
        {
            Debug.LogWarning("[EscapeManager] No solitary spawn points found — sending player to their cell instead.");
            player.SendToCell();
            return;
        }

        var spawn = solitarySpawnPoints[(timesCaught - 1) % solitarySpawnPoints.Length];
        var pc = player.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.ForceTeleport(spawn.position);
        }
        else
        {
            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            player.transform.position = spawn.position;
            player.transform.rotation = spawn.rotation;
            if (cc != null) cc.enabled = true;
        }
    }
}
