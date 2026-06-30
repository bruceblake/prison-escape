namespace Prison
{
    /// <summary>
    /// Mandatory phases require position compliance and may grant travel grace on entry.
    /// Flexible phases (e.g. yard time) are low-stakes — late arrival to the next phase is not penalized the same way.
    /// </summary>
    public static class PrisonEventRules
    {
        public static bool IsMandatory(PrisonEventType evt) =>
            evt != PrisonEventType.FreeTime;

        public static bool IsFlexible(PrisonEventType evt) =>
            evt == PrisonEventType.FreeTime;

        /// <summary>High-stakes handoff: still relaxing, but the next block is mandatory (roll call, meals, etc.).</summary>
        public static bool IsHighStakesUpcoming(PrisonEventType current, PrisonEventType next) =>
            IsFlexible(current) && IsMandatory(next);
    }
}
