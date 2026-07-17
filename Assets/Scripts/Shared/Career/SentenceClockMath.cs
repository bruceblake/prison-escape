namespace Prison.Career
{
    /// <summary>
    /// County sentence clock math. Day indices are 1-based and increment at each Morning Count,
    /// so the Morning Count that starts day N means N−1 full days served: with the default
    /// 7-day sentence, day 7's count does not transfer (6 served) and day 8's count does.
    /// Spec: docs/PrisonEscape/02 Features/Facility Transfer & Graduation.md § Trigger 2.
    /// </summary>
    public static class SentenceClockMath
    {
        /// <summary>Full days served once the Morning Count of <paramref name="dayIndex"/> has fired.</summary>
        public static int DaysServed(int dayIndex) => dayIndex <= 1 ? 0 : dayIndex - 1;

        /// <summary>True at the Morning Count that completes the sentence (never for facilities without a clock).</summary>
        public static bool ShouldTransferAtMorningCount(int dayIndex, int sentenceDays) =>
            sentenceDays > 0 && DaysServed(dayIndex) >= sentenceDays;

        /// <summary>"Days served: N / 7" HUD line, clamped so the ceremony frame never shows 8/7.</summary>
        public static string HudLine(int dayIndex, int sentenceDays)
        {
            int served = DaysServed(dayIndex);
            if (sentenceDays > 0 && served > sentenceDays) served = sentenceDays;
            return $"Days served: {served} / {sentenceDays}";
        }
    }
}
