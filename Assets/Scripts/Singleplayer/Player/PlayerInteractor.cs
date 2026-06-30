using UnityEngine;
using TMPro;

public class PlayerInteractor : MonoBehaviour
{
    public static bool IsHoldInteracting { get; private set; }

    [Header("Interaction")]
    public float interactRange = 3f;
    public PlayerInventory inventory;

    [Header("UI")]
    public TMP_Text promptText;
    [Tooltip("If set, this rect follows the last raycast hit (Screen Space: optional). For World Space, parent under canvas + position z.")]
    public RectTransform worldPromptAnchor;
    [Header("World-space prompt (tactical)")]
    [Tooltip("Drives 3D/world text at the impact point; set promptText empty when on.")]
    public bool useWorldSpaceInteractionPrompt;
    [Tooltip("Root with world Canvas + TMP + optional CameraBillboard; positioned at each hit point.")]
    public Transform worldSpacePromptRoot;
    public TMP_Text worldSpacePromptText;
    public Vector3 worldSpacePromptOffset = new Vector3(0f, 0.2f, 0f);
    [Tooltip("Caution yellow (TMP color when world prompt active)")]
    public Color worldPromptColor = new Color(0.96f, 0.82f, 0.25f, 1f);
    public InventoryUI inventoryUI;
    public StolenNotebookUI stolenNotebook;
    [Tooltip("Shown when not looking at any interactable. Leave empty to show nothing.")]
    public string idlePrompt = "";

    [Tooltip("Shown when guard is escorting you to your cell")]
    public string arrestedPrompt = "Being escorted to your cell...";

    private IInteractable currentTarget;
    private float holdTimer;
    private float requiredHoldTime;
    private RaycastHit _lastHit;
    private bool _hadHit;

    public bool HasCurrentInteractable => currentTarget != null;

    /// <summary>0–1 when holding a hold-to-complete interaction (e.g. unscrew); else 0.</summary>
    public float HoldProgress01 { get; private set; }

    /// <summary>Non-null while the crosshair ray hits a <see cref="PillowStash"/>.</summary>
    public PillowStash CurrentPillowStash { get; private set; }

    void Start()
    {
        if (inventory == null)
            inventory = GetComponentInParent<PlayerInventory>();

        if (inventoryUI == null)
            inventoryUI = FindFirstObjectByType<InventoryUI>();

        if (stolenNotebook == null)
            stolenNotebook = FindFirstObjectByType<StolenNotebookUI>();

        if (inventoryUI != null && inventory != null)
            inventoryUI.SetInventory(inventory);

        if (stolenNotebook != null && inventory != null)
            stolenNotebook.SetInventory(inventory);

        var hotbar = FindFirstObjectByType<HotbarUI>();
        if (hotbar != null && inventory != null)
            hotbar.SetInventory(inventory);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && inventoryUI != null)
            inventoryUI.Toggle();

        if (stolenNotebook != null && stolenNotebook.IsOpen)
        {
            ClearTarget();
            SetPrompt("");
            return;
        }

        if (inventoryUI != null && inventoryUI.IsOpen)
        {
            ClearTarget();
            SetPrompt("");
            return;
        }

