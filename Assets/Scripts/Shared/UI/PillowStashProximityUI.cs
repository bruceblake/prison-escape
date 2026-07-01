using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>One-slot stash readout: only relevant when the player uses the bed pillow.</summary>
public class PillowStashProximityUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private PlayerInteractor interactor;

    private void Reset()
    {
        interactor = FindFirstObjectByType<PlayerInteractor>();
    }

    private void Update()
    {
        if (interactor == null) interactor = FindFirstObjectByType<PlayerInteractor>();
        if (panel == null) return;

        var stash = interactor != null ? interactor.CurrentPillowStash : null;
        bool show = stash != null;
        panel.SetActive(show);
        if (!show) return;

        var stored = stash.StoredItem;
        if (itemIcon != null)
        {
            if (stored != null && stored.icon != null)
            {
                itemIcon.enabled = true;
                itemIcon.sprite = stored.icon;
            }
            else
            {
                itemIcon.enabled = false;
            }
        }

        if (hintText != null)
        {
            if (stored != null)
                hintText.text = stored.itemName;
            else
                hintText.text = "Empty — equip an item to hide it";
        }
    }
}
