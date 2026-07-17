using UnityEngine;
using UnityEngine.AI;
using Prison;

public class PrisonerAI : MonoBehaviour, Prison.IPrisoner
{
    [Header("Prisoner")]
    public int cellIndex;

    [Header("Movement")]
    public NavMeshAgent agent;
    [Tooltip("Distance to stand point / NavMesh destination to count as compliant (meters)")]
    public float arriveDistance = 0.85f;
    [Tooltip("When true, stop the NavMeshAgent on arrival so idle NPCs do not circle.")]
    public bool stopAgentWhenIdle = true;

    [Header("Debug")]
    [Tooltip("Verbose logs for schedule, pathfinding, and compliance.")]
    public bool debugLogs = true;
    [Tooltip("While moving or non-compliant, print detailed stats at most this often (seconds).")]
    public float debugThrottleSeconds = 0.35f;
    [Tooltip("How far to search for nearby NavMesh when spawn/standpoint is slightly off mesh.")]
    public float navMeshSnapRadius = 4f;

    private PrisonEventType _currentEvent;
    private Transform _currentDestination;
    private Vector3 _resolvedDestination;
    private bool _hasResolvedDestination;
    private bool _holdingAtStand;
    private bool _isCompliant;
    private bool _isAtRequiredLocation;
    private bool _movementBlocked;
    private bool _hadPreviousComplianceSample;
    private float _nextDebugLogTime;
    private bool _lastMovementBlockedLogged;
    private bool _releasedFromRollCallToNextPhase;

    public bool IsCompliant => _isCompliant;
    public bool IsAtRequiredLocation => _isAtRequiredLocation;
    public bool IsRollCallShakedownComplete =>
        MorningRollCallTracker.Instance != null && MorningRollCallTracker.Instance.IsInmateShakedownComplete(this);
    public int CellIndex => cellIndex;
    public bool MovementBlocked => _movementBlocked;
    public float RollCallReleaseAllowedAfter { get; private set; }

    private string DbgPrefix => $"[PrisonerAI][{gameObject.name}][cell {cellIndex}]";

    private void Dbg(string msg)
    {
        if (debugLogs) Debug.Log($"{DbgPrefix} {msg}", this);
    }

    private void DbgWarn(string msg)
    {
        if (debugLogs) Debug.LogWarning($"{DbgPrefix} {msg}", this);
    }

    private void Start()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();

        Dbg($"Start: position={transform.position}, hasAgent={(agent != null)}, agentEnabled={(agent != null && agent.enabled)}, isOnNavMesh={(agent != null && agent.isOnNavMesh)}");
        EnsureAgentOnNavMesh("Start");

        if (PrisonLocationRegistry.Instance == null)
            DbgWarn("PrisonLocationRegistry.Instance is NULL — cannot resolve stand points or register cell.");
        else
        {
            bool ok = PrisonLocationRegistry.Instance.TryRegisterCellOccupant(cellIndex, gameObject);
            Dbg($"TryRegisterCellOccupant(cell={cellIndex}, self) => {ok}");
            if (!ok)
                Debug.LogWarning($"{DbgPrefix} Cell already occupied; NPC may conflict.", this);
        }

        if (PrisonTimeManager.Instance == null)
            DbgWarn("PrisonTimeManager.Instance is NULL — will not receive schedule changes.");
        else
        {
            _currentEvent = PrisonTimeManager.Instance.CurrentEvent;
            Dbg($"Subscribing to OnEventChanged. Initial CurrentEvent={_currentEvent}, time={PrisonTimeManager.Instance.CurrentTimeMinutes:F1} min, entryIdx={PrisonTimeManager.Instance.CurrentEntryIndex}");
            PrisonTimeManager.Instance.OnEventChanged += HandleScheduleChange;
        }

        MorningRollCallTracker.EnsureInstance();
        if (MorningRollCallTracker.Instance != null)
        {
            MorningRollCallTracker.Instance.OnCellShakedownComplete += OnRollCallCellShakedownComplete;
            if (MorningRollCallTracker.Instance.IsInmateShakedownComplete(this))
                RollCallReleaseAllowedAfter = Time.unscaledTime + 2f;
        }

