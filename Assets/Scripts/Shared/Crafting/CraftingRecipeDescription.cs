using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>Rich TMP lines for crafting requirements (have / need, colors).</summary>
public static class CraftingRecipeDescription
{
    static readonly Color32 HaveOk = new Color32(138, 220, 160, 255);
    static readonly Color32 Partial = new Color32(255, 196, 120, 255);
    static readonly Color32 Missing = new Color32(240, 120, 114, 255);

    /// <returns>Paragraph with one ingredient per line.</returns>
    public static string IngredientsRichParagraph(CraftingRecipe recipe, PlayerInventory inventory)
    {
        if (recipe == null || recipe.ingredients == null || recipe.ingredients.Length == 0)
            return "<i>No ingredients defined.</i>";

        var sb = new StringBuilder();
        foreach (CraftingIngredient ing in recipe.ingredients)
        {
            if (ing.item == null) continue;
            int have = inventory != null ? inventory.CountItem(ing.item) : 0;
            int need = Mathf.Max(1, ing.amount);
            bool ok = have >= need;
            Color32 c = ok ? HaveOk : (have > 0 ? Partial : Missing);
            string name = Escape(string.IsNullOrEmpty(ing.item.itemName) ? ing.item.name : ing.item.itemName);
            sb.Append($"<color=#{ColorUtility.ToHtmlStringRGBA(c)}>");
            sb.Append(ok ? "✔ " : "• ");
            sb.Append(name);
            sb.Append($" · {have}/{need}</color>\n");
        }
        return sb.ToString().TrimEnd();
    }

    /// <summary>Single-line collapsed requirements for compact HUD.</summary>
    public static IEnumerable<string> IngredientRequirementLines(CraftingRecipe recipe)
    {
        if (recipe?.ingredients == null) yield break;
        foreach (CraftingIngredient ing in recipe.ingredients)
        {
            if (ing.item == null) continue;
            string n = string.IsNullOrEmpty(ing.item.itemName) ? ing.item.name : ing.item.itemName;
            yield return $"{n} ×{Mathf.Max(1, ing.amount)}";
        }
    }

    static string Escape(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return s.Replace("&", "&amp;")
            .Replace("<", "\\<")
            .Replace(">", "\\>");
    }
}
