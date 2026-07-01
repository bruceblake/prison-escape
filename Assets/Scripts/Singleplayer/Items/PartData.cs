using UnityEngine;

[CreateAssetMenu(fileName = "New Part", menuName = "Inventory/Crafting Part")]
public class PartData : ItemData
{
    // Parts might not need any extra logic, but keeping them strictly typed 
    // helps if you later want to add things like "Stack Size"
    public int maxStackSize = 5;

    private void OnEnable()
    {
        category = ItemCategory.CraftingPart;
    }
}