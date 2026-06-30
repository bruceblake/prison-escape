using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One HUD tooltip for <see cref="ItemData"/> hover.
/// Assign <see cref="panelRoot"/> to the GameObject that has the dark backing <see cref="Image"/> and contains the TMP children (so text is not drawn on the world behind it).
/// </summary>
public class ItemTooltipUI : MonoBehaviour
{
    public static ItemTooltipUI Instance { get; private set; }

    [Header("Refs")]
    public GameObject panelRoot;
    public RectTransform tooltipRect;
    public TMP_Text titleText;
    public TMP_Text descriptionText;
    public TMP_Text statsText;
    public Canvas parentCanvas;
    [Tooltip("If null, uses Image on panelRoot.")]
    public Image backdropImage;

    [Header("Placement")]
    [Tooltip("Pushed down/right from pointer; tooltip uses top-left pivot so it hangs below typical hotbars.")]
    public Vector2 screenOffset = new Vector2(22f, -48f);

    [Header("Readable defaults")]
    [Tooltip("Once in Awake: dark panel, border outline, text colors (title tint still follows rarity when shown).")]
    public bool applyReadableDefaults = true;
    [Tooltip("Preferred minimum width — description/stats wrap inside this.")]
    public float minTooltipWidth = 372f;

    static readonly Color StatsLabelColor = new Color(0.62f, 0.67f, 0.74f);
    static readonly Color StatsValueColor = new Color(0.82f, 0.86f, 0.92f);

    [Header("Floating layer")]
    [Tooltip("Parents tooltip Rect to the HUD canvas root and pins anchors — fixes cursor follow when tooltip lived under Layout Groups.")]
    public bool normalizeForCanvasFloating = true;
    [Tooltip("Draw above fullscreen dim overlays & other HUD.")]
    public int overlaySortOrder = 800;

    private bool _visible;
    private bool _appliedDefaults;
    private bool _floatLayoutApplied;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (parentCanvas == null) parentCanvas = GetComponentInParent<Canvas>();
        if (backdropImage == null && panelRoot != null)
            backdropImage = panelRoot.GetComponent<Image>();
        if (backdropImage == null && panelRoot != null && applyReadableDefaults)
            backdropImage = CreateFallbackBackdrop(panelRoot);

