namespace Prison
{
    public static class PrisonEventExtensions
    {
        public static bool IsMorningLineUp(PrisonEventType t) =>
            t == PrisonEventType.RollCall || t == PrisonEventType.MorningRollCall;

        public static bool IsNightBedPhase(PrisonEventType t) =>
            t == PrisonEventType.NightRollCall || t == PrisonEventType.LightsOut;

        /// <summary>Presence-only counts held in the inmate's cell (no shakedown sweep, doors stay open).</summary>
        public static bool IsCellCountPhase(PrisonEventType t) =>
            t == PrisonEventType.MiddayCount || t == PrisonEventType.EveningCount;

        /// <summary>Any of the day's formal headcounts: morning line-up, midday/evening cell counts, night bed check.</summary>
        public static bool IsFormalCount(PrisonEventType t) =>
            IsMorningLineUp(t) || IsCellCountPhase(t) || t == PrisonEventType.NightRollCall;
    }
}
