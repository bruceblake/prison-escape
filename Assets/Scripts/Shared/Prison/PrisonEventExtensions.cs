namespace Prison
{
    public static class PrisonEventExtensions
    {
        public static bool IsMorningLineUp(PrisonEventType t) =>
            t == PrisonEventType.RollCall || t == PrisonEventType.MorningRollCall;

        public static bool IsNightBedPhase(PrisonEventType t) =>
            t == PrisonEventType.NightRollCall || t == PrisonEventType.LightsOut;
    }
}
