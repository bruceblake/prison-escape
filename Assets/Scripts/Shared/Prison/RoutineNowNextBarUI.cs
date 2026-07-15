using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Prison
{
    /// <summary>
    /// "Now and Next" routine bar with mandatory-transition states and optional adaptive presence:
    /// all-clear (minimal bar + timer) vs pressure (full HUD).
    /// </summary>
    public class RoutineNowNextBarUI : MonoBehaviour
    {
        public enum RoutineBarVisualState
        {
            Chill,
            MandatoryWarning,
            TravelGrace,
            Enforcement
        }

        /// <summary>0 = all-clear minimal strip, 1 = full expanded HUD.</summary>
        public enum RoutineHudPresence
        {
            AllClear,
            Pressure
        }

        [Header("Now / Next labels")]
        public TMP_Text currentPhaseText;
        [Tooltip("Seconds remaining — right of the progress bar.")]
        public TMP_Text timeRemainingText;
        public TMP_Text nextPhaseText;
        [Tooltip("Prefix before next phase name, e.g. \"Next: \"")]
        public string nextPhasePrefix = "NEXT: ";
        public string timeRemainingFormat = "{0}s";
        [Tooltip("Optional monospaced/digital font for the ghost timer on the bar.")]
        public TMP_FontAsset digitalTimerFont;

        [Header("Phase progress (fill within current phase)")]
        public Image phaseProgressFill;
        [Tooltip("Optional background behind the fill")]
        public Image phaseProgressTrack;

        [Header("Mandatory warning (State 1 — on Next slot)")]
        public Graphic nextMandatoryBorder;
        public GameObject nextMandatoryIcon;
        public Color mandatoryWarningBorderColor = new Color(0.95f, 0.55f, 0.15f, 1f);
        public Color mandatoryWarningNextTextColor;
        public Color mandatoryWarningStatusColor;
        [Tooltip("Unused — next phase is shown only on the far right.")]
        public string mandatoryWarningStatusFormat = "";
        [Tooltip("Prepended to Next label during warning (optional).")]
        public string mandatoryWarningNextPrefix = "";
        [Tooltip("Uses UI Outline on NextSlot when border image is missing.")]
        public bool useOutlineFallbackForBorder = true;
        public Vector2 mandatoryOutlineDistance = new Vector2(2f, -2f);
        [Min(0f)] public float mandatoryWarningPulseHz = 1.5f;
        [Range(0.4f, 1f)] public float mandatoryWarningPulseMinAlpha = 0.65f;

        [Header("Status row (detail strip)")]
        public TMP_Text statusText;
        [Tooltip("Middle dot between status and path — used with HorizontalLayoutGroup.")]
        public TMP_Text detailSeparatorText;
        [Tooltip("Path segment e.g. CELL 0 -> CAFETERIA")]
        public TMP_Text pathText;
        public TMP_Text locationText;
        public TMP_Text goToText;
        public string statusCompliantFormat = "IN POSITION";
        public string statusGraceFormat = "TRAVEL GRACE ({0}s)";
        public string statusRollCallShakedownFormat = "SHAKEDOWN {0}/{1}";
        public string statusRollCallAwaitingShakedownText = "WAITING TO BE CLEARED";
        public string statusNonCompliantText = "NON-COMPLIANT";
        [Tooltip("ASCII-only arrow between location labels (avoids missing TMP glyphs).")]
        public string pathArrow = " TO ";
        [Tooltip("Character between status and path segments.")]
        public string detailSeparatorCharacter = "|";
        [HideInInspector]
        public string detailSeparator = "   \u00b7   ";
        public string locationFormat = "Loc: {0}";
        public string goToFormat = "GO TO: {0}";

        [Header("Typography")]
        [Tooltip("When enabled, sets TMP point sizes each frame (scene labels were ~32pt before script override).")]
        public bool applyTypographyFromScript = true;
        public float phaseLabelFontSize = 40f;
        public float timerFontSize = 36f;
        public float nextLabelFontSize = 36f;
        public float bottomRowFontSize = 32f;

        [Header("Colors — progress fill by state")]
        [Tooltip("Full-width empty track behind the fill (semi-transparent so total phase time reads clearly).")]
        public Color progressTrackColor = new Color(0f, 0f, 0f, 0.42f);
        [Tooltip("Subtle outline around the track.")]
        public Color progressTrackBorderColor = new Color(0f, 0f, 0f, 0.65f);
        public Vector2 progressTrackBorderDistance = new Vector2(1f, -1f);
        [Tooltip("Compliant / routine in progress (no warning, grace, or bust).")]
        public Color chillFillColor = new Color(0.32f, 0.52f, 0.72f, 1f);
        [Tooltip("Flexible phase ending soon — mandatory event next.")]
        public Color mandatoryWarningFillColor = new Color(0.85f, 0.55f, 0.18f, 1f);
        [Tooltip("Mandatory travel grace countdown.")]
        public Color travelGraceFillColor; // defaults in Reset
        [Tooltip("Non-compliant during mandatory phase.")]
        public Color enforcementFillColor; // defaults in Reset
        public Color compliantStatusColor = Color.white;
        public Color travelGraceStatusColor;
        public Color enforcementStatusColor;

        [Header("Hierarchy & callouts")]
        [Range(0.2f, 1f)]
        [Tooltip("Next phase label alpha when not in mandatory-warning (Now stays full opacity).")]
        public float nextPhasePreviewAlpha = 0.5f;
        [Min(0.02f)] public float goToEnforcementPulseHz = 2.2f;
        [Range(0.4f, 1f)] public float goToEnforcementPulseMinAlpha = 0.55f;

        [Header("Enforcement pulse (State 3)")]
        public Graphic enforcementPulseTarget;
        [Min(0.02f)] public float enforcementFlashHz = 2f;
        [Range(0.4f, 1f)] public float enforcementFlashMinAlpha = 0.55f;

        [Header("UI juice")]
        [Tooltip("White flash + scale bump when entering travel grace. Off if RoutineBarDisplayController handles juice.")]
        public bool graceTransitionSlamEnabled = false;
        [Min(0.05f)] public float graceSlamDuration = 0.2f;
        [Range(0f, 0.25f)] public float graceSlamScaleBoost = 0.08f;

        [Header("Adaptive HUD")]
        [Tooltip("Legacy per-element fade. Prefer RoutineBarDisplayController CanvasGroup on this object.")]
        public bool adaptiveHudEnabled = false;
        [Range(0.5f, 0.95f)]
        [Tooltip("Time remaining fraction above this + in correct location = all-clear minimal mode.")]
        public float allClearMinTimeRemaining = 0.5f;
        [Range(0.05f, 0.5f)]
        [Tooltip("At or below this fraction always uses pressure (full) mode.")]
        public float pressureMaxTimeRemaining = 0.25f;
        [Min(0.5f)] public float presenceTransitionSpeed = 8f;
        public float minimalTopRowHeight = 58f;
        public float minimalLocRowHeight = 22f;
        public float minimalBarHeight = 6f;
        [Range(0.1f, 1f)] public float minimalTrackAlpha = 0.35f;
        [Range(0.5f, 1f)] public float minimalLabelAlpha = 0.92f;
        public float minimalPhaseFontSize = 30f;
        public float minimalNextFontSize = 26f;
        public float minimalTimerFontSize = 26f;
        public float minimalLocationFontSize = 24f;

        [Header("Layout repair")]
        [Tooltip("Runs hierarchy/layout repair once on Start (fixes zero-width TMP and overlapping rows).")]
        public bool fixLayoutOnStart = true;
        [Tooltip("Positions rows by pixel width (recommended). Prevents Next text overlapping the progress bar.")]
        public bool useManualWidthLayout = true;
        [Header("Bridge layout (phase | bar | next + detail strip)")]
        public float bridgeRowHeight = 42f;
        public float detailRowHeight = 30f;
        public float bridgeBarHeight = 24f;
        [Tooltip("Padding inside the dark command strip (horizontal).")]
        public float stripPaddingHorizontal = 16f;
        [Tooltip("Padding inside the dark command strip (vertical).")]
        public float stripPaddingVertical = 11f;
        [Tooltip("Gap between phase, progress bar, and next label.")]
        public float bridgeColumnGap = 18f;
        [Tooltip("Gap between the bridge row and detail strip.")]
        public float bridgeDetailGap = 8f;
        [Tooltip("Inset from screen edge to the command strip.")]
        public float screenEdgePadding = 10f;
        [Tooltip("Dark panel behind the command strip.")]
        public Color commandStripColor = new Color(0.04f, 0.06f, 0.08f, 0.88f);
        public float topRowHeight = 56f;
        public float bottomRowHeight = 40f;
        public float rowGap = 8f;
        public float columnGap = 18f;
        public float currentColumnWidth = 280f;
        public float timerColumnWidth = 80f;
        public float nextColumnWidth = 300f;
        public float statusColumnWidth = 380f;
        public float locationColumnWidth = 150f;
        public float goToColumnWidth = 220f;

        private PrisonerController _prisoner;
        private RoutineBarVisualState _state;
        private RoutineBarVisualState _prevVisualState = RoutineBarVisualState.Chill;
        private bool _layoutRepaired;
        private Outline _nextSlotOutline;
        private Outline _trackOutline;
        private TMP_Text _warningIconText;
        private RectTransform _progressBarRect;
        private Vector3 _progressBarBaseScale = Vector3.one;
        private float _graceSlamTimer;
        private float _presenceBlend = 1f;
        private float _prevPresenceBlend = 1f;
        private CanvasGroup _rowBottomGroup;
        private CanvasGroup _phaseLabelGroup;
        private CanvasGroup _nextSlotGroup;
        private CanvasGroup _locationGroup;
        private RectTransform _rootRect;
        private Image _commandStrip;
        private RectTransform _progressBarRectForTimer;

        [SerializeField, HideInInspector]
        private int _typographyDefaultsVersion;

        [SerializeField, HideInInspector]
        private int _layoutStructureVersion;

        [SerializeField, HideInInspector]
        private int _detailCopyDefaultsVersion;

        private const int TypographyDefaultsVersion = 5;
        private const int LayoutStructureVersion = 6;
        private const int DetailCopyDefaultsVersion = 1;

        private void Reset()
        {
            travelGraceFillColor = PrisonUITheme.CautionYellow;
            travelGraceStatusColor = PrisonUITheme.CautionYellow;
            enforcementFillColor = PrisonUITheme.HazardRed;
            enforcementStatusColor = PrisonUITheme.HazardRed;
            mandatoryWarningNextTextColor = new Color(0.98f, 0.72f, 0.22f, 1f);
            mandatoryWarningStatusColor = new Color(0.98f, 0.72f, 0.22f, 1f);
        }

        private void OnValidate()
        {
            if (travelGraceFillColor == default) travelGraceFillColor = PrisonUITheme.CautionYellow;
            if (travelGraceStatusColor == default) travelGraceStatusColor = PrisonUITheme.CautionYellow;
            if (enforcementFillColor == default) enforcementFillColor = PrisonUITheme.HazardRed;
            if (enforcementStatusColor == default) enforcementStatusColor = PrisonUITheme.HazardRed;
            if (mandatoryWarningNextTextColor == default) mandatoryWarningNextTextColor = new Color(0.98f, 0.72f, 0.22f, 1f);
            if (mandatoryWarningStatusColor == default) mandatoryWarningStatusColor = new Color(0.98f, 0.72f, 0.22f, 1f);
            if (progressTrackColor.a < 0.01f) progressTrackColor = new Color(0f, 0f, 0f, 0.42f);
            SanitizeDetailCopyFields();
        }

        private void Start()
        {
            EnsureTypographyDefaults();
            EnsureDetailCopyDefaults();
            EnsureLayoutStructureVersion();
            _prisoner = FindFirstObjectByType<PrisonerController>(FindObjectsInactive.Exclude);
            if (fixLayoutOnStart)
            {
                _layoutRepaired = false;
                ApplyFullLayoutFix();
            }
            else
            {
                TryWireReferencesFromHierarchy();
                EnsureAdaptiveCanvasGroups();
                ConfigureProgressFillImage();
            }

            if (goToText != null)
                goToText.gameObject.SetActive(false);
            if (locationText != null)
                locationText.gameObject.SetActive(false);

            EnsureDisplayController();
            EnsureTimerReadability();
        }

        private void EnsureDisplayController()
        {
            if (GetComponent<RoutineBarDisplayController>() == null)
                gameObject.AddComponent<RoutineBarDisplayController>();
        }

        private void EnsureTimerReadability()
        {
            if (timeRemainingText == null)
                timeRemainingText = FindTmp("TimeRemainingLabel");
            if (timeRemainingText != null && timeRemainingText.GetComponent<RoutineBarTimerReadability>() == null)
                timeRemainingText.gameObject.AddComponent<RoutineBarTimerReadability>();
        }

        private void OnEnable()
        {
            if (!fixLayoutOnStart) return;
            if (HasBrokenRoutineRects(transform))
            {
                _layoutRepaired = false;
                ApplyFullLayoutFix();
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Fix Layout And Wire References")]
        private void FixLayoutAndWireReferencesEditor()
        {
            Undo.RecordObject(this, "Fix Routine Bar");
            foreach (var t in GetComponentsInChildren<Transform>(true))
                Undo.RecordObject(t, "Fix Routine Bar");
            _layoutRepaired = false;
            ApplyFullLayoutFix();
            EditorUtility.SetDirty(this);
            Debug.Log("[RoutineNowNextBarUI] Layout + references updated. Save the scene.", this);
        }
#endif

        /// <summary>Repairs zero-width TMP, reparents rows, applies layout groups.</summary>
        public void ApplyFullLayoutFix()
        {
            EnsureTypographyDefaults();
            EnsureLayoutStructureVersion();
            var root = transform;
            if (_layoutRepaired && !HasBrokenRoutineRects(root))
                return;
            _layoutRepaired = true;
            NormalizeRoutineBarRoot(root);
            EnsureRowsExist(root);
            ReparentChildrenToRows(root);
            RenameLegacyStatusLabels(root);
            RemoveContentSizeFitters(root);
            ApplyRecommendedLayout(root, topRowHeight, bottomRowHeight);
            TryWireReferencesFromHierarchy();
            EnsureAdaptiveCanvasGroups();
            ConfigureProgressFillImage();
            ConfigureAllRoutineLabels(root);
            EnsureDetailRowLayout(root);
            ApplyTypography();

            if (useManualWidthLayout)
                ApplyBridgeLayout();
            else
            {
                foreach (var le in GetComponentsInChildren<LayoutElement>(true))
                    le.enabled = true;
                foreach (var lg in GetComponentsInChildren<LayoutGroup>(true))
                    lg.enabled = true;
                if (root is RectTransform rt)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            }
        }

        private static bool HasBrokenRoutineRects(Transform root)
        {
            if (FindDeepChild(root, "Row_Bar") != null)
                return true;
            if (FindDeepChild(root, "Row_Bridge") == null)
                return true;
            if (FindDeepChild(root, "DetailLayout") == null)
                return true;

            var progress = FindDeepChild(root, "ProgressBar");
            if (progress != null && progress.parent != null)
            {
                string p = progress.parent.name;
                if (p != "Row_Bridge" && p != "Row_Meter" && p != "Row_Top")
                    return true;
            }

            var label = FindDeepChild(root, "NextPhaseLabel") as RectTransform;
            if (label != null && (label.sizeDelta.x < 0f || label.sizeDelta.y < 0f))
                return true;
            if (label != null && label.GetComponent<LayoutElement>() != null)
                return true;
            var nextSlot = FindDeepChild(root, "NextSlot");
            if (nextSlot != null && nextSlot.GetComponent<HorizontalLayoutGroup>() != null)
                return true;
            return false;
        }

        private static void NormalizeRoutineBarRoot(Transform root)
        {
            if (root is not RectTransform rt) return;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -8f);
            rt.sizeDelta = new Vector2(0f, 124f);
        }

        private static void EnsureRowsExist(Transform root)
        {
            EnsureNamedRow(root, "Row_Bridge");
            EnsureNamedRow(root, "Row_Detail");
        }

        private static void EnsureNamedRow(Transform root, string rowName)
        {
            if (FindDeepChild(root, rowName) != null) return;
            var go = new GameObject(rowName, typeof(RectTransform));
            go.transform.SetParent(root, false);
        }

        private static void ReparentChildrenToRows(Transform root)
        {
            EnsureRowsExist(root);
            var rowBridge = FindDeepChild(root, "Row_Bridge");
            var rowDetail = FindDeepChild(root, "Row_Detail");
            if (rowBridge == null || rowDetail == null) return;

            CollapseLegacyRowBar(root, rowBridge);
            MigrateLegacyStackRows(root, rowBridge, rowDetail);

            ReparentIfFound(root, rowBridge, "CurrentPhaseLabel", "ProgressBar", "TimeRemainingLabel", "NextSlot");
            ReparentIfFound(root, rowDetail, "StatusText", "LocationText", "GoToText", "DetailSeparator", "PathText");

            var rowTop = FindDeepChild(root, "Row_Top");
            if (rowTop != null)
            {
                ReparentIfFound(rowTop, rowBridge, "CurrentPhaseLabel", "ProgressBar", "TimeRemainingLabel", "NextSlot");
                ReparentIfFound(rowTop, rowDetail, "StatusText", "LocationText", "GoToText");
            }

            var rowBottom = FindDeepChild(root, "Row_Bottom");
            if (rowBottom != null)
                ReparentIfFound(rowBottom, rowDetail, "StatusText", "LocationText", "GoToText");

            DeactivateLegacyRow(root, "Row_Top");
            DeactivateLegacyRow(root, "Row_Bottom");
            DeactivateLegacyRow(root, "Row_Context");
            DeactivateLegacyRow(root, "Row_Meter");
            DeactivateLegacyRow(root, "Row_Action");

            rowBridge.SetSiblingIndex(1);
            rowDetail.SetSiblingIndex(2);
        }

        private static void MigrateLegacyStackRows(Transform root, Transform rowBridge, Transform rowDetail)
        {
            foreach (var legacyName in new[] { "Row_Context", "Row_Meter", "Row_Action" })
            {
                var legacy = FindDeepChild(root, legacyName);
                if (legacy == null) continue;
                if (legacyName == "Row_Context")
                    ReparentIfFound(legacy, rowBridge, "CurrentPhaseLabel");
                else if (legacyName == "Row_Meter")
                    ReparentIfFound(legacy, rowBridge, "ProgressBar", "TimeRemainingLabel", "NextSlot");
                else
                    ReparentIfFound(legacy, rowDetail, "StatusText", "LocationText", "GoToText");
            }
        }

        private static void DeactivateLegacyRow(Transform root, string rowName)
        {
            var row = FindDeepChild(root, rowName);
            if (row == null || row.childCount > 0) return;
#if UNITY_EDITOR
            if (!Application.isPlaying)
                Undo.DestroyObjectImmediate(row.gameObject);
            else
#endif
                Object.Destroy(row.gameObject);
        }

        private static void CollapseLegacyRowBar(Transform root, Transform rowTop)
        {
            var rowBar = FindDeepChild(root, "Row_Bar");
            if (rowBar == null) return;

            for (int i = rowBar.childCount - 1; i >= 0; i--)
                rowBar.GetChild(i).SetParent(rowTop, false);

#if UNITY_EDITOR
            if (!Application.isPlaying)
                Undo.DestroyObjectImmediate(rowBar.gameObject);
            else
#endif
                Destroy(rowBar.gameObject);
        }

        private static void ReparentIfFound(Transform root, Transform newParent, params string[] names)
        {
            foreach (var name in names)
            {
                var t = FindDeepChild(root, name);
                if (t != null && t.parent != newParent)
                    t.SetParent(newParent, false);
            }
        }

        private static void RenameLegacyStatusLabels(Transform root)
        {
            var loc = FindDeepChild(root, "LocationText") ?? FindDeepChild(root, "StatusText (1)");
            if (loc != null && loc.name != "LocationText")
                loc.name = "LocationText";

            var go = FindDeepChild(root, "GoToText") ?? FindDeepChild(root, "StatusText (2)");
            if (go != null && go.name != "GoToText")
                go.name = "GoToText";

            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.StartsWith("StatusText") && t.name != "StatusText")
                    t.gameObject.SetActive(false);
            }
        }

        private static void RemoveContentSizeFitters(Transform root)
        {
            foreach (var fitter in root.GetComponentsInChildren<ContentSizeFitter>(true))
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    Undo.DestroyObjectImmediate(fitter);
                else
#endif
                    Destroy(fitter);
            }
        }

        private static void ApplyRecommendedLayout(Transform root, float topHeight, float bottomHeight)
        {
            float topLabelH = Mathf.Max(topHeight - 8f, 44f);
            float bottomLabelH = Mathf.Max(bottomHeight - 6f, 34f);
            var vertical = root.GetComponent<VerticalLayoutGroup>();
            if (vertical == null) vertical = root.gameObject.AddComponent<VerticalLayoutGroup>();
            vertical.padding = new RectOffset(12, 12, 6, 6);
            vertical.spacing = 6;
            vertical.childAlignment = TextAnchor.UpperCenter;
            vertical.childForceExpandWidth = true;
            vertical.childForceExpandHeight = false;
            vertical.childControlWidth = true;
            vertical.childControlHeight = true;

            var rowTop = FindDeepChild(root, "Row_Top");
            if (rowTop != null)
            {
                var h = rowTop.GetComponent<HorizontalLayoutGroup>();
                if (h == null) h = rowTop.gameObject.AddComponent<HorizontalLayoutGroup>();
                h.spacing = 12;
                h.padding = new RectOffset(0, 0, 0, 0);
                h.childAlignment = TextAnchor.MiddleLeft;
                h.childForceExpandWidth = false;
                h.childForceExpandHeight = true;
                h.childControlWidth = true;
                h.childControlHeight = true;
                var le = rowTop.GetComponent<LayoutElement>();
                if (le == null) le = rowTop.gameObject.AddComponent<LayoutElement>();
                le.preferredHeight = topHeight;
                le.minHeight = topHeight;
                le.flexibleHeight = 0;
            }

            var rowBottom = FindDeepChild(root, "Row_Bottom");
            if (rowBottom != null)
            {
                var h = rowBottom.GetComponent<HorizontalLayoutGroup>();
                if (h == null) h = rowBottom.gameObject.AddComponent<HorizontalLayoutGroup>();
                h.spacing = 24;
                h.childAlignment = TextAnchor.MiddleLeft;
                h.childForceExpandWidth = false;
                h.childForceExpandHeight = true;
                h.childControlWidth = true;
                h.childControlHeight = true;
                var le = rowBottom.GetComponent<LayoutElement>();
                if (le == null) le = rowBottom.gameObject.AddComponent<LayoutElement>();
                le.preferredHeight = bottomHeight;
                le.minHeight = bottomHeight;
                le.flexibleHeight = 0;
            }

            SetLayoutElement(FindDeepChild(root, "CurrentPhaseLabel"), minWidth: 210, preferredWidth: 220, flexibleWidth: 0, preferredHeight: topLabelH);
            SetLayoutElement(FindDeepChild(root, "ProgressBar"), minWidth: 200, flexibleWidth: 1, preferredHeight: 26);
            SetLayoutElement(FindDeepChild(root, "TimeRemainingLabel"), minWidth: 64, preferredWidth: 72, flexibleWidth: 0, preferredHeight: topLabelH);
            SetLayoutElement(FindDeepChild(root, "NextSlot"), minWidth: 200, preferredWidth: 260, flexibleWidth: 0, preferredHeight: topLabelH);
            SetLayoutElement(FindDeepChild(root, "StatusText"), minWidth: 300, flexibleWidth: 1, preferredHeight: bottomLabelH);
            SetLayoutElement(FindDeepChild(root, "LocationText"), minWidth: 150, flexibleWidth: 0, preferredHeight: bottomLabelH);
            SetLayoutElement(FindDeepChild(root, "GoToText"), minWidth: 220, flexibleWidth: 0, preferredHeight: bottomLabelH);

            PrepareVerticalLayoutRow(FindDeepChild(root, "Row_Top") as RectTransform, topHeight);
            PrepareVerticalLayoutRow(FindDeepChild(root, "Row_Bottom") as RectTransform, bottomHeight);
            PrepareHorizontalLayoutChild(FindDeepChild(root, "CurrentPhaseLabel") as RectTransform, 220f, topLabelH);
            PrepareHorizontalLayoutChild(FindDeepChild(root, "ProgressBar") as RectTransform, 300f, 26f);
            PrepareHorizontalLayoutChild(FindDeepChild(root, "TimeRemainingLabel") as RectTransform, 72f, topLabelH);
            PrepareHorizontalLayoutChild(FindDeepChild(root, "NextSlot") as RectTransform, 260f, topLabelH);
            PrepareHorizontalLayoutChild(FindDeepChild(root, "StatusText") as RectTransform, 300f, bottomLabelH);
            PrepareHorizontalLayoutChild(FindDeepChild(root, "LocationText") as RectTransform, 150f, bottomLabelH);
            PrepareHorizontalLayoutChild(FindDeepChild(root, "GoToText") as RectTransform, 220f, bottomLabelH);

            StretchRect(FindDeepChild(root, "Track"));
            StretchRect(FindDeepChild(root, "Fill"), 2f);
            ReorderSibling(FindDeepChild(root, "Track"), 0);
            ReorderSibling(FindDeepChild(root, "Fill"), 1);

            ConfigureNextSlotLayout(root);
            StripLayoutElementFromNonLayoutChildren(root);
        }

        /// <summary>
        /// Child of a HorizontalLayoutGroup: single anchor point — avoids Unity's red-X layout conflict gizmo.
        /// </summary>
        private static void PrepareHorizontalLayoutChild(RectTransform rt, float width, float height)
        {
            if (rt == null) return;
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(Mathf.Max(1f, width), Mathf.Max(1f, height));
            rt.localScale = Vector3.one;
        }

        /// <summary>Child of RoutineBar VerticalLayoutGroup: full width, fixed height row.</summary>
        private static void PrepareVerticalLayoutRow(RectTransform rt, float height)
        {
            if (rt == null) return;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, Mathf.Max(1f, height));
            rt.localScale = Vector3.one;
        }

        private static void ConfigureNextSlotLayout(Transform root)
        {
            var nextSlot = FindDeepChild(root, "NextSlot");
            var nextLabel = FindDeepChild(root, "NextPhaseLabel");
            var border = FindDeepChild(root, "MandatoryBorder");

            if (nextSlot != null)
            {
                // Nested layout groups + stretch children = Unity red-X warnings. NextSlot is a fixed-size box only.
                var nestedLayout = nextSlot.GetComponent<HorizontalLayoutGroup>();
                if (nestedLayout != null)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                        Undo.DestroyObjectImmediate(nestedLayout);
                    else
#endif
                        Destroy(nestedLayout);
                }
            }

            if (border != null)
            {
                StretchRect(border, 0f);
                if (border.TryGetComponent<Image>(out var borderImg))
                {
                    borderImg.raycastTarget = false;
                    borderImg.color = new Color(0.95f, 0.55f, 0.15f, 0.35f);
                }
            }

            if (nextLabel is RectTransform labelRt)
            {
                RemoveLayoutElementIfPresent(nextLabel);
                labelRt.anchorMin = Vector2.zero;
                labelRt.anchorMax = Vector2.one;
                labelRt.pivot = new Vector2(1f, 0.5f);
                labelRt.offsetMin = new Vector2(6f, 2f);
                labelRt.offsetMax = new Vector2(-6f, -2f);
                labelRt.sizeDelta = Vector2.zero;
                labelRt.anchoredPosition = Vector2.zero;
            }
        }

        private static void StripLayoutElementFromNonLayoutChildren(Transform root)
        {
            RemoveLayoutElementIfPresent(FindDeepChild(root, "NextPhaseLabel"));
            RemoveLayoutElementIfPresent(FindDeepChild(root, "Track"));
            RemoveLayoutElementIfPresent(FindDeepChild(root, "Fill"));
            RemoveLayoutElementIfPresent(FindDeepChild(root, "MandatoryBorder"));
            RemoveLayoutElementIfPresent(FindDeepChild(root, "TimeRemainingLabel"));
        }

        private static void RemoveLayoutElementIfPresent(Transform t)
        {
            if (t == null) return;
            var le = t.GetComponent<LayoutElement>();
            if (le == null) return;
#if UNITY_EDITOR
            if (!Application.isPlaying)
                Undo.DestroyObjectImmediate(le);
            else
#endif
                Destroy(le);
        }

        private void ConfigureAllRoutineLabels(Transform root)
        {
            ConfigureTmp(FindDeepChild(root, "CurrentPhaseLabel")?.GetComponent<TMP_Text>(), TextAlignmentOptions.MidlineLeft, phaseLabelFontSize);
            ConfigureTmp(FindDeepChild(root, "TimeRemainingLabel")?.GetComponent<TMP_Text>(), TextAlignmentOptions.MidlineRight, timerFontSize);
            ConfigureTmp(FindDeepChild(root, "NextPhaseLabel")?.GetComponent<TMP_Text>(), TextAlignmentOptions.MidlineRight, nextLabelFontSize);
            ConfigureTmp(FindDeepChild(root, "StatusText")?.GetComponent<TMP_Text>(), TextAlignmentOptions.MidlineLeft, bottomRowFontSize);
            ConfigureTmp(FindDeepChild(root, "DetailSeparator")?.GetComponent<TMP_Text>(), TextAlignmentOptions.MidlineLeft, bottomRowFontSize);
            ConfigureTmp(FindDeepChild(root, "PathText")?.GetComponent<TMP_Text>(), TextAlignmentOptions.MidlineLeft, bottomRowFontSize);
            ConfigureTmp(FindDeepChild(root, "LocationText")?.GetComponent<TMP_Text>(), TextAlignmentOptions.MidlineLeft, bottomRowFontSize);
            ConfigureTmp(FindDeepChild(root, "GoToText")?.GetComponent<TMP_Text>(), TextAlignmentOptions.MidlineLeft, bottomRowFontSize);
        }

        private void EnsureLayoutStructureVersion()
        {
            if (_layoutStructureVersion >= LayoutStructureVersion)
                return;
            _layoutStructureVersion = LayoutStructureVersion;
            _layoutRepaired = false;
        }

        /// <summary>Bumps saved scenes that still have the old 15–18pt override values.</summary>
        private void EnsureTypographyDefaults()
        {
            if (_typographyDefaultsVersion >= TypographyDefaultsVersion)
                return;

            phaseLabelFontSize = 40f;
            timerFontSize = 36f;
            nextLabelFontSize = 36f;
            bottomRowFontSize = 32f;
            topRowHeight = 56f;
            bottomRowHeight = 40f;
            currentColumnWidth = 280f;
            nextColumnWidth = 300f;
            timerColumnWidth = 80f;
            statusColumnWidth = 380f;
            applyTypographyFromScript = true;
            _typographyDefaultsVersion = TypographyDefaultsVersion;
            _layoutRepaired = false;
        }

        private void EnsureDetailCopyDefaults()
        {
            if (_detailCopyDefaultsVersion >= DetailCopyDefaultsVersion)
                return;
            _detailCopyDefaultsVersion = DetailCopyDefaultsVersion;
            statusNonCompliantText = "NON-COMPLIANT";
            pathArrow = " TO ";
            detailSeparatorCharacter = "|";
            _layoutRepaired = false;
        }

        private void SanitizeDetailCopyFields()
        {
            statusNonCompliantText = NormalizeStatusLabel(statusNonCompliantText);
            pathArrow = SanitizePathArrow(pathArrow);
            if (string.IsNullOrWhiteSpace(detailSeparatorCharacter))
                detailSeparatorCharacter = "|";
        }

        private static string NormalizeStatusLabel(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "NON-COMPLIANT";

            string s = value.Trim();
            if (s.StartsWith("Status:", System.StringComparison.OrdinalIgnoreCase))
                s = s.Substring(7).Trim();
            s = s.TrimEnd('.', '!', '?', ' ');
            return string.IsNullOrEmpty(s) ? "NON-COMPLIANT" : s;
        }

        private static string SanitizePathArrow(string arrow)
        {
            if (string.IsNullOrWhiteSpace(arrow))
                return " TO ";

            string trimmed = arrow.Trim();
            foreach (char c in trimmed)
            {
                if (c > 127)
                    return " TO ";
            }

            if (trimmed.Contains("->") || trimmed.Contains("=>") || trimmed.Contains("\uFFFD"))
                return " TO ";

            return " TO ";
        }

        private void ApplyTypography()
        {
            if (!applyTypographyFromScript)
                return;

            SetFontSize(currentPhaseText, phaseLabelFontSize);
            SetFontSize(timeRemainingText, timerFontSize);
            SetFontSize(nextPhaseText, nextLabelFontSize);
            SetFontSize(statusText, bottomRowFontSize);
            SetFontSize(detailSeparatorText, bottomRowFontSize);
            SetFontSize(pathText, bottomRowFontSize);
            SetFontSize(locationText, bottomRowFontSize);
            SetFontSize(goToText, bottomRowFontSize);
        }

        private void EnsureTimeRemainingLabel(Transform root)
        {
            timeRemainingText ??= FindTmp("TimeRemainingLabel");
            if (timeRemainingText != null) return;

            var rowTop = FindDeepChild(root, "Row_Top");
            if (rowTop == null) return;

            var go = new GameObject("TimeRemainingLabel", typeof(RectTransform));
            go.transform.SetParent(rowTop, false);

            var progress = FindDeepChild(rowTop, "ProgressBar");
            if (progress != null)
                go.transform.SetSiblingIndex(progress.GetSiblingIndex() + 1);

            timeRemainingText = go.AddComponent<TextMeshProUGUI>();
            timeRemainingText.text = "0s";
            timeRemainingText.raycastTarget = false;
            timeRemainingText.alignment = TextAlignmentOptions.MidlineRight;
            if (currentPhaseText != null)
                timeRemainingText.font = currentPhaseText.font;
        }

        private static void SetFontSize(TMP_Text tmp, float size)
        {
            if (tmp == null || size <= 0f) return;
            tmp.enableAutoSizing = false;
            tmp.fontSizeMin = size;
            tmp.fontSizeMax = size + 12f;
            tmp.fontSize = size;
        }

        private static void ConfigureTmp(TMP_Text tmp, TextAlignmentOptions alignment, float fontSize)
        {
            if (tmp == null) return;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.enableAutoSizing = false;
            tmp.alignment = alignment;
            tmp.margin = Vector4.zero;
            if (fontSize > 0f)
                tmp.fontSize = fontSize;
            tmp.ForceMeshUpdate();
        }

        private static void SetLayoutElement(Transform t, float minWidth = 0, float preferredWidth = -1, float flexibleWidth = -1, float preferredHeight = -1)
        {
            if (t == null) return;
            var le = t.GetComponent<LayoutElement>();
            if (le == null) le = t.gameObject.AddComponent<LayoutElement>();
            if (minWidth > 0) le.minWidth = minWidth;
            if (preferredWidth >= 0) le.preferredWidth = preferredWidth;
            if (flexibleWidth >= 0) le.flexibleWidth = flexibleWidth;
            if (preferredHeight >= 0)
            {
                le.preferredHeight = preferredHeight;
                le.minHeight = preferredHeight;
            }
        }

        private static void StretchRect(Transform t, float inset = 0f)
        {
            if (t is not RectTransform rt) return;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
            rt.anchoredPosition = Vector2.zero;
        }

        private static void ReorderSibling(Transform t, int index)
        {
            if (t == null) return;
            t.SetSiblingIndex(index);
        }

        private void TryWireReferencesFromHierarchy()
        {
            currentPhaseText ??= FindTmp("CurrentPhaseLabel");
            EnsureTimeRemainingLabel(transform);
            timeRemainingText ??= FindTmp("TimeRemainingLabel");
            nextPhaseText ??= FindTmp("NextPhaseLabel");
            statusText ??= FindTmp("StatusText");
            detailSeparatorText ??= FindTmp("DetailSeparator");
            pathText ??= FindTmp("PathText");
            locationText ??= FindTmp("LocationText") ?? FindTmp("StatusText (1)");
            goToText ??= FindTmp("GoToText") ?? FindTmp("StatusText (2)");

            if (phaseProgressFill == null)
            {
                var fill = transform.Find("Fill") ?? FindDeep("Fill");
                if (fill != null) phaseProgressFill = fill.GetComponent<Image>();
            }
            if (phaseProgressTrack == null)
            {
                var track = transform.Find("Track") ?? FindDeep("Track");
                if (track != null) phaseProgressTrack = track.GetComponent<Image>();
            }
            if (nextMandatoryBorder == null)
            {
                var border = FindDeep("MandatoryBorder");
                if (border != null) nextMandatoryBorder = border.GetComponent<Graphic>();
            }
            if (nextMandatoryIcon == null)
            {
                var icon = FindDeep("MandatoryIcon");
                if (icon != null) nextMandatoryIcon = icon.gameObject;
            }
            ResolveProgressBarImages();
            enforcementPulseTarget ??= phaseProgressFill;

            var nextSlot = FindDeep("NextSlot");
            EnsureMandatoryOutlineReference(nextSlot);
            if (nextMandatoryBorder == null && nextSlot != null)
                nextMandatoryBorder = FindDeep("MandatoryBorder")?.GetComponent<Graphic>();
        }

        private void ConfigureProgressFillImage()
        {
            if (phaseProgressFill == null) return;
            EnsureDefaultUISprite(phaseProgressFill);
            if (phaseProgressTrack != null)
            {
                EnsureDefaultUISprite(phaseProgressTrack);
                phaseProgressTrack.type = Image.Type.Simple;
            }

            phaseProgressFill.type = Image.Type.Filled;
            phaseProgressFill.fillMethod = Image.FillMethod.Horizontal;
            phaseProgressFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            phaseProgressFill.fillAmount = 1f;
            phaseProgressFill.raycastTarget = false;
            ApplyProgressBarVisual(1f, chillFillColor);
        }

        /// <summary>Unity needs any sprite before Image Type (e.g. Filled) is available in the Inspector.</summary>
        private static void EnsureDefaultUISprite(Image image)
        {
            if (image == null || image.sprite != null) return;
            image.sprite = GetDefaultUISprite();
        }

        private static Sprite _defaultUiSprite;

        private static Sprite GetDefaultUISprite()
        {
            if (_defaultUiSprite != null) return _defaultUiSprite;

            // Built-in UI/Skin/*.psd sprites are not available in all Unity versions; use a solid white quad.
            Texture2D tex = Texture2D.whiteTexture;
            _defaultUiSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            return _defaultUiSprite;
        }

        private void ResolveProgressBarImages()
        {
            var barRoot = FindDeepChild(transform, "ProgressBar");
            if (barRoot == null) return;

            var fillImg = FindDeepChild(barRoot, "Fill")?.GetComponent<Image>();
            var trackImg = FindDeepChild(barRoot, "Track")?.GetComponent<Image>();

            if (fillImg != null && trackImg != null
                && fillImg.type != Image.Type.Filled && trackImg.type == Image.Type.Filled)
            {
                (fillImg, trackImg) = (trackImg, fillImg);
            }

            if (fillImg != null) phaseProgressFill = fillImg;
            if (trackImg != null) phaseProgressTrack = trackImg;
        }

        private void ApplyProgressBarVisual(float fillAmount, Color fillColor)
        {
            ResolveProgressBarImages();

            if (phaseProgressFill != null)
            {
                EnsureDefaultUISprite(phaseProgressFill);
                phaseProgressFill.type = Image.Type.Filled;
                phaseProgressFill.fillMethod = Image.FillMethod.Horizontal;
                phaseProgressFill.fillOrigin = (int)Image.OriginHorizontal.Left;
                phaseProgressFill.fillAmount = Mathf.Clamp01(fillAmount);
                phaseProgressFill.raycastTarget = false;
                phaseProgressFill.color = fillColor;
            }

            EnsureProgressTrackVisible();
        }

        private void EnsureProgressTrackVisible()
        {
            if (phaseProgressTrack == null)
            {
                var barRoot = FindDeepChild(transform, "ProgressBar");
                if (barRoot == null) return;
                var trackGo = new GameObject("Track", typeof(RectTransform), typeof(Image));
                trackGo.transform.SetParent(barRoot, false);
                trackGo.transform.SetAsFirstSibling();
                phaseProgressTrack = trackGo.GetComponent<Image>();
                StretchRect(trackGo.transform);
            }

            EnsureDefaultUISprite(phaseProgressTrack);
            phaseProgressTrack.type = Image.Type.Simple;
            phaseProgressTrack.raycastTarget = false;
            phaseProgressTrack.enabled = true;
            phaseProgressTrack.color = progressTrackColor;
            phaseProgressTrack.transform.SetAsFirstSibling();

            _trackOutline ??= phaseProgressTrack.GetComponent<Outline>();
            if (_trackOutline == null)
                _trackOutline = phaseProgressTrack.gameObject.AddComponent<Outline>();
            _trackOutline.effectColor = progressTrackBorderColor;
            _trackOutline.effectDistance = progressTrackBorderDistance;
            _trackOutline.useGraphicAlpha = true;
        }

        private void EnsureProgressBarRect()
        {
            if (_progressBarRect != null) return;
            var bar = FindDeepChild(transform, "ProgressBar") as RectTransform;
            if (bar == null) return;
            _progressBarRect = bar;
            _progressBarBaseScale = bar.localScale;
        }

        private TMP_Text FindTmp(string objectName)
        {
            var t = FindDeep(objectName);
            return t != null ? t.GetComponent<TMP_Text>() : null;
        }

        private Transform FindDeep(string childName) => FindDeepChild(transform, childName);

        private static Transform FindDeepChild(Transform parent, string childName)
        {
            if (parent == null) return null;
            foreach (var t in parent.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == childName) return t;
            }
            return null;
        }

        private void EnsureCommandStrip(RectTransform root, float totalHeight, float stripX, float stripWidth)
        {
            if (_commandStrip == null)
            {
                var existing = transform.Find("CommandStrip");
                if (existing != null)
                    _commandStrip = existing.GetComponent<Image>();
            }

            if (_commandStrip == null)
            {
                var go = new GameObject("CommandStrip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(transform, false);
                go.transform.SetAsFirstSibling();
                _commandStrip = go.GetComponent<Image>();
                _commandStrip.raycastTarget = false;
            }

            _commandStrip.color = commandStripColor;
            if (_commandStrip.rectTransform is RectTransform stripRt)
            {
                stripRt.anchorMin = new Vector2(0f, 1f);
                stripRt.anchorMax = new Vector2(0f, 1f);
                stripRt.pivot = new Vector2(0f, 1f);
                stripRt.anchoredPosition = new Vector2(stripX, -screenEdgePadding);
                stripRt.sizeDelta = new Vector2(stripWidth, totalHeight);
            }
        }

        private void ApplyBridgeLayout()
        {
            var root = transform as RectTransform;
            if (root == null) return;

            Canvas.ForceUpdateCanvases();

            EnsureDetailRowLayout(transform);
            SetBridgeLayoutComponentsEnabled(false);

            var rowBridge = FindDeepChild(transform, "Row_Bridge") as RectTransform;
            var rowDetail = FindDeepChild(transform, "Row_Detail") as RectTransform;
            if (rowBridge == null || rowDetail == null)
                return;

            float stripX = screenEdgePadding;
            float stripWidth = Mathf.Max(360f, root.rect.width - stripX * 2f);
            float contentW = stripWidth - stripPaddingHorizontal * 2f;
            float contentX = stripX + stripPaddingHorizontal;
            float totalH = stripPaddingVertical + bridgeRowHeight + bridgeDetailGap + detailRowHeight + stripPaddingVertical;
            root.sizeDelta = new Vector2(0f, totalH + screenEdgePadding);

            EnsureCommandStrip(root, totalH, stripX, stripWidth);

            float y = -(stripPaddingVertical + screenEdgePadding);
            PlaceRowContainer(rowBridge, contentX, y, contentW, bridgeRowHeight);

            float gap = bridgeColumnGap;
            float phaseW = MeasureTmpWidth(currentPhaseText, currentColumnWidth);
            float nextW = MeasureTmpWidth(nextPhaseText, nextColumnWidth);
            phaseW = Mathf.Clamp(phaseW + 4f, 148f, contentW * 0.36f);
            nextW = Mathf.Clamp(nextW + 4f, 128f, contentW * 0.30f);
            float barW = Mathf.Max(96f, contentW - phaseW - nextW - gap * 2f);

            float x = 0f;
            PlaceLabelInBridgeRow(rowBridge, "CurrentPhaseLabel", x, phaseW, bridgeRowHeight);
            x += phaseW + gap;
            PlaceProgressBarInBridgeRow(rowBridge, x, barW, bridgeRowHeight);
            x += barW + gap;
            PlaceLabelInBridgeRow(rowBridge, "NextSlot", x, nextW, bridgeRowHeight);

            var progress = FindDeepChild(rowBridge, "ProgressBar") as RectTransform;
            if (progress != null)
                _progressBarRectForTimer = progress;

            AlignNextLabelInSlot(rowBridge);

            y -= bridgeRowHeight + bridgeDetailGap;
            PlaceRowContainer(rowDetail, contentX, y, contentW, detailRowHeight);
            LayoutDetailRow(rowDetail, contentW);
            SuppressLegacyDetailLabels(transform);

            RemoveRectMaskIfPresent(FindDeepChild(transform, "ProgressBar"));
            EnsureRectMask(FindDeepChild(rowBridge, "NextSlot"));

            ApplyTypographyHierarchy();
        }

        private void EnsureDetailRowLayout(Transform root)
        {
            var rowDetail = FindDeepChild(root, "Row_Detail");
            if (rowDetail == null) return;

            var layoutRoot = FindDeepChild(rowDetail, "DetailLayout");
            if (layoutRoot == null)
            {
                var go = new GameObject("DetailLayout", typeof(RectTransform));
                layoutRoot = go.transform;
                layoutRoot.SetParent(rowDetail, false);
            }

            EnsureDetailTmp(ref statusText, layoutRoot, "StatusText", "IN POSITION");
            EnsureDetailTmp(ref detailSeparatorText, layoutRoot, "DetailSeparator", "|");
            EnsureDetailTmp(ref pathText, layoutRoot, "PathText", "");

            ReparentIfFound(rowDetail, layoutRoot, "StatusText", "DetailSeparator", "PathText");
            ReparentIfFound(root, layoutRoot, "StatusText", "DetailSeparator", "PathText");

            if (layoutRoot is RectTransform layoutRt)
            {
                layoutRt.anchorMin = Vector2.zero;
                layoutRt.anchorMax = Vector2.one;
                layoutRt.offsetMin = Vector2.zero;
                layoutRt.offsetMax = Vector2.zero;
                layoutRt.pivot = new Vector2(0f, 0.5f);
            }

            var hlg = layoutRoot.GetComponent<HorizontalLayoutGroup>();
            if (hlg == null) hlg = layoutRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6f;
            hlg.padding = new RectOffset(0, 8, 0, 0);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            ConfigureDetailChildLayout(statusText);
            ConfigureDetailChildLayout(detailSeparatorText);
            ConfigureDetailChildLayout(pathText);
            SyncDetailRowFonts();

            var rowHlg = rowDetail.GetComponent<HorizontalLayoutGroup>();
            if (rowHlg != null)
                rowHlg.enabled = false;

            SuppressLegacyDetailLabels(root);
        }

        private void SetBridgeLayoutComponentsEnabled(bool enabled)
        {
            var detailLayout = FindDeepChild(transform, "DetailLayout");
            foreach (var lg in GetComponentsInChildren<LayoutGroup>(true))
            {
                if (detailLayout != null && lg.transform.IsChildOf(detailLayout))
                    continue;
                if (lg.transform == detailLayout)
                    continue;
                lg.enabled = enabled;
            }

            foreach (var le in GetComponentsInChildren<LayoutElement>(true))
            {
                if (detailLayout != null && le.transform.IsChildOf(detailLayout))
                    continue;
                le.enabled = enabled;
            }
        }

        private void EnableDetailLayoutDrivenLayout(Transform rowDetail)
        {
            var layout = FindDeepChild(rowDetail, "DetailLayout");
            if (layout == null)
                return;

            var hlg = layout.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null)
                hlg.enabled = true;

            foreach (var le in layout.GetComponentsInChildren<LayoutElement>(true))
                le.enabled = true;
            foreach (var fitter in layout.GetComponentsInChildren<ContentSizeFitter>(true))
                fitter.enabled = true;
        }

        private static void SuppressLegacyDetailLabels(Transform root)
        {
            var rowDetail = FindDeepChild(root, "Row_Detail");
            if (rowDetail == null)
                return;

            var detailLayout = FindDeepChild(rowDetail, "DetailLayout");
            foreach (var tmp in rowDetail.GetComponentsInChildren<TMP_Text>(true))
            {
                if (detailLayout != null && tmp.transform.IsChildOf(detailLayout))
                    continue;
                tmp.gameObject.SetActive(false);
            }

            var rowHlg = rowDetail.GetComponent<HorizontalLayoutGroup>();
            if (rowHlg != null)
                rowHlg.enabled = false;
        }

        private void SyncDetailRowFonts()
        {
            if (statusText == null)
                return;

            foreach (var tmp in new[] { detailSeparatorText, pathText })
            {
                if (tmp == null)
                    continue;
                tmp.font = statusText.font;
                if (statusText.fontSharedMaterial != null)
                    tmp.fontSharedMaterial = statusText.fontSharedMaterial;
            }
        }

        private static void PrepareDetailLayoutChild(RectTransform rt)
        {
            if (rt == null)
                return;
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        private static void EnsureDetailTmp(ref TMP_Text field, Transform parent, string name, string defaultText)
        {
            if (field != null) return;
            var existing = FindDeepChild(parent, name);
            if (existing != null)
            {
                field = existing.GetComponent<TMP_Text>();
                return;
            }

            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            field = go.AddComponent<TextMeshProUGUI>();
            field.text = defaultText;
            field.raycastTarget = false;
            field.enableWordWrapping = false;
            field.overflowMode = TextOverflowModes.Overflow;
        }

        private static void ConfigureDetailChildLayout(TMP_Text tmp)
        {
            if (tmp == null) return;
            PrepareDetailLayoutChild(tmp.rectTransform);

            var le = tmp.GetComponent<LayoutElement>();
            if (le == null) le = tmp.gameObject.AddComponent<LayoutElement>();
            le.minWidth = 8f;
            le.preferredHeight = 28f;
            le.flexibleWidth = 0f;
            le.enabled = true;

            var fitter = tmp.GetComponent<ContentSizeFitter>();
            if (fitter == null) fitter = tmp.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.enabled = true;
        }

        private void LayoutDetailRow(RectTransform rowDetail, float width)
        {
            var layout = FindDeepChild(rowDetail, "DetailLayout") as RectTransform;
            if (layout == null) return;

            layout.anchorMin = Vector2.zero;
            layout.anchorMax = Vector2.one;
            layout.pivot = new Vector2(0f, 0.5f);
            layout.anchoredPosition = Vector2.zero;
            layout.sizeDelta = Vector2.zero;
            layout.offsetMin = Vector2.zero;
            layout.offsetMax = Vector2.zero;

            EnableDetailLayoutDrivenLayout(rowDetail);
            LayoutRebuilder.ForceRebuildLayoutImmediate(layout);
        }

        private void PlaceLabelInBridgeRow(RectTransform row, string childName, float x, float width, float rowHeight)
        {
            var child = FindDeepChild(row, childName) as RectTransform;
            if (child == null) return;
            float h = Mathf.Max(1f, rowHeight - 4f);
            child.anchorMin = new Vector2(0f, 0.5f);
            child.anchorMax = new Vector2(0f, 0.5f);
            child.pivot = new Vector2(0f, 0.5f);
            child.anchoredPosition = new Vector2(x, 0f);
            child.sizeDelta = new Vector2(Mathf.Max(1f, width), h);
            child.localScale = Vector3.one;
        }

        private void PlaceProgressBarInBridgeRow(RectTransform row, float x, float width, float rowHeight)
        {
            var child = FindDeepChild(row, "ProgressBar") as RectTransform;
            if (child == null) return;
            float barH = Mathf.Min(bridgeBarHeight, rowHeight - 8f);
            float y = (rowHeight - barH) * 0.5f;
            child.anchorMin = new Vector2(0f, 0f);
            child.anchorMax = new Vector2(0f, 0f);
            child.pivot = new Vector2(0f, 0f);
            child.anchoredPosition = new Vector2(x, y);
            child.sizeDelta = new Vector2(Mathf.Max(1f, width), barH);
            child.localScale = Vector3.one;
        }

        private void AlignNextLabelInSlot(RectTransform rowBridge)
        {
            var nextLabel = FindDeepChild(rowBridge, "NextPhaseLabel") as RectTransform;
            if (nextLabel == null) return;
            nextLabel.anchorMin = new Vector2(0f, 0.5f);
            nextLabel.anchorMax = new Vector2(1f, 0.5f);
            nextLabel.pivot = new Vector2(1f, 0.5f);
            nextLabel.anchoredPosition = Vector2.zero;
            nextLabel.offsetMin = new Vector2(4f, 0f);
            nextLabel.offsetMax = new Vector2(-8f, 0f);
            nextLabel.sizeDelta = Vector2.zero;
        }

        private void ApplyTypographyHierarchy()
        {
            if (!applyTypographyFromScript) return;

            if (currentPhaseText != null)
            {
                currentPhaseText.fontStyle = FontStyles.Bold;
                SetFontSize(currentPhaseText, phaseLabelFontSize);
            }

            if (nextPhaseText != null)
            {
                nextPhaseText.fontStyle = FontStyles.Normal;
                SetFontSize(nextPhaseText, nextLabelFontSize);
            }

            if (statusText != null)
            {
                statusText.fontStyle = FontStyles.Normal;
                SetFontSize(statusText, bottomRowFontSize);
                statusText.characterSpacing = 4f;
            }

            ApplyTimerTypography();
        }

        private void ApplyTimerTypography()
        {
            if (timeRemainingText == null) return;
            if (digitalTimerFont != null)
                timeRemainingText.font = digitalTimerFont;
            timeRemainingText.fontStyle = FontStyles.Bold;
            SetFontSize(timeRemainingText, timerFontSize * 0.85f);
        }

        private void ApplyGhostTimer(float fill01)
        {
            if (timeRemainingText == null || _progressBarRectForTimer == null)
                return;

            timeRemainingText.transform.SetParent(_progressBarRectForTimer, false);
            timeRemainingText.gameObject.SetActive(true);

            var rt = timeRemainingText.rectTransform;
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            float barW = Mathf.Max(1f, _progressBarRectForTimer.rect.width);
            float edgeX = Mathf.Clamp(fill01 * barW, 32f, barW - 32f);
            float barH = _progressBarRectForTimer.rect.height;
            rt.anchoredPosition = new Vector2(edgeX, 0f);
            rt.sizeDelta = new Vector2(68f, Mathf.Max(18f, barH - 4f));

            timeRemainingText.alignment = TextAlignmentOptions.Center;
            timeRemainingText.raycastTarget = false;
        }

        private static float MeasureTmpWidth(TMP_Text tmp, float fallback)
        {
            if (tmp == null || !tmp.gameObject.activeInHierarchy)
                return fallback;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.ForceMeshUpdate();
            float w = tmp.GetPreferredValues(tmp.text).x;
            return Mathf.Max(fallback * 0.5f, w + 18f);
        }

        private static void FitLabelInsideSlot(Transform slot, Transform label)
        {
            if (slot is not RectTransform slotRt || label is not RectTransform labelRt) return;
            labelRt.SetParent(slot, false);
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.pivot = new Vector2(1f, 0.5f);
            labelRt.offsetMin = new Vector2(4f, 0f);
            labelRt.offsetMax = new Vector2(-4f, 0f);
            labelRt.sizeDelta = Vector2.zero;
        }

        private static void RemoveRectMaskIfPresent(Transform t)
        {
            if (t == null) return;
            var mask = t.GetComponent<RectMask2D>();
            if (mask != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(mask);
                else
#endif
                    Destroy(mask);
            }
        }

        private void PlaceRowContainer(RectTransform row, float x, float yTop, float width, float height)
        {
            row.anchorMin = new Vector2(0f, 1f);
            row.anchorMax = new Vector2(0f, 1f);
            row.pivot = new Vector2(0f, 1f);
            row.anchoredPosition = new Vector2(x, yTop);
            row.sizeDelta = new Vector2(width, height);
        }

        private void PlaceInRow(RectTransform row, string childName, float x, float width)
        {
            var child = FindDeepChild(row, childName) as RectTransform;
            if (child == null) return;
            child.anchorMin = new Vector2(0f, 0f);
            child.anchorMax = new Vector2(0f, 1f);
            child.pivot = new Vector2(0f, 0.5f);
            child.anchoredPosition = new Vector2(x, 0f);
            child.sizeDelta = new Vector2(Mathf.Max(1f, width), 0f);
            if (childName != "ProgressBar")
                child.localScale = Vector3.one;
        }

        private static void EnsureRectMask(Transform t)
        {
            if (t == null) return;
            if (t.GetComponent<RectMask2D>() == null)
                t.gameObject.AddComponent<RectMask2D>();
        }

        private void Update()
        {
            var tm = PrisonTimeManager.Instance;
            if (tm == null) return;
            if (_prisoner == null)
                _prisoner = FindFirstObjectByType<PrisonerController>(FindObjectsInactive.Exclude);

            tm.GetNextEventInfo(out PrisonEventType nextEvent, out _);
            string goTo = _prisoner != null
                ? PrisonRoutineLabels.GetGoToLabel(tm.CurrentEvent, _prisoner.CellIndex)
                : PrisonRoutineLabels.GetGoToLabel(nextEvent, 0);

            _state = ResolveState(tm);
            string nextGoTo = _prisoner != null
                ? PrisonRoutineLabels.GetGoToLabel(nextEvent, _prisoner.CellIndex)
                : PrisonRoutineLabels.GetGoToLabel(nextEvent, 0);
            ApplyState(tm, nextEvent, goTo, nextGoTo);
        }

        public RoutineBarVisualState CurrentVisualState => _state;
        public float AdaptivePresenceBlend => _presenceBlend;
        public RoutineHudPresence CurrentHudPresence =>
            _presenceBlend < 0.5f ? RoutineHudPresence.AllClear : RoutineHudPresence.Pressure;

        private RoutineBarVisualState ResolveState(PrisonTimeManager tm)
        {
            if (tm.IsMandatoryTravelGraceActive && !tm.IsMorningRollCallShakedownGateActive)
                return RoutineBarVisualState.TravelGrace;

            if (_prisoner != null && !tm.IsMandatoryTravelGraceActive && PrisonEventRules.IsMandatory(tm.CurrentEvent)
                && !_prisoner.IsCompliant && !MorningRollCallTracker.IsInmateReleasedFromRollCallStand(_prisoner))
                return RoutineBarVisualState.Enforcement;

            if (tm.IsHighStakesTransitionWarningActive)
                return RoutineBarVisualState.MandatoryWarning;

            return RoutineBarVisualState.Chill;
        }

        private void ApplyState(PrisonTimeManager tm, PrisonEventType nextEvent, string goTo, string nextGoTo)
        {
            string currentTitle = PrisonRoutineLabels.FormatPhaseTitle(tm.CurrentEvent);
            string nextTitle = PrisonRoutineLabels.FormatPhaseTitle(nextEvent);

            if (currentPhaseText != null)
            {
                currentPhaseText.text = currentTitle;
                currentPhaseText.color = compliantStatusColor;
                currentPhaseText.fontStyle = FontStyles.Bold;
            }

            bool showMandatoryNext = _state == RoutineBarVisualState.MandatoryWarning;
            if (nextPhaseText != null)
            {
                string prefix = showMandatoryNext ? mandatoryWarningNextPrefix : "";
                nextPhaseText.text = $"{prefix}{nextPhasePrefix}{nextTitle}";
                if (showMandatoryNext)
                    nextPhaseText.color = PulseMandatoryColor(mandatoryWarningNextTextColor);
                else
                    nextPhaseText.color = WithAlpha(compliantStatusColor, nextPhasePreviewAlpha);
            }

            ApplyMandatoryWarningChrome(showMandatoryNext);
            DetectGraceTransitionSlam();

            float fill = GetProgressFill01(tm);
            Color fillColor = chillFillColor;
            switch (_state)
            {
                case RoutineBarVisualState.MandatoryWarning:
                    fillColor = mandatoryWarningFillColor;
                    break;
                case RoutineBarVisualState.TravelGrace:
                    fillColor = travelGraceFillColor;
                    fill = GetGraceFill01(tm);
                    break;
                case RoutineBarVisualState.Enforcement:
                    fillColor = enforcementFillColor;
                    break;
            }

            ApplyProgressBarVisual(fill, fillColor);
            ApplyTimeRemainingDisplay(tm);
            ApplyDetailStrip(tm, goTo, nextGoTo);

            if (goToText != null)
                goToText.gameObject.SetActive(false);

            if (applyTypographyFromScript)
                ApplyTypography();
            if (GetComponent<RoutineBarDisplayController>() == null)
                ApplyEnforcementPulse(_state == RoutineBarVisualState.Enforcement, fillColor);

            UpdateAdaptivePresence(tm);

            if (useManualWidthLayout)
                ApplyBridgeLayout();

            ApplyGhostTimer(fill);

            ApplyAdaptiveVisuals(tm);
            ApplyGraceTransitionSlam(fillColor);

            _prevVisualState = _state;
            _prevPresenceBlend = _presenceBlend;
        }

        private void EnsureAdaptiveCanvasGroups()
        {
            _rootRect = transform as RectTransform;
            _rowBottomGroup = EnsureCanvasGroup(
                FindDeepChild(transform, "Row_Detail")?.gameObject ?? FindDeepChild(transform, "Row_Bottom")?.gameObject);
            _phaseLabelGroup = EnsureCanvasGroup(currentPhaseText != null ? currentPhaseText.gameObject : null);
            _nextSlotGroup = EnsureCanvasGroup(FindDeepChild(transform, "NextSlot")?.gameObject);
            _locationGroup = EnsureCanvasGroup(locationText != null ? locationText.gameObject : null);
        }

        private static CanvasGroup EnsureCanvasGroup(GameObject go)
        {
            if (go == null) return null;
            var g = go.GetComponent<CanvasGroup>();
            if (g == null) g = go.AddComponent<CanvasGroup>();
            g.interactable = false;
            g.blocksRaycasts = false;
            return g;
        }

        private float GetTimeRemainingFraction(PrisonTimeManager tm)
        {
            if (tm.IsMandatoryTravelGraceActive)
                return tm.MandatoryTravelGraceRemaining01;
            return tm.CurrentPhaseTimeRemainingFraction;
        }

        private void ApplyDetailStrip(PrisonTimeManager tm, string goTo, string nextGoTo)
        {
            if (statusText == null) return;

            SanitizeDetailCopyFields();
            SuppressLegacyDetailLabels(transform);

            if (locationText != null)
                locationText.gameObject.SetActive(false);
            if (goToText != null)
                goToText.gameObject.SetActive(false);

            // One plain-language instruction, coloured by tone. Matches the objective
            // waypoint's destination (both come from PrisonRoutineDestination).
            var instruction = PrisonRoutineLabels.GetInstruction(tm, _prisoner);
            Color lineColor = ToneColor(instruction.Tone);
            string text = instruction.Text;

            if ((instruction.Tone == PrisonRoutineLabels.RoutineInstructionTone.MustMove
                 || instruction.Tone == PrisonRoutineLabels.RoutineInstructionTone.Enforcement)
                && _prisoner != null)
            {
                var obj = PrisonRoutineDestination.ResolveActiveDestination(tm, _prisoner);
                if (obj.Stand != null)
                {
                    Vector3 d = obj.Stand.position - _prisoner.transform.position;
                    d.y = 0f;
                    int m = Mathf.RoundToInt(d.magnitude);
                    if (m > 1) text += $"  ·  {m}m";
                }
                if (tm.IsMandatoryTravelGraceActive)
                {
                    int g = Mathf.CeilToInt(tm.ComplianceGraceSecondsRemaining);
                    if (g > 0) text += $"  ·  {g}s to move";
                }
            }

            statusText.gameObject.SetActive(true);
            statusText.text = text;
            lineColor.a = 0.95f;
            statusText.color = lineColor;

            // Legacy GPS "HERE TO DEST" fragment retired — one clear line instead.
            if (detailSeparatorText != null) detailSeparatorText.gameObject.SetActive(false);
            if (pathText != null) pathText.gameObject.SetActive(false);

            var detailLayout = FindDeepChild(transform, "DetailLayout") as RectTransform;
            if (detailLayout != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(detailLayout);

            SyncDetailRowFonts();
        }

        private Color ToneColor(PrisonRoutineLabels.RoutineInstructionTone tone)
        {
            switch (tone)
            {
                case PrisonRoutineLabels.RoutineInstructionTone.InPosition: return compliantStatusColor;
                case PrisonRoutineLabels.RoutineInstructionTone.FreeRoam: return new Color(0.55f, 0.78f, 1f);
                case PrisonRoutineLabels.RoutineInstructionTone.Wait: return travelGraceStatusColor;
                case PrisonRoutineLabels.RoutineInstructionTone.MustMove: return travelGraceStatusColor;
                case PrisonRoutineLabels.RoutineInstructionTone.Enforcement: return FlashColor(enforcementStatusColor);
                default: return compliantStatusColor;
            }
        }

        private string GetStatusFragment(PrisonTimeManager tm, string goTo, string nextGoTo, ref Color color)
        {
            switch (_state)
            {
                case RoutineBarVisualState.Enforcement:
                    color = FlashColor(enforcementStatusColor);
                    return statusNonCompliantText;
                case RoutineBarVisualState.TravelGrace:
                    if (ShouldShowCompliantStatus(tm))
                    {
                        color = compliantStatusColor;
                        return statusCompliantFormat;
                    }
                    color = travelGraceStatusColor;
                    int g = Mathf.CeilToInt(tm.ComplianceGraceSecondsRemaining);
                    return string.Format(statusGraceFormat, g);
                case RoutineBarVisualState.MandatoryWarning:
                    if (_prisoner != null && _prisoner.IsCompliant)
                    {
                        color = compliantStatusColor;
                        return statusCompliantFormat;
                    }
                    color = PulseMandatoryColor(mandatoryWarningStatusColor);
                    return "PREPARE";
            }

            if (tm.IsMorningRollCallShakedownGateActive)
                return GetMorningRollCallStatusFragment(tm, ref color);

            string guidance = PrisonScheduleGuidance.BuildStatusLine(tm, _prisoner, _state, nextGoTo);
            if (!string.IsNullOrEmpty(guidance))
            {
                color = compliantStatusColor;
                return guidance;
            }

            if (_prisoner != null && !_prisoner.IsCompliant)
            {
                color = travelGraceStatusColor;
                return "MOVE";
            }

            color = compliantStatusColor;
            return statusCompliantFormat;
        }

        private string GetMorningRollCallStatusFragment(PrisonTimeManager tm, ref Color color)
        {
            var tracker = MorningRollCallTracker.Instance;
            if (_prisoner != null && tracker != null && tracker.IsInmateShakedownComplete(_prisoner))
            {
                color = compliantStatusColor;
                return "CLEARED";
            }

            if (_prisoner != null && tracker != null && !tracker.IsInmateShakedownComplete(_prisoner))
            {
                color = travelGraceStatusColor;
                return statusRollCallAwaitingShakedownText;
            }

            int done = tracker != null
                ? tracker.CountInmatesShakedownComplete(out int total)
                : PrisonerPresence.CountAccountedFor(out total);
            color = done >= total ? compliantStatusColor : travelGraceStatusColor;
            return string.Format(statusRollCallShakedownFormat, done, total);
        }

        private string GetPathFragment(string goTo, string nextGoTo)
        {
            if (!ShouldShowPath(goTo, nextGoTo))
                return string.Empty;

            string here = FormatGpsLabel(GetLocationDisplay());
            string dest = FormatGpsLabel(GetDestinationLabel(goTo, nextGoTo));
            if (string.IsNullOrEmpty(here) || string.IsNullOrEmpty(dest) || here == dest)
                return string.Empty;

            return here + SanitizePathArrow(pathArrow) + dest;
        }

        private bool ShouldShowPath(string goTo, string nextGoTo)
        {
            var tm = PrisonTimeManager.Instance;
            if (tm != null && PrisonScheduleGuidance.ShouldAlwaysShowTravelPath(tm))
                return true;

            if (_state == RoutineBarVisualState.Enforcement)
                return true;
            if (_state == RoutineBarVisualState.TravelGrace && _prisoner != null && !ShouldShowCompliantStatus(PrisonTimeManager.Instance))
                return true;
            if (_state == RoutineBarVisualState.MandatoryWarning && _prisoner != null && !_prisoner.IsCompliant)
                return true;

            if (tm != null && tm.IsMorningRollCallShakedownGateActive && _prisoner != null
                && MorningRollCallTracker.Instance != null
                && MorningRollCallTracker.Instance.IsInmateShakedownComplete(_prisoner))
                return true;

            return _prisoner != null && !_prisoner.IsCompliant;
        }

        private string GetDestinationLabel(string goTo, string nextGoTo)
        {
            var tm = PrisonTimeManager.Instance;
            if (_state == RoutineBarVisualState.MandatoryWarning
                || (_state == RoutineBarVisualState.TravelGrace && _prisoner != null && tm != null
                    && !ShouldShowCompliantStatus(tm)))
                return nextGoTo;

            // Morning roll call: show line-up / stand until this inmate's cell is shakedown-cleared; then show next phase venue (e.g. breakfast).
            if (tm != null && tm.IsMorningRollCallShakedownGateActive)
            {
                bool releasedToNextPhase = _prisoner != null && MorningRollCallTracker.Instance != null
                    && MorningRollCallTracker.Instance.IsInmateShakedownComplete(_prisoner);
                if (releasedToNextPhase)
                    return nextGoTo;
                int cellIdx = _prisoner?.CellIndex ?? 0;
                return PrisonRoutineLabels.GetMorningRollCallLineUpDestinationLabel(cellIdx);
            }

            if (tm != null && tm.CurrentEvent == PrisonEventType.FreeTime)
                return nextGoTo;

            return goTo;
        }

        private static string FormatGpsLabel(string label)
        {
            if (string.IsNullOrEmpty(label) || label == "—")
                return string.Empty;
            return label.ToUpperInvariant();
        }

        private string GetLocationDisplay()
        {
            if (_prisoner == null) return "—";
            string loc = PrisonRoutineLabels.FormatPlayerLocation(_prisoner.GetCurrentLocationLabel());
            if (!string.IsNullOrEmpty(loc) && loc != "—")
                return loc;
            var reg = PrisonLocationRegistry.Instance;
            if (reg != null)
                return reg.GetCellHudLabel(_prisoner.CellIndex);
            return "—";
        }

        private bool ShouldShowCompliantStatus(PrisonTimeManager tm)
        {
            if (_prisoner == null) return false;
            if (tm.IsMandatoryTravelGraceActive)
                return _prisoner.IsAtRequiredLocation;
            return _prisoner.IsCompliant;
        }

        private bool QualifiesForAllClear(PrisonTimeManager tm)
        {
            if (!adaptiveHudEnabled || _prisoner == null)
                return false;
            if (tm.IsMorningRollCallShakedownGateActive)
                return false;
            if (!ShouldShowCompliantStatus(tm))
                return false;
            if (_state != RoutineBarVisualState.Chill)
                return false;
            return GetTimeRemainingFraction(tm) > allClearMinTimeRemaining;
        }

        private float GetAdaptivePresenceTarget(PrisonTimeManager tm)
        {
            if (!adaptiveHudEnabled)
                return 1f;
            if (tm.IsMorningRollCallShakedownGateActive)
                return 1f;
            if (QualifiesForAllClear(tm))
                return 0f;

            if (_state == RoutineBarVisualState.Enforcement)
                return 1f;
            if (_prisoner != null && !_prisoner.IsCompliant)
                return 1f;
            if (GetTimeRemainingFraction(tm) <= pressureMaxTimeRemaining)
                return 1f;
            if (_state == RoutineBarVisualState.TravelGrace
                || _state == RoutineBarVisualState.MandatoryWarning)
                return 1f;

            return 1f;
        }

        private void UpdateAdaptivePresence(PrisonTimeManager tm)
        {
            float target = GetAdaptivePresenceTarget(tm);
            float step = presenceTransitionSpeed * Time.unscaledDeltaTime;
            _presenceBlend = Mathf.MoveTowards(_presenceBlend, target, step);
        }

        private void ApplyAdaptiveVisuals(PrisonTimeManager tm)
        {
            EnsureAdaptiveCanvasGroups();
            float b = _presenceBlend;
            float labelAlpha = Mathf.Lerp(minimalLabelAlpha, 1f, b);

            if (_phaseLabelGroup != null)
            {
                _phaseLabelGroup.alpha = labelAlpha;
                if (currentPhaseText != null)
                    currentPhaseText.gameObject.SetActive(true);
            }

            if (_nextSlotGroup != null)
            {
                _nextSlotGroup.alpha = labelAlpha;
                if (nextPhaseText != null && nextPhaseText.transform.parent != null)
                    nextPhaseText.transform.parent.gameObject.SetActive(true);
            }

            if (_locationGroup != null)
                _locationGroup.alpha = labelAlpha;

            if (_rowBottomGroup != null)
                _rowBottomGroup.gameObject.SetActive(true);

            bool rollCallHud = tm.IsMorningRollCallShakedownGateActive;
            bool showDetailRow = b > 0.35f || rollCallHud;
            if (statusText != null)
                statusText.gameObject.SetActive(showDetailRow);
            if (goToText != null && !showDetailRow)
                goToText.gameObject.SetActive(false);

            if (phaseProgressTrack != null)
            {
                Color track = progressTrackColor;
                track.a = Mathf.Lerp(minimalTrackAlpha, progressTrackColor.a, b);
                phaseProgressTrack.color = track;
            }

            if (applyTypographyFromScript)
            {
                SetFontSize(currentPhaseText, Mathf.Lerp(minimalPhaseFontSize, phaseLabelFontSize, b));
                SetFontSize(nextPhaseText, Mathf.Lerp(minimalNextFontSize, nextLabelFontSize, b));
                SetFontSize(timeRemainingText, Mathf.Lerp(minimalTimerFontSize, timerFontSize, b));
                SetFontSize(locationText, Mathf.Lerp(minimalLocationFontSize, bottomRowFontSize, b));
            }

            if (_rootRect != null && !useManualWidthLayout)
            {
                float pad = 12f;
                float h = pad + Mathf.Lerp(minimalTopRowHeight, topRowHeight, b)
                    + rowGap * b + Mathf.Lerp(0f, bottomRowHeight, b) + pad;
                _rootRect.sizeDelta = new Vector2(_rootRect.sizeDelta.x, h);
            }

            bool expanding = _prevPresenceBlend < 0.35f && b > 0.55f && GetAdaptivePresenceTarget(tm) > 0.5f;
            if (expanding && _rootRect != null)
            {
                float pop = 1f + 0.04f * Mathf.Sin((1f - b) * Mathf.PI);
                _rootRect.localScale = Vector3.one * pop;
            }
            else if (_rootRect != null)
                _rootRect.localScale = Vector3.one;
        }

        private void ApplyMinimalWidthLayout() => ApplyBridgeLayout();

        private void ApplyBlendedLayoutHeights(float blend) => ApplyBridgeLayout();

        private void ApplyTimeRemainingDisplay(PrisonTimeManager tm)
        {
            EnsureTimeRemainingLabel(transform);
            if (timeRemainingText == null) return;

            int seconds = GetSecondsRemainingForDisplay(tm);
            timeRemainingText.text = string.Format(timeRemainingFormat, seconds);
            timeRemainingText.color = GetTimerTextColor();
            ApplyTimerTypography();
        }

        private int GetSecondsRemainingForDisplay(PrisonTimeManager tm)
        {
            if (_state == RoutineBarVisualState.TravelGrace)
                return Mathf.Max(0, Mathf.CeilToInt(tm.ComplianceGraceSecondsRemaining));
            return Mathf.Max(0, Mathf.CeilToInt(tm.SecondsRemainingInCurrentPhaseRealTime));
        }

        private Color GetTimerTextColor()
        {
            switch (_state)
            {
                case RoutineBarVisualState.TravelGrace:
                    return travelGraceStatusColor;
                case RoutineBarVisualState.Enforcement:
                    return FlashColor(enforcementStatusColor);
                case RoutineBarVisualState.MandatoryWarning:
                    return PulseMandatoryColor(mandatoryWarningStatusColor);
                default:
                    return compliantStatusColor;
            }
        }

        private void DetectGraceTransitionSlam()
        {
            if (GetComponent<RoutineBarDisplayController>() != null) return;
            if (!graceTransitionSlamEnabled || !Application.isPlaying) return;
            if (_state == RoutineBarVisualState.TravelGrace && _prevVisualState != RoutineBarVisualState.TravelGrace)
                _graceSlamTimer = graceSlamDuration;
        }

        private void ApplyGraceTransitionSlam(Color targetFillColor)
        {
            EnsureProgressBarRect();
            if (_progressBarRect != null && _graceSlamTimer <= 0f)
                _progressBarRect.localScale = _progressBarBaseScale;

            if (_graceSlamTimer <= 0f) return;

            _graceSlamTimer -= Time.unscaledDeltaTime;
            float elapsed = graceSlamDuration - Mathf.Max(0f, _graceSlamTimer);
            float t = graceSlamDuration > 0.001f ? Mathf.Clamp01(elapsed / graceSlamDuration) : 1f;
            float flash = Mathf.Sin(t * Mathf.PI);

            if (phaseProgressFill != null)
            {
                Color slamColor = Color.Lerp(Color.white, targetFillColor, t);
                phaseProgressFill.color = Color.Lerp(slamColor, targetFillColor, flash * 0.35f);
            }

            if (_progressBarRect != null)
            {
                float scale = 1f + graceSlamScaleBoost * flash;
                _progressBarRect.localScale = _progressBarBaseScale * scale;
            }
        }

        private Color GetGoToTextColor()
        {
            if (_state == RoutineBarVisualState.MandatoryWarning)
                return PulseMandatoryColor(mandatoryWarningStatusColor);
            if (_state == RoutineBarVisualState.TravelGrace)
                return travelGraceStatusColor;
            if (_state == RoutineBarVisualState.Enforcement)
                return PulseAlpha(enforcementStatusColor, goToEnforcementPulseHz, goToEnforcementPulseMinAlpha);
            return compliantStatusColor;
        }

        private static float GetProgressFill01(PrisonTimeManager tm) =>
            Mathf.Clamp01(tm.CurrentPhaseTimeRemainingFraction);

        private static float GetGraceFill01(PrisonTimeManager tm) =>
            tm.MandatoryTravelGraceRemaining01;

        private void ApplyEnforcementPulse(bool active, Color currentFillColor)
        {
            if (enforcementPulseTarget == null) return;
            if (!active)
            {
                // Fill color is set by ApplyProgressBarVisual; never force red when compliant / grace / warning.
                if (enforcementPulseTarget == phaseProgressFill && _graceSlamTimer <= 0f)
                    enforcementPulseTarget.color = currentFillColor;
                return;
            }

            enforcementPulseTarget.color = FlashColor(enforcementFillColor);
        }

        private void ApplyMandatoryWarningChrome(bool active)
        {
            Color borderColor = active ? PulseMandatoryColor(mandatoryWarningBorderColor) : mandatoryWarningBorderColor;

            if (nextMandatoryBorder != null)
            {
                nextMandatoryBorder.enabled = active;
                if (active)
                    nextMandatoryBorder.color = borderColor;
            }

            if (useOutlineFallbackForBorder)
            {
                if (_nextSlotOutline == null)
                    EnsureMandatoryOutlineReference(FindDeepChild(transform, "NextSlot"));
                if (_nextSlotOutline != null)
                {
                    _nextSlotOutline.enabled = active;
                    if (active)
                    {
                        _nextSlotOutline.effectColor = borderColor;
                        _nextSlotOutline.effectDistance = mandatoryOutlineDistance;
                    }
                }
            }

            ApplyWarningIcon(active, borderColor);
        }

        private void EnsureMandatoryOutlineReference(Transform nextSlot)
        {
            if (nextSlot == null) return;
            _nextSlotOutline = nextSlot.GetComponent<Outline>();
            if (_nextSlotOutline == null && useOutlineFallbackForBorder)
                _nextSlotOutline = nextSlot.gameObject.AddComponent<Outline>();
            if (_nextSlotOutline != null)
            {
                _nextSlotOutline.effectDistance = mandatoryOutlineDistance;
                _nextSlotOutline.useGraphicAlpha = true;
                _nextSlotOutline.enabled = false;
            }
        }

        private void ApplyWarningIcon(bool active, Color color)
        {
            if (nextMandatoryIcon == null) return;

            if (IconHasValidSprite(nextMandatoryIcon))
            {
                nextMandatoryIcon.SetActive(active);
                return;
            }

            var img = nextMandatoryIcon.GetComponent<Image>();
            if (img != null)
                img.enabled = false;

            _warningIconText ??= nextMandatoryIcon.GetComponent<TMP_Text>()
                ?? nextMandatoryIcon.GetComponentInChildren<TMP_Text>(true);
            if (_warningIconText == null)
            {
                var go = new GameObject("WarningMark", typeof(RectTransform));
                go.transform.SetParent(nextMandatoryIcon.transform, false);
                _warningIconText = go.AddComponent<TextMeshProUGUI>();
                var rt = _warningIconText.rectTransform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                _warningIconText.fontSize = Mathf.Max(22f, nextLabelFontSize * 0.65f);
                _warningIconText.alignment = TextAlignmentOptions.Center;
                _warningIconText.raycastTarget = false;
                if (nextPhaseText != null)
                    _warningIconText.font = nextPhaseText.font;
            }

            _warningIconText.text = "!";
            _warningIconText.color = color;
            nextMandatoryIcon.SetActive(active);
        }

        private static bool IconHasValidSprite(GameObject iconRoot)
        {
            var img = iconRoot.GetComponent<Image>();
            return img != null && img.sprite != null && img.enabled;
        }

        private Color PulseMandatoryColor(Color baseColor)
        {
            if (!Application.isPlaying || mandatoryWarningPulseHz < 0.01f)
                return baseColor;
            float t = (Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f * mandatoryWarningPulseHz) * 0.5f) + 0.5f;
            float a = Mathf.Lerp(mandatoryWarningPulseMinAlpha, 1f, t);
            var c = baseColor;
            c.a = a;
            return c;
        }

        private Color FlashColor(Color baseColor) =>
            PulseAlpha(baseColor, enforcementFlashHz, enforcementFlashMinAlpha);

        private static Color WithAlpha(Color c, float alpha)
        {
            c.a = alpha;
            return c;
        }

        private static Color PulseAlpha(Color baseColor, float hz, float minAlpha)
        {
            if (hz < 0.01f) return baseColor;
            float t = (Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f * hz) * 0.5f) + 0.5f;
            float a = Mathf.Lerp(minAlpha, 1f, t);
            var c = baseColor;
            c.a = a;
            return c;
        }
    }
}
