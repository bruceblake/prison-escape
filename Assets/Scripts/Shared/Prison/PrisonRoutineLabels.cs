using UnityEngine;

namespace Prison
{
    /// <summary>Shared display strings for routine / compliance HUD widgets.</summary>
    public static class PrisonRoutineLabels
    {
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
                    if (reg != null)
                        return reg.GetCellHudLabel(cellIndex);
                    return $"CELL {cellIndex}";

                case PrisonEventType.FreeTime:
                    if (reg?.GetYard() != null)
                        return reg.GetYard().GetHudLabel();
                    return "YARD";

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
