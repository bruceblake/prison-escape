using System.Collections.Generic;
using UnityEngine;

namespace Prison
{
    public class PrisonLocationRegistry : MonoBehaviour
    {
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
                    var cell = GetCell(cellIndex);
                    if (cell != null && (cell.rollCallStandPoint != null || cell.spawnPoint != null))
                        return cell.rollCallStandPoint != null ? cell.rollCallStandPoint : cell.spawnPoint;
                    if (rollCallArea != null)
                        return rollCallArea.GetRandomStandPoint();
                    break;
                case PrisonEventType.NightRollCall:
                case PrisonEventType.LightsOut:
                    cell = GetCell(cellIndex);
                    return cell?.spawnPoint;
                case PrisonEventType.Breakfast:
                case PrisonEventType.Lunch:
                case PrisonEventType.Dinner:
                    return cafeteria != null ? cafeteria.GetRandomStandPoint() : null;
                case PrisonEventType.FreeTime:
                    return yard != null ? yard.GetRandomStandPoint() : (cafeteria != null ? cafeteria.GetRandomStandPoint() : null);
            }
            return null;
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}