        if (applyReadableDefaults && !_appliedDefaults)
            ApplyReadableDefaults();
        if (tooltipRect == null && backdropImage != null)
            tooltipRect = backdropImage.rectTransform;
        EnsureTmpUnderPanelAndOrder();
        EnsureFloatingPresentation();
        if (panelRoot != null) panelRoot.SetActive(false);
        _visible = false;
    }

    /// <summary>Dark plate + neutral type colors so white TMP is never left floating on the 3D view.</summary>
    private void ApplyReadableDefaults()
    {
        _appliedDefaults = true;
        if (backdropImage != null)
        {
            backdropImage.color = new Color(0.045f, 0.05f, 0.065f, 0.98f);
            if (backdropImage.GetComponent<Outline>() == null)
            {
                Outline o = backdropImage.gameObject.AddComponent<Outline>();
                o.effectColor = new Color(0.32f, 0.52f, 0.82f, 0.42f);
                o.effectDistance = new Vector2(2f, -2f);
            }
            if (backdropImage.gameObject.GetComponent<Shadow>() == null)
            {
                Shadow sh = backdropImage.gameObject.AddComponent<Shadow>();
                sh.effectColor = new Color(0f, 0f, 0f, 0.55f);
                sh.effectDistance = new Vector2(8f, -8f);
            }
        }

        LayoutElement le = panelRoot != null ? panelRoot.GetComponent<LayoutElement>() : null;
        if (le == null && panelRoot != null) le = panelRoot.AddComponent<LayoutElement>();
        if (le != null)
        {
            le.minWidth = minTooltipWidth;
            le.preferredWidth = Mathf.Max(le.preferredWidth, minTooltipWidth);
        }

        RectTransform panelRt = panelRoot != null ? panelRoot.transform as RectTransform : null;
        if (panelRt != null)
            EnsurePanelLayout(panelRt);

        ApplyTooltipLayoutHintsToTexts();

        EnsureFloatingPresentation();
    }

    private void ApplyTooltipLayoutHintsToTexts()
    {
        float pw = Mathf.Max(minTooltipWidth, 280f);
        foreach (TMP_Text t in new[] { titleText, descriptionText, statsText })
        {
            if (t == null) continue;
            LayoutElement le = t.gameObject.GetComponent<LayoutElement>();
            if (le == null) le = t.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;
            le.preferredWidth = pw;
            le.minWidth = pw * 0.92f;
            t.raycastTarget = false;
        }

        if (titleText != null)
        {
            titleText.color = new Color(0.96f, 0.96f, 0.98f);
            titleText.fontSize = Mathf.Max(titleText.fontSize, 22f);
            titleText.enableWordWrapping = true;
            titleText.overflowMode = TextOverflowModes.Overflow;
            titleText.margin = new Vector4(0, 0, 6, 4);
            titleText.characterSpacing = 0.4f;
        }
        if (descriptionText != null)
        {
            descriptionText.color = new Color(0.74f, 0.77f, 0.84f);
            descriptionText.fontSize = Mathf.Max(descriptionText.fontSize, 15f);
            descriptionText.lineSpacing = 6f;
            descriptionText.margin = Vector4.zero;
            descriptionText.richText = true;
            descriptionText.enableWordWrapping = true;
        }
        if (statsText != null)
        {
            statsText.color = StatsValueColor;
            statsText.fontSize = Mathf.Max(statsText.fontSize, 14f);
            statsText.margin = Vector4.zero;
            statsText.richText = true;
            statsText.enableWordWrapping = true;
            statsText.lineSpacing = 10f;
        }
    }

    private void EnsureTmpUnderPanelAndOrder()
    {
        if (tooltipRect == null) return;
        int i = 0;
        SafeReparentTMP(titleText, ref i);
        SafeReparentTMP(descriptionText, ref i);
        SafeReparentTMP(statsText, ref i);
    }

    private void SafeReparentTMP(TMP_Text t, ref int siblingIndex)
    {
        if (t == null) return;
        if (t.rectTransform.parent != tooltipRect.transform)
            t.rectTransform.SetParent(tooltipRect.transform, false);
        int hi = Mathf.Max(0, tooltipRect.transform.childCount - 1);
        int idx = Mathf.Clamp(siblingIndex, 0, hi);
        t.rectTransform.SetSiblingIndex(idx);
        siblingIndex = idx + 1;
    }

    private void EnsureFloatingPresentation()
    {
        if (!normalizeForCanvasFloating || tooltipRect == null || parentCanvas == null) return;

        if (backdropImage != null)
            backdropImage.raycastTarget = false;

        if (panelRoot != null)
        {
            Canvas overlay = panelRoot.GetComponent<Canvas>();
            if (overlay == null) overlay = panelRoot.AddComponent<Canvas>();
            overlay.overrideSorting = true;
            overlay.sortingOrder = overlaySortOrder;
            overlay.additionalShaderChannels = parentCanvas.additionalShaderChannels;
        }

        RectTransform canvasRt = parentCanvas.transform as RectTransform;
        if (canvasRt == null) return;

        if (!_floatLayoutApplied || tooltipRect.parent != canvasRt)
        {
            tooltipRect.SetParent(canvasRt, false);
            tooltipRect.anchorMin = tooltipRect.anchorMax = new Vector2(0.5f, 0.5f);
            tooltipRect.pivot = new Vector2(0f, 1f);
            tooltipRect.localRotation = Quaternion.identity;
            tooltipRect.localScale = Vector3.one;
            tooltipRect.SetAsLastSibling();
            _floatLayoutApplied = true;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void LateUpdate()
    {
        if (!_visible || tooltipRect == null || parentCanvas == null) return;

        Vector2 pointer = (Vector2)Input.mousePosition + screenOffset;
        RectTransform canvasRt = parentCanvas.transform as RectTransform;
        Camera cam = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : parentCanvas.worldCamera;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, pointer, cam, out Vector2 canvasLocal))
            return;

        const float pad = 14f;
        Rect bounds = canvasRt.rect;
        float halfW = bounds.width * 0.5f - pad;
        float halfH = bounds.height * 0.5f - pad;
        float tipW = Mathf.Max(tooltipRect.rect.width, 1f);
        float tipH = Mathf.Max(tooltipRect.rect.height, 1f);

        canvasLocal.x = Mathf.Clamp(canvasLocal.x, -halfW + pad, halfW - pad - tipW);
        canvasLocal.y = Mathf.Clamp(canvasLocal.y, -halfH + pad + tipH, halfH - pad);

        tooltipRect.anchoredPosition = canvasLocal;
    }

    public void Show(ItemData item)
    {
        if (item == null || panelRoot == null) return;
        EnsureFloatingPresentation();
        EnsureTmpUnderPanelAndOrder();
        ApplyTooltipLayoutHintsToTexts();

        _visible = true;
        panelRoot.SetActive(true);

        if (titleText != null)
        {
            titleText.text = item.itemName;
            titleText.fontStyle = FontStyles.Bold;
        }

        if (descriptionText != null)
        {
            string d = string.IsNullOrEmpty(item.description) ? string.Empty : item.description.Trim();
            descriptionText.text = string.IsNullOrEmpty(d) ? "<i>No description.</i>" : CapitalizeSentence(d);
        }

        if (statsText != null)
            statsText.text = BuildStatsRich(item);

        if (titleText != null)
            titleText.color = RarityTitleColor(item.rarity);

        RebuildTooltipLayout();
        tooltipRect.SetAsLastSibling();

        WarnIfTmpDetachedFromPanel();
    }

    private void WarnIfTmpDetachedFromPanel()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (tooltipRect == null) return;
        if (titleText != null && !titleText.rectTransform.IsChildOf(tooltipRect.transform))
            Debug.LogWarning($"[ItemTooltipUI] `{titleText.name}` must be nested under `{tooltipRect.name}` so the description follows the tooltip panel.", this);
