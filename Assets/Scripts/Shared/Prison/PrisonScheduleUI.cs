using UnityEngine;
using TMPro;

namespace Prison
{
    /// <summary>
    /// Displays prison schedule info: current time, current event, time until next event.
    /// Add to a Canvas and assign the text fields.
    /// </summary>
    public class PrisonScheduleUI : MonoBehaviour
    {
        [Header("Time Display")]
        public TMP_Text timeText;
        [Tooltip("Format: {0}=hours, {1}=minutes. E.g. \"{0:D2}:{1:D2}\" for 24h")]
        public string timeFormat = "{0:D2}:{1:D2}";

        [Header("Event Display")]
        public TMP_Text currentEventText;
        public TMP_Text nextEventText;
        public TMP_Text timeUntilNextText;

        [Header("Labels (optional)")]
        public string currentEventLabel = "Current: ";
        public string nextEventLabel = "Next: ";
        public string timeUntilNextLabel = "In: ";

        [Header("Compliance (optional)")]
        [Tooltip("Shows if player is in the correct location for current event")]
        public TMP_Text complianceText;
        public string compliantLabel = "Compliant";
        public string nonCompliantLabel = "Not compliant - return to area!";
        [Tooltip("Shown while travel grace is active (same object can be complianceText if you leave others null).")]
        public string graceCompliantLabel = "Travel grace — move to the new area (not penalized yet)";

        [Header("Phase warning (optional)")]
        [Tooltip("Shows a countdown before the current phase ends / next rules apply.")]
        public TMP_Text phaseWarningText;
        [Tooltip("Show phaseWarningText when this many real-time seconds or fewer remain in the current phase (0 = always show countdown in phaseWarningText).")]
        public float warningLeadRealSeconds = 75f;
        [Tooltip("Hide warning text when above the lead threshold and not in grace.")]
        public bool hideWarningWhenInactive = true;
        public string phaseWarningFormat = "Next: {0} in {1}s — get in position!";
        public string graceCountdownFormat = "Travel grace: {0}s";

        private void Update()
        {
            if (PrisonTimeManager.Instance == null) return;

            var tm = PrisonTimeManager.Instance;
            float minutes = tm.CurrentTimeMinutes;
            float remaining = tm.EventTimeRemaining;

            if (timeText != null)
            {
                int hours = Mathf.FloorToInt(minutes / 60f) % 24;
                int mins = Mathf.FloorToInt(minutes % 60f);
                timeText.text = string.Format(timeFormat, hours, mins);
            }

            if (currentEventText != null)
            {
                currentEventText.text = currentEventLabel + FormatEvent(tm.CurrentEvent);
            }

            if (timeUntilNextText != null)
            {
                float realSec = tm.SecondsRemainingInCurrentPhaseRealTime;
                int remMins = Mathf.Max(1, Mathf.CeilToInt(remaining));
                int secCount = Mathf.Max(0, Mathf.CeilToInt(realSec));
                timeUntilNextText.text = timeUntilNextLabel + $"{remMins} min (~{secCount}s)";
            }

            if (nextEventText != null && tm.schedule != null && tm.schedule.entries != null && tm.schedule.entries.Length > 0)
            {
                tm.GetNextEventInfo(out PrisonEventType nextEvt, out _);
                nextEventText.text = nextEventLabel + FormatEvent(nextEvt);
            }

            if (complianceText != null)
            {
                var prisoner = FindFirstObjectByType<PrisonerController>(FindObjectsInactive.Exclude);
                if (prisoner == null)
                    complianceText.text = "";
                else if (tm.IsMandatoryTravelGraceActive && !prisoner.IsAtRequiredLocation)
                    complianceText.text = graceCompliantLabel + $" ({Mathf.CeilToInt(tm.ComplianceGraceSecondsRemaining)}s)";
                else if (tm.IsMorningRollCallShakedownGateActive)
                {
                    var tracker = MorningRollCallTracker.Instance;
                    if (tracker != null && tracker.IsInmateShakedownComplete(prisoner))
                        complianceText.text = "Cleared — shakedown complete";
                    else
                    {
                        int total = 0;
                        int ok = tracker != null
                            ? tracker.CountInmatesShakedownComplete(out total)
                            : 0;
                        if (total < 1)
                            total = 1;
                        complianceText.text = $"Shakedown: {ok}/{total}";
                    }
                }
                else
                    complianceText.text = prisoner.IsCompliant ? compliantLabel : nonCompliantLabel;
            }

            if (phaseWarningText != null)
            {
                float phaseLeft = tm.SecondsRemainingInCurrentPhaseRealTime;
                tm.GetNextEventInfo(out PrisonEventType warnNext, out _);
                bool showGrace = tm.IsMandatoryTravelGraceActive;
                bool showPhase = warningLeadRealSeconds <= 0.01f || phaseLeft <= warningLeadRealSeconds;
                if (showGrace)
                {
                    int g = Mathf.CeilToInt(tm.ComplianceGraceSecondsRemaining);
                    phaseWarningText.text = string.Format(graceCountdownFormat, g);
                    phaseWarningText.enabled = true;
                }
                else if (showPhase)
                {
                    int s = Mathf.CeilToInt(phaseLeft);
                    phaseWarningText.text = string.Format(phaseWarningFormat, FormatEvent(warnNext), s);
                    phaseWarningText.enabled = true;
                }
                else if (hideWarningWhenInactive)
                {
                    phaseWarningText.enabled = false;
                }
                else
                {
                    phaseWarningText.text = "";
                    phaseWarningText.enabled = true;
                }
            }
        }

        private string FormatEvent(PrisonEventType evt)
        {
            return PrisonRoutineLabels.FormatPhaseTitle(evt, uppercase: false);
        }
    }
}
