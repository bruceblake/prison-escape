using UnityEngine;

namespace Prison
{
    public enum ZoneType
    {
        Cell,
        Cafeteria,
        Yard,
        RollCallArea,
        Workshop,
        Showers,
        Security,
        Corridor,
        Solitary
    }

    public class PrisonLocationZone : MonoBehaviour
    {
        [Header("Zone")]
        public ZoneType zoneType;

        [Tooltip("For Cell zones: index of this cell. Ignored for other types.")]
        public int cellIndex;

        [Tooltip("HUD string e.g. CELL BLOCK B, YARD. If empty, auto label from type + index.")]
        public string hudDisplayName;

        [Header("Stand Points")]
        [Tooltip("Transforms where prisoners stand. Use child objects or assign manually.")]
        public Transform[] standPoints;

        private void OnValidate()
        {
            if (standPoints == null || standPoints.Length == 0)
            {
                var children = new System.Collections.Generic.List<Transform>();
                foreach (Transform child in transform)
                    children.Add(child);
                if (children.Count > 0)
                {
                    standPoints = children.ToArray();
                }
            }
        }

        public Transform GetRandomStandPoint()
        {
            if (standPoints == null || standPoints.Length == 0)
                return transform;
            return standPoints[Random.Range(0, standPoints.Length)];
        }

        public string GetHudLabel()
        {
            if (!string.IsNullOrEmpty(hudDisplayName))
                return hudDisplayName;
            if (zoneType == ZoneType.Cell)
                return $"CELL {cellIndex}";
            if (zoneType == ZoneType.Cafeteria) return "CAFETERIA";
            if (zoneType == ZoneType.Yard) return "YARD";
            if (zoneType == ZoneType.RollCallArea) return "ROLL CALL";
            if (zoneType == ZoneType.Workshop) return "WORKSHOP";
            if (zoneType == ZoneType.Showers) return "SHOWERS";
            if (zoneType == ZoneType.Security) return "SECURITY";
            if (zoneType == ZoneType.Corridor) return "CORRIDOR";
            if (zoneType == ZoneType.Solitary) return "SOLITARY";
            return zoneType.ToString().ToUpperInvariant();
        }

        private void OnTriggerEnter(Collider other)
        {
            var prisoner = other.GetComponent<PrisonerController>();
            if (prisoner != null)
                prisoner.EnterZone(this);
        }

        private void OnTriggerExit(Collider other)
        {
            var prisoner = other.GetComponent<PrisonerController>();
            if (prisoner != null)
                prisoner.ExitZone(this);
        }
    }
}
