using UnityEngine;

namespace Prison
{
    /// <summary>
    /// Pure math for the player's mental health / strength stats (EditMode-testable).
    /// Spec: docs/PrisonEscape/02 Features/Escape Completion System.md
    /// </summary>
    public static class PlayerStatsMath
    {
        public const float MinStat = 0f;
        public const float MaxStat = 100f;

        public const float SolitaryMentalHealthPenalty = 20f;
        public const float SolitaryStrengthPenalty = 10f;
        public const float DailyRegenAmount = 5f;

        public const float LowStrengthThreshold = 50f;
        public const float NormalSprintMultiplier = 2f;
        public const float WeakSprintMultiplier = 1.5f;

        public static float Clamp(float value) => Mathf.Clamp(value, MinStat, MaxStat);

        public static float ApplySolitaryToMentalHealth(float current) =>
            Clamp(current - SolitaryMentalHealthPenalty);

        public static float ApplySolitaryToStrength(float current) =>
            Clamp(current - SolitaryStrengthPenalty);

        public static float ApplyDailyRegen(float current) =>
            Clamp(current + DailyRegenAmount);

        /// <summary>Sprint slows down when strength drops below the threshold.</summary>
        public static float SprintMultiplierFor(float strength) =>
            strength < LowStrengthThreshold ? WeakSprintMultiplier : NormalSprintMultiplier;
    }
}
