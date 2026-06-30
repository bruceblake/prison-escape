using UnityEngine;
using System.Collections.Generic;
using System;

// This helper class makes our inventory slots visible in the Unity Inspector
[System.Serializable]
public class InventorySlot
{
    public ItemData item;
    public int quantity;

    public InventorySlot(ItemData newItem, int amount)
    {
        item = newItem;
        quantity = amount;
    }

    public bool IsEmpty => item == null || quantity <= 0;
}

public class PlayerInventory : MonoBehaviour
{
    [Header("Inventory Settings")]
    public int maxSlots = 6;
    public List<InventorySlot> inventorySlots = new List<InventorySlot>();
    public int selectedSlotIndex;

    [Header("Hover State (Read Only)")]
    public bool isHoveringOverItem = false;
    public GameObject currentHoveredObject;
    public ItemData currentHoveredItemData;

    /// <summary>Fires after swap, add, remove (for HUD refresh).</summary>
    public event Action SlotsChanged;

    private void Awake()
    {
        EnsureSlotCapacity();
    }

    public void EnsureSlotCapacity()
    {
        while (inventorySlots.Count < maxSlots)
            inventorySlots.Add(new InventorySlot(null, 0));
        if (inventorySlots.Count > maxSlots)
            inventorySlots.RemoveRange(maxSlots, inventorySlots.Count - maxSlots);
    }

    /// <summary>Swap two occupied or empty logical slots by index.</summary>
    public void SwapSlots(int indexA, int indexB)
    {
        if (indexA == indexB) return;
        if (indexA < 0 || indexB < 0 || indexA >= maxSlots || indexB >= maxSlots) return;
        EnsureSlotCapacity();
        InventorySlot tmp = inventorySlots[indexA];
        inventorySlots[indexA] = inventorySlots[indexB];
        inventorySlots[indexB] = tmp;
        SlotsChanged?.Invoke();
    }

    private void RaiseSlotsChanged() => SlotsChanged?.Invoke();

    // --------------------------------------------------------
    // CORE INVENTORY LOGIC
    // --------------------------------------------------------

    public bool AddItem(ItemData itemToAdd, int amount = 1)
    {
        // 1. Check if we already have this item to stack it
        EnsureSlotCapacity();
        foreach (InventorySlot slot in inventorySlots)
        {
            if (slot.item != itemToAdd) continue;

            // If it's a Crafting Part, we can stack it. (Tools usually don't stack)
            if (itemToAdd.category == ItemCategory.CraftingPart)
            {
                slot.quantity += amount;
                Debug.Log($"Stacked {amount} {itemToAdd.itemName}(s). Total: {slot.quantity}");
                RaiseSlotsChanged();
                return true;
            }
        }

        for (int i = 0; i < inventorySlots.Count && i < maxSlots; i++)
        {
            if (inventorySlots[i].IsEmpty)
            {
                inventorySlots[i] = new InventorySlot(itemToAdd, amount);
                Debug.Log($"Added {itemToAdd.itemName} to slot {i}.");
                RaiseSlotsChanged();
                return true;
            }
        }

        Debug.Log("Inventory is full!");
        return false;
    }

    public bool HasItem(ItemData itemToCheck, int requiredAmount = 1)
    {
        if (itemToCheck == null) return false;
        return CountItem(itemToCheck) >= requiredAmount;
    }

    /// <summary>Total quantity across stacks for this item identity (references or matching name).</summary>
    public int CountItem(ItemData itemToCount)
    {
        if (itemToCount == null) return 0;
        EnsureSlotCapacity();
        int sum = 0;
        foreach (InventorySlot slot in inventorySlots)
        {
            if (slot.item == null || slot.quantity <= 0) continue;
            bool match = slot.item == itemToCount || slot.item.itemName == itemToCount.itemName;
            if (match) sum += slot.quantity;
        }
        return sum;
    }

    public bool RemoveItem(ItemData itemToRemove, int amountToRemove = 1)
    {
        if (itemToRemove == null) return false;

        for (int i = 0; i < inventorySlots.Count; i++)
        {
            if (inventorySlots[i].item == null) continue;
            bool match = inventorySlots[i].item == itemToRemove || inventorySlots[i].item.itemName == itemToRemove.itemName;
            if (!match) continue;

            if (inventorySlots[i].quantity >= amountToRemove)
            {
                inventorySlots[i].quantity -= amountToRemove;

                if (inventorySlots[i].quantity <= 0)
                {
                    inventorySlots[i].item = null;
                    inventorySlots[i].quantity = 0;
                }

                Debug.Log($"Removed {amountToRemove} {itemToRemove.itemName}(s).");
                RaiseSlotsChanged();
                return true;
            }
        }
        
        Debug.LogWarning("Tried to remove an item you don't have enough of!");
        return false;
    }

    // --------------------------------------------------------
    // EQUIPPED / SELECTED SLOT
    // --------------------------------------------------------

    public ItemData GetEquippedItem()
    {
        EnsureSlotCapacity();
        if (selectedSlotIndex < 0 || selectedSlotIndex >= maxSlots || selectedSlotIndex >= inventorySlots.Count)
            return null;
        InventorySlot slot = inventorySlots[selectedSlotIndex];
        return slot.item;
    }

    public bool IsEquipped(ItemData item)
    {
        ItemData equipped = GetEquippedItem();
        if (equipped == null || item == null) return false;
        return equipped == item || equipped.itemName == item.itemName;
    }

    // --------------------------------------------------------
    // TOOL LOOKUP
    // --------------------------------------------------------

    public ToolData GetToolInInventory(ToolData toolToFind)
    {
        if (toolToFind == null) return null;

        foreach (InventorySlot slot in inventorySlots)
        {
            if (slot.item == null) continue;

            bool match = slot.item == toolToFind
                || slot.item.itemName == toolToFind.itemName;

            if (match && slot.quantity > 0 && slot.item is ToolData foundTool)
                return foundTool;
        }
        return null;
    }

    // --------------------------------------------------------
    // RAYCAST HOVER LOGIC
    // --------------------------------------------------------

    public void SetHoverState(GameObject obj, ItemData data)
    {
        isHoveringOverItem = true;
        currentHoveredObject = obj;
        currentHoveredItemData = data;
        
        // TODO: Update your UI crosshair text here to say "Press E to pick up [data.itemName]"
    }

    public void ClearHoverState()
    {
        isHoveringOverItem = false;
        currentHoveredObject = null;
        currentHoveredItemData = null;
        
        // TODO: Clear your UI crosshair text here
    }
}