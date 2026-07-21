using UnityEngine;
using UnityEngine.AI;
using Prison;
using Prison.Social;

public class PrisonerAI : MonoBehaviour, Prison.IPrisoner
{
    private enum LoiterState
    {
        Traveling,
        Idle,
        Walking
    }

    [Header("Prisoner")]
    public int cellIndex;

    [Header("Movement")]
    public NavMeshAgent agent;
    [Tooltip("Distance to stand point / NavMesh destination to count as arrived (meters)")]
    public float arriveDistance = 0.85f;
    [Tooltip("When true, stop the NavMeshAgent while idling so NPCs do not circle.")]
    public bool stopAgentWhenIdle = true;

    [Header("Personality loiter")]
    [Tooltip("Enable idle/wander after reaching the phase stand. Driven by archetype + traits when SocialWorld is available.")]
    public bool enablePersonalityLoiter = true;

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
    private float _postEscortImmunityUntil = float.NegativeInfinity;

    private LoiterState _loiterState = LoiterState.Traveling;
    private float _loiterUntil;
    private bool _personalityCached;
    private float _idleBias = 0.4f;
    private float _wanderRadius = 5f;
    private float _minIdleSeconds = 2.5f;
    private float _maxIdleSeconds = 8f;

    private bool _talkEngaged;
    private Transform _talkFaceTarget;
    private bool _agentUpdateRotationBeforeTalk = true;

    [Tooltip("After escort dump-to-cell, NPCs count as compliant this long so guards do not re-arrest them in a loop.")]
    [Min(5f)]
    public float postEscortImmunitySeconds = 40f;

    public bool IsCompliant => _isCompliant;
    public bool IsAtRequiredLocation => _isAtRequiredLocation;
    public bool IsRollCallShakedownComplete =>
        MorningRollCallTracker.Instance != null && MorningRollCallTracker.Instance.IsInmateShakedownComplete(this);
    public int CellIndex => cellIndex;
    public bool MovementBlocked => _movementBlocked;
    public bool HasPostEscortImmunity => Time.unscaledTime < _postEscortImmunityUntil;
    public float RollCallReleaseAllowedAfter { get; private set; }

    /// <summary>On mandatory travel to a required stand — Talk is refused with a bark.</summary>
    public bool IsBusyForTalk =>
        !_movementBlocked &&
        PrisonTimeManager.Instance != null &&
        PrisonEventRules.IsMandatory(_currentEvent) &&
        !_isAtRequiredLocation;

    private string DbgPrefix => $"[PrisonerAI][{gameObject.name}][cell {cellIndex}]";

    private void Dbg(string msg)
    {
        if (debugLogs) Debug.Log($"{DbgPrefix} {msg}", this);
    }

    private void DbgWarn(string msg)
    {
        if (debugLogs) Debug.LogWarning($"{DbgPrefix} {msg}", this);
    }

    private void OnEnable()
    {
        PrisonerRegistry.Register(this);
    }

    private void OnDisable()
    {
        PrisonerRegistry.Unregister(this);
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
        SetTalkEngaged(false, null);
        ResetLoiter("schedule-change");
        GoToExpectedLocation("HandleScheduleChange");
    }

