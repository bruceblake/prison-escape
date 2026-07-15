using System.Collections.Generic;
using UnityEngine;

namespace Prison
{
    /// <summary>
    /// A volume where presence counts as an escape attempt: always, or only during listed phases.
    /// Uses collider-bounds queries (robust to teleports) instead of trigger enter/exit tracking.
    /// Guards who spot a prisoner inside an active restricted zone arrest them to solitary
    /// instead of the normal walk-back escort.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class RestrictedZone : MonoBehaviour
    {
        private static readonly List<RestrictedZone> All = new();

        [Tooltip("Display name for alerts, e.g. 'Perimeter band'.")]
        public string zoneName;

        [Tooltip("Restricted at all times (vent corridors, beyond the fence, outer band).")]
        public bool alwaysRestricted = true;

        [Tooltip("If not always restricted, the phases during which this zone is off-limits.")]
        public PrisonEventType[] restrictedDuring;

        private BoxCollider _box;

        public bool IsRestrictedNow
        {
            get
            {
                var evt = PrisonTimeManager.Instance != null
                    ? PrisonTimeManager.Instance.CurrentEvent
                    : PrisonEventType.RollCall;
                return RestrictedZoneRules.IsRestricted(alwaysRestricted, restrictedDuring, evt);
            }
        }

        public bool ContainsPosition(Vector3 worldPos)
        {
            if (_box == null) _box = GetComponent<BoxCollider>();
            if (_box == null) return false;
            return _box.bounds.Contains(worldPos);
        }

        private void Awake()
        {
            _box = GetComponent<BoxCollider>();
            if (_box != null) _box.isTrigger = true;
        }

        private void OnEnable() => All.Add(this);
        private void OnDisable() => All.Remove(this);

        /// <summary>True if this prisoner is currently inside any active restricted zone.</summary>
        public static bool IsPrisonerInActiveRestrictedZone(MonoBehaviour prisoner)
        {
            return GetActiveZoneContaining(prisoner) != null;
        }

        public static RestrictedZone GetActiveZoneContaining(MonoBehaviour prisoner)
        {
            if (prisoner == null) return null;
            Vector3 probe = prisoner.transform.position + Vector3.up * 0.5f;
            for (int i = 0; i < All.Count; i++)
            {
                var zone = All[i];
                if (zone == null) continue;
                if (zone.IsRestrictedNow && zone.ContainsPosition(probe))
                    return zone;
            }
            return null;
        }
    }
}
