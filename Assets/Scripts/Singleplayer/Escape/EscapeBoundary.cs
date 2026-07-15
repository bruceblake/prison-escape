using UnityEngine;

/// <summary>
/// "Outside the walls" trigger ring around the facility — the player entering it has escaped,
/// regardless of which route they used.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class EscapeBoundary : MonoBehaviour
{
    private void Awake()
    {
        var box = GetComponent<BoxCollider>();
        if (box != null) box.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponentInParent<PrisonerController>();
        if (player == null) return;

        // Only the local human player wins; NPC prisoners crossing (unlikely) are ignored.
        if (player.GetComponent<PlayerController>() == null) return;

        EscapeManager.EnsureInstance().OnPlayerEscaped(player);
    }
}
