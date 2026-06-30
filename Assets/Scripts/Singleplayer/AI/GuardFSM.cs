using UnityEngine;
using UnityEngine.AI;
using Prison;

public class GuardFSM : MonoBehaviour
{
    public enum GuardState
    {
        Patrol,
        Enforce,
        Escort
    }

    [Header("References")]
    public NavMeshAgent agent;
    public GuardDetection detection;

    [Header("Patrol")]
    public Transform[] patrolWaypoints;
    [Tooltip("Distance to waypoint to consider arrived")]
    public float waypointArriveDistance = 1.5f;

    [Header("Movement (NavMeshAgent)")]
    [Tooltip("Walk/run speed while patrolling and verifying cells.")]
    public float patrolMoveSpeed = 8f;
    [Tooltip("Speed while chasing or leading to cell (chase + escort). This is applied directly — raise to 24–32 if it still feels slow.")]
    public float escortMoveSpeed = 24f;
    [Tooltip("Only used if Escort Move Speed is 0: escort speed = patrol × this.")]
    public float escortSpeedMultiplier = 2.75f;
    [Tooltip("Turn rate while patrolling / night verify.")]
    public float turnSpeed = 720f;
    [Tooltip("Turn rate while escorting (usually higher than patrol).")]
    public float escortTurnSpeed = 960f;
    [Tooltip("Acceleration while patrolling.")]
    public float acceleration = 45f;
    [Tooltip("Acceleration while escorting (higher = snappier sprint).")]
    public float escortAcceleration = 120f;

    [Header("Escort")]
    [Tooltip("Distance to prisoner to arrest")]
    public float arrestDistance = 2f;
    [Tooltip("Guard must be within this distance of cell spawn to count as 'at cell' (NavMesh often stops outside the door).")]
    public float cellArriveDistance = 2.5f;
    [Tooltip("Escort also completes when the prisoner is this close to cell spawn (usually easier than the guard reaching the exact bed point).")]
    public float prisonerCellArriveDistance = 4f;
    [Tooltip("If escort stalls (guard stuck on NavMesh), force SendToCell after this many seconds once the prisoner is arrested.")]
    public float escortForceCompleteSeconds = 35f;

    [Header("Debug")]
    public bool debugLogs;

    public enum GuardDuty
    {
        StandardPatrol,
        NightCellVerifier,
    }

    [Header("Duty")]
    [Tooltip("Night verifier walks each cell during Night Roll Call / Lights Out and runs a bed presence check.")]
    public GuardDuty duty = GuardDuty.StandardPatrol;
    [Tooltip("Distance to cell approach transform before running VerifyCellBedPresence.")]
    public float nightApproachArriveDistance = 1.75f;

    private GuardState _state = GuardState.Patrol;
    private int _currentWaypointIndex;
    private Prison.IPrisoner _escortTarget;
    private int _nightVerifyCellIndex;
    private bool _nightSweepDoneThisPhase;
    private float _escortArrestedRealtime = -1f;

    public GuardState State => _state;

    private void OnEnable()
    {
        if (PrisonTimeManager.Instance != null)
            PrisonTimeManager.Instance.OnEventChanged += OnPrisonScheduleChanged;
    }

    private void OnDisable()
    {
        if (PrisonTimeManager.Instance != null)
            PrisonTimeManager.Instance.OnEventChanged -= OnPrisonScheduleChanged;
    }

    private void OnPrisonScheduleChanged(PrisonEventType evt)
    {
        SyncNightVerificationToSchedule(evt);
    }

    /// <summary>Resets night cell sweep when (re)enabled during a night phase — call from <see cref="GuardShiftController"/>.</summary>
    public void SyncNightVerificationToSchedule(PrisonEventType currentEvent)
    {
        if (!PrisonEventExtensions.IsNightBedPhase(currentEvent)) return;
        _nightSweepDoneThisPhase = false;
        _nightVerifyCellIndex = 0;
        if (debugLogs)
            Debug.Log($"[GuardFSM][{gameObject.name}] Night phase sync — reset cell verification index.", this);
    }

    private void Start()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (detection == null) detection = GetComponent<GuardDetection>();
        if (debugLogs)
            Debug.Log($"[GuardFSM][{gameObject.name}] Start: state=Patrol, duty={duty}, agent={(agent != null)}, detection={(detection != null)}, waypointCount={patrolWaypoints?.Length ?? 0}", this);
        if (patrolWaypoints != null && patrolWaypoints.Length > 0)
            agent.SetDestination(patrolWaypoints[0].position);

