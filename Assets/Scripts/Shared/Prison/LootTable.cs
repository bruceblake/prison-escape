using System.Collections.Generic;
using UnityEngine;

namespace Prison
{
    [CreateAssetMenu(fileName = "NewLootTable", menuName = "Prison/Items/Loot Table")]
    public class LootTable : ScriptableObject
    {
        [Tooltip("Pool for weighted random selection. Null entries are ignored.")]
        public List<ItemData> possibleItems = new List<ItemData>();

        /// <summary>Base weight only from rarity (Common 60, Uncommon 25, Rare 10, Legendary 5). Final weight = base × <see cref="ItemData.weightMultiplier"/>.</summary>
        public static float GetRarityBaseWeight(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Common: return 60f;
                case ItemRarity.Uncommon: return 25f;
                case ItemRarity.Rare: return 10f;
                case ItemRarity.Legendary: return 5f;
                default: return 60f;
            }
        }

        public ItemData GetRandomItem()
        {
            if (possibleItems == null || possibleItems.Count == 0) return null;

            float total = 0f;
            for (int i = 0; i < possibleItems.Count; i++)
            {
                ItemData item = possibleItems[i];
                if (item == null) continue;
                float w = GetRarityBaseWeight(item.rarity) * Mathf.Max(0.01f, item.weightMultiplier);
                total += w;
            }

            if (total <= 0f) return null;

            float roll = UnityEngine.Random.Range(0f, total);
            float acc = 0f;
            for (int i = 0; i < possibleItems.Count; i++)
            {
                ItemData item = possibleItems[i];
                if (item == null) continue;
                acc += GetRarityBaseWeight(item.rarity) * Mathf.Max(0.01f, item.weightMultiplier);
                if (roll < acc) return item;
            }

            for (int i = possibleItems.Count - 1; i >= 0; i--)
                if (possibleItems[i] != null) return possibleItems[i];
            return null;
        }
    }
}
