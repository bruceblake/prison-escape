using System.Collections.Generic;
using UnityEngine;

namespace Prison
{
    /// <summary>
    /// Maps cell indices to <see cref="CellDoorController"/> instances so morning shakedown
    /// can open each door as its cell is cleared.
    /// </summary>
    public class CellDoorRegistry : MonoBehaviour
    {
        public static CellDoorRegistry Instance { get; private set; }

        private readonly Dictionary<int, CellDoorController> _doorsByCell = new();

        public static void EnsureInstance()
        {
            if (Instance != null) return;
            var existing = FindAnyObjectByType<CellDoorRegistry>();
            if (existing != null)
            {
                Instance = existing;
                return;
            }

            var go = new GameObject("CellDoorRegistry");
            Instance = go.AddComponent<CellDoorRegistry>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Rebuild();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void Rebuild()
        {
            _doorsByCell.Clear();
            var doors = FindObjectsByType<CellDoorController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var registry = PrisonLocationRegistry.Instance;

            for (int i = 0; i < doors.Length; i++)
            {
                var door = doors[i];
                if (door == null || !door.gameObject.activeInHierarchy) continue;

                int cellIndex = ParseCellIndexFromName(door.name);
                if (cellIndex < 0 && registry != null)
                    cellIndex = NearestCellIndex(door.transform.position, registry);

                if (cellIndex < 0 || _doorsByCell.ContainsKey(cellIndex))
                    continue;

                _doorsByCell[cellIndex] = door;
            }
        }

        public static void OpenCellDoor(int cellIndex)
        {
            EnsureInstance();
            Instance?.OpenCellDoorInternal(cellIndex);
        }

        /// <summary>Fail-soft when the sweeper stalls — inmates can leave cells for breakfast.</summary>
        public static void OpenAllCellDoors()
        {
            EnsureInstance();
            Instance?.OpenAllCellDoorsInternal();
        }

        private void OpenAllCellDoorsInternal()
        {
            Rebuild();
            foreach (var kv in _doorsByCell)
            {
                if (kv.Value != null)
                    kv.Value.SetForcedOpen(true);
            }
        }

        private void OpenCellDoorInternal(int cellIndex)
        {
            if (!_doorsByCell.TryGetValue(cellIndex, out var door) || door == null)
            {
                Rebuild();
                _doorsByCell.TryGetValue(cellIndex, out door);
            }

            if (door != null)
                door.SetForcedOpen(true);
        }

        private static int ParseCellIndexFromName(string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
                return -1;

            const string cellDoorPrefix = "Cell_";
            const string cellDoorSuffix = "_Door";
            if (objectName.StartsWith(cellDoorPrefix) && objectName.EndsWith(cellDoorSuffix))
            {
                string middle = objectName.Substring(cellDoorPrefix.Length, objectName.Length - cellDoorPrefix.Length - cellDoorSuffix.Length);
                if (int.TryParse(middle, out int oneBased))
                    return oneBased - 1;
            }

            const string jailPrefix = "JailCell_";
            if (objectName.StartsWith(jailPrefix)
                && int.TryParse(objectName.Substring(jailPrefix.Length), out int jailNum))
                return jailNum - 1;

            return -1;
        }

        private static int NearestCellIndex(Vector3 worldPos, PrisonLocationRegistry registry)
        {
            int best = -1;
            float bestDist = float.MaxValue;
            for (int i = 0; i < registry.CellCount; i++)
            {
                var cell = registry.GetCell(i);
                if (cell == null) continue;
                float dist = Vector3.Distance(worldPos, cell.RollCallPosition);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = i;
                }
            }

            return bestDist <= 8f ? best : -1;
        }
    }
}