        ApplyAgentMovementTuning();
    }

    private void ApplyAgentMovementTuning()
    {
        if (agent == null) return;

        bool escort = _state == GuardState.Escort;
        if (escort)
        {
            if (escortMoveSpeed > 0.01f)
                agent.speed = Mathf.Max(0.5f, escortMoveSpeed);
            else
                agent.speed = Mathf.Max(0.5f, patrolMoveSpeed) * Mathf.Max(1f, escortSpeedMultiplier);

            agent.angularSpeed = Mathf.Max(1f, escortTurnSpeed);
            agent.acceleration = Mathf.Max(1f, escortAcceleration);
        }
        else
        {
            agent.speed = Mathf.Max(0.5f, patrolMoveSpeed);
            agent.angularSpeed = Mathf.Max(1f, turnSpeed);
            agent.acceleration = Mathf.Max(1f, acceleration);
        }
    }

    private void Update()
    {
        ApplyAgentMovementTuning();

        if (_state == GuardState.Escort)
        {
            UpdateEscort();
            return;
        }

        if (duty == GuardDuty.NightCellVerifier
            && PrisonTimeManager.Instance != null
            && PrisonEventExtensions.IsNightBedPhase(PrisonTimeManager.Instance.CurrentEvent))
        {
            UpdateNightCellVerification();
            return;
        }

        switch (_state)
        {
            case GuardState.Patrol:
                UpdatePatrol();
                break;
            case GuardState.Enforce:
                UpdateEnforce();
                break;
        }
    }

    private void UpdateNightCellVerification()
    {
        var reg = PrisonLocationRegistry.Instance;
        if (reg == null || agent == null) return;

        var intruder = detection?.FindNonCompliantPrisoner();
        if (intruder != null)
        {
            if (debugLogs)
                Debug.Log($"[GuardFSM][{gameObject.name}] Night verify -> Escort (non-compliant in sight)", this);
            _escortTarget = intruder;
            _state = GuardState.Escort;
            return;
        }

        if (_nightSweepDoneThisPhase)
        {
            UpdatePatrol();
            return;
        }

        if (_nightVerifyCellIndex >= reg.CellCount)
        {
            _nightSweepDoneThisPhase = true;
            if (debugLogs)
                Debug.Log($"[GuardFSM][{gameObject.name}] Night verify: completed all cells.", this);
            return;
        }

        var cell = reg.GetCell(_nightVerifyCellIndex);
        if (cell == null)
        {
            _nightVerifyCellIndex++;
            return;
        }

        Transform approach = cell.NightCheckApproachTransform;
        if (approach == null)
        {
            _nightVerifyCellIndex++;
            return;
        }

        Vector3 targetPos = approach.position;
        agent.SetDestination(targetPos);

        float dist = Vector3.Distance(transform.position, targetPos);
        if (agent.pathPending || dist > nightApproachArriveDistance)
            return;

        bool ok = detection != null && detection.VerifyCellBedPresence(_nightVerifyCellIndex);
        if (debugLogs)
            Debug.Log($"[GuardFSM][{gameObject.name}] Night verify cell {_nightVerifyCellIndex}: presenceOk={ok}", this);

        if (!ok)
            PrisonSecurityAlerts.RaiseLockdown($"Bed check failed — cell {_nightVerifyCellIndex}.");

        _nightVerifyCellIndex++;
    }

    private void UpdatePatrol()
    {
        var target = detection?.FindNonCompliantPrisoner();
        if (target != null)
        {
            var mb = target as MonoBehaviour;
            if (debugLogs)
                Debug.Log($"[GuardFSM][{gameObject.name}] Patrol -> Escort: target={(mb != null ? mb.name : "null")} cell={target.CellIndex} compliant={target.IsCompliant}", this);
            _escortTarget = target;
            _state = GuardState.Escort;
            if (agent != null)
                agent.isStopped = false;
            return;
        }

        if (patrolWaypoints == null || patrolWaypoints.Length == 0)
        {
            if (debugLogs && Time.frameCount % 120 == 0)
                Debug.LogWarning($"[GuardFSM][{gameObject.name}] Patrol: no patrol waypoints assigned.", this);
            return;
        }

        float dist = agent.remainingDistance;
        if (!agent.pathPending && dist != float.PositiveInfinity && dist <= waypointArriveDistance)
        {
            _currentWaypointIndex = (_currentWaypointIndex + 1) % patrolWaypoints.Length;
            if (debugLogs)
                Debug.Log($"[GuardFSM][{gameObject.name}] Patrol: advance waypoint index={_currentWaypointIndex} -> {patrolWaypoints[_currentWaypointIndex].position}", this);
            agent.SetDestination(patrolWaypoints[_currentWaypointIndex].position);
        }
    }

    private void UpdateEnforce()
    {
        if (_escortTarget == null)
        {
            if (debugLogs) Debug.Log($"[GuardFSM][{gameObject.name}] Enforce -> Patrol (lost target)", this);
            _state = GuardState.Patrol;
            return;
        }
        if (debugLogs) Debug.Log($"[GuardFSM][{gameObject.name}] Enforce -> Escort", this);
        _state = GuardState.Escort;
    }

    private void UpdateEscort()
    {
        if (_escortTarget == null)
        {
            if (debugLogs) Debug.Log($"[GuardFSM][{gameObject.name}] Escort -> Patrol (null target)", this);
            _escortArrestedRealtime = -1f;
            _state = GuardState.Patrol;
            return;
        }

        var targetTransform = (_escortTarget as MonoBehaviour)?.transform;
        if (targetTransform == null)
        {
            if (debugLogs) Debug.Log($"[GuardFSM][{gameObject.name}] Escort -> Patrol (target not MonoBehaviour)", this);
            _escortArrestedRealtime = -1f;
            _escortTarget = null;
            _state = GuardState.Patrol;
            return;
        }

        if (!_escortTarget.MovementBlocked)
        {
            _escortArrestedRealtime = -1f;
            if (agent != null)
                agent.isStopped = false;

            float distToPrisoner = Vector3.Distance(transform.position, targetTransform.position);
            if (distToPrisoner <= arrestDistance)
            {
                if (debugLogs) Debug.Log($"[GuardFSM][{gameObject.name}] Escort: within arrestDistance ({distToPrisoner:F2} <= {arrestDistance}) -> SetMovementBlocked(true) on prisoner", this);
                _escortTarget.SetMovementBlocked(true);
                _escortArrestedRealtime = Time.realtimeSinceStartup;
            }
            if (debugLogs && Time.frameCount % 30 == 0)
                Debug.Log($"[GuardFSM][{gameObject.name}] Escort CHASE: distToPrisoner={distToPrisoner:F2} arrest≤{arrestDistance} hasPath={agent?.hasPath} pathStatus={agent?.pathStatus} rem={agent?.remainingDistance:F2} stopped={agent?.isStopped} onMesh={agent?.isOnNavMesh}", this);
            if (agent != null)
                agent.SetDestination(targetTransform.position);
            return;
        }

        var cc = (_escortTarget as MonoBehaviour)?.GetComponent<CharacterController>();
        if (cc != null)
        {
            Vector3 followPos = transform.position - transform.forward * 1.5f;
            followPos.y = targetTransform.position.y;
            cc.enabled = false;
            targetTransform.position = followPos;
            cc.enabled = true;
        }
        else
        {
            var prisonerAgent = (_escortTarget as MonoBehaviour)?.GetComponent<NavMeshAgent>();
            if (prisonerAgent != null)
            {
                Vector3 followPos = transform.position - transform.forward * 1.5f;
                followPos.y = targetTransform.position.y;
                targetTransform.position = followPos;
            }
        }

        var cell = PrisonLocationRegistry.Instance?.GetCell(_escortTarget.CellIndex);
        if (cell == null)
        {
            if (debugLogs) Debug.LogWarning($"[GuardFSM][{gameObject.name}] Escort: no cell for CellIndex={_escortTarget.CellIndex} -> release prisoner, Patrol", this);
            _escortArrestedRealtime = -1f;
            _escortTarget.SetMovementBlocked(false);
            _escortTarget = null;
            _state = GuardState.Patrol;
            return;
        }

        Vector3 spawnPos = cell.SpawnPosition;
        if (agent != null)
        {
            agent.isStopped = false;
            agent.SetDestination(spawnPos);
        }

        float guardToSpawn = Vector3.Distance(transform.position, spawnPos);
        float prisonerToSpawn = Vector3.Distance(targetTransform.position, spawnPos);
        bool guardAtCell = guardToSpawn <= cellArriveDistance;
        bool prisonerAtCell = prisonerToSpawn <= prisonerCellArriveDistance;
        bool stalled = _escortArrestedRealtime >= 0f &&
                         escortForceCompleteSeconds > 0f &&
                         Time.realtimeSinceStartup - _escortArrestedRealtime >= escortForceCompleteSeconds &&
                         prisonerToSpawn <= prisonerCellArriveDistance * 2f;

        if (debugLogs && Time.frameCount % 30 == 0)
            Debug.Log($"[GuardFSM][{gameObject.name}] Escort LEAD: cell={_escortTarget.CellIndex} guard→spawn={guardToSpawn:F2} prisoner→spawn={prisonerToSpawn:F2} | hasPath={agent?.hasPath} status={agent?.pathStatus} rem={agent?.remainingDistance:F2} stopped={agent?.isStopped} onMesh={agent?.isOnNavMesh} vel={agent?.velocity.magnitude:F2} dest={agent?.destination}", this);

        if (guardAtCell || prisonerAtCell || stalled)
        {
            if (debugLogs)
                Debug.Log($"[GuardFSM][{gameObject.name}] Escort: complete (guardAt={guardAtCell} prisonerAt={prisonerAtCell} stalled={stalled}) -> SendToCell", this);
            _escortArrestedRealtime = -1f;
            _escortTarget.SendToCell();
            _escortTarget = null;
            _state = GuardState.Patrol;
        }
    }
}
