using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using Prison;

public class HotbarUI : MonoBehaviour
{
    [Header("References")]
    public PlayerInventory inventory;
    public Transform slotContainer;
    public GameObject slotPrefab;
    [Header("Visibility")]
    [Tooltip("Keeps slots visible full-time; skips auto-hide and ignores Start Hidden.")]
    public bool alwaysVisible = true;
    [Tooltip("If set, hotbar alpha fades; otherwise slotContainer.gameObject is toggled.")]
    public CanvasGroupFader fader;
    [Tooltip("When Always Visible is off and this is on, the bar is hidden until scroll or 1–6.")]
    public bool startHidden = false;
    public bool autoHideWhenHiddenMode = true;
    [Min(0.05f)] public float showSeconds = 0.2f;
    [Min(0.1f)] public float autoHideAfterSeconds = 1.5f;
    [Tooltip("Lift hotbar off the bottom screen edge (pixels at 1080p reference).")]
    public float bottomMargin = 24f;
    [Tooltip("Mouse wheel: cycle selection (and shows bar if hidden)")]
    public bool useScrollWheel = true;

    public int hotbarSlotCount = 6;

    [Header("Layout")]
    [Tooltip("Hotbar slot size in canvas pixels (applied to GridLayoutGroup when present).")]
    public Vector2 slotSize = new Vector2(56f, 56f);
    public Vector2 slotSpacing = new Vector2(4f, 4f);

    private List<InventorySlotUI> slotUIs = new List<InventorySlotUI>();
    private bool isBuilt;
    private float _hideAfterUnscaled = float.NegativeInfinity;
    private bool _visible = true;

    void OnEnable()
    {
        if (inventory != null)
            inventory.SlotsChanged += OnInventorySlotsChanged;
    }

    void OnDisable()
    {
        if (inventory != null)
            inventory.SlotsChanged -= OnInventorySlotsChanged;
    }

    private void OnInventorySlotsChanged() => RefreshSlots();

    void Start()
    {
        if (inventory != null)
            BuildSlots();

        if (fader == null && slotContainer != null)
        {
            fader = slotContainer.GetComponent<CanvasGroupFader>();
            if (fader == null) fader = slotContainer.gameObject.AddComponent<CanvasGroupFader>();
            if (slotContainer.GetComponent<CanvasGroup>() == null) slotContainer.gameObject.AddComponent<CanvasGroup>();
        }

        if (alwaysVisible)
            SetBarVisible(true, false);
        else if (startHidden)
            SetBarVisible(false, false);
        else if (fader != null)
            fader.SetImmediate(true, true);

        ApplyBottomMargin();
        ApplySlotLayout();
    }

    private void ApplySlotLayout()
    {
        if (slotContainer == null) return;
        var grid = slotContainer.GetComponent<GridLayoutGroup>();
        if (grid == null) return;
        grid.cellSize = slotSize;
        grid.spacing = slotSpacing;

        var rt = slotContainer as RectTransform;
        if (rt == null) return;
        float width = slotSize.x * hotbarSlotCount + slotSpacing.x * (hotbarSlotCount - 1)
            + grid.padding.left + grid.padding.right;
        float height = slotSize.y + grid.padding.top + grid.padding.bottom;
        rt.sizeDelta = new Vector2(width, height);
    }

