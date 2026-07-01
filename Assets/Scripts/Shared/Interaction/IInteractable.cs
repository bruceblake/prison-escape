public enum InteractionInputType
{
    Press,
    Hold
}

public interface IInteractable
{
    string GetInteractionPrompt(PlayerInventory inventory);
    bool CanInteract(PlayerInventory inventory);
    void Interact(PlayerInventory inventory);
    InteractionInputType InputType { get; }
    float HoldDuration { get; }
}