        GoToExpectedLocation("Start");
    }

    private void OnDestroy()
    {
        if (debugLogs) Debug.Log($"{DbgPrefix} OnDestroy: unregistering cell + unsubscribing schedule.", this);
        PrisonLocationRegistry.Instance?.UnregisterCellOccupant(gameObject);

        if (PrisonTimeManager.Instance != null)
            PrisonTimeManager.Instance.OnEventChanged -= HandleScheduleChange;

        if (MorningRollCallTracker.Instance != null)
            MorningRollCallTracker.Instance.OnCellShakedownComplete -= OnRollCallCellShakedownComplete;
    }

    private void OnRollCallCellShakedownComplete(int completedCell)
    {
        if (completedCell != cellIndex)
            return;
        RollCallReleaseAllowedAfter = 0f;
        TryHeadToNextPhaseAfterRollCallCleared();
    }

    private void HandleScheduleChange(PrisonEventType evt)
    {
        Dbg($"HandleScheduleChange: {_currentEvent} -> {evt}");
        _releasedFromRollCallToNextPhase = false;
        RollCallReleaseAllowedAfter = 0f;
        _currentEvent = evt;
        GoToExpectedLocation("HandleScheduleChange");
    }

    private void Update()
    {
        if (debugLogs && _movementBlocked != _lastMovementBlockedLogged)
        {
            _lastMovementBlockedLogged = _movementBlocked;
            Dbg($"MovementBlocked changed => {_movementBlocked}");
        }

        UpdateCompliance();
        TryHeadToNextPhaseAfterRollCallCleared();
        ThrottledMovementDebug();
    }

    /// <summary>Once shakedown marks this NPC cleared, path to the upcoming phase instead of standing at roll call.</summary>
    private void TryHeadToNextPhaseAfterRollCallCleared()
    {
        if (_releasedFromRollCallToNextPhase || _movementBlocked)
            return;

        var tm = PrisonTimeManager.Instance;
        if (tm == null || !PrisonEventExtensions.IsMorningLineUp(_currentEvent))
            return;
        if (!IsRollCallShakedownComplete)
            return;

        tm.GetNextEventInfo(out PrisonEventType nextEvt, out _);
        if (nextEvt == _currentEvent)
            return;

        _releasedFromRollCallToNextPhase = true;
        Dbg($"Roll call cleared — heading to next phase destination: {nextEvt}");
        GoToExpectedLocationForEvent(nextEvt, "RollCallCleared");
    }

    private void ThrottledMovementDebug()
    {
        if (!debugLogs || Time.unscaledTime < _nextDebugLogTime) return;
        if (_movementBlocked) return;

        _nextDebugLogTime = Time.unscaledTime + debugThrottleSeconds;

        if (_currentDestination == null)
        {
            Dbg($"[tick] no destination; compliant={_isCompliant}");
            return;
        }

        if (agent == null || !agent.enabled)
        {
            Dbg($"[tick] agent missing/disabled; compliant={_isCompliant}, dest={_currentDestination.name}");
            return;
        }

        float navDist = agent.remainingDistance;
        float posDist = Vector3.Distance(transform.position, _currentDestination.position);
        Dbg($"[tick] event={_currentEvent} dest={_currentDestination.name} destPos={_currentDestination.position} " +
            $"myPos={transform.position} navRemDist={navDist} posDist={posDist:F3} arrive≤{arriveDistance} " +
            $"pathPending={agent.pathPending} pathStatus={agent.pathStatus} hasPath={agent.hasPath} isStopped={agent.isStopped} " +
            $"isOnNavMesh={agent.isOnNavMesh} velocity={agent.velocity.magnitude:F2} compliant={_isCompliant}");
    }

    private bool EnsureAgentOnNavMesh(string reason)
    {
        if (agent == null || !agent.enabled) return false;
        if (agent.isOnNavMesh) return true;

        if (NavMesh.SamplePosition(transform.position, out var hit, navMeshSnapRadius, NavMesh.AllAreas))
        {
            bool warped = agent.Warp(hit.position);
            DbgWarn($"Agent was off NavMesh during {reason}. Warp to nearest mesh => {warped}, hitPos={hit.position}");
            return warped && agent.isOnNavMesh;
        }

        DbgWarn($"Agent OFF NavMesh during {reason} and no mesh found within {navMeshSnapRadius}m from {transform.position}. Check cell spawnPoint / NavMesh bake.");
        return false;
    }

    private bool TryGetReachableDestination(Vector3 raw, out Vector3 resolved)
    {
        resolved = raw;
        if (NavMesh.SamplePosition(raw, out var hit, navMeshSnapRadius, NavMesh.AllAreas))
        {
            resolved = hit.position;
            return true;
        }
        return false;
    }

    private void GoToExpectedLocation(string reason) => GoToExpectedLocationForEvent(_currentEvent, reason);

    private void GoToExpectedLocationForEvent(PrisonEventType evt, string reason)
    {
        if (PrisonLocationRegistry.Instance == null)
        {
            DbgWarn($"GoToExpectedLocation({reason}) aborted: no registry.");
            return;
        }

        var standPoint = PrisonLocationRegistry.Instance.GetStandPointForEvent(evt, cellIndex);
        if (standPoint == null)
        {
            Dbg($"GoToExpectedLocation({reason}): NO stand point for event={evt}, cellIndex={cellIndex}. Marking compliant, clearing destination.");
            _currentDestination = null;
            _hasResolvedDestination = false;
            _holdingAtStand = false;
            _isCompliant = true;
            StopAgentAtStand("no-stand");
            return;
        }

        Vector3 rawTarget = standPoint.position;
        if (PrisonLocationRegistry.Instance.TryGetSpreadStandPosition(evt, cellIndex, out var spread))
            rawTarget = spread;

        Dbg($"GoToExpectedLocation({reason}): event={evt} standPoint='{standPoint.name}' raw={standPoint.position} spread={rawTarget}");

        if (agent == null)
        {
            DbgWarn("GoToExpectedLocation: NavMeshAgent is NULL — cannot move.");
            return;
        }

        if (!agent.enabled)
        {
            DbgWarn($"GoToExpectedLocation: NavMeshAgent disabled (MovementBlocked={_movementBlocked}) — skip SetDestination.");
            return;
        }

        if (!EnsureAgentOnNavMesh($"GoToExpectedLocation/{reason}"))
            return;

        if (!TryGetReachableDestination(rawTarget, out var destination))
        {
            DbgWarn($"StandPoint {standPoint.name} at {rawTarget} is off NavMesh and no nearby mesh found within {navMeshSnapRadius}m. NPC cannot path for event={evt}.");
            _isCompliant = false;
            _currentDestination = standPoint;
            _resolvedDestination = rawTarget;
            _hasResolvedDestination = true;
            _holdingAtStand = false;
            return;
        }

        // Already holding near the same target — do not repath (avoids crowd circling).
        if (_holdingAtStand && _hasResolvedDestination &&
            Vector3.Distance(transform.position, destination) <= arriveDistance * 1.25f &&
            Vector3.Distance(_resolvedDestination, destination) <= 0.35f)
        {
            Dbg($"GoToExpectedLocation({reason}): already holding near destination — skip repath.");
            _isCompliant = true;
            _isAtRequiredLocation = true;
            return;
        }

        _isCompliant = false;
        _holdingAtStand = false;
        _currentDestination = standPoint;
        _resolvedDestination = destination;
        _hasResolvedDestination = true;
        agent.stoppingDistance = Mathf.Max(0.15f, arriveDistance * 0.65f);
        agent.isStopped = false;
        agent.SetDestination(destination);

        Dbg($"After SetDestination: hasPath={agent.hasPath} pathPending={agent.pathPending} pathStatus={agent.pathStatus} " +
            $"destination={agent.destination} resolvedDest={destination} isOnNavMesh={agent.isOnNavMesh} remainingDistance={agent.remainingDistance}");
    }

    private void StopAgentAtStand(string reason)
    {
        if (!stopAgentWhenIdle || agent == null || !agent.enabled)
            return;
        if (!agent.isOnNavMesh)
            return;

        agent.isStopped = true;
        agent.ResetPath();
        agent.velocity = Vector3.zero;
        Dbg($"StopAgentAtStand({reason}): velocity cleared, isStopped=true");
    }

    private void UpdateCompliance()
    {
        if (_movementBlocked)
            return;

        if (MorningRollCallTracker.IsInmateReleasedFromRollCallStand(this))
        {
            _isAtRequiredLocation = true;
            _isCompliant = true;
            if (!_holdingAtStand)
            {
                _holdingAtStand = true;
                StopAgentAtStand("roll-call-released");
            }
            return;
        }

        bool prev = _isCompliant;

        if (_currentDestination == null || agent == null || !agent.enabled)
        {
            _isAtRequiredLocation = true;
            _isCompliant = true;
            LogComplianceTransition(prev, "early-exit: no dest or no agent/disabled");
            return;
        }

        float navDist = agent.remainingDistance;
        bool navArrived = !agent.pathPending && navDist != float.PositiveInfinity && navDist <= arriveDistance;
        Vector3 targetPos = _hasResolvedDestination ? _resolvedDestination : _currentDestination.position;
        float posDist = Vector3.Distance(transform.position, targetPos);
        bool posArrived = posDist <= arriveDistance;
        // Near-zero speed near destination also counts — agents often never reach exact point in crowds.
        bool idleNear = agent.velocity.magnitude < 0.08f && posDist <= arriveDistance * 1.6f && !agent.pathPending;
        _isAtRequiredLocation = navArrived || posArrived || idleNear;

        if (_isAtRequiredLocation && !_holdingAtStand)
        {
            _holdingAtStand = true;
            StopAgentAtStand("arrived");
        }
        else if (!_isAtRequiredLocation && _holdingAtStand)
        {
            _holdingAtStand = false;
            if (agent != null && agent.enabled)
                agent.isStopped = false;
        }

        if (PrisonTimeManager.Instance != null && PrisonTimeManager.Instance.IsMandatoryTravelGraceActive)
        {
            _isCompliant = true;
            string detail = $"grace; atRequired={_isAtRequiredLocation} navRem={navDist} posDist={posDist:F3}";
            LogComplianceTransition(prev, detail);
            return;
        }

        _isCompliant = _isAtRequiredLocation;

        string detail2 = $"navRem={navDist} posDist={posDist:F3} pathPending={agent.pathPending} navArrived={navArrived} posArrived={posArrived} idleNear={idleNear} holding={_holdingAtStand}";
        LogComplianceTransition(prev, detail2);
    }

    private void LogComplianceTransition(bool previousCompliant, string detail)
    {
        if (!debugLogs) return;

        if (!_hadPreviousComplianceSample)
        {
            _hadPreviousComplianceSample = true;
            Dbg($"Compliance init: compliant={_isCompliant} ({detail})");
            return;
        }

        if (previousCompliant != _isCompliant)
            Dbg($"COMPLIANCE: {previousCompliant} -> {_isCompliant} | {detail}");
    }

    public void SetMovementBlocked(bool blocked)
    {
        Dbg($"SetMovementBlocked({blocked})");
        _movementBlocked = blocked;
        if (agent != null)
            agent.enabled = !blocked;
    }

    public void SendToCell(int targetCellIndex = -1)
    {
        int idx = targetCellIndex >= 0 ? targetCellIndex : cellIndex;
        Dbg($"SendToCell: targetCellIndex arg={targetCellIndex} resolved idx={idx}");
        var cell = PrisonLocationRegistry.Instance?.GetCell(idx);
        if (cell == null)
        {
            DbgWarn($"SendToCell: no CellData for index {idx}");
            return;
        }

        Dbg($"SendToCell: teleporting to cell spawn {cell.SpawnPosition}");
        SetMovementBlocked(true);

        if (agent != null)
        {
            agent.enabled = false;
            transform.position = cell.SpawnPosition;
            transform.rotation = cell.SpawnRotation;
            agent.enabled = true;
        }
        else
        {
            transform.position = cell.SpawnPosition;
            transform.rotation = cell.SpawnRotation;
        }

        Invoke(nameof(ReleaseMovement), 1f);
    }

    private void ReleaseMovement()
    {
        Dbg("ReleaseMovement (Invoke after SendToCell)");
        SetMovementBlocked(false);
        GoToExpectedLocation("ReleaseMovement");
    }
}
