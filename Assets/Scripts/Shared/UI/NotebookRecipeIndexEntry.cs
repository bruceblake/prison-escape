using UnityEngine;
using UnityEngine.UI;

/// <summary>Caches recipe reference + optional visuals for notebook index row selection.</summary>
[RequireComponent(typeof(Button))]
public class NotebookRecipeIndexEntry : MonoBehaviour
{
    public CraftingRecipe recipe;
    [Tooltip("Optional row background for selected state tint.")]
    public Image rowBackground;
}