        HandleInteraction();
    }

    private void HandleInteraction()
    {
        var prisoner = GetComponentInParent<PrisonerController>();
        if (prisoner != null && prisoner.MovementBlocked)
        {
            HoldProgress01 = 0f;
            SetPrompt(arrestedPrompt);
            return;
        }

        Ray ray = new Ray(transform.position, transform.forward);

        if (!Physics.Raycast(ray, out RaycastHit hit, interactRange))
        {
            CurrentPillowStash = null;
            _hadHit = false;
            IsHoldInteracting = false;
            HoldProgress01 = 0f;
            ClearTarget();
            SetPrompt(string.IsNullOrEmpty(idlePrompt) ? "" : idlePrompt);
            return;
        }

        _lastHit = hit;
        _hadHit = true;

        IInteractable interactable = hit.collider.GetComponent<IInteractable>()
            ?? hit.collider.GetComponentInParent<IInteractable>();

        if (interactable == null)
        {
            CurrentPillowStash = null;
            IsHoldInteracting = false;
            HoldProgress01 = 0f;
            ClearTarget();
            SetPrompt(string.IsNullOrEmpty(idlePrompt) ? "" : idlePrompt);
            return;
        }

        CurrentPillowStash = interactable as PillowStash;

        string prompt = interactable.GetInteractionPrompt(inventory);
        if (interactable.InputType == InteractionInputType.Hold && holdTimer > 0f && requiredHoldTime > 0f)
        {
            int pct = Mathf.Clamp(Mathf.RoundToInt(holdTimer / requiredHoldTime * 100f), 0, 100);
            prompt += $" ({pct}%)";
        }
        SetPrompt(prompt);

        if (!ReferenceEquals(currentTarget, interactable))
        {
            ResetScrewAnimation();
            ClearTarget();
            currentTarget = interactable;
        }

        if (!interactable.CanInteract(inventory))
            return;

        if (interactable.InputType == InteractionInputType.Press)
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                interactable.Interact(inventory);
                ClearTarget();
                SetPrompt("");
            }
        }
        else if (interactable.InputType == InteractionInputType.Hold)
        {
            IsHoldInteracting = true;
            requiredHoldTime = interactable.HoldDuration;

            ToolData tool = GetHeldToolForInteractable(interactable);
            if (tool != null && tool.interactionSpeedModifier > 0f)
                requiredHoldTime /= tool.interactionSpeedModifier;

            HoldProgress01 = 0f;
            if (Input.GetMouseButton(0))
            {
                if (holdTimer == 0f && interactable is InteractableScrew startScrew)
                    startScrew.StartUnscrewing();

                holdTimer += Time.deltaTime;
                if (requiredHoldTime > 0.0001f) HoldProgress01 = Mathf.Clamp01(holdTimer / requiredHoldTime);

                if (interactable is InteractableScrew animScrew)
                    animScrew.UpdateUnscrewing(holdTimer / requiredHoldTime);

                if (holdTimer >= requiredHoldTime)
                {
                    interactable.Interact(inventory);
                    ClearTarget();
                    SetPrompt("");
                }
            }
            else
            {
                ResetScrewAnimation();
                holdTimer = 0f;
                HoldProgress01 = 0f;
            }
        }
        else
        {
            IsHoldInteracting = false;
            HoldProgress01 = 0f;
        }
    }

    private ToolData GetHeldToolForInteractable(IInteractable interactable)
    {
        if (interactable is InteractableScrew screw && screw.requiredTool != null && inventory.IsEquipped(screw.requiredTool))
            return inventory.GetEquippedItem() as ToolData;

        return null;
    }

    private void ClearTarget()
    {
        IsHoldInteracting = false;
        HoldProgress01 = 0f;
        ResetScrewAnimation();
        currentTarget = null;
        holdTimer = 0f;
        requiredHoldTime = 0f;
    }

    private void ResetScrewAnimation()
    {
        if (currentTarget is InteractableScrew screw && screw != null)
            screw.ResetUnscrewing();
    }

    private void SetPrompt(string text)
    {
        if (useWorldSpaceInteractionPrompt && worldSpacePromptRoot != null && worldSpacePromptText != null)
        {
            bool canPlaceWorld = _hadHit && !string.IsNullOrEmpty(text);
            worldSpacePromptRoot.gameObject.SetActive(canPlaceWorld);
            worldSpacePromptText.text = text;
            worldSpacePromptText.color = worldPromptColor;
            if (promptText != null) promptText.text = canPlaceWorld ? "" : text;
        }
        else
        {
            if (promptText != null) promptText.text = text;
            if (worldSpacePromptRoot != null) worldSpacePromptRoot.gameObject.SetActive(false);
        }
    }

    private void LateUpdate()
    {
        if (worldSpacePromptRoot != null && useWorldSpaceInteractionPrompt && _hadHit)
        {
            worldSpacePromptRoot.position = _lastHit.point + worldSpacePromptOffset;
        }
        if (worldPromptAnchor == null) return;
        if (!_hadHit) return;
        if (Camera.main == null) return;
        var cam = Camera.main;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            worldPromptAnchor.parent as RectTransform,
            cam.WorldToScreenPoint(_lastHit.point),
            cam, out var local))
        {
            worldPromptAnchor.anchoredPosition = local;
        }
    }
}