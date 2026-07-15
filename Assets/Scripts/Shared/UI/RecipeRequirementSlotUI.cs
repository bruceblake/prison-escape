using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Prison;

/// <summary>
/// Single ingredient thumbnail + have/need readout for the notebook crafting card.
/// </summary>
public class RecipeRequirementSlotUI : MonoBehaviour
{
    public Image iconImage;
    public TMP_Text amountLabel;
    public Image backingImage;

    private static readonly Color IconSatisfied = Color.white;
    private static readonly Color IconShortage = new Color(0.5f, 0.5f, 0.5f, 0.8f);

    private static Color TextSatisfied => PrisonUITheme.InkGreen;
    private static readonly Color TextShortage = new Color(1.0f, 0.4f, 0.4f);

    private static readonly Color DefaultBackingTint = new Color(0.11f, 0.12f, 0.148f, 0.94f);

    private void Awake()
    {
        if (iconImage != null)
            iconImage.preserveAspect = true;
        if (backingImage != null)
        {
            backingImage.raycastTarget = false;
            Color oc = backingImage.color;
            float lum = oc.r * 0.299f + oc.g * 0.587f + oc.b * 0.114f;
            if (lum > 0.92f && oc.a > 0.85f)
                backingImage.color = new Color(DefaultBackingTint.r, DefaultBackingTint.g, DefaultBackingTint.b, Mathf.Max(DefaultBackingTint.a, oc.a * 0.97f));
        }
    }

    public void ClearSlot()
    {
        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
            iconImage.color = IconSatisfied;
        }
        if (amountLabel != null) amountLabel.text = "";
    }

    /// <summary>Uses <see cref="PlayerInventory.CountItem"/> vs <paramref name="ingredient"/>.amount.</summary>
    public void SetIngredient(CraftingIngredient ingredient, PlayerInventory inventory)
    {
        if (ingredient == null || ingredient.item == null)
        {
            ClearSlot();
            return;
        }

        int need = Mathf.Max(1, ingredient.amount);
        int have = inventory != null ? inventory.CountItem(ingredient.item) : 0;
        bool satisfied = have >= need;

        if (iconImage != null)
        {
            iconImage.enabled = true;
            iconImage.sprite = ingredient.item.icon;
            iconImage.color = satisfied ? IconSatisfied : IconShortage;
        }

        if (amountLabel != null)
        {
            amountLabel.color = satisfied ? TextSatisfied : TextShortage;
            amountLabel.text = $"{have}/{need}";
            amountLabel.enableWordWrapping = false;
            amountLabel.overflowMode = TextOverflowModes.Overflow;
            amountLabel.alignment = TextAlignmentOptions.Center;
            amountLabel.fontSize = 18;
        }

        if (backingImage != null)
        {
            Color ink = PrisonUITheme.InkGreen;
            backingImage.color = satisfied
                ? new Color(ink.r * 0.35f, ink.g * 0.35f, ink.b * 0.35f, 0.85f)
                : new Color(0.1f, 0.1f, 0.1f, 0.65f);
        }
    }
}
