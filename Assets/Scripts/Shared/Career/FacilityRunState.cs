using System;

namespace Prison.Career
{
    /// <summary>
    /// Per-visit local run state, serialized inside the world file and overwritten wholesale on
    /// every facility entry (revisits and transfers both reset it — "from scratch" means the
    /// facility run, never the career). Inventory/NPC/heat blobs are reserved for mid-run resume,
    /// which is out of scope for the first code ship; day/seed/visitIndex are live.
    /// </summary>
    [Serializable]
    public class FacilityRunState
    {
        public string facilityId = "";
        /// <summary>1-based per-facility visit number within this world.</summary>
        public int visitIndex;
        /// <summary>1-based day index; increments at each Morning Count.</summary>
        public int day = 1;
        /// <summary>Deterministic seed for this visit's loot layout (see <see cref="CareerSeed"/>).</summary>
        public int worldSeed;

        // Reserved for mid-run resume (spec § Data model); unused by the first code ship.
        public string inventoryBlob = "";
        public string stashBlob = "";
        public float heat;
        public string npcRelationshipBlob = "";
        public int cellAssignment = -1;

        public bool IsActive => !string.IsNullOrEmpty(facilityId);
    }
}
