using UnityEngine;

namespace Prison
{
    /// <summary>
    /// Marks a transform where <see cref="GameManager.PopulateWorldSpawns"/> may spawn a world pickup from <see cref="lootTable"/>.
    /// </summary>
    public class ItemSpawnNode : MonoBehaviour
    {
        public LootTable lootTable;

        [Range(0f, 1f)]
        [Tooltip("Probability that this node spawns one item when PopulateWorldSpawns runs.")]
        public float spawnChance = 0.5f;
    }
}
