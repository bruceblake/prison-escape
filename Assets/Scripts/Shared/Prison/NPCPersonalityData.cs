using System.Collections.Generic;
using UnityEngine;

namespace Prison
{
    [CreateAssetMenu(fileName = "NewPersonality", menuName = "Prison/Social/Personality")]
    public class NPCPersonalityData : ScriptableObject
    {
        [Header("Identity")]
        public string personalityName;

        [Header("Core Behavior")]
        [Tooltip("How quickly this NPC gains affinity from positive actions.")]
        [Min(0f)]
        public float affinityGainMultiplier = 1f;

        [Tooltip("Affinity threshold where this NPC may report the player.")]
        [Range(-100f, 100f)]
        public float snitchThreshold = -50f;

        [Tooltip("Affinity penalty applied for betrayal-style actions (e.g. -50).")]
        public int betrayalPenalty = -50;

        [Tooltip("Minimum affinity before this NPC will interact in full (e.g. Bully gate).")]
        [Range(-100, 100)]
        public int minAffinityToInteract = -100;

        [Header("Gift Preferences")]
        [Tooltip("Items that grant bonus affinity when gifted.")]
        public List<ItemData> favoredItems = new List<ItemData>();
    }
}
