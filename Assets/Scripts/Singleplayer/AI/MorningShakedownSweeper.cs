using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Prison;

/// <summary>How close the inmate must be for morning shakedown to count (when enforcement is enabled).</summary>
public enum OccupantRollCallClearanceMode
{
    AtRollCallStand,

    /// <summary>Inside CellData interior sphere (shakedown center + interior radius).</summary>
    InsideCellInterior
}

/// <summary>
/// During morning roll call, walks each cell (door / line-up spot first, then interior sweep).
/// Works on a dedicated MorningShakedown guard or your standard patrol guard (via <see cref="GuardShiftController"/>).
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class MorningShakedownSweeper : MonoBehaviour
{
    private static MorningShakedownSweeper _activeSweeper;

    public NavMeshAgent agent;

    [Header("Movement")]
    [Tooltip("NavMeshAgent speed while sweeping cells (0 = leave prefab unchanged).")]
    public float sweeperMoveSpeed = 7f;
    [Tooltip("Wait after roll call starts so inmates can reach their stand points.")]
    [Min(0f)] public float delayBeforeFirstCellSeconds = 2f;

    [Header("Visit each inmate")]
    [Tooltip("Stop at door first, then path to interior sweep center. Turn off for direct interior sweep only.")]
    public bool visitRollCallStandFirst = true;
    [Tooltip("If set, the cell is not marked cleared unless a registered occupant satisfies Occupant clearance mode.")]
    public bool requireOccupantAtStandForClearance = true;

    [Tooltip("Where the inmate must be when the guard finishes the visit (if requirement enabled).")]
    public OccupantRollCallClearanceMode occupantClearanceMode = OccupantRollCallClearanceMode.AtRollCallStand;

    [Tooltip("When the cell has no registered occupant, still mark it cleared after the guard visit.")]
    public bool clearCellsWithNoRegisteredOccupant = true;
    [Min(0.1f)] public float occupantStandClearanceDistance = 3.5f;

    [Tooltip("Multiplies CellData.InteriorRadius when Occupant clearance = Inside Cell Interior.")]
    [Range(0.5f, 2f)]
    public float interiorClearanceRadiusMultiplier = 1f;
    [Tooltip("After stopping at the door, walk to the interior sweep point (needs NavMesh into the cell).")]
    public bool pathIntoCellAfterDoorStop = false;

    [Tooltip("Seconds the guard holds position at the door (and scales interior dwell slightly when Path Into Cell is on).")]
    [Min(0f)] public float visitStandPointSeconds = 1.75f;

    public float arriveDistance = 1.25f;
    [Min(0f)] public float pauseBetweenCells = 0.5f;
    [Tooltip("After a full pass, wait before retrying uncleared cells (when occupant clearance is enforced).")]
    [Min(0f)] public float pauseBetweenSweepLapsSeconds = 2f;
    [Tooltip("Max seconds to wait for pathing to each stop.")]
    public float maxTravelWaitSeconds = 14f;
    [Tooltip("NavMesh sample radius when resolving stand / sweep points.")]
    public float navMeshSampleRadius = 4f;

    [Header("Sweep")]
    [Tooltip("Physics layers for contraband overlap (default Everything).")]
    public LayerMask sweepLayers = ~0;

    [Header("Debug")]
    public bool debugLogs;

    private Coroutine _sweepRoutine;

    private void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        ApplySweeperSpeed();
    }

    private void ApplySweeperSpeed()
    {
        if (agent != null && sweeperMoveSpeed > 0.01f)
            agent.speed = sweeperMoveSpeed;
    }

    private void OnEnable()
    {
        ApplySweeperSpeed();
        if (PrisonTimeManager.Instance != null)
            PrisonTimeManager.Instance.OnEventChanged += OnPrisonEvent;
    }

    private void OnDisable()
    {
        if (PrisonTimeManager.Instance != null)
            PrisonTimeManager.Instance.OnEventChanged -= OnPrisonEvent;
        StopSweep();
    }

    private void OnPrisonEvent(PrisonEventType evt)
    {
        if (PrisonEventExtensions.IsMorningLineUp(evt))
            TryStartSweepForCurrentPhase();
        else
            StopSweep();
    }

    /// <summary>Call when this component is enabled mid-phase (e.g. patrol guard switches to sweep duty).</summary>
    public void TryStartSweepForCurrentPhase()
    {
        if (!isActiveAndEnabled) return;
        var tm = PrisonTimeManager.Instance;
        if (tm == null || !PrisonEventExtensions.IsMorningLineUp(tm.CurrentEvent))
            return;
        if (_sweepRoutine != null) return;
        if (_activeSweeper != null && _activeSweeper != this)
        {
            if (debugLogs)
                Debug.Log($"[MorningShakedownSweeper] {name}: another guard is already sweeping — skipped.", this);
            return;
        }

        _activeSweeper = this;
        _sweepRoutine = StartCoroutine(SweepAllCells());
    }

    private void StopSweep()
    {
        if (_sweepRoutine != null)
        {
            StopCoroutine(_sweepRoutine);
            _sweepRoutine = null;
        }

        if (_activeSweeper == this)
            _activeSweeper = null;
    }

    private IEnumerator SweepAllCells()
    {
        var reg = PrisonLocationRegistry.Instance;
        if (reg == null || agent == null)
        {
            if (debugLogs) Debug.LogWarning($"[MorningShakedownSweeper] {name}: missing registry or agent.", this);
            FinishSweep();
            yield break;
        }

        ApplySweeperSpeed();

        if (delayBeforeFirstCellSeconds > 0f)
            yield return new WaitForSeconds(delayBeforeFirstCellSeconds);

        if (!EnsureAgentOnNavMesh())
        {
            if (debugLogs) Debug.LogWarning($"[MorningShakedownSweeper] {name}: not on NavMesh — cannot sweep.", this);
            FinishSweep();
            yield break;
        }

        agent.isStopped = false;

        while (true)
        {
            var tmPhase = PrisonTimeManager.Instance;
            if (tmPhase == null || !PrisonEventExtensions.IsMorningLineUp(tmPhase.CurrentEvent))
                break;

            MorningRollCallTracker.EnsureInstance();
            var rollTracker = MorningRollCallTracker.Instance;
            if (rollTracker != null && rollTracker.AreAllInmatesShakedownComplete())
                break;

            for (int i = 0; i < reg.CellCount; i++)
            {
                var tm = PrisonTimeManager.Instance;
                if (tm == null || !PrisonEventExtensions.IsMorningLineUp(tm.CurrentEvent))
                    break;

                MorningRollCallTracker.EnsureInstance();
                var tracker = MorningRollCallTracker.Instance;
                if (tracker != null && tracker.HasCellBeenShakedown(i))
                    continue;

                var cell = reg.GetCell(i);
                if (cell == null)
                    continue;

                if (visitRollCallStandFirst)
                {
                    Vector3 doorDest = GetRollCallStandPosition(cell);
                    bool arrived = false;
                    yield return TravelTo(doorDest, result => arrived = result);
                    if (!arrived)
                    {
                        if (debugLogs)
                            Debug.LogWarning($"[MorningShakedownSweeper] {name}: did not reach roll-call stand for cell {i} — not marking cleared.", this);
                        continue;
                    }

                    yield return HoldPositionAt(doorDest, visitStandPointSeconds);
                    if (!IsAtWorldPosition(doorDest))
                    {
                        if (debugLogs)
                            Debug.LogWarning($"[MorningShakedownSweeper] {name}: left cell {i} stand before check finished — not marking cleared.", this);
                        continue;
                    }

                    if (pathIntoCellAfterDoorStop)
                    {
                        Vector3 interior = cell.ShakedownSweepWorldCenter;
                        bool insideArrived = false;
                        yield return TravelTo(interior, result => insideArrived = result);
                        if (!insideArrived)
                        {
                            if (debugLogs)
                                Debug.LogWarning($"[MorningShakedownSweeper] {name}: did not reach interior sweep for cell {i} — not marking cleared.", this);
                            continue;
                        }

                        float dwell = Mathf.Clamp(visitStandPointSeconds * 0.45f, 0.35f, 2.5f);
                        yield return HoldPositionAt(interior, dwell);
                        if (!IsAtWorldPosition(interior))
                        {
                            if (debugLogs)
                                Debug.LogWarning($"[MorningShakedownSweeper] {name}: left cell {i} interior before check finished — not marking cleared.", this);
                            continue;
                        }
                    }
                }
                else
                {
                    bool arrived = false;
                    yield return TravelTo(cell.ShakedownSweepWorldCenter, result => arrived = result);
                    if (!arrived)
                        continue;
                }

                ConfiscatePickupsInCell(cell);
                if (!ShouldMarkCellClearedAfterVisit(i, cell, GetRollCallStandPosition(cell)))
                {
                    if (debugLogs)
                        Debug.Log($"[MorningShakedownSweeper] {name}: cell {i} — occupant clearance not met — will retry.", this);
                    if (pauseBetweenCells > 0f)
                        yield return new WaitForSeconds(pauseBetweenCells);
                    continue;
                }

                MorningRollCallTracker.NotifyCellShakedownComplete(i);
                if (debugLogs)
                    Debug.Log($"[MorningShakedownSweeper] {name}: cell {i} shakedown complete at stand.", this);

                if (pauseBetweenCells > 0f)
                    yield return new WaitForSeconds(pauseBetweenCells);
            }

            if (!requireOccupantAtStandForClearance)
                break;

            if (pauseBetweenSweepLapsSeconds > 0f)
                yield return new WaitForSeconds(pauseBetweenSweepLapsSeconds);
        }

        FinishSweep();
    }

    private void FinishSweep()
    {
        _sweepRoutine = null;
        if (_activeSweeper == this)
            _activeSweeper = null;
    }

    private static Vector3 GetRollCallStandPosition(CellData cell)
    {
        if (cell.rollCallStandPoint != null)
            return cell.rollCallStandPoint.position;
        return cell.RollCallPosition;
    }

    private bool ShouldMarkCellClearedAfterVisit(int cellIndex, CellData cell, Vector3 standWorld)
    {
        if (!requireOccupantAtStandForClearance)
            return true;

        var reg = PrisonLocationRegistry.Instance;
        if (reg == null || cell == null)
            return true;

        if (!reg.TryGetCellOccupant(cellIndex, out var occupant) || occupant == null)
            return clearCellsWithNoRegisteredOccupant;

        var pc = occupant.GetComponentInParent<PrisonerController>();
        Vector3 p = pc != null ? pc.transform.position : occupant.transform.position;

        if (occupantClearanceMode == OccupantRollCallClearanceMode.InsideCellInterior)
        {
            float r = Mathf.Max(0.5f, cell.InteriorRadius * interiorClearanceRadiusMultiplier);
            return Vector3.Distance(p, cell.ShakedownSweepWorldCenter) <= r;
        }

        float maxDist = occupantStandClearanceDistance;
        if (pc != null)
            maxDist = Mathf.Max(maxDist, pc.compliantDistance);

        return Vector3.Distance(p, standWorld) <= maxDist;
    }

    private bool IsAtWorldPosition(Vector3 worldPosition) =>
        agent != null && Vector3.Distance(agent.transform.position, worldPosition) <= arriveDistance;

    private IEnumerator HoldPositionAt(Vector3 worldPosition, float seconds)
    {
        if (seconds <= 0f)
            yield break;

        float elapsed = 0f;
        while (elapsed < seconds)
        {
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                if (Vector3.Distance(agent.transform.position, worldPosition) > arriveDistance)
                    agent.isStopped = false;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (agent != null && agent.isOnNavMesh)
            agent.isStopped = false;
    }

    private IEnumerator TravelTo(Vector3 worldDestination, System.Action<bool> onComplete)
    {
        bool success = false;
        onComplete?.Invoke(false);

        if (agent == null)
            yield break;

        if (!EnsureAgentOnNavMesh())
            yield break;

        if (!TryResolveNavMeshDestination(worldDestination, out Vector3 dest))
            dest = worldDestination;

        agent.isStopped = false;
        agent.SetDestination(dest);

        float wait = 0f;
        while (agent.pathPending && wait < 3f)
        {
            wait += Time.deltaTime;
            yield return null;
        }

        wait = 0f;
        while (wait < maxTravelWaitSeconds)
        {
            if (IsAtWorldPosition(worldDestination))
            {
                success = true;
                break;
            }

            if (agent.isOnNavMesh && !agent.pathPending)
            {
                if (!agent.hasPath && agent.remainingDistance <= 0.01f)
                {
                    if (IsAtWorldPosition(worldDestination))
                        success = true;
                    break;
                }
            }

            wait += Time.deltaTime;
            yield return null;
        }

        if (!success)
            success = IsAtWorldPosition(worldDestination);

        onComplete?.Invoke(success);
    }

    private bool EnsureAgentOnNavMesh()
    {
        if (agent == null || !agent.enabled)
            return false;
        if (agent.isOnNavMesh)
            return true;

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
            return agent.isOnNavMesh;
        }

        return false;
    }

    private bool TryResolveNavMeshDestination(Vector3 raw, out Vector3 resolved)
    {
        resolved = raw;
        if (NavMesh.SamplePosition(raw, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
        {
            resolved = hit.position;
            return true;
        }
        return false;
    }

    public static bool ShouldConfiscate(ItemData data)
    {
        if (data == null) return false;
        return data.category == ItemCategory.Contraband
               || data.category == ItemCategory.Tool
               || data.category == ItemCategory.Weapon;
    }

    private void ConfiscatePickupsInCell(CellData cell)
    {
        if (cell == null) return;

        Vector3 c = cell.ShakedownSweepWorldCenter;
        float r = cell.InteriorRadius;
        var cols = Physics.OverlapSphere(c, r, sweepLayers, QueryTriggerInteraction.Collide);
        foreach (var col in cols)
        {
            var pickup = col.GetComponentInParent<PickupItem>();
            if (pickup == null || pickup.itemData == null)
                continue;
            if (!ShouldConfiscate(pickup.itemData))
                continue;

            Destroy(pickup.gameObject);
        }
    }
}
