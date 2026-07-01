using UnityEngine;

public class InteractableScrew : MonoBehaviour, IInteractable
{
    [Header("Requirements")]
    public ToolData requiredTool;

    [Header("Vent Link")]
    public VentCover parentVent;

    [Header("Timing")]
    public float unscrewTime = 2f;

    [Header("Animation")]
    public float totalRotation = 1080f;
    public float moveOutDistance = 0.05f;
    [Tooltip("Axis the screw rotates around (e.g. forward for Phillips head)")]
    public Vector3 rotationAxis = Vector3.forward;
    [Tooltip("Axis the screw moves along when unscrewing. Use (0,0,1) for towards camera if screw faces you.")]
    public Vector3 moveOutAxis = Vector3.forward;

    private Vector3 startLocalPosition;
    private Quaternion startLocalRotation;
    private bool isBeingUnscrewed;

    public InteractionInputType InputType => InteractionInputType.Hold;
    public float HoldDuration => unscrewTime;

    void Awake()
    {
        startLocalPosition = transform.localPosition;
        startLocalRotation = transform.localRotation;
    }

    public string GetInteractionPrompt(PlayerInventory inventory)
    {
        if (!CanInteract(inventory))
            return requiredTool != null ? $"Requires {requiredTool.itemName} (equip in hotbar)" : "Requires tool";

        return "Hold LMB to unscrew";
    }

    public bool CanInteract(PlayerInventory inventory)
    {
        if (requiredTool == null) return true;
        return inventory.IsEquipped(requiredTool);
    }

    public void Interact(PlayerInventory inventory)
    {
        if (parentVent != null)
            parentVent.OnScrewRemoved(this);

        Debug.Log("[InteractableScrew] Screw removed.");
        Destroy(gameObject);
    }

    public void StartUnscrewing()
    {
        isBeingUnscrewed = true;
    }

    public void UpdateUnscrewing(float progress)
    {
        if (!isBeingUnscrewed) return;

        float clampedProgress = Mathf.Clamp01(progress);

        transform.localRotation = startLocalRotation * Quaternion.AngleAxis(clampedProgress * totalRotation, rotationAxis);
        transform.localPosition = startLocalPosition + startLocalRotation * moveOutAxis * (clampedProgress * moveOutDistance);
    }

    public void ResetUnscrewing()
    {
        isBeingUnscrewed = false;
        transform.localPosition = startLocalPosition;
        transform.localRotation = startLocalRotation;
    }
}