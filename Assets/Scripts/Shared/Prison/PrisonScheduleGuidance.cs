using UnityEngine;

namespace Prison
{
    /// <summary>
    /// Plain-language schedule guidance: what the player may do now vs what they must do next.
    /// </summary>
    public static class PrisonScheduleGuidance
    {
        public static string GetAllowedNowLabel(PrisonTimeManager tm, PrisonerController prisoner)
        {
            if (tm == null)
                return "—";

            switch (tm.CurrentEvent)
            {
                case PrisonEventType.FreeTime:
                    return "Yard, cafeteria, or your cell";
                case PrisonEventType.MorningRollCall:
                case PrisonEventType.RollCall:
                    if (prisoner != null && MorningRollCallTracker.IsInmateReleasedFromRollCallStand(prisoner))
                        return "Travel to next location";
                    return "Stay in your cell";
                case PrisonEventType.Breakfast:
                case PrisonEventType.Lunch:
                case PrisonEventType.Dinner:
                    return "Cafeteria only";
                case PrisonEventType.LightsOut:
                case PrisonEventType.NightRollCall:
                    return "Your cell only";
                case PrisonEventType.MiddayCount:
                case PrisonEventType.EveningCount:
                    return "Return to your cell for count";
                case PrisonEventType.WorkProgram:
                    return "Workshop only";
                default:
                    return PrisonRoutineLabels.FormatPhaseTitle(tm.CurrentEvent, uppercase: false);
            }
        }

        public static string GetRequiredNextLabel(PrisonTimeManager tm, PrisonerController prisoner, string nextGoTo)
        {
            if (tm == null)
                return string.Empty;

            tm.GetNextEventInfo(out PrisonEventType nextEvent, out _);
            string phase = PrisonRoutineLabels.FormatPhaseTitle(nextEvent);

            if (tm.IsHighStakesTransitionWarningActive || tm.IsMandatoryTravelGraceActive)
            {
                if (!string.IsNullOrEmpty(nextGoTo))
                    return $"{phase} — go to {nextGoTo}";
            }

            if (tm.CurrentEvent == PrisonEventType.FreeTime)
            {
                if (!string.IsNullOrEmpty(nextGoTo))
                    return $"Next: {phase} at {nextGoTo}";
                return $"Next: {phase}";
            }

            if (prisoner != null && !prisoner.IsCompliant && !string.IsNullOrEmpty(nextGoTo))
                return $"Required: {nextGoTo}";

            return string.Empty;
        }

        public static string BuildStatusLine(
            PrisonTimeManager tm,
            PrisonerController prisoner,
            RoutineNowNextBarUI.RoutineBarVisualState state,
            string nextGoTo)
        {
            if (tm == null)
                return string.Empty;

            string allowed = GetAllowedNowLabel(tm, prisoner);
            string required = GetRequiredNextLabel(tm, prisoner, nextGoTo);

            if (state == RoutineNowNextBarUI.RoutineBarVisualState.Enforcement)
                return "OUT OF POSITION — move now";

            if (state == RoutineNowNextBarUI.RoutineBarVisualState.TravelGrace)
            {
                if (prisoner != null && prisoner.IsCompliant)
                    return $"OK: {allowed}";
                return $"GO: {nextGoTo}";
            }

            if (state == RoutineNowNextBarUI.RoutineBarVisualState.MandatoryWarning)
            {
                if (!string.IsNullOrEmpty(required))
                    return $"PREPARE — {required}";
                return "PREPARE for next phase";
            }

            if (tm.IsMorningRollCallShakedownGateActive)
                return string.Empty;

            if (prisoner != null && !prisoner.IsCompliant)
                return $"GO: {PrisonRoutineLabels.GetGoToLabel(tm.CurrentEvent, prisoner.CellIndex)}";

            if (!string.IsNullOrEmpty(required) && tm.CurrentEvent == PrisonEventType.FreeTime)
                return $"OK: {allowed}";

            if (!string.IsNullOrEmpty(allowed))
                return $"OK: {allowed}";

            return "IN POSITION";
        }

        public static bool ShouldAlwaysShowTravelPath(PrisonTimeManager tm)
        {
            if (tm == null) return false;
            return tm.CurrentEvent == PrisonEventType.FreeTime
                || tm.IsHighStakesTransitionWarningActive
                || tm.IsMandatoryTravelGraceActive;
        }
    }
}
