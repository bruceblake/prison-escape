using System;

namespace Prison.Career
{
    /// <summary>
    /// Career Respect: a simple 0–100 counter owned by the transfer system until Social v2 lands
    /// (then it becomes the seed/output of that system's Respect axis — same save field).
    /// Spec: docs/PrisonEscape/01 Game Design/Prison Career Ladder.md § Career Respect.
    /// </summary>
    public static class CareerRespectMath
    {
        public const float Min = 0f;
        public const float Max = 100f;

        /// <summary>Escape a facility: +8 + 2×tier.</summary>
        public static float EscapeAward(int tier) => 8f + 2f * Math.Max(0, tier);

        /// <summary>Serve out the County sentence: +5.</summary>
        public const float SentenceServedAward = 5f;

        /// <summary>Each full day survived at tier ≥ 4 (Federal facilities): +0.5.</summary>
        public static float DailySurvivalAward(int tier) => tier >= 4 ? 0.5f : 0f;

        /// <summary>Caught escaping (solitary): −2.</summary>
        public const float CaughtEscapingPenalty = -2f;

        public static float Clamp(float respect) =>
            respect < Min ? Min : (respect > Max ? Max : respect);

        /// <summary>
        /// Arrival treatment: how a fresh facility population is seeded toward you.
        /// Respect &lt; 25 → baseline; 25–50 → +10; 50–75 → +20; 75+ → +30 (fame cuts both ways —
        /// the top band also makes guards prioritize shaking *you* down; flagged tunable).
        /// </summary>
        public static float ArrivalAffinitySeed(float respect)
        {
            if (respect >= 75f) return 30f;
            if (respect >= 50f) return 20f;
            if (respect >= 25f) return 10f;
            return 0f;
        }
    }
}
