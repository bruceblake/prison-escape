using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Prison
{
    /// <summary>
    /// Bare schedule readout only: previous phase, current, next, and real-time countdown to the next phase.
    /// Disable or hide other HUD objects in the Canvas; this script stands alone.
    /// </summary>
    public class MinimalSchedulePhaseTextHUD : MonoBehaviour
    {
        [Header("Texts (assign one pattern)")]
        [Tooltip("Optional: one block with newlines.")]
        public TMP_Text combinedText;
        public TMP_Text previousPhaseText;
        public TMP_Text currentPhaseText;
        public TMP_Text nextPhaseText;
        public TMP_Text timeUntilNextText;

        [Header("Format")]
        [Tooltip("Combined template: {0}=prev, {1}=current, {2}=next, {3}=time countdown label")]
        public string combinedFormat =
            "Previous: {0}\nCurrent: {1}\nNext: {2}\n{3}";
        public string countdownLabelPrefix = "Time until next:";
        public string previousLabel = "Previous:";
        public string currentLabel = "Current:";
        public string nextLabel = "Next:";

        [Tooltip("If true, countdown shows whole seconds remaining in this phase.")]
        public bool countdownAsWholeSeconds = true;

        [Header("Style (safe defaults)")]
        public Color textColorPrimary = new Color(0.96f, 0.97f, 0.98f, 1f);

        [Header("Readability (combined text only)")]
        [Tooltip("Adds a dark rounded-style plate behind Combined Text so white type stays legible on bright floors.")]
        public bool autoBackdropBehindCombinedText = true;
        public Color backdropColor = new Color(0.04f, 0.045f, 0.06f, 0.78f);
        [Tooltip("Inspector override: assign any Image to tint; leave empty to auto-create.")]
        public Image backingImage;

        private bool _styled;
        private bool _backingCreated;

        private void Awake() => EnsureTypography();

        private void EnsureTypography()
        {
            if (_styled) return;
            _styled = true;
            Stylize(combinedText);
            Stylize(previousPhaseText);
            Stylize(currentPhaseText);
            Stylize(nextPhaseText);
            Stylize(timeUntilNextText);
        }

        private void Stylize(TMP_Text t)
        {
            if (t == null) return;
            t.color = textColorPrimary;
            t.fontStyle = FontStyles.Bold;
            t.margin = new Vector4(4f, 2f, 4f, 2f);
        }

        private void Update()
        {
            EnsureTypography();
            var tm = PrisonTimeManager.Instance;
            if (tm == null || tm.schedule == null || tm.schedule.entries == null || tm.schedule.entries.Length == 0)
            {
                SetAll("—");
                return;
            }

            int n = tm.schedule.entries.Length;
            int cur = tm.CurrentEntryIndex;
            int prevIdx = (cur - 1 + n) % n;
            int nextIdx = (cur + 1) % n;

            string prev = FormatEvent(tm.schedule.entries[prevIdx].eventType);
            string current = FormatEvent(tm.CurrentEvent);
            string next = FormatEvent(tm.schedule.entries[nextIdx].eventType);

            float sec = tm.SecondsRemainingInCurrentPhaseRealTime;
            if (countdownAsWholeSeconds) sec = Mathf.CeilToInt(sec);
            string timeStr = $"{countdownLabelPrefix} {FormatDuration(sec)}";

            if (combinedText != null)
            {
                EnsureScheduleBackdrop();
                combinedText.text = string.Format(combinedFormat, prev, current, next, timeStr);
                FitScheduleBackdrop();
            }
            else
            {
                if (previousPhaseText != null) previousPhaseText.text = $"{previousLabel} {prev}";
                if (currentPhaseText != null) currentPhaseText.text = $"{currentLabel} {current}";
                if (nextPhaseText != null) nextPhaseText.text = $"{nextLabel} {next}";
                if (timeUntilNextText != null) timeUntilNextText.text = timeStr;
            }
        }

        private void SetAll(string placeholder)
        {
            if (combinedText != null)
            {
                EnsureScheduleBackdrop();
                combinedText.text = placeholder;
                FitScheduleBackdrop();
            }
            if (previousPhaseText != null) previousPhaseText.text = placeholder;
            if (currentPhaseText != null) currentPhaseText.text = placeholder;
            if (nextPhaseText != null) nextPhaseText.text = placeholder;
            if (timeUntilNextText != null) timeUntilNextText.text = placeholder;
        }

        private static string FormatDuration(float totalSeconds)
        {
            if (totalSeconds < 0f) totalSeconds = 0f;
            int s = Mathf.FloorToInt(totalSeconds);
            int m = s / 60;
            int r = s % 60;
            if (m > 0) return $"{m}m {r}s";
            return $"{r}s";
        }

        private void EnsureScheduleBackdrop()
        {
            if (!autoBackdropBehindCombinedText || combinedText == null)
                return;

            if (backingImage != null)
            {
                if (backingImage.color.a < 0.01f) backingImage.color = backdropColor;
                backingImage.raycastTarget = false;
                _backingCreated = false;
                return;
            }

            if (_backingCreated) return;

            GameObject go = new GameObject("ScheduleReadabilityBacking", typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            backingImage = go.GetComponent<Image>();

            Texture2D t = Texture2D.whiteTexture;
            backingImage.sprite = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f);
            backingImage.type = Image.Type.Simple;
            backingImage.color = backdropColor;
            backingImage.raycastTarget = false;

            rt.SetParent(combinedText.rectTransform.parent, false);
            Outline o = go.AddComponent<Outline>();
            o.effectColor = new Color(0.35f, 0.5f, 0.75f, 0.25f);
            o.effectDistance = new Vector2(1.5f, -1.5f);

            int idx = Mathf.Max(0, combinedText.transform.GetSiblingIndex());
            rt.SetSiblingIndex(idx);

            Shadow sh = go.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.42f);
            sh.effectDistance = new Vector2(5f, -5f);

            _backingCreated = true;
        }

        private void FitScheduleBackdrop()
        {
            if (backingImage == null || combinedText == null || !_backingCreated)
                return;

            RectTransform tr = combinedText.rectTransform;
            RectTransform br = backingImage.rectTransform;

            Vector2 pref = combinedText.GetPreferredValues(
                combinedText.text,
                Mathf.Max(260f, tr.rect.width > 8f ? tr.rect.width : 360f),
                0f);
            Vector2 pad = new Vector2(26f, 18f);

            br.anchorMin = tr.anchorMin;
            br.anchorMax = tr.anchorMax;
            br.pivot = tr.pivot;
            br.rotation = tr.rotation;
            br.localScale = tr.localScale;

            Vector2 anchored = tr.anchoredPosition;
            br.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, pref.x + pad.x);
            br.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, pref.y + pad.y);
            br.anchoredPosition = anchored;
        }

        private static string FormatEvent(PrisonEventType evt)
        {
            switch (evt)
            {
                case PrisonEventType.MorningRollCall: return "Morning Roll Call";
                case PrisonEventType.NightRollCall: return "Night Roll Call";
                case PrisonEventType.RollCall: return "Roll Call";
                case PrisonEventType.Breakfast: return "Breakfast";
                case PrisonEventType.Lunch: return "Lunch";
                case PrisonEventType.Dinner: return "Dinner";
                case PrisonEventType.FreeTime: return "Yard Time";
                case PrisonEventType.LightsOut: return "Lights Out";
                default: return evt.ToString();
            }
        }
    }
}