    private void Update()
    {
        if (debugLogs && _movementBlocked != _lastMovementBlockedLogged)
        {
            _lastMovementBlockedLogged = _movementBlocked;
            Dbg($"MovementBlocked changed => {_movementBlocked}");
        }

        if (_movementBlocked)
            return;

        if (_talkEngaged)
            return;

        UpdateCompliance();
        TryHeadToNextPhaseAfterRollCallCleared();
        UpdatePersonalityLoiter();
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
        ResetLoiter("roll-call-cleared");
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
        _loiterState = LoiterState.Traveling;
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

        if (HasPostEscortImmunity)
        {
            _isCompliant = true;
            return;
        }

        // Cleared for roll call but not yet redirected — stay compliant at stand without hard-freezing forever.
        if (MorningRollCallTracker.IsInmateReleasedFromRollCallStand(this) && !_releasedFromRollCallToNextPhase)
        {
            _isAtRequiredLocation = true;
            _isCompliant = true;
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

        float presenceRadius = GetPresenceRadius();
        float navDist = agent.remainingDistance;
        bool navArrived = !agent.pathPending && navDist != float.PositiveInfinity && navDist <= arriveDistance;
        Vector3 targetPos = _hasResolvedDestination ? _resolvedDestination : _currentDestination.position;
        float posDist = Vector3.Distance(transform.position, targetPos);
        bool posArrived = posDist <= arriveDistance;
        bool idleNear = agent.velocity.magnitude < 0.08f && posDist <= arriveDistance * 1.6f && !agent.pathPending;
        bool inPresence = posDist <= presenceRadius;
        _isAtRequiredLocation = navArrived || posArrived || idleNear || inPresence;

        if (_isAtRequiredLocation && _loiterState == LoiterState.Traveling)
        {
            // Arrived at phase stand — begin personality idle/wander instead of permanent freeze.
            BeginIdleBurst("arrived");
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

        // Free time is never mandatory; during other phases stay within presence of the stand.
        if (!PrisonEventRules.IsMandatory(_currentEvent))
            _isCompliant = true;
        else
            _isCompliant = _isAtRequiredLocation;

        MorningRollCallTracker.TryOpenDoorWhenInmateAtStand(this);

        string detail2 = $"navRem={navDist} posDist={posDist:F3} presence≤{presenceRadius:F1} pathPending={agent.pathPending} " +
                         $"navArrived={navArrived} posArrived={posArrived} idleNear={idleNear} loiter={_loiterState}";
        LogComplianceTransition(prev, detail2);
    }

    private void ResetLoiter(string reason)
    {
        _loiterState = LoiterState.Traveling;
        _loiterUntil = 0f;
        _holdingAtStand = false;
        Dbg($"ResetLoiter({reason})");
    }

    private void EnsurePersonalityCached()
    {
        if (_personalityCached)
            return;
        _personalityCached = true;

        _idleBias = 0.4f;
        _wanderRadius = 5f;
        _minIdleSeconds = 2.5f;
        _maxIdleSeconds = 8f;

        var world = SocialWorld.Instance;
        if (world == null)
            return;

        int actorId = world.GetActorId(gameObject);
        var identity = world.GetIdentity(actorId);
        if (identity == null || identity.isGuard)
            return;

        float sociability = identity.traits.sociability / 100f;
        float aggression = identity.traits.aggression / 100f;
        float nerve = identity.traits.nerve / 100f;

        // High sociability → more walking; loners / old-timers idle more.
        _idleBias = Mathf.Clamp01(0.75f - sociability * 0.55f);
        _wanderRadius = Mathf.Lerp(3.5f, 9f, Mathf.Clamp01(0.35f + sociability * 0.4f + aggression * 0.35f));
        _minIdleSeconds = Mathf.Lerp(1.2f, 4f, _idleBias);
        _maxIdleSeconds = Mathf.Lerp(4f, 14f, _idleBias);

        switch (identity.archetype)
        {
            case PrisonerArchetype.Loner:
                _idleBias = Mathf.Clamp01(_idleBias + 0.28f);
                _wanderRadius *= 0.55f;
                _minIdleSeconds += 1.5f;
                _maxIdleSeconds += 4f;
                break;
            case PrisonerArchetype.OldTimer:
                _idleBias = Mathf.Clamp01(_idleBias + 0.22f);
                _wanderRadius *= 0.7f;
                _maxIdleSeconds += 3f;
                break;
            case PrisonerArchetype.Hustler:
                _idleBias = Mathf.Clamp01(_idleBias - 0.2f);
                _wanderRadius *= 1.15f;
                _minIdleSeconds = Mathf.Max(0.8f, _minIdleSeconds - 0.8f);
                break;
            case PrisonerArchetype.Bruiser:
                _idleBias = Mathf.Clamp01(_idleBias - 0.08f);
                _wanderRadius *= 1.35f;
                break;
            case PrisonerArchetype.Soldier:
                _wanderRadius *= 1.1f;
                break;
            case PrisonerArchetype.Snitch:
                // Nervous short pacing — rarely stands still long, but stays close.
                _idleBias = Mathf.Clamp01(_idleBias - 0.15f);
                _wanderRadius = Mathf.Min(_wanderRadius, 3.2f);
                _minIdleSeconds = 0.8f;
                _maxIdleSeconds = 3.5f;
                break;
            case PrisonerArchetype.ShotCaller:
                _idleBias = Mathf.Clamp01(_idleBias + 0.12f);
                _wanderRadius *= 0.9f;
                break;
        }

        // Low nerve → slightly more idle (keeps head down).
        _idleBias = Mathf.Clamp01(_idleBias + (1f - nerve) * 0.08f);

        Dbg($"Personality loiter: archetype={identity.archetype} idleBias={_idleBias:F2} wanderR={_wanderRadius:F1} idle={_minIdleSeconds:F1}-{_maxIdleSeconds:F1}s");
    }

    private float GetPresenceRadius()
    {
        EnsurePersonalityCached();
        if (PrisonEventExtensions.IsFormalCount(_currentEvent) || _currentEvent == PrisonEventType.LightsOut)
            return Mathf.Max(arriveDistance * 2.5f, 2.8f);
        if (_currentEvent == PrisonEventType.FreeTime)
            return Mathf.Max(_wanderRadius + 4f, 10f);
        // Meals / work — loiter near the hall/workshop stand.
        return Mathf.Max(_wanderRadius + 1.5f, 6f);
    }

    private float GetWanderRadiusForPhase()
    {
        EnsurePersonalityCached();
        if (PrisonEventExtensions.IsFormalCount(_currentEvent) || _currentEvent == PrisonEventType.LightsOut)
            return Mathf.Clamp(_wanderRadius * 0.25f, 0.8f, 2.2f);
        if (_currentEvent == PrisonEventType.FreeTime)
            return _wanderRadius;
        return _wanderRadius * 0.7f;
    }

    private void BeginIdleBurst(string reason)
    {
        EnsurePersonalityCached();
        _loiterState = LoiterState.Idle;
        _holdingAtStand = true;
        float duration = Random.Range(_minIdleSeconds, _maxIdleSeconds);
        // Formal counts: stand longer.
        if (PrisonEventExtensions.IsFormalCount(_currentEvent))
            duration *= 1.6f;
        _loiterUntil = Time.unscaledTime + duration;
        StopAgentAtStand(reason);
        Dbg($"BeginIdleBurst({reason}): {duration:F1}s idleBias={_idleBias:F2}");
    }

    private void UpdatePersonalityLoiter()
    {
        if (!enablePersonalityLoiter || _movementBlocked || _talkEngaged)
            return;
        if (agent == null || !agent.enabled || !_hasResolvedDestination)
            return;

        EnsurePersonalityCached();

        if (_loiterState == LoiterState.Traveling)
            return;

        if (_loiterState == LoiterState.Walking)
        {
            float rem = agent.remainingDistance;
            bool arrived = (!agent.pathPending && rem != float.PositiveInfinity && rem <= arriveDistance)
                           || Vector3.Distance(transform.position, agent.destination) <= arriveDistance * 1.4f;
            if (arrived || (!agent.pathPending && !agent.hasPath && agent.velocity.magnitude < 0.05f))
                BeginIdleBurst("wander-arrived");
            return;
        }

        // Idle — when timer elapses, either wander or idle again (personality-weighted).
        if (Time.unscaledTime < _loiterUntil)
            return;

        float wanderChance = 1f - _idleBias;
        if (_currentEvent == PrisonEventType.FreeTime)
            wanderChance = Mathf.Max(wanderChance, 0.55f);
        else if (PrisonEventExtensions.IsFormalCount(_currentEvent) || _currentEvent == PrisonEventType.LightsOut)
            wanderChance *= 0.35f; // mostly stand during counts, occasional shift
        else
            wanderChance = Mathf.Lerp(0.25f, wanderChance, 0.85f);

        if (Random.value <= wanderChance && TryStartWander())
            return;

        BeginIdleBurst("idle-again");
    }

    private bool TryStartWander()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return false;

        float radius = GetWanderRadiusForPhase();
        Vector3 home = _resolvedDestination;
        Vector2 disk = Random.insideUnitCircle * radius;
        Vector3 candidate = home + new Vector3(disk.x, 0f, disk.y);

        if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, Mathf.Max(2f, radius * 0.5f), NavMesh.AllAreas))
            return false;

