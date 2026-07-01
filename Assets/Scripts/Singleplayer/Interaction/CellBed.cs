using UnityEngine;

/// <summary>
/// Player bed: place Fake Bed Dummy when the correct item is equipped (hotbar selection).
/// </summary>
public class CellBed : MonoBehaviour, IInteractable
{
    [Tooltip("Cell index for bed presence / dummy (0-based, same as PrisonerController.cellIndex).")]
    public int cellIndex;

    [Tooltip("Item the player must have equipped to place the dummy (e.g. crafted Fake Bed Dummy tool).")]
    public ItemData fakeBedDummyItem;

    [Tooltip("Where the dummy prefab is spawned (defaults to this transform).")]
    public Transform dummySpawnPoint;

    [Tooltip("Prefab root must include FakeBedDummy + colliders for guard overlap checks.")]
    public GameObject fakeBedDummyPrefab;

    public InteractionInputType InputType => InteractionInputType.Press;
    public float HoldDuration => 0f;

    private Transform SpawnTransform => dummySpawnPoint != null ? dummySpawnPoint : transform;

    public string GetInteractionPrompt(PlayerInventory inventory)
    {
        if (fakeBedDummyItem == null || fakeBedDummyPrefab == null)
            return "";
        if (inventory != null && inventory.IsEquipped(fakeBedDummyItem))
            return "Press F to place fake bed dummy";
        if (inventory != null && inventory.HasItem(fakeBedDummyItem, 1))
            return $"Select {fakeBedDummyItem.itemName} in hotbar to place dummy";
        return "Need fake bed dummy item";
    }

    public bool CanInteract(PlayerInventory inventory)
    {
        if (inventory == null || fakeBedDummyItem == null || fakeBedDummyPrefab == null)
            return false;
        return inventory.IsEquipped(fakeBedDummyItem) && inventory.HasItem(fakeBedDummyItem, 1);
    }

    public void Interact(PlayerInventory inventory)
    {
        if (!CanInteract(inventory))
            return;

        if (!inventory.RemoveItem(fakeBedDummyItem, 1))
            return;

        var t = SpawnTransform;
        var go = Instantiate(fakeBedDummyPrefab, t.position, t.rotation, t);
        var dummy = go.GetComponent<FakeBedDummy>();
        if (dummy == null)
            dummy = go.AddComponent<FakeBedDummy>();
        dummy.cellIndex = cellIndex;
    }
}
