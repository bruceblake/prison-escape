using UnityEngine;

public class PickupItem : MonoBehaviour, IInteractable
{
    public ItemData itemData;

    public InteractionInputType InputType => InteractionInputType.Press;
    public float HoldDuration => 0f;

    public string GetInteractionPrompt(PlayerInventory inventory)
    {
        if (itemData == null) return "";
        return $"Press F to pick up {itemData.itemName}";
    }

    public bool CanInteract(PlayerInventory inventory)
    {
        return itemData != null;
    }

    public void Interact(PlayerInventory inventory)
    {
        if (inventory.AddItem(itemData, 1))
        {
            Destroy(gameObject);
        }
    }
}