#endif
    }

    private void RebuildTooltipLayout()
    {
        if (tooltipRect == null) return;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);
        if (tooltipRect.parent is RectTransform prt)
            LayoutRebuilder.ForceRebuildLayoutImmediate(prt);
    }

    private static Color RarityTitleColor(ItemRarity r)
    {
        switch (r)
        {
            case ItemRarity.Uncommon: return new Color(0.55f, 0.95f, 0.65f);
            case ItemRarity.Rare: return new Color(0.55f, 0.78f, 1f);
            case ItemRarity.Legendary: return new Color(1f, 0.78f, 0.35f);
            default: return new Color(0.96f, 0.96f, 0.98f);
        }
    }

    public void Hide()
    {
        _visible = false;
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private static string TmpColorOpen(Color32 c) => $"<color=#{ColorUtility.ToHtmlStringRGBA(c)}>";

    private static string BuildStatsRich(ItemData item)
    {
        Color32 lc32 = StatsLabelColor;
        Color32 vc32 = StatsValueColor;

        var sb = new StringBuilder();
        sb.Append($"{TmpColorOpen(lc32)}<size=107%><b>DETAILS</b></size></color>\n\n");
        AppendStatLine(sb, lc32, vc32, "Category", $"{item.category}");
        AppendStatLine(sb, lc32, vc32, "Rarity", $"{item.rarity}");
        if (item is ToolData tool)
        {
            AppendStatLine(sb, lc32, vc32, "Durability", $"{tool.durability}");
            AppendStatLine(sb, lc32, vc32, "Interact speed", $"×{tool.interactionSpeedModifier:0.##}");
        }
        if (item is PartData part)
            AppendStatLine(sb, lc32, vc32, "Max stack", $"{part.maxStackSize}");
        return sb.ToString();
    }

    private static void AppendStatLine(StringBuilder sb, Color32 lc, Color32 vc, string label, string value)
    {
        sb.Append($"{TmpColorOpen(lc)}{label}</color>");
        sb.Append("  ");
        sb.Append($"{TmpColorOpen(vc)}{EscapeTmp(value)}</color>\n\n");
    }

    private static string CapitalizeSentence(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        char c = s[0];
        return char.IsLower(c) ? char.ToUpperInvariant(c) + s.Substring(1) : s;
    }

    private static string EscapeTmp(string str)
    {
        if (string.IsNullOrEmpty(str)) return str;
        return str.Replace("&", "&amp;", System.StringComparison.Ordinal)
            .Replace("<", "\\<", System.StringComparison.Ordinal)
            .Replace(">", "\\>", System.StringComparison.Ordinal);
    }

    private static void EnsurePanelLayout(RectTransform panelRt)
    {
        if (panelRt == null) return;
        if (panelRt.GetComponent<HorizontalLayoutGroup>() != null ||
            panelRt.GetComponent<GridLayoutGroup>() != null)
            return;
        VerticalLayoutGroup v = panelRt.GetComponent<VerticalLayoutGroup>();
        if (v == null) v = panelRt.gameObject.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(24, 24, 20, 22);
        v.spacing = 12f;
        v.childForceExpandHeight = false;
        v.childForceExpandWidth = true;
        v.childAlignment = TextAnchor.UpperLeft;
        v.childControlHeight = true;
        v.childControlWidth = true;

        ContentSizeFitter csf = panelRt.GetComponent<ContentSizeFitter>();
        if (csf == null) csf = panelRt.gameObject.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private static Image CreateFallbackBackdrop(GameObject root)
    {
        var img = root.AddComponent<Image>();
        Texture2D t = Texture2D.whiteTexture;
        img.sprite = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f);
        img.type = Image.Type.Simple;
        return img;
    }
}
