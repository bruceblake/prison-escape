using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Prison
{
    /// <summary>
    /// Ensures a morning shakedown guard starts sweeping near the cells.
    /// Spawns a dedicated RollCallOfficer — never adopts scene StandardPatrol sweepers.
    /// Once a sweep is running, do not warp/restart it — that cancelled every cell stop.
    /// </summary>
    public class MorningRollCallSweeperDirector : MonoBehaviour
    {
        public static MorningRollCallSweeperDirector Instance { get; private set; }

        [SerializeField] private float sweeperKickoffDelaySeconds = 0.15f;
        [SerializeField] private float respawnOfficerIfLostSeconds = 8f;
        [SerializeField] private float openAllDoorsIfNoProgressSeconds = 35f;
        [SerializeField] private bool debugLogs = true;

        private Coroutine _watchRoutine;
        private static GameObject _spawnedOfficer;
        private float _phaseStartedUnscaled = float.NegativeInfinity;
        private bool _openedAllDoorsFailSoft;

        public static void EnsureInstance()
        {
            if (Instance != null) return;

            var tm = PrisonTimeManager.Instance;
            if (tm != null)
            {
                var onTm = tm.GetComponent<MorningRollCallSweeperDirector>();
                if (onTm == null)
                    onTm = tm.gameObject.AddComponent<MorningRollCallSweeperDirector>();
                Instance = onTm;
                return;
            }

            var existing = FindAnyObjectByType<MorningRollCallSweeperDirector>();
            if (existing != null)
            {
                Instance = existing;
                return;
            }

            var go = new GameObject("MorningRollCallSweeperDirector");
            Instance = go.AddComponent<MorningRollCallSweeperDirector>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            DestroySpawnedOfficer();
        }

        private void OnEnable()
        {
            if (PrisonTimeManager.Instance != null)
                PrisonTimeManager.Instance.OnEventChanged += OnScheduleEvent;

            var tm = PrisonTimeManager.Instance;
            if (tm != null && PrisonEventExtensions.IsMorningLineUp(tm.CurrentEvent))
                StartWatch();
        }

        private void OnDisable()
        {
            if (PrisonTimeManager.Instance != null)
                PrisonTimeManager.Instance.OnEventChanged -= OnScheduleEvent;
            StopWatch();
        }

        private void OnScheduleEvent(PrisonEventType evt)
        {
            if (PrisonEventExtensions.IsMorningLineUp(evt))
                StartWatch();
            else
            {
                StopWatch();
                DestroySpawnedOfficer();
                _openedAllDoorsFailSoft = false;
            }
        }

        /// <summary>Called from <see cref="MorningRollCallTracker.BeginPhase"/> for immediate kickoff.</summary>
        public void KickoffRollCallSweep()
        {
            _phaseStartedUnscaled = Time.unscaledTime;
            _openedAllDoorsFailSoft = false;
            StartWatch();
            StartNearCellSweep(forceRestart: false);
        }

        private void StartWatch()
        {
            StopWatch();
            _watchRoutine = StartCoroutine(WatchMorningSweep());
        }

        private void StopWatch()
        {
            if (_watchRoutine != null)
            {
                StopCoroutine(_watchRoutine);
                _watchRoutine = null;
            }
        }

        private static void DestroySpawnedOfficer()
        {
            if (_spawnedOfficer == null) return;
            Destroy(_spawnedOfficer);
            _spawnedOfficer = null;
        }

        private IEnumerator WatchMorningSweep()
        {
            if (sweeperKickoffDelaySeconds > 0f)
                yield return new WaitForSeconds(sweeperKickoffDelaySeconds);

            StartNearCellSweep(forceRestart: false);

            float elapsed = sweeperKickoffDelaySeconds;
            while (true)
            {
                var tm = PrisonTimeManager.Instance;
                if (tm == null || !PrisonEventExtensions.IsMorningLineUp(tm.CurrentEvent))
                    yield break;

                MorningRollCallTracker.EnsureInstance();
                var tracker = MorningRollCallTracker.Instance;
                if (tracker != null && tracker.AreAllInmatesShakedownComplete())
                    yield break;

                TryFailSoftOpenDoors(tracker);

                if (_spawnedOfficer == null || !_spawnedOfficer.activeInHierarchy)
                {
                    _spawnedOfficer = null;
                    if (debugLogs)
                        Debug.LogWarning("[MorningRollCallSweeperDirector] RollCallOfficer lost — respawning.", this);
                    StartNearCellSweep(forceRestart: true);
                }
                else if (!IsDedicatedSweeperActive())
                {
                    if (debugLogs)
                        Debug.LogWarning("[MorningRollCallSweeperDirector] No active sweep — restarting RollCallOfficer.", this);
                    StartNearCellSweep(forceRestart: true);
                }

                yield return new WaitForSeconds(2f);
                elapsed += 2f;

                if (elapsed >= respawnOfficerIfLostSeconds && !IsDedicatedSweeperActive())
                    StartNearCellSweep(forceRestart: true);
            }
        }

        private void TryFailSoftOpenDoors(MorningRollCallTracker tracker)
        {
            if (_openedAllDoorsFailSoft || openAllDoorsIfNoProgressSeconds <= 0f)
                return;
            if (_phaseStartedUnscaled <= 0f)
                return;
            if (Time.unscaledTime - _phaseStartedUnscaled < openAllDoorsIfNoProgressSeconds)
                return;

            int cleared = tracker != null ? tracker.CountInmatesShakedownComplete(out _) : 0;
            if (cleared > 0)
                return;

            _openedAllDoorsFailSoft = true;
            CellDoorRegistry.OpenAllCellDoors();
            if (debugLogs)
                Debug.LogWarning("[MorningRollCallSweeperDirector] No shakedown progress — opened all cell doors (fail-soft).", this);
        }

        private bool IsDedicatedSweeperActive()
        {
            if (_spawnedOfficer == null)
                return false;

            var dedicated = _spawnedOfficer.GetComponent<MorningShakedownSweeper>();
            return dedicated != null && dedicated.isActiveAndEnabled && dedicated.IsSweeping;
        }

        private void StartNearCellSweep(bool forceRestart)
        {
            MorningShakedownSweeper sweeper = null;
            if (_spawnedOfficer != null)
                sweeper = _spawnedOfficer.GetComponent<MorningShakedownSweeper>();

            if (sweeper == null)
                sweeper = SpawnOfficer();

            if (sweeper == null) return;

            if (sweeper.IsSweeping && !forceRestart)
                return;

            if (forceRestart && sweeper.IsSweeping)
                sweeper.ForceStopSweep();

            RepositionSweeperNearCells(sweeper);
            if (sweeper.agent == null)
                sweeper.agent = sweeper.GetComponent<NavMeshAgent>();

            sweeper.ForceStartSweepForCurrentPhase();
        }

        private MorningShakedownSweeper SpawnOfficer()
        {
            if (_spawnedOfficer != null)
            {
                var existing = _spawnedOfficer.GetComponent<MorningShakedownSweeper>();
                if (existing != null)
                    return existing;
            }

            var gm = FindAnyObjectByType<GameManager>();
            if (gm == null || gm.guardPrefab == null)
            {
                Debug.LogWarning("[MorningRollCallSweeperDirector] Cannot spawn roll-call officer — no GameManager/guardPrefab.");
                return null;
            }

            Vector3 spawnPos = ResolveSpawnPosition(out string anchorLabel);

            _spawnedOfficer = Instantiate(gm.guardPrefab, spawnPos, Quaternion.identity);
            _spawnedOfficer.name = "RollCallOfficer";
            _spawnedOfficer.SetActive(true);

            var shift = _spawnedOfficer.GetComponent<GuardShiftController>();
            if (shift != null)
                shift.enabled = false;

            var fsm = _spawnedOfficer.GetComponent<GuardFSM>();
            if (fsm != null) fsm.enabled = false;
            var detection = _spawnedOfficer.GetComponent<GuardDetection>();
            if (detection != null) detection.enabled = false;

            var nameLabel = _spawnedOfficer.GetComponent<Prison.Visuals.CharacterNameLabel>();
            if (nameLabel != null)
                nameLabel.SetDisplayName("Roll Call Officer");

            var sweeper = _spawnedOfficer.GetComponent<MorningShakedownSweeper>();
            if (sweeper == null)
                sweeper = _spawnedOfficer.AddComponent<MorningShakedownSweeper>();
            sweeper.agent = _spawnedOfficer.GetComponent<NavMeshAgent>();
            sweeper.sweeperMoveSpeed = 3.5f;
            sweeper.debugLogs = true;
            sweeper.requireOccupantAtStandForClearance = false;
            sweeper.delayBeforeFirstCellSeconds = 0.35f;
            sweeper.enabled = true;

            var agent = sweeper.agent;
            if (agent != null)
            {
                agent.enabled = true;
                PlaceAgentOnNavMesh(agent, spawnPos);
            }

            Debug.Log($"[MorningRollCallSweeperDirector] Spawned RollCallOfficer at {anchorLabel} ({spawnPos}).", _spawnedOfficer);
            return sweeper;
        }

        private static Vector3 ResolveSpawnPosition(out string anchorLabel)
        {
            if (TryResolveNavMeshAnchor(out Vector3 onMesh, out anchorLabel))
                return onMesh;

            anchorLabel = "raw-fallback";
            var registry = PrisonLocationRegistry.Instance;
            if (registry != null && registry.CellCount > 0)
            {
                var cell = registry.GetCell(0);
                if (cell != null)
                    return cell.RollCallPosition;
            }

            return PrisonLayoutAnchors.CellSouthConnectorCenter;
        }

        private static void RepositionSweeperNearCells(MorningShakedownSweeper sweeper)
        {
            if (sweeper == null) return;
            if (sweeper.IsSweeping) return;

            var agent = sweeper.agent != null ? sweeper.agent : sweeper.GetComponent<NavMeshAgent>();
            Vector3 pos = ResolveSpawnPosition(out _);
            if (agent != null)
                PlaceAgentOnNavMesh(agent, pos);
            else
                sweeper.transform.position = pos;
        }

        private static bool TryResolveNavMeshAnchor(out Vector3 position, out string label)
        {
            var candidates = new List<(Vector3 pos, string name)>();

            var registry = PrisonLocationRegistry.Instance;
            if (registry != null)
            {
                var playerCell = registry.GetCell(0);
                if (playerCell?.rollCallStandPoint != null)
                    candidates.Add((playerCell.rollCallStandPoint.position, "Cell0RollCall"));
                if (playerCell?.NightCheckApproachTransform != null)
                    candidates.Add((playerCell.NightCheckApproachTransform.position, "Cell0NightApproach"));
            }

            candidates.Add((PrisonLayoutAnchors.CellSouthConnectorCenter, "CellSouthConnector"));
            candidates.Add((PrisonLayoutAnchors.CellBlockCenter, "CellBlockCenter"));

            if (registry != null)
            {
                for (int i = 0; i < registry.CellCount; i++)
                {
                    var cell = registry.GetCell(i);
                    if (cell?.NightCheckApproachTransform != null)
                        candidates.Add((cell.NightCheckApproachTransform.position, $"Cell{i}NightApproach"));
                }
            }

            foreach (var (pos, name) in candidates)
            {
                if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 18f, NavMesh.AllAreas))
                {
                    position = hit.position;
                    label = name;
                    return true;
                }
            }

            position = PrisonLayoutAnchors.CellSouthConnectorCenter;
            label = "none";
            return false;
        }

        private static void PlaceAgentOnNavMesh(NavMeshAgent agent, Vector3 near)
        {
            if (agent == null)
                return;

            agent.enabled = true;
            for (int attempt = 0; attempt < 4; attempt++)
            {
                float radius = 6f + attempt * 8f;
                if (NavMesh.SamplePosition(near, out NavMeshHit hit, radius, NavMesh.AllAreas))
                {
                    agent.Warp(hit.position);
                    if (agent.isOnNavMesh)
                        return;
                }
            }

            agent.transform.position = near;
            agent.Warp(near);
        }
    }
}
