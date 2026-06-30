using UnityEngine;

/// <summary>
/// World pickup: place on a prefab (or add at runtime). Assign <see cref="itemData"/> after spawn from <see cref="Prison.ItemSpawnNode"/> / <see cref="GameManager"/>.
/// </summary>
public class WorldItemPickup : MonoBehaviour, IInteractable
{
    public ItemData itemData;

    public InteractionInputType InputType => InteractionInputType.Press;
    public float HoldDuration => 0f;

    public string GetInteractionPrompt(PlayerInventory inventory)
    {
        if (itemData == null) return "[F] Pick up";
        return $"[F] Pick up {itemData.itemName}";
    }

    public bool CanInteract(PlayerInventory inventory)
    {
        return itemData != null;
    }

    public void Interact(PlayerInventory inventory)
    {
        if (inventory == null || itemData == null) return;
        // AddItem returns false if inventory is full (or unstackable rules fail) — do not destroy the world pickup in that case.
        if (inventory.AddItem(itemData, 1))
            Destroy(gameObject);
    }
}
