using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Prison
{
    /// <summary>Top center routine bar: schedule segments, needle, phase title over the active band, end-of-phase pulse.</summary>
    public class DailyRoutineBarUI : MonoBehaviour
    {
        public enum TimelineOrientation
        {
            Horizontal,
            Vertical
        }

        [Header("Layout")]
        [Tooltip("Horizontal = left-to-right timeline, Vertical = bottom-to-top timeline.")]
        public TimelineOrientation orientation = TimelineOrientation.Horizontal;
        [Tooltip("If set, phase name is anchored to the horizontal center of the active schedule segment (same width as segmentContainer parent).")]
        public RectTransform phaseNameTrack;
        [Tooltip("Stencil / HUD title: parent should be phaseNameTrack for segment-centered alignment.")]
        public TMP_Text currentPhaseNameText;
        [Tooltip("ALL CAPS for \"YARD TIME\" look")]
        public bool useUppercasePhaseTitle = true;

        [Header("Bar")]
        [Tooltip("Child Images under this rect: one per schedule entry, left-to-right. Widths set at runtime from durations.")]
        public RectTransform segmentContainer;
        [Tooltip("Default tint per segment (cycled if fewer colors than entries).")]
        public Color[] segmentColors =
        {
            new Color(0.25f, 0.28f, 0.32f),
            new Color(0.22f, 0.32f, 0.25f),
            new Color(0.32f, 0.28f, 0.2f)
        };
        [Tooltip("Current phase extra highlight (multiplied with base segment color).")]
        public Color currentPhaseEmphasis = new Color(1.25f, 1.2f, 1.1f, 1f);

        [Header("Time marker (red needle)")]
        [Tooltip("For horizontal: narrow vertical needle moves X 0..1. For vertical: thin horizontal needle moves Y 0..1.")]
        public RectTransform timeMarker;

        [Header("Labels")]
        public TMP_Text clockText;
        [Tooltip("24h time format; {0}=hours, {1}=minutes")]
        public string clockFormat = "{0:D2}:{1:D2}";

        [Header("Phase end warning (last 10% of current phase)")]
        [Min(0.02f)] public float endPhaseWarningPortion = 0.1f;
        [Tooltip("Slow pulse on the *active* segment so the player repositions in time")]
        public float endPhasePulsePeriodSeconds = 1.1f;
        public Color endPhaseWarningTint = new Color(0.7f, 0.2f, 0.2f, 1f);
        [Tooltip("Blend amount toward endPhaseWarningTint when time is low")]
        [Range(0f, 1f)] public float endPhaseWarningBlend = 0.5f;

        [Header("Compliance (phase name color)")]
        [Tooltip("Shakes or pulses when local prisoner is not compliant (not during grace).")]
        public CanvasGroup compliancePulseGroup;
        public AnimationCurve pulseAlpha = AnimationCurve.EaseInOut(0, 0.4f, 1, 1f);
        [Tooltip("Seconds per full pulse loop")]
        public float nonCompliantPulsePeriod = 0.85f;
        [Tooltip("Optional: color the phase name when non-compliant")]
        public Color compliantPhaseColor = Color.white;
        public Color nonCompliantPhaseColor = new Color(0.75f, 0.2f, 0.2f, 1f);

        private bool _layoutBuilt;
        private Image[] _segmentImages;
        private PrisonerController _prisoner;

        private void Start()
        {
            _prisoner = FindFirstObjectByType<PrisonerController>(FindObjectsInactive.Exclude);
        }

        private void Update()
        {
            if (PrisonTimeManager.Instance == null) return;
            var tm = PrisonTimeManager.Instance;

            if (clockText != null)
            {
                float minutes = tm.CurrentTimeMinutes;
                int hours = Mathf.FloorToInt(minutes / 60f) % 24;
                int mins = Mathf.FloorToInt(minutes % 60f);
                clockText.text = string.Format(clockFormat, hours, mins);
            }

            if (currentPhaseNameText != null)
            {
                string t = FormatEvent(tm.CurrentEvent);
                if (useUppercasePhaseTitle) t = t.ToUpperInvariant();
                currentPhaseNameText.text = t;
                if (_prisoner == null) _prisoner = FindFirstObjectByType<PrisonerController>(FindObjectsInactive.Exclude);
                bool grace = tm.IsMandatoryTravelGraceActive;
                bool bad = _prisoner != null && !_prisoner.IsCompliant && !grace;
                currentPhaseNameText.color = bad ? nonCompliantPhaseColor : compliantPhaseColor;
            }

            float p = tm.ScheduleProgress01;
            if (timeMarker != null)
            {
                if (orientation == TimelineOrientation.Horizontal)
                    timeMarker.anchorMin = timeMarker.anchorMax = new Vector2(p, 0.5f);
                else
                    timeMarker.anchorMin = timeMarker.anchorMax = new Vector2(0.5f, p);
            }

            if (phaseNameTrack != null && currentPhaseNameText != null
                && currentPhaseNameText.rectTransform.parent == phaseNameTrack)
            {
                float cx = GetCurrentSegmentCenter01(tm);
                var rt = currentPhaseNameText.rectTransform;
                if (orientation == TimelineOrientation.Horizontal)
                    rt.anchorMin = rt.anchorMax = new Vector2(cx, 0.5f);
                else
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, cx);
                rt.anchoredPosition = Vector2.zero;
            }

            EnsureSegmentLayout(tm);
            UpdateSegmentHighlights(tm);

            if (compliancePulseGroup != null)
            {
                if (_prisoner == null) _prisoner = FindFirstObjectByType<PrisonerController>(FindObjectsInactive.Exclude);
                bool grace = tm.IsMandatoryTravelGraceActive;
                bool showPulse = _prisoner != null && !_prisoner.IsCompliant && !grace;
                compliancePulseGroup.gameObject.SetActive(showPulse);
                if (showPulse && nonCompliantPulsePeriod > 0.01f)
                {
                    float f = (Mathf.Sin(Time.unscaledTime * (Mathf.PI * 2f / nonCompliantPulsePeriod)) * 0.5f) + 0.5f;
                    compliancePulseGroup.alpha = pulseAlpha.Evaluate(f);
                }
            }
        }

        private void EnsureSegmentLayout(PrisonTimeManager tm)
        {
            if (segmentContainer == null || tm.schedule == null || tm.schedule.entries == null) return;
            if (_layoutBuilt) return;
            int n = tm.schedule.entries.Length;
            if (n == 0) return;

            var h = segmentContainer.GetComponent<HorizontalLayoutGroup>();
            var v = segmentContainer.GetComponent<VerticalLayoutGroup>();
            if (orientation == TimelineOrientation.Horizontal)
            {
                if (h != null) h.enabled = true;
                if (v != null) v.enabled = false;
            }
            else
            {
                if (v != null) v.enabled = true;
                if (h != null) h.enabled = false;
            }

            if (segmentContainer.childCount >= n)
            {
                _segmentImages = new Image[n];
                for (int i = 0; i < n; i++)
                {
                    var img = segmentContainer.GetChild(i).GetComponent<Image>();
                    if (img == null) img = segmentContainer.GetChild(i).gameObject.AddComponent<Image>();
                    float ratio = tm.TotalScheduleDurationMinutes > 0.0001f
                        ? (tm.schedule.entries[i].durationMinutes / tm.TotalScheduleDurationMinutes)
                        : (1f / n);
                    var le = segmentContainer.GetChild(i).GetComponent<LayoutElement>();
                    if (le == null) le = segmentContainer.GetChild(i).gameObject.AddComponent<LayoutElement>();
                    if (orientation == TimelineOrientation.Horizontal)
                    {
                        le.flexibleWidth = ratio;
                        le.minWidth = 4f;
                        le.flexibleHeight = 0f;
                    }
                    else
                    {
                        le.flexibleHeight = ratio;
                        le.minHeight = 4f;
                        le.flexibleWidth = 0f;
                    }
                    int c = segmentColors != null && segmentColors.Length > 0 ? i % segmentColors.Length : 0;
                    img.color = segmentColors != null && segmentColors.Length > 0 ? segmentColors[c] : Color.gray;
                    _segmentImages[i] = img;
                }
            }
            else
            {
                _segmentImages = new Image[n];
                for (int i = 0; i < n; i++)
                {
                    var tr = new GameObject($"Seg_{i}_{tm.schedule.entries[i].eventType}").AddComponent<RectTransform>();
                    tr.SetParent(segmentContainer, false);
                    var img = tr.gameObject.AddComponent<Image>();
                    float ratio = tm.TotalScheduleDurationMinutes > 0.0001f
                        ? (tm.schedule.entries[i].durationMinutes / tm.TotalScheduleDurationMinutes)
                        : (1f / n);
                    var le = tr.gameObject.AddComponent<LayoutElement>();
                    if (orientation == TimelineOrientation.Horizontal)
                    {
                        le.flexibleWidth = ratio;
                        le.minWidth = 4f;
                        le.flexibleHeight = 0f;
                    }
                    else
                    {
                        le.flexibleHeight = ratio;
                        le.minHeight = 4f;
                        le.flexibleWidth = 0f;
                    }
                    int c = segmentColors != null && segmentColors.Length > 0 ? i % segmentColors.Length : 0;
                    img.color = segmentColors != null && segmentColors.Length > 0 ? segmentColors[c] : Color.gray;
                    _segmentImages[i] = img;
                }
            }

            _layoutBuilt = true;
        }

        private void UpdateSegmentHighlights(PrisonTimeManager tm)
        {
            if (_segmentImages == null) return;
            int cur = tm.CurrentEntryIndex;
            bool endComing = tm.IsInLastPortionOfCurrentPhase(endPhaseWarningPortion);
            float warnPulse = 0f;
            if (endComing && endPhasePulsePeriodSeconds > 0.01f)
            {
                warnPulse = (Mathf.Sin(Time.unscaledTime * (Mathf.PI * 2f) / endPhasePulsePeriodSeconds) * 0.5f) + 0.5f;
            }

            for (int i = 0; i < _segmentImages.Length; i++)
            {
                if (_segmentImages[i] == null) continue;
                int c = segmentColors != null && segmentColors.Length > 0 ? i % segmentColors.Length : 0;
                Color baseCol = segmentColors != null && segmentColors.Length > 0 ? segmentColors[c] : Color.gray;
                if (i == cur)
                {
                    var lit = new Color(
                        Mathf.Clamp01(baseCol.r * currentPhaseEmphasis.r),
                        Mathf.Clamp01(baseCol.g * currentPhaseEmphasis.g),
                        Mathf.Clamp01(baseCol.b * currentPhaseEmphasis.b),
                        1f);
                    if (endComing)
                    {
                        float b = endPhaseWarningBlend * warnPulse;
                        lit = Color.Lerp(lit, endPhaseWarningTint, b);
                    }
                    _segmentImages[i].color = lit;
                }
                else
                    _segmentImages[i].color = baseCol;
            }
        }

        /// <summary>0–1 horizontal center of the current schedule block (for anchoring a title over that segment).</summary>
        public static float GetCurrentSegmentCenter01(PrisonTimeManager tm)
        {
            if (tm == null) return 0.5f;
            float total = tm.TotalScheduleDurationMinutes;
            if (total < 0.0001f) return 0.5f;
            if (tm.schedule == null || tm.schedule.entries == null) return 0.5f;
            int cur = tm.CurrentEntryIndex;
            if (cur < 0 || cur >= tm.schedule.entries.Length) return 0.5f;
            float acc = 0f;
            for (int i = 0; i < cur; i++) acc += tm.schedule.entries[i].durationMinutes;
            float half = 0.5f * tm.schedule.entries[cur].durationMinutes;
            return Mathf.Clamp01((acc + half) / total);
        }

        private static string FormatEvent(PrisonEventType evt)
        {
            switch (evt)
            {
                case PrisonEventType.MorningRollCall: return "Morning Roll Call";
                case PrisonEventType.NightRollCall: return "Night Roll Call";
                case PrisonEventType.Breakfast: return "Breakfast";
                case PrisonEventType.Lunch: return "Lunch";
                case PrisonEventType.Dinner: return "Dinner";
                case PrisonEventType.FreeTime: return "Yard Time";
                case PrisonEventType.LightsOut: return "Lights Out";
                case PrisonEventType.RollCall: return "Roll Call";
                default: return evt.ToString();
            }
        }
    }
}
