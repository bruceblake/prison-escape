using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Prison
{
    public class PrisonLocationRegistry : MonoBehaviour
    {
        private const float BedStandOffsetMeters = 1.1f;
        private const float BedCenterMatchEpsilon = 0.55f;

        private static PrisonLocationRegistry _instance;
        public static PrisonLocationRegistry Instance
        {
            get => _instance;
            private set
            {
                if (_instance != null && _instance != value)
                {
                    Destroy(value.gameObject);
                    return;
                }
                _instance = value;
            }
        }

        [Header("Cells")]
        [Tooltip("Cell data in order (index 0 = player cell, etc.)")]
        public CellData[] cells = new CellData[0];

        [Header("Zones (Optional - auto-found if empty)")]
        public PrisonLocationZone cafeteria;
        public PrisonLocationZone yard;
        public PrisonLocationZone rollCallArea;
        public PrisonLocationZone workshop;

        private List<PrisonLocationZone> _allZones = new List<PrisonLocationZone>();

        /// <summary>Cell array index -> occupant (player or NPC). Used so dynamic NPC spawn skips taken cells.</summary>
        private readonly Dictionary<int, GameObject> _cellOccupants = new Dictionary<int, GameObject>();

        private void Awake()
        {
            Instance = this;
            RefreshZones();
        }

        private void RefreshZones()
        {
            _allZones.Clear();
            var found = FindObjectsByType<PrisonLocationZone>(FindObjectsSortMode.None);
            foreach (var z in found)
                _allZones.Add(z);

            if (cafeteria == null) cafeteria = FindZone(ZoneType.Cafeteria);
            if (yard == null) yard = FindZone(ZoneType.Yard);
            if (rollCallArea == null) rollCallArea = FindZone(ZoneType.RollCallArea);
            if (workshop == null) workshop = FindZone(ZoneType.Workshop);
        }

        private PrisonLocationZone FindZone(ZoneType type)
        {
            foreach (var z in _allZones)
                if (z.zoneType == type) return z;
            return null;
        }

        public CellData GetCell(int index)
        {
            if (cells == null || index < 0 || index >= cells.Length) return null;
            return cells[index];
        }

        public int CellCount => cells?.Length ?? 0;

        /// <summary>Returns true if another occupant holds this cell (destroyed refs are cleared).</summary>
        public bool IsCellOccupied(int cellIndex)
        {
            if (cells == null || cellIndex < 0 || cellIndex >= cells.Length) return true;
            if (!_cellOccupants.TryGetValue(cellIndex, out var go)) return false;
            if (go == null)
            {
                _cellOccupants.Remove(cellIndex);
                return false;
            }
            return true;
        }

        /// <summary>Lock a cell for this session. Same occupant re-registering the same cell succeeds.</summary>
        public bool TryRegisterCellOccupant(int cellIndex, GameObject occupant)
        {
            if (occupant == null || cells == null || cellIndex < 0 || cellIndex >= cells.Length) return false;

            if (_cellOccupants.TryGetValue(cellIndex, out var existing) && existing != null && existing != occupant)
                return false;

            _cellOccupants[cellIndex] = occupant;
            return true;
        }

        /// <summary>Registered occupant for this cell (player or NPC), if any.</summary>
        public bool TryGetCellOccupant(int cellIndex, out GameObject occupant)
        {
            occupant = null;
            if (cells == null || cellIndex < 0 || cellIndex >= cells.Length)
                return false;
            if (!_cellOccupants.TryGetValue(cellIndex, out var go) || go == null)
            {
                if (_cellOccupants.ContainsKey(cellIndex))
                    _cellOccupants.Remove(cellIndex);
                return false;
            }

            occupant = go;
            return true;
        }

        public void UnregisterCellOccupant(GameObject occupant)
        {
            if (occupant == null) return;
            var keys = new List<int>();
            foreach (var kv in _cellOccupants)
            {
                if (kv.Value == occupant)
                    keys.Add(kv.Key);
            }
            foreach (var k in keys)
                _cellOccupants.Remove(k);
        }

        public PrisonLocationZone GetCafeteria() => cafeteria;
        public PrisonLocationZone GetYard() => yard;
        public PrisonLocationZone GetRollCallArea() => rollCallArea;
        public PrisonLocationZone GetWorkshop() => workshop;

        /// <summary>HUD label for a cell block zone matching <paramref name="cellIndex"/>, or a fallback.</summary>
        public string GetCellHudLabel(int cellIndex)
        {
            RefreshZones();
            foreach (var z in _allZones)
            {
                if (z != null && z.zoneType == ZoneType.Cell && z.cellIndex == cellIndex)
                    return z.GetHudLabel();
            }
            return $"CELL {cellIndex}";
        }

        public Transform GetStandPointForEvent(PrisonEventType evt, int cellIndex)
        {
            switch (evt)
            {
                case PrisonEventType.RollCall:
                case PrisonEventType.MorningRollCall:
                case PrisonEventType.MiddayCount:
                case PrisonEventType.EveningCount:
                    var cell = GetCell(cellIndex);
                    if (cell != null && (cell.rollCallStandPoint != null || cell.spawnPoint != null))
                        return cell.rollCallStandPoint != null ? cell.rollCallStandPoint : cell.spawnPoint;
                    if (rollCallArea != null)
                        return rollCallArea.GetStandPointForIndex(cellIndex);
                    break;
                case PrisonEventType.NightRollCall:
                case PrisonEventType.LightsOut:
                {
                    cell = GetCell(cellIndex);
                    if (cell == null) return null;
                    return cell.spawnPoint;
                }
                case PrisonEventType.Breakfast:
                case PrisonEventType.Lunch:
                case PrisonEventType.Dinner:
                    return cafeteria != null ? cafeteria.GetStandPointForIndex(cellIndex) : null;
                case PrisonEventType.FreeTime:
                    if (yard != null) return yard.GetStandPointForIndex(cellIndex);
                    return cafeteria != null ? cafeteria.GetStandPointForIndex(cellIndex) : null;
                case PrisonEventType.WorkProgram:
                    if (workshop != null) return workshop.GetStandPointForIndex(cellIndex);
                    if (cafeteria != null) return cafeteria.GetStandPointForIndex(cellIndex);
                    return yard != null ? yard.GetStandPointForIndex(cellIndex) : null;
            }
            return null;
        }

        /// <summary>
        /// NavMesh-friendly stand position with per-cell spread so NPCs idle without stacking.
        /// </summary>
        public bool TryGetSpreadStandPosition(PrisonEventType evt, int cellIndex, out Vector3 worldPos)
        {
            worldPos = Vector3.zero;
            PrisonLocationZone zone = null;
            switch (evt)
            {
                case PrisonEventType.Breakfast:
                case PrisonEventType.Lunch:
                case PrisonEventType.Dinner:
                    zone = cafeteria;
                    break;
                case PrisonEventType.FreeTime:
                    zone = yard != null ? yard : cafeteria;
                    break;
                case PrisonEventType.WorkProgram:
                    zone = workshop != null ? workshop : (cafeteria != null ? cafeteria : yard);
                    break;
                case PrisonEventType.RollCall:
                case PrisonEventType.MorningRollCall:
                case PrisonEventType.MiddayCount:
                case PrisonEventType.EveningCount:
                {
                    var cell = GetCell(cellIndex);
                    if (cell != null && TryGetCountStandPosition(cell, out worldPos))
                        return true;
                    zone = rollCallArea;
                    break;
                }
                case PrisonEventType.NightRollCall:
                case PrisonEventType.LightsOut:
                {
                    var cell = GetCell(cellIndex);
                    if (cell == null) return false;
                    return TryGetCellFloorStand(cell, out worldPos);
                }
            }

            if (zone == null) return false;
            worldPos = zone.GetSpreadStandPosition(cellIndex);
            return true;
        }

        /// <summary>Floor stand beside the bed (toward the door), not on the mattress. Used for night + spawn teleports.</summary>
        public bool TryGetCellFloorStand(CellData cell, out Vector3 worldPos)
        {
            worldPos = Vector3.zero;
            if (cell == null) return false;

            Vector3 bed = cell.BedPresenceWorldCenter;
            Vector3 door = cell.NightCheckApproachTransform != null
                ? cell.NightCheckApproachTransform.position
                : bed;

            Vector3 toBed = bed - door;
            toBed.y = 0f;
            if (toBed.sqrMagnitude < 0.04f)
                toBed = Vector3.forward;
            else
                toBed.Normalize();

            Vector3 stand = bed - toBed * BedStandOffsetMeters;
            stand.y = Mathf.Max(PrisonLayoutAnchors.FloorY, bed.y);

            if (NavMesh.SamplePosition(stand, out NavMeshHit hit, 2.5f, NavMesh.AllAreas))
                worldPos = hit.position;
            else
                worldPos = stand;
            return true;
        }

        private bool TryGetCountStandPosition(CellData cell, out Vector3 worldPos)
        {
            worldPos = cell.RollCallPosition;
            if (cell.rollCallStandPoint == null || IsOnBedCenter(cell, worldPos))
            {
                if (TryGetCellFloorStand(cell, out var floor))
                {
                    worldPos = floor;
                    return true;
                }
            }

            if (cell.rollCallStandPoint != null || cell.spawnPoint != null)
                return true;

            return false;
        }

        private static bool IsOnBedCenter(CellData cell, Vector3 pos)
        {
            if (cell == null) return false;
            Vector3 bed = cell.BedPresenceWorldCenter;
            float horizontal = Vector3.Distance(
                new Vector3(pos.x, 0f, pos.z),
                new Vector3(bed.x, 0f, bed.z));
            return horizontal <= BedCenterMatchEpsilon;
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}
