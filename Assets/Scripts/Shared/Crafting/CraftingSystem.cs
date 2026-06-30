using UnityEngine;

public static class CraftingSystem
{
    public static bool CanCraft(CraftingRecipe recipe, PlayerInventory inventory)
    {
        if (recipe == null || inventory == null)
            return false;

        foreach (CraftingIngredient ingredient in recipe.ingredients)
        {
            if (!inventory.HasItem(ingredient.item, ingredient.amount))
                return false;
        }

        return true;
    }

    public static bool TryCraft(CraftingRecipe recipe, PlayerInventory inventory)
    {
        if (!CanCraft(recipe, inventory))
            return false;

        foreach (CraftingIngredient ingredient in recipe.ingredients)
        {
            inventory.RemoveItem(ingredient.item, ingredient.amount);
        }

        bool added = inventory.AddItem(recipe.result, recipe.resultAmount);
        if (!added)
        {
            Debug.LogWarning($"[CraftingSystem] Crafted {recipe.result.itemName} but inventory was full. Parts were consumed.");
        }

        Debug.Log($"[CraftingSystem] Crafted {recipe.resultAmount}x {recipe.result.itemName}");
        return true;
    }
}