    private void ApplyBottomMargin()
    {
        if (slotContainer == null) return;
        var rt = slotContainer as RectTransform;
        if (rt == null) return;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, bottomMargin);
    }

    void Update()
    {
        if (inventory == null) return;

        // Safety check: Hide if inventory is open
        InventoryUI[] allInvs = GameObject.FindObjectsByType<InventoryUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        bool shouldHideForInventory = false;
        foreach(var inv in allInvs) {
            if (inv.IsOpen && inv.hideHotbarWhileInventoryOpen) {
                shouldHideForInventory = true;
                break; 
            }
        }
        
        if (shouldHideForInventory)
        {
            if (slotContainer != null && slotContainer.gameObject.activeSelf) 
                slotContainer.gameObject.SetActive(false);
            return;
        }

        // Ensure it's active if not hiding for inventory
        if (slotContainer != null && !slotContainer.gameObject.activeSelf)
        {
            if (alwaysVisible || (Time.unscaledTime <= _hideAfterUnscaled))
            {
                slotContainer.gameObject.SetActive(true);
            }
        }

        if (!isBuilt)
            BuildSlots();

        inventory.EnsureSlotCapacity();

        bool selectionInput = HandleSelectionInput();
        if (selectionInput) PingShow();
        if (useScrollWheel && (Input.mouseScrollDelta.y > 0.01f || Input.mouseScrollDelta.y < -0.01f))
        {
            int dir = Input.mouseScrollDelta.y > 0f ? 1 : -1;
            inventory.selectedSlotIndex = (inventory.selectedSlotIndex + dir + hotbarSlotCount) % hotbarSlotCount;
            PingShow();
        }

        RefreshSlots();

        if (alwaysVisible && slotContainer != null)
        {
            var cg = slotContainer.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                float target = UIMenuFocus.IsAnyMenuOpen ? 0.04f : 1f;
                cg.alpha = Mathf.MoveTowards(cg.alpha, target, Time.unscaledDeltaTime * 10f);
            }
        }

        if (!alwaysVisible && startHidden && autoHideWhenHiddenMode && _visible && fader != null && Time.unscaledTime > _hideAfterUnscaled)
            SetBarVisible(false, true);
    }

    private void PingShow()
    {
        if (alwaysVisible || !startHidden) return;
        _visible = true;
        if (fader != null) fader.FadeTo(true, true, showSeconds);
        _hideAfterUnscaled = Time.unscaledTime + autoHideAfterSeconds;
    }

    private void SetBarVisible(bool on, bool useFade)
    {
        _visible = on;
        if (fader != null)
        {
            if (useFade) fader.FadeTo(on, on, showSeconds);
            else fader.SetImmediate(on, on);
        }
        else if (slotContainer != null)
            slotContainer.gameObject.SetActive(on);
    }

    public void SetInventory(PlayerInventory playerInventory)
    {
        if (inventory != null)
            inventory.SlotsChanged -= OnInventorySlotsChanged;
        inventory = playerInventory;
        if (inventory != null)
            inventory.SlotsChanged += OnInventorySlotsChanged;
        isBuilt = false;
        BuildSlots();
    }

    private bool HandleSelectionInput()
    {
        bool any = false;
        if (Input.GetKeyDown(KeyCode.Alpha1)) { inventory.selectedSlotIndex = 0; any = true; }
        else if (Input.GetKeyDown(KeyCode.Alpha2)) { inventory.selectedSlotIndex = 1; any = true; }
        else if (Input.GetKeyDown(KeyCode.Alpha3)) { inventory.selectedSlotIndex = 2; any = true; }
        else if (Input.GetKeyDown(KeyCode.Alpha4)) { inventory.selectedSlotIndex = 3; any = true; }
        else if (Input.GetKeyDown(KeyCode.Alpha5)) { inventory.selectedSlotIndex = 4; any = true; }
        else if (Input.GetKeyDown(KeyCode.Alpha6)) { inventory.selectedSlotIndex = 5; any = true; }

        inventory.selectedSlotIndex = Mathf.Clamp(inventory.selectedSlotIndex, 0, hotbarSlotCount - 1);
        return any;
    }

    private void BuildSlots()
    {
        if (isBuilt || inventory == null || slotContainer == null || slotPrefab == null) return;
        isBuilt = true;

        foreach (var existing in slotUIs)
        {
            if (existing != null) Destroy(existing.gameObject);
        }
        slotUIs.Clear();

        for (int i = 0; i < hotbarSlotCount; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotContainer);
            InventorySlotUI slotUI = slotObj.GetComponent<InventorySlotUI>();
            slotUI.Configure(i, inventory, dragSwapEnabled: false);
            slotUI.showName = false;
            slotUI.ClearSlot();
            AddKeyHint(slotObj.transform, (i + 1).ToString());
            slotUIs.Add(slotUI);
        }
    }

    private static void AddKeyHint(Transform slotRoot, string key)
    {
        var hintGo = new GameObject("KeyHint", typeof(RectTransform));
        hintGo.transform.SetParent(slotRoot, false);
        var rt = (RectTransform)hintGo.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(4f, 4f);
        rt.sizeDelta = new Vector2(18f, 16f);
        var tmp = hintGo.AddComponent<TextMeshProUGUI>();
        tmp.text = key;
        tmp.fontSize = 13f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = new Color(0.65f, 0.68f, 0.72f, 0.9f);
        tmp.alignment = TextAlignmentOptions.BottomLeft;
        tmp.raycastTarget = false;
    }

    private void RefreshSlots()
    {
        inventory.EnsureSlotCapacity();
        for (int i = 0; i < slotUIs.Count; i++)
        {
            if (i < inventory.inventorySlots.Count)
            {
                InventorySlot slot = inventory.inventorySlots[i];
                if (slot != null && !slot.IsEmpty)
                    slotUIs[i].SetSlot(slot);
                else
                    slotUIs[i].ClearSlot();
            }
            else
                slotUIs[i].ClearSlot();

            slotUIs[i].SetSelected(i == inventory.selectedSlotIndex);
        }
    }
}
