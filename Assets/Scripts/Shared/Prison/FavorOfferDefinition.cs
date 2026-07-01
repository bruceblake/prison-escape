using System.Collections.Generic;
using UnityEngine;

namespace Prison
{
    [CreateAssetMenu(fileName = "NewFavorOffer", menuName = "Prison/Social/Favor Offer")]
    public class FavorOfferDefinition : ScriptableObject
    {
        public string requestLabel = "I need an item. Can you get it?";

        public ItemData requiredItem;

        [Tooltip("Affinity granted when the player delivers the item.")]
        public int affinityReward = 15;

        [Tooltip("If empty, this favor can roll in any schedule phase.")]
        public List<PrisonEventType> activeDuringPhases = new List<PrisonEventType>();

        [Tooltip("If empty, any personality can get this. Otherwise only the listed types.")]
        public List<NPCPersonalityData> onlyForPersonalities = new List<NPCPersonalityData>();

        /// <summary>
        /// True if this offer may be rolled for the given phase and NPC personality (empty lists = no constraint).
        /// </summary>
        public bool IsValidFor(PrisonEventType phase, NPCPersonalityData personality)
        {
            if (activeDuringPhases != null && activeDuringPhases.Count > 0 && !activeDuringPhases.Contains(phase))
                return false;

            if (onlyForPersonalities != null && onlyForPersonalities.Count > 0 && !onlyForPersonalities.Contains(personality))
                return false;

            return true;
        }
    }
}