        // Stay inside presence so mandatory phases remain compliant.
        if (Vector3.Distance(hit.position, home) > GetPresenceRadius() * 0.95f)
            return false;

        _loiterState = LoiterState.Walking;
        _holdingAtStand = false;
        agent.isStopped = false;
        agent.SetDestination(hit.position);
        // Cap walk time so a bad path doesn't stall forever.
        _loiterUntil = Time.unscaledTime + 12f;
        Dbg($"Wander to {hit.position} (r={radius:F1})");
        return true;
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
        if (blocked)
            SetTalkEngaged(false, null);
        _movementBlocked = blocked;
        if (agent != null)
            agent.enabled = !blocked;
    }

    /// <summary>
    /// Soft-pause for Talk: stop and face the player unless mandatory travel to a required stand is in progress.
    /// Does not use SetMovementBlocked (arrest semantics).
    /// </summary>
    public void SetTalkEngaged(bool engaged, Transform faceTarget)
    {
        if (engaged)
        {
            if (PrisonEventRules.IsMandatory(_currentEvent) && !_isAtRequiredLocation)
            {
                Dbg("SetTalkEngaged skipped — mandatory travel to required stand");
                return;
            }

            _talkEngaged = true;
            _talkFaceTarget = faceTarget;
            ResetLoiter("talk-open");
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                _agentUpdateRotationBeforeTalk = agent.updateRotation;
                agent.updateRotation = false;
                agent.isStopped = true;
                agent.ResetPath();
                agent.velocity = Vector3.zero;
            }

            Dbg("SetTalkEngaged(true)");
            return;
        }

        if (!_talkEngaged)
            return;

        _talkEngaged = false;
        _talkFaceTarget = null;
        if (agent != null && agent.enabled)
        {
            agent.updateRotation = _agentUpdateRotationBeforeTalk;
            agent.isStopped = false;
        }

        _holdingAtStand = false;
        Dbg("SetTalkEngaged(false)");
    }

    private void UpdateTalkFacing()
    {
        if (_talkFaceTarget == null)
            return;

        Vector3 to = _talkFaceTarget.position - transform.position;
        to.y = 0f;
        if (to.sqrMagnitude < 0.001f)
            return;

        Quaternion target = Quaternion.LookRotation(to.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, Time.unscaledDeltaTime * 8f);
    }

    private void LateUpdate()
    {
        if (_talkEngaged)
            UpdateTalkFacing();
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

        Vector3 dest = cell.SpawnPosition;
        if (PrisonLocationRegistry.Instance != null &&
            PrisonLocationRegistry.Instance.TryGetCellFloorStand(cell, out var floorStand))
            dest = floorStand;

        Dbg($"SendToCell: teleporting to cell floor stand {dest}");
        SetMovementBlocked(true);

        if (agent != null)
        {
            agent.enabled = false;
            transform.position = dest;
            transform.rotation = cell.SpawnRotation;
            agent.enabled = true;
        }
        else
        {
            transform.position = dest;
            transform.rotation = cell.SpawnRotation;
        }

        Invoke(nameof(ReleaseMovement), 1.25f);
        _postEscortImmunityUntil = Time.unscaledTime + Mathf.Max(5f, postEscortImmunitySeconds);
        _isCompliant = true;
    }

    private void ReleaseMovement()
    {
        Dbg("ReleaseMovement (Invoke after SendToCell)");
        SetMovementBlocked(false);
        GoToExpectedLocation("ReleaseMovement");
    }
}
