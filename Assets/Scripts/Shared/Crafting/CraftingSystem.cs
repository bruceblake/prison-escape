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

        var consumed = new System.Collections.Generic.List<(ItemData item, int amount)>();
        foreach (CraftingIngredient ingredient in recipe.ingredients)
        {
            if (!inventory.RemoveItem(ingredient.item, ingredient.amount))
            {
                foreach (var (item, amount) in consumed)
                    inventory.AddItem(item, amount);
                return false;
            }

            consumed.Add((ingredient.item, ingredient.amount));
        }

        if (!inventory.AddItem(recipe.result, recipe.resultAmount))
        {
            foreach (var (item, amount) in consumed)
                inventory.AddItem(item, amount);
            return false;
        }

        Debug.Log($"[CraftingSystem] Crafted {recipe.resultAmount}x {recipe.result.itemName}");
        return true;
    }
}
