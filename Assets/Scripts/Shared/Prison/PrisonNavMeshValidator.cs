using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Prison
{
    /// <summary>
    /// Runtime checker that reports which prison points are off NavMesh.
    /// Add to SinglePlayerScene while tuning cell/zone transforms.
    /// </summary>
    public class PrisonNavMeshValidator : MonoBehaviour
    {
        [Header("References")]
        public PrisonLocationRegistry registry;

        [Header("Validation")]
        [Tooltip("Run one full validation automatically on Start.")]
        public bool validateOnStart = true;
        [Tooltip("If true, also validates all PrisonLocationZone stand points in scene.")]
        public bool includeZoneStandPoints = true;
        [Tooltip("If true, also validates every guard patrol waypoint in the GameManager spawn table.")]
        public bool includePatrolWaypoints = true;
        [Tooltip("How far from each point we allow a NavMesh sample.")]
        public float sampleRadius = 1.2f;
        [Tooltip("Log each valid point too (very noisy).")]
        public bool logValidPoints;

        public void Start()
        {
            if (validateOnStart)
                ValidateNow();
        }

        [ContextMenu("Validate Prison NavMesh Now")]
        public void ValidateNow()
        {
            if (registry == null)
                registry = PrisonLocationRegistry.Instance ?? FindAnyObjectByType<PrisonLocationRegistry>();

            if (registry == null)
            {
                Debug.LogWarning("[PrisonNavMeshValidator] No PrisonLocationRegistry found.");
                return;
            }

            int okCount = 0;
            int badCount = 0;
            var badLines = new List<string>();

            for (int i = 0; i < registry.CellCount; i++)
            {
                var cell = registry.GetCell(i);
                if (cell == null)
                {
                    badCount++;
                    badLines.Add($"Cell[{i}] missing CellData reference.");
                    continue;
                }

                CheckTransform(ref okCount, ref badCount, badLines, $"Cell[{i}].spawnPoint", cell.spawnPoint);
                CheckTransform(ref okCount, ref badCount, badLines, $"Cell[{i}].rollCallStandPoint", cell.rollCallStandPoint);
                CheckTransform(ref okCount, ref badCount, badLines, $"Cell[{i}].nightCheckApproachPoint", cell.nightCheckApproachPoint);
                CheckTransform(ref okCount, ref badCount, badLines, $"Cell[{i}].bedPresenceCenter", cell.bedPresenceCenter);
                CheckTransform(ref okCount, ref badCount, badLines, $"Cell[{i}].shakedownSweepCenter", cell.shakedownSweepCenter);
            }

            if (includeZoneStandPoints)
            {
                var zones = FindObjectsByType<PrisonLocationZone>(FindObjectsInactive.Exclude);
                foreach (var z in zones)
                {
                    if (z == null) continue;
                    if (z.standPoints == null || z.standPoints.Length == 0)
                    {
                        CheckPosition(ref okCount, ref badCount, badLines, $"Zone[{z.zoneType}] '{z.name}' (zone transform)", z.transform.position);
                        continue;
                    }

                    for (int i = 0; i < z.standPoints.Length; i++)
                    {
                        CheckTransform(ref okCount, ref badCount, badLines, $"Zone[{z.zoneType}] '{z.name}'.standPoints[{i}]", z.standPoints[i]);
                    }
                }
            }

            if (includePatrolWaypoints)
            {
                var gm = FindAnyObjectByType<GameManager>();
                if (gm != null && gm.guardSpawnTable != null)
                {
                    for (int g = 0; g < gm.guardSpawnTable.Length; g++)
                    {
                        var entry = gm.guardSpawnTable[g];
                        if (entry == null || entry.patrolWaypoints == null) continue;
                        for (int i = 0; i < entry.patrolWaypoints.Length; i++)
                            CheckTransform(ref okCount, ref badCount, badLines,
                                $"GuardSpawn[{g}] '{entry.displayName}'.patrolWaypoints[{i}]", entry.patrolWaypoints[i]);
                    }
                }
            }

            if (badCount == 0)
            {
                Debug.Log($"[PrisonNavMeshValidator] PASS: {okCount} points validated on NavMesh.", this);
                return;
            }

            Debug.LogWarning($"[PrisonNavMeshValidator] FOUND {badCount} NavMesh issues ({okCount} valid). See details below.", this);
            foreach (var line in badLines)
                Debug.LogWarning($"[PrisonNavMeshValidator] {line}", this);
        }

        private void CheckTransform(ref int okCount, ref int badCount, List<string> badLines, string label, Transform t)
        {
            if (t == null)
            {
                badCount++;
                badLines.Add($"{label} is NULL.");
                return;
            }

            CheckPosition(ref okCount, ref badCount, badLines, $"{label} ('{t.name}')", t.position);
        }

        private void CheckPosition(ref int okCount, ref int badCount, List<string> badLines, string label, Vector3 position)
        {
            if (NavMesh.SamplePosition(position, out var hit, sampleRadius, NavMesh.AllAreas))
            {
                okCount++;
                if (logValidPoints)
                    Debug.Log($"[PrisonNavMeshValidator] OK: {label} at {position} (nearest mesh {hit.position}).", this);
                return;
            }

            badCount++;
            badLines.Add($"{label} at {position} is OFF NavMesh (radius {sampleRadius}).");
        }
    }
}
