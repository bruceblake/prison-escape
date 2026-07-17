using UnityEngine;

// 1. The Enum must exist here so all other scripts can find it!
public enum ItemCategory
{
    CraftingPart,
    Tool,
    Weapon,
    Consumable,
    /// <summary>Illegal goods; confiscated during morning shakedown like tools/weapons.</summary>
    Contraband,
}

public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Legendary
}

[CreateAssetMenu(fileName = "Item", menuName = "Prison/Items/Item (Base)")]
public class ItemData : ScriptableObject
{
    [Header("Network Data")]
    public ushort networkId; 

    [Header("Basic Info")]
    public string itemName;
    public string description;
    public Sprite icon; 
    
    // 2. The category variable must exist here so PartData and ToolData can use it!
    public ItemCategory category;

    [Header("Economy")]
    [Tooltip("Base cash value for trading. 0 = derive from rarity (Common 8, Uncommon 15, Rare 30, Legendary 60).")]
    [Min(0f)]
    public float baseValue;

    [Header("Loot & Spawning")]
    public ItemRarity rarity = ItemRarity.Common;
    [Tooltip("Multiplies the loot table’s rarity base weight in weighted random selection.")]
    [Min(0.01f)]
    public float weightMultiplier = 1f;

    [Header("World")]
    [Tooltip("Prefab spawned at ItemSpawnNode positions (should include WorldItemPickup or one will be added).")]
    public GameObject worldPrefab;
}