using TMPro;
using UnityEngine;

namespace Prison
{
    /// <summary>
    /// Text-only tactical HUD for schedule and compliance states.
    /// Modes: Active, Transit (grace), Non-compliant.
    /// </summary>
    public class TextRoutineComplianceHUD : MonoBehaviour
    {
        [Header("References")]
        public TMP_Text headerText;
        public TMP_Text timerText;
        public TMP_Text statusLineText;
        public TMP_Text paLineText;

        [Header("Colors")]
        public Color normalColor = Color.white;
        public Color transitColor = new Color(0.956f, 0.815f, 0.247f, 1f); // #F4D03F
        public Color mandatoryWarnColor = new Color(0.95f, 0.55f, 0.15f, 1f);
        public Color dangerColor = new Color(0.753f, 0.224f, 0.169f, 1f);  // #C0392B

        [Header("Danger Flash")]
        public float dangerFlashHz = 2f;
        [Range(0.45f, 1f)] public float dangerMinAlpha = 0.55f;

        [Header("PA Line")]
        public bool showPaLine = true;
        public string paPrefix = "ALL INMATES REPORT TO";

        private PrisonerController _prisoner;

        private void Start()
        {
            _prisoner = FindAnyObjectByType<PrisonerController>(FindObjectsInactive.Exclude);
        }

        private void Update()
        {
            var tm = PrisonTimeManager.Instance;
            if (tm == null) return;
            if (_prisoner == null) _prisoner = FindAnyObjectByType<PrisonerController>(FindObjectsInactive.Exclude);
            if (_prisoner == null) return;

            bool transit = tm.IsMandatoryTravelGraceActive;
            bool atRequired = _prisoner.IsAtRequiredLocation;
            bool nonCompliant = !_prisoner.IsCompliant && !transit;

            string target = PrisonRoutineLabels.GetGoToLabel(tm.CurrentEvent, _prisoner.CellIndex);
            string location = PrisonRoutineLabels.FormatPlayerLocation(_prisoner.GetCurrentLocationLabel());

            if (nonCompliant)
            {
                SetHeader("STATUS: NON-COMPLIANT", true);
                SetTimer($"TARGET: {target}", true);
                SetStatus($"{location} - Status: AT RISK", true);
                SetPaLine(showPaLine ? $"{paPrefix} {target}." : "", true);
                return;
            }

            if (transit && !atRequired)
            {
                int graceLeft = Mathf.CeilToInt(tm.ComplianceGraceSecondsRemaining);
                SetHeader($"TRANSIT TO: {target}", false, transitColor);
                SetTimer($"GRACE PERIOD: 00:{Mathf.Clamp(graceLeft, 0, 99):D2}", false, transitColor);
                SetStatus($"{location} - Status: MOVING...", false, transitColor);
                SetPaLine(showPaLine ? $"{paPrefix} {target}." : "", false, transitColor);
                return;
            }

            if (transit && atRequired)
            {
                SetHeader("STATUS: COMPLIANT", false, normalColor);
                SetTimer($"AT: {target}", false, normalColor);
                SetStatus($"{location} - Status: IN POSITION", false, normalColor);
                SetPaLine("", false, normalColor);
                return;
            }

            int phaseLeft = Mathf.CeilToInt(tm.SecondsRemainingInCurrentPhaseRealTime);
            tm.GetNextEventInfo(out PrisonEventType nextEvent, out _);
            string nextTarget = PrisonRoutineLabels.GetGoToLabel(nextEvent, _prisoner.CellIndex);
            SetHeader($"CURRENT: {PrisonRoutineLabels.FormatPhaseTitle(tm.CurrentEvent)}", false, normalColor);
            SetTimer($"TIME REMAINING: {FormatMmSs(phaseLeft)}", false, normalColor);
            if (tm.IsHighStakesTransitionWarningActive)
            {
                SetStatus($"{location} - Next: {PrisonRoutineLabels.FormatPhaseTitle(nextEvent)}", false, mandatoryWarnColor);
                SetPaLine(showPaLine ? $"{paPrefix} {nextTarget}." : "", false, mandatoryWarnColor);
            }
            else
            {
                SetStatus($"{location} - Status: Clear", false, normalColor);
                SetPaLine(showPaLine ? $"{paPrefix} {nextTarget}." : "", false, normalColor);
            }
        }

        private void SetHeader(string text, bool danger, Color? fixedColor = null)
        {
            if (headerText == null) return;
            headerText.text = text;
            headerText.color = fixedColor ?? (danger ? FlashColor(dangerColor) : normalColor);
        }

        private void SetTimer(string text, bool danger, Color? fixedColor = null)
        {
            if (timerText == null) return;
            timerText.text = text;
            timerText.color = fixedColor ?? (danger ? FlashColor(dangerColor) : normalColor);
        }

        private void SetStatus(string text, bool danger, Color? fixedColor = null)
        {
            if (statusLineText == null) return;
            statusLineText.text = text;
            statusLineText.color = fixedColor ?? (danger ? FlashColor(dangerColor) : normalColor);
        }

        private void SetPaLine(string text, bool danger, Color? fixedColor = null)
        {
            if (paLineText == null) return;
            paLineText.text = text;
            paLineText.color = fixedColor ?? (danger ? FlashColor(dangerColor) : normalColor);
        }

        private Color FlashColor(Color baseColor)
        {
            if (dangerFlashHz < 0.01f) return baseColor;
            float t = (Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f * dangerFlashHz) * 0.5f) + 0.5f;
            float a = Mathf.Lerp(dangerMinAlpha, 1f, t);
            var c = baseColor;
            c.a = a;
            return c;
        }

        private static string FormatMmSs(int totalSeconds)
        {
            int s = Mathf.Max(0, totalSeconds);
            int m = s / 60;
            int rem = s % 60;
            return $"{m:D2}:{rem:D2}";
        }

    }
}
