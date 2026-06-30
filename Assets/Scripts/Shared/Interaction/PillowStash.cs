using UnityEngine;

/// <summary>
/// Hides one item under the pillow. Player can store the equipped item or take it back.
/// </summary>
public class PillowStash : MonoBehaviour, IInteractable
{
    [Header("Pillow Stash")]
    [Tooltip("Optional: visual to show when something is hidden (e.g. slight bulge)")]
    public GameObject stashIndicator;

    private ItemData storedItem;

    /// <summary>Item hidden under the pillow, if any. Used by bed-only stash UI.</summary>
    public ItemData StoredItem => storedItem;

    public InteractionInputType InputType => InteractionInputType.Press;
    public float HoldDuration => 0f;

    public string GetInteractionPrompt(PlayerInventory inventory)
    {
        if (storedItem != null)
            return $"Press F to take {storedItem.itemName} from under pillow";
        ItemData equipped = inventory?.GetEquippedItem();
        if (equipped != null)
            return $"Press F to hide {equipped.itemName} under pillow";
        return "Press F to hide item (select item in hotbar first)";
    }

    public bool CanInteract(PlayerInventory inventory)
    {
        if (inventory == null) return false;
        if (storedItem != null) return true;
        return inventory.GetEquippedItem() != null;
    }

    public void Interact(PlayerInventory inventory)
    {
        if (inventory == null) return;

        if (storedItem != null)
        {
            if (inventory.AddItem(storedItem, 1))
            {
                storedItem = null;
                UpdateStashIndicator(false);
            }
        }
        else
        {
            ItemData equipped = inventory.GetEquippedItem();
            if (equipped != null && inventory.RemoveItem(equipped, 1))
            {
                storedItem = equipped;
                UpdateStashIndicator(true);
            }
        }
    }

    private void UpdateStashIndicator(bool hasItem)
    {
        if (stashIndicator != null)
            stashIndicator.SetActive(hasItem);
    }
}
