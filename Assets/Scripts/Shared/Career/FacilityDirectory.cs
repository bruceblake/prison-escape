using System.Collections.Generic;
using UnityEngine;

namespace Prison.Career
{
    /// <summary>
    /// Runtime lookup for <see cref="FacilityDefinition"/>s. Prefers the tunable assets under
    /// Resources/Facilities (created by the editor installer); any slot with no asset yet falls
    /// back to a transient definition built from <see cref="FacilityCatalog"/>, so the career
    /// system works before the assets exist.
    /// </summary>
    public static class FacilityDirectory
    {
        public const string ResourcesFolder = "Facilities";

        private static Dictionary<string, FacilityDefinition> _byId;

        public static FacilityDefinition Get(string facilityId)
        {
            EnsureLoaded();
            return facilityId != null && _byId.TryGetValue(facilityId, out var def) ? def : null;
        }

        /// <summary>All 10 slots: dev sandbox first, then ladder order 0–8.</summary>
        public static List<FacilityDefinition> AllInLadderOrder()
        {
            EnsureLoaded();
            var list = new List<FacilityDefinition>(FacilityCatalog.All.Length)
            {
                Get(FacilityIds.DevSandbox),
            };
            foreach (string id in FacilityIds.LadderOrder)
                list.Add(Get(id));
            list.RemoveAll(d => d == null);
            return list;
        }

        /// <summary>Drops cached definitions (domain reloads clear statics anyway; tests call this).</summary>
        public static void Reset() => _byId = null;

        private static void EnsureLoaded()
        {
            if (_byId != null) return;
            _byId = new Dictionary<string, FacilityDefinition>();

            foreach (var asset in Resources.LoadAll<FacilityDefinition>(ResourcesFolder))
                if (asset != null && !string.IsNullOrEmpty(asset.id))
                    _byId[asset.id] = asset;

            foreach (var info in FacilityCatalog.All)
            {
                if (_byId.ContainsKey(info.id)) continue;
                var def = ScriptableObject.CreateInstance<FacilityDefinition>();
                def.hideFlags = HideFlags.HideAndDontSave;
                def.name = "Facility_" + info.id + " (catalog fallback)";
                def.PopulateFrom(info);
                _byId[info.id] = def;
            }
        }
    }
}
