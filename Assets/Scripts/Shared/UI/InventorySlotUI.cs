using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class InventorySlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public Image iconImage;
    public TMP_Text quantityText;
    public TMP_Text nameText;
    public Image backgroundImage;
    public Image selectionHighlight;

    [Header("New Features")]
    public Slider durabilityBar;
    public Outline contrabandOutline;

    [Header("Slot wiring")]
    public int slotIndex { get; private set; }
    public PlayerInventory inventoryRef { get; private set; }
    [Tooltip("When on (inventory bag only), dragging swaps slot indices.")]
    public bool dragSwapEnabled;
    public bool HasItemForSwap => _hasContent;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (inventoryRef != null)
        {
            inventoryRef.selectedSlotIndex = slotIndex;
            // The RefreshSlots call in UI will update the highlight
        }
    }

    [Header("Empty-slot look")]
    [Tooltip("Background when slot has no item (avoid blazing white placeholders).")]
    public Color emptySlotColor = new Color(0.12f, 0.135f, 0.156f, 0.94f);

    [Header("Illegal / contraband (optional)")]
    [Tooltip("Shaded when the slot has contraband / weapon / tool")]
    public Image illegalWarningGlow;

    [Header("Typography")]
    [Tooltip("Keeps name/quantity labels inside each slot.")]
    public bool constrainLabelsToSlot = true;
    [Range(10f, 24f)] public float maxLabelFontSize = 13f;
    [Tooltip("Text on occupied slots reads against tinted backgrounds.")]
    public Color occupiedNameColor = new Color(0.93f, 0.955f, 0.975f);
    public Color occupiedQuantityColor = new Color(0.72f, 0.8f, 0.92f);

    static readonly Color SlotWellFallback = new Color(0.19f, 0.208f, 0.246f, 0.94f);

    [Header("Drag preview")]
    public float dragGhostPixelSize = 76f;
    [Range(0.35f, 1f)] public float dragGhostAlpha = 0.92f;
    [Tooltip("Draw above other UI")]
    public int dragGhostSortingOffset = 200;

    private Color defaultBackgroundColor;
    private CanvasGroup _canvasGroup;
    private static readonly Color IllegalTint = new Color(0.55f, 0.1f, 0.1f, 0.5f);
    private bool hasInit;
    private bool _hasContent;
    private ItemData _itemForTooltip;

    private RectTransform _dragGhostRoot;
    private Canvas _ghostCanvasParent;
    private static InventorySlotUI _activeDragOrigin;

    public void Configure(int index, PlayerInventory inventory, bool dragSwapEnabled)
    {
        slotIndex = index;
        inventoryRef = inventory;
        this.dragSwapEnabled = dragSwapEnabled;
        if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void Init()
    {
        if (hasInit) return;
        hasInit = true;

        FindChildren();

        if (backgroundImage != null)
        {
            Color c = backgroundImage.color;
            float lum = c.r * 0.299f + c.g * 0.587f + c.b * 0.114f;
            defaultBackgroundColor = lum > 0.82f || (c.maxColorComponent >= 0.92f && c.a > 0.5f)
                ? SlotWellFallback
                : c;
        }

        ReapplyRaycastTargets();
        ApplySlotTypographyConstraints();
    }

    private void ApplySlotTypographyConstraints()
    {
        if (!constrainLabelsToSlot) return;

        if (nameText != null)
        {
            nameText.enableWordWrapping = false;
            nameText.overflowMode = TextOverflowModes.Ellipsis;
            nameText.fontSize = Mathf.Min(nameText.fontSize, maxLabelFontSize);
            nameText.margin = Vector4.zero;
        }

        if (quantityText != null)
        {
            quantityText.enableWordWrapping = false;
            quantityText.overflowMode = TextOverflowModes.Overflow;
            quantityText.fontSize = Mathf.Min(quantityText.fontSize, Mathf.Max(10f, maxLabelFontSize - 2f));
        }
    }

    private void FindChildren()
    {
        Image[] images = GetComponentsInChildren<Image>(true);
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);

        foreach (Image img in images)
        {
            if (img.gameObject == gameObject) continue;
            string n = img.gameObject.name.ToLower();

            if (backgroundImage == null && n.Contains("background"))
                backgroundImage = img;
            else if (iconImage == null && (n.Contains("icon") || n.Contains("item")))
                iconImage = img;
            else if (selectionHighlight == null && (n.Contains("selection") || n.Contains("highlight") || n.Contains("border")))
                selectionHighlight = img;
            else if (illegalWarningGlow == null && (n.Contains("illegal") || n.Contains("contraband")))
                illegalWarningGlow = img;
        }

        foreach (TMP_Text txt in texts)
        {
            string n = txt.gameObject.name.ToLower();

            if (quantityText == null && (n.Contains("quantity") || n.Contains("amount") || n.Contains("count")))
                quantityText = txt;
            else if (nameText == null && (n.Contains("name") || n.Contains("label") || n.Contains("title")))
                nameText = txt;
        }

        if (images.Length >= 2 && (backgroundImage == null || iconImage == null))
        {
            foreach (Image img in images)
            {
                if (img.gameObject == gameObject) continue;
                if (backgroundImage == null) { backgroundImage = img; continue; }
                if (iconImage == null) { iconImage = img; break; }
            }
        }

        if (texts.Length >= 2 && (quantityText == null || nameText == null))
        {
            foreach (TMP_Text txt in texts)
            {
                if (quantityText == null) { quantityText = txt; continue; }
                if (nameText == null) { nameText = txt; break; }
            }
        }
        else if (texts.Length == 1 && nameText == null)
        {
            nameText = texts[0];
        }
    }

    public bool showName = true;
    public bool showQuantity = true;

    public void SetSlot(InventorySlot slot)
    {
        Init();

        if (slot == null || slot.item == null)
        {
            ClearSlot();
            return;
        }

        _hasContent = true;
        _itemForTooltip = slot.item;

        UpdateVisualState(slot.item);

        if (iconImage != null)
        {
            iconImage.enabled = true;
            iconImage.sprite = slot.item.icon;
        }

        if (quantityText != null)
        {
            quantityText.enabled = showQuantity;
            quantityText.text = $"x{slot.quantity}";
            quantityText.color = occupiedQuantityColor;
        }

        if (nameText != null)
        {
            nameText.enabled = showName;
            nameText.text = slot.item.itemName;
            nameText.overflowMode = TextOverflowModes.Ellipsis;
            nameText.enableWordWrapping = false;
            nameText.color = occupiedNameColor;
        }

        if (backgroundImage != null)
        {
            bool illegal = IsIllegalCategory(slot.item.category);
            backgroundImage.color = illegal
                ? Color.Lerp(defaultBackgroundColor, IllegalTint, 0.45f)
                : defaultBackgroundColor;
            backgroundImage.raycastTarget = true;
        }
        if (illegalWarningGlow != null) illegalWarningGlow.enabled = IsIllegalCategory(slot.item.category);
    }

    public void UpdateVisualState(ItemData data)
    {
        if (contrabandOutline != null)
        {
            // Only show red outline for actual contraband, not tools or weapons
            contrabandOutline.enabled = data.category == ItemCategory.Contraband;
            contrabandOutline.effectColor = Color.red;
        }

        if (durabilityBar != null)
{
            durabilityBar.gameObject.SetActive(false);
        }
    }

    public void ClearSlot()
    {
        Init();

        _hasContent = false;
        _itemForTooltip = null;

        if (contrabandOutline != null) contrabandOutline.enabled = false;
        if (durabilityBar != null) durabilityBar.gameObject.SetActive(false);

        if (iconImage != null)
        {
            iconImage.enabled = false;
            iconImage.sprite = null;
        }

        if (quantityText != null)
        {
            quantityText.text = "";
        }

        if (nameText != null)
        {
            nameText.text = "";
        }

        if (backgroundImage != null)
        {
            // Reset to default dark grey for empty look
            backgroundImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            backgroundImage.raycastTarget = true;
        }
        if (illegalWarningGlow != null) illegalWarningGlow.enabled = false;
    }

    private void OnDestroy()
    {
        KillDragGhost();
        if (_activeDragOrigin == this) _activeDragOrigin = null;
        HideTooltipFromThisHover();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Init();
        if (!_hasContent || _itemForTooltip == null) return;
        if (ItemTooltipUI.Instance != null)
            ItemTooltipUI.Instance.Show(_itemForTooltip);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideTooltipFromThisHover();
    }

    private void HideTooltipFromThisHover()
    {
        if (ItemTooltipUI.Instance != null)
            ItemTooltipUI.Instance.Hide();
    }

    public void SetSelected(bool selected)
    {
        Init();
        if (selectionHighlight != null)
            selectionHighlight.enabled = selected;
    }

    private static bool IsIllegalCategory(ItemCategory c)
    {
        return c == ItemCategory.Weapon
            || c == ItemCategory.Tool
            || c == ItemCategory.Contraband;
    }

    private void ReapplyRaycastTargets()
    {
        if (backgroundImage != null) backgroundImage.raycastTarget = true;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        HideTooltipFromThisHover();
        if (!dragSwapEnabled || !_hasContent || inventoryRef == null || _canvasGroup == null) return;
        if (_activeDragOrigin != null && _activeDragOrigin != this)
            _activeDragOrigin.KillDragGhost();
        _canvasGroup.alpha = 0.55f;
        _canvasGroup.blocksRaycasts = false;
        SpawnDragGhost(eventData);
        _activeDragOrigin = this;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_dragGhostRoot == null) return;
        UpdateDragGhostScreenPosition(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        KillDragGhost();
        if (_activeDragOrigin == this) _activeDragOrigin = null;
        if (_canvasGroup == null) return;
        _canvasGroup.alpha = 1f;
        _canvasGroup.blocksRaycasts = true;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (!dragSwapEnabled || inventoryRef == null) return;
        var src = eventData.pointerDrag ? eventData.pointerDrag.GetComponent<InventorySlotUI>() : null;
        if (src == null || !src.dragSwapEnabled || !src.HasItemForSwap) return;
        if (src.slotIndex == slotIndex) return;
        inventoryRef.SwapSlots(src.slotIndex, slotIndex);
    }

    /// <summary>Sprite preview under same canvas; extra Canvas boosts draw order.</summary>
    private void SpawnDragGhost(PointerEventData eventData)
    {
        KillDragGhost();
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;
        _ghostCanvasParent = canvas;

        var go = new GameObject("InventoryDragGhost");
        RectTransform rt = go.AddComponent<RectTransform>();
        CanvasGroup cg = go.AddComponent<CanvasGroup>();
        cg.alpha = Mathf.Clamp01(dragGhostAlpha);
        cg.blocksRaycasts = false;
        cg.interactable = false;

        Canvas overlay = go.AddComponent<Canvas>();
        overlay.overrideSorting = true;
        overlay.sortingOrder = canvas.sortingOrder + Mathf.Clamp(dragGhostSortingOffset, 0, 32767);

        Image img = go.AddComponent<Image>();
        img.raycastTarget = false;
        img.preserveAspect = true;

        _dragGhostRoot = rt;
        rt.SetParent(canvas.transform, false);
        rt.SetAsLastSibling();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(dragGhostPixelSize, dragGhostPixelSize);

        if (iconImage != null && iconImage.sprite != null)
        {
            img.sprite = iconImage.sprite;
            img.color = Color.white;
        }
        else
        {
            img.sprite = null;
            img.color = new Color(0.45f, 0.47f, 0.52f, 0.85f);
        }

        UpdateDragGhostScreenPosition(eventData);
    }

    private void UpdateDragGhostScreenPosition(PointerEventData eventData)
    {
        if (_dragGhostRoot == null || _ghostCanvasParent == null) return;
        RectTransform canvasRect = _ghostCanvasParent.transform as RectTransform;
        if (canvasRect == null) return;
        Camera cam = _ghostCanvasParent.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : _ghostCanvasParent.worldCamera != null ? _ghostCanvasParent.worldCamera : eventData.pressEventCamera;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, eventData.position, cam, out Vector2 localPoint))
            _dragGhostRoot.anchoredPosition = localPoint;
    }

    public void KillDragGhost()
    {
        if (_dragGhostRoot != null)
        {
            Destroy(_dragGhostRoot.gameObject);
            _dragGhostRoot = null;
        }
        _ghostCanvasParent = null;
    }
}
