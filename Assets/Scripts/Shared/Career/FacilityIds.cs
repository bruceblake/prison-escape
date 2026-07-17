namespace Prison.Career
{
    /// <summary>
    /// Canonical facility ids for the career ladder. Ids are permanent save-file keys;
    /// display titles live in <see cref="FacilityCatalog"/> and may be renamed freely.
    /// Spec: docs/PrisonEscape/01 Game Design/Prison Career Ladder.md § Facility catalog.
    /// </summary>
    public static class FacilityIds
    {
        /// <summary>Layout/tooling/playtest prison — visible in dev builds, not on the career path.</summary>
        public const string DevSandbox = "dev_sandbox";

        public const string County = "county";
        public const string StateMin = "state_min";
        public const string StateMed = "state_med";
        public const string StateMax = "state_max";
        public const string FedCamp = "fed_camp";
        public const string FedLow = "fed_low";
        public const string FedMed = "fed_med";
        public const string FedHigh = "fed_high";
        public const string FedAdx = "fed_adx";

        /// <summary>The career ladder in strictly increasing difficulty (index 0–8).</summary>
        public static readonly string[] LadderOrder =
        {
            County, StateMin, StateMed, StateMax,
            FedCamp, FedLow, FedMed, FedHigh, FedAdx,
        };

        /// <summary>Ladder index (tier) of a facility, or -1 when not on the career path (dev sandbox, unknown).</summary>
        public static int LadderIndexOf(string facilityId)
        {
            for (int i = 0; i < LadderOrder.Length; i++)
                if (LadderOrder[i] == facilityId)
                    return i;
            return -1;
        }

        public static bool IsOnLadder(string facilityId) => LadderIndexOf(facilityId) >= 0;

        /// <summary>Escaping the top facility is the career win, not a transfer.</summary>
        public static bool IsTopOfLadder(string facilityId) =>
            LadderIndexOf(facilityId) == LadderOrder.Length - 1;

        /// <summary>The facility you are sentenced to after escaping <paramref name="facilityId"/>, or null at the top.</summary>
        public static string NextOnLadder(string facilityId)
        {
            int idx = LadderIndexOf(facilityId);
            if (idx < 0 || idx >= LadderOrder.Length - 1) return null;
            return LadderOrder[idx + 1];
        }
    }
}
