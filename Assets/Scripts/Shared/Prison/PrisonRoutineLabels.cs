using UnityEngine;

namespace Prison
{
    /// <summary>Shared display strings for routine / compliance HUD widgets.</summary>
    public static class PrisonRoutineLabels
    {
        /// <summary>Colour intent for the single routine instruction line.</summary>
        public enum RoutineInstructionTone { InPosition, FreeRoam, MustMove, Wait, Enforcement }

        /// <summary>One plain-language directive plus the tone the HUD should colour it.</summary>
        public struct RoutineInstruction
        {
            public string Text;
            public RoutineInstructionTone Tone;
        }

        /// <summary>
        /// The single, plain-language "what do I do right now" line for the routine bar.
        /// Built from the same objective the waypoint uses, so the two never disagree.
        /// </summary>
        public static RoutineInstruction GetInstruction(PrisonTimeManager tm, PrisonerController prisoner)
        {
            if (tm == null || prisoner == null)
                return new RoutineInstruction { Text = string.Empty, Tone = RoutineInstructionTone.InPosition };

            var obj = PrisonRoutineDestination.ResolveActiveDestination(tm, prisoner);

            // Morning roll call before this cell is cleared: wait in the cell.
            if (tm.IsMorningRollCallShakedownGateActive)
            {
                bool released = MorningRollCallTracker.Instance != null
                    && MorningRollCallTracker.Instance.IsInmateShakedownComplete(prisoner);
                if (!released)
                    return new RoutineInstruction { Text = "Wait in your cell for roll call", Tone = RoutineInstructionTone.Wait };
            }

            // Nowhere you must be (free time, no upcoming mandatory warning).
            if (!obj.Show)
            {
                if (!PrisonEventRules.IsMandatory(tm.CurrentEvent))
                    return new RoutineInstruction { Text = "Free time — go anywhere", Tone = RoutineInstructionTone.FreeRoam };
                return new RoutineInstruction { Text = "You're in the right place", Tone = RoutineInstructionTone.InPosition };
            }

            string where = DirectiveFor(obj);

            // Non-compliant during a mandatory phase (no grace) reads as enforcement.
            bool enforcement = !tm.IsMandatoryTravelGraceActive
                && PrisonEventRules.IsMandatory(tm.CurrentEvent)
                && !prisoner.IsCompliant
                && !MorningRollCallTracker.IsInmateReleasedFromRollCallStand(prisoner)
                && !tm.IsMorningRollCallShakedownGateActive;
            if (enforcement)
                return new RoutineInstruction { Text = $"Out of position — {where} now", Tone = RoutineInstructionTone.Enforcement };

            return new RoutineInstruction { Text = CapitalizeFirst(where), Tone = RoutineInstructionTone.MustMove };
        }

        private static string DirectiveFor(PrisonRoutineDestination.RoutineObjective obj)
        {
            if (obj.InCell)
            {
                if (obj.Event is PrisonEventType.MiddayCount or PrisonEventType.EveningCount)
                    return "return to your cell for count";
                if (obj.Event is PrisonEventType.LightsOut or PrisonEventType.NightRollCall)
                    return "get to your bunk";
                return "wait in your cell for roll call";
            }
            return $"go to the {FriendlyVenue(obj.Label)}";
        }

        private static string FriendlyVenue(string rawLabel)
        {
            if (string.IsNullOrEmpty(rawLabel)) return "meeting point";
            string t = rawLabel.ToLowerInvariant();
            if (t.Contains("cafeteria")) return "Cafeteria";
            if (t.Contains("yard") || t.Contains("courtyard")) return "Yard";
            if (t.Contains("workshop")) return "Workshop";
            if (t.Contains("shower")) return "Showers";
            return CapitalizeFirst(t);
        }

        private static string CapitalizeFirst(string s)
            => string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);

