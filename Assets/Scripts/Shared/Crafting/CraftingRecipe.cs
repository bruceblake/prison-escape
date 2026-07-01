using UnityEngine;

[System.Serializable]
public class CraftingIngredient
{
    public ItemData item;
    public int amount = 1;
}

[CreateAssetMenu(fileName = "New Recipe", menuName = "Inventory/Crafting Recipe")]
public class CraftingRecipe : ScriptableObject
{
    public string recipeName;
    public CraftingIngredient[] ingredients;
    public ItemData result;
    public int resultAmount = 1;
}
