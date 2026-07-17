using System.Collections.Generic;
using UnityEngine;

public static class CraftingSystem
{
    /// <summary>Raised after every successful craft (escape-run stats, achievements).</summary>
    public static event System.Action<CraftingRecipe> OnItemCrafted;

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

    /// <summary>
    /// Transactional craft: either the ingredients are consumed AND the result lands in the
    /// inventory, or nothing changes. Consumed parts are refunded when the result can't be
    /// added (they always fit back into the slots they just freed).
    /// </summary>
    public static bool TryCraft(CraftingRecipe recipe, PlayerInventory inventory)
    {
        if (!CanCraft(recipe, inventory))
            return false;

        var consumed = new List<CraftingIngredient>();
        foreach (CraftingIngredient ingredient in recipe.ingredients)
        {
            if (inventory.RemoveItem(ingredient.item, ingredient.amount))
            {
                consumed.Add(ingredient);
                continue;
            }
            Debug.LogWarning($"[CraftingSystem] Could not consume {ingredient.amount}x {ingredient.item?.itemName} — craft aborted, parts returned.");
            Refund(inventory, consumed);
            return false;
        }

        if (!inventory.AddItem(recipe.result, recipe.resultAmount))
        {
            Debug.LogWarning($"[CraftingSystem] No room for {recipe.result.itemName} — craft aborted, parts returned.");
            Refund(inventory, consumed);
            return false;
        }

        Debug.Log($"[CraftingSystem] Crafted {recipe.resultAmount}x {recipe.result.itemName}");
        OnItemCrafted?.Invoke(recipe);
        return true;
    }

    private static void Refund(PlayerInventory inventory, List<CraftingIngredient> consumed)
    {
        foreach (CraftingIngredient ingredient in consumed)
            inventory.AddItem(ingredient.item, ingredient.amount);
    }
}