        public static string FormatPhaseTitle(PrisonEventType evt, bool uppercase = true)
        {
            string t = evt switch
            {
                PrisonEventType.MorningRollCall => "Morning Roll Call",
                PrisonEventType.NightRollCall => "Night Roll Call",
                PrisonEventType.RollCall => "Roll Call",
                PrisonEventType.Breakfast => "Breakfast",
                PrisonEventType.Lunch => "Lunch",
                PrisonEventType.Dinner => "Dinner",
                PrisonEventType.FreeTime => "Free Time",
                PrisonEventType.LightsOut => "Lights Out",
                PrisonEventType.WorkProgram => "Work / Programs",
                PrisonEventType.MiddayCount => "Midday Count",
                PrisonEventType.EveningCount => "Evening Count",
                _ => evt.ToString()
            };
            return uppercase ? t.ToUpperInvariant() : t;
        }

        /// <summary>Destination for travel grace / non-compliant prompts (no LOC: prefix).</summary>
        public static string GetGoToLabel(PrisonEventType evt, int cellIndex)
        {
            var reg = PrisonLocationRegistry.Instance;
            switch (evt)
            {
                case PrisonEventType.MorningRollCall:
                case PrisonEventType.RollCall:
                    if (reg != null)
                    {
                        string cellBlock = reg.GetCellHudLabel(cellIndex);
                        if (!string.IsNullOrEmpty(cellBlock))
                            return cellBlock;
                        if (reg.GetRollCallArea() != null)
                            return reg.GetRollCallArea().GetHudLabel();
                    }
                    return "ROLL CALL AREA";

                case PrisonEventType.Breakfast:
                case PrisonEventType.Lunch:
                case PrisonEventType.Dinner:
                    if (reg?.GetCafeteria() != null)
                        return reg.GetCafeteria().GetHudLabel();
                    return "CAFETERIA";

                case PrisonEventType.NightRollCall:
                case PrisonEventType.LightsOut:
                case PrisonEventType.MiddayCount:
                case PrisonEventType.EveningCount:
                    if (reg != null)
                        return reg.GetCellHudLabel(cellIndex);
                    return $"CELL {cellIndex}";

                case PrisonEventType.FreeTime:
                    if (reg?.GetYard() != null)
                        return reg.GetYard().GetHudLabel();
                    return "YARD";

                case PrisonEventType.WorkProgram:
                    if (reg?.GetWorkshop() != null)
                        return reg.GetWorkshop().GetHudLabel();
                    return "WORKSHOP";

                default:
                    return evt.ToString().ToUpperInvariant();
            }
        }

        /// <summary>
        /// Destination shown on the HUD during morning roll call before shakedown marks this cell clear — not the next meal venue.
        /// </summary>
        public static string GetMorningRollCallLineUpDestinationLabel(int cellIndex)
        {
            var reg = PrisonLocationRegistry.Instance;
            if (reg != null && reg.GetRollCallArea() != null)
            {
                string area = reg.GetRollCallArea().GetHudLabel();
                return string.IsNullOrEmpty(area) ? "WAIT IN YOUR CELL" : $"{area} | WAIT IN CELL";
            }

            string cell = reg != null ? reg.GetCellHudLabel(cellIndex) : $"CELL {cellIndex}";
            return string.IsNullOrEmpty(cell)
                ? "WAIT IN YOUR CELL"
                : $"WAIT IN CELL ({cell})";
        }

        /// <summary>Strips leading "LOC: " from <see cref="PrisonerController.GetCurrentLocationLabel"/>.</summary>
        public static string FormatPlayerLocation(string locationLabelFromPrisoner)
        {
            if (string.IsNullOrEmpty(locationLabelFromPrisoner))
                return "—";
            const string prefix = "LOC: ";
            if (locationLabelFromPrisoner.StartsWith(prefix))
                return locationLabelFromPrisoner.Substring(prefix.Length);
            return locationLabelFromPrisoner;
        }
    }
}
