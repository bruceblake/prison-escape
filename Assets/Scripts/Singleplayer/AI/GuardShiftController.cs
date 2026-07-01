using UnityEngine;
using UnityEngine.AI;
using Prison;

/// <summary>
/// Defines how a guard is configured when spawned from <see cref="GameManager"/> (one row per guard instance).
/// </summary>
public enum GuardSpawnRole
{
    StandardPatrol,
    NightCellVerifier,
    MorningShakedown,
}

[System.Serializable]
public class GuardSpawnEntry
{
    [Tooltip("Renames the instance in the Hierarchy for debugging.")]
    public string displayName;

    [Tooltip("Exact spawn position/rotation for this guard.")]
    public Transform spawnPoint;

    [Tooltip("Patrol waypoints for Standard / Night verifier (when not sweeping). Leave empty to keep prefab defaults.")]
    public Transform[] patrolWaypoints;

    public GuardSpawnRole role = GuardSpawnRole.StandardPatrol;

    [Tooltip("If empty, this guard is always active. If set, NavMeshAgent + behaviour scripts are only enabled during these schedule events.")]
    public PrisonEventType[] onDutyDuring;
}

/// <summary>
/// Enables/disables guard behaviour by schedule so multiple guards can share one prefab with different roles and shifts.
/// Attach to the guard prefab (or added at runtime by GameManager).
/// </summary>
[DisallowMultipleComponent]
public class GuardShiftController : MonoBehaviour
{
    private GuardSpawnRole _role;
    private PrisonEventType[] _onDutyDuring;

    private GuardFSM _fsm;
    private GuardDetection _detection;
    private NavMeshAgent _agent;
    private MorningShakedownSweeper _sweeper;

    private void Awake()
    {
        CacheComponents();
    }

    private void Start()
    {
        if (PrisonTimeManager.Instance != null)
            PrisonTimeManager.Instance.OnEventChanged += OnPrisonEvent;

        var evt = PrisonTimeManager.Instance != null ? PrisonTimeManager.Instance.CurrentEvent : default;
        ApplyShift(evt);
    }

    private void OnDestroy()
    {
        if (PrisonTimeManager.Instance != null)
            PrisonTimeManager.Instance.OnEventChanged -= OnPrisonEvent;
    }

    private void CacheComponents()
    {
        _fsm = GetComponent<GuardFSM>();
        _detection = GetComponent<GuardDetection>();
        _agent = GetComponent<NavMeshAgent>();
        _sweeper = GetComponent<MorningShakedownSweeper>();
    }

    /// <summary>Called by GameManager immediately after Instantiate (before this component's Start).</summary>
    public void Initialize(GuardSpawnRole role, PrisonEventType[] onDutyDuring)
    {
        CacheComponents();
        _role = role;
        _onDutyDuring = onDutyDuring != null && onDutyDuring.Length > 0 ? (PrisonEventType[])onDutyDuring.Clone() : null;
    }

    private void OnPrisonEvent(PrisonEventType evt) => ApplyShift(evt);

    private bool IsOnDutyFor(PrisonEventType current)
    {
        if (_onDutyDuring == null || _onDutyDuring.Length == 0)
            return true;
        for (int i = 0; i < _onDutyDuring.Length; i++)
        {
            if (_onDutyDuring[i] == current)
                return true;
        }
        return false;
    }

    private void ApplyShift(PrisonEventType currentEvent)
    {
        bool onShift = IsOnDutyFor(currentEvent);

        if (!onShift)
        {
            SetEnabled(_agent, false);
            SetEnabled(_fsm, false);
            SetEnabled(_detection, false);
            SetEnabled(_sweeper, false);
            return;
        }

        switch (_role)
        {
            case GuardSpawnRole.MorningShakedown:
                EnsureSweeperComponent();
                SetEnabled(_fsm, false);
                SetEnabled(_detection, false);
                SetEnabled(_agent, true);
                SetEnabled(_sweeper, _sweeper != null);
                if (PrisonEventExtensions.IsMorningLineUp(currentEvent))
                    _sweeper?.TryStartSweepForCurrentPhase();
                break;
            case GuardSpawnRole.NightCellVerifier:
            case GuardSpawnRole.StandardPatrol:
                ApplyStandardPatrolShift(currentEvent);
                break;
        }
    }

    private static void SetEnabled(MonoBehaviour mb, bool on)
    {
        if (mb == null) return;
        mb.enabled = on;
    }

    private static void SetEnabled(NavMeshAgent a, bool on)
    {
        if (a == null) return;
        a.enabled = on;
    }

    /// <summary>During morning roll call, the same patrol guard walks each cell door for shakedown instead of waypoints.</summary>
    private void ApplyStandardPatrolShift(PrisonEventType currentEvent)
    {
        bool morningLineUp = PrisonEventExtensions.IsMorningLineUp(currentEvent);

        if (morningLineUp)
        {
            EnsureSweeperComponent();
            SetEnabled(_fsm, false);
            SetEnabled(_detection, false);
            SetEnabled(_agent, true);
            SetEnabled(_sweeper, _sweeper != null);
            _sweeper?.TryStartSweepForCurrentPhase();
            return;
        }

        SetEnabled(_sweeper, false);
        SetEnabled(_agent, true);
        SetEnabled(_detection, true);
        SetEnabled(_fsm, true);
        if (_fsm != null)
            _fsm.SyncNightVerificationToSchedule(currentEvent);
    }

    private void EnsureSweeperComponent()
    {
        if (_sweeper != null) return;
        _sweeper = GetComponent<MorningShakedownSweeper>();
        if (_sweeper == null)
            _sweeper = gameObject.AddComponent<MorningShakedownSweeper>();
    }
}
