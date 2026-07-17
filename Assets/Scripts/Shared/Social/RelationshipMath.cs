using UnityEngine;

namespace Prison.Social
{
    /// <summary>
    /// Pure relationship math for the v3 social ecosystem: Standing formula, band mapping,
    /// the delta modifier pipeline (personality → gang factor → positive soft cap → clamp),
    /// and the prison-wide reputation tier. The positive soft-cap curve is carried over from v1.
    /// </summary>
    public static class RelationshipMath
    {
        public static float Standing(in RelationshipRecord record) =>
            Standing(record.trust, record.respect);

        public static float Standing(float trust, float respect) =>
            SocialTuning.StandingTrustWeight * trust + SocialTuning.StandingRespectWeight * respect;

        public static StandingBand GetBand(float standing)
        {
            if (standing >= SocialTuning.ConfidantMin) return StandingBand.Confidant;
            if (standing >= SocialTuning.AllyMin) return StandingBand.Ally;
            if (standing >= SocialTuning.FriendlyMin) return StandingBand.Friendly;
            if (standing <= SocialTuning.EnemyMax) return StandingBand.Enemy;
            if (standing <= SocialTuning.HostileMax) return StandingBand.Hostile;
            return StandingBand.Neutral;
        }

        public static bool IsFriendBand(StandingBand band) =>
            band == StandingBand.Friendly || band == StandingBand.Ally || band == StandingBand.Confidant;

        public static bool IsEnemyBand(StandingBand band) =>
            band == StandingBand.Hostile || band == StandingBand.Enemy;

        /// <summary>
        /// Positive soft cap kept from v1: gains shrink as the axis approaches +100
        /// (<c>delta × (1 − current/100)</c>). Losses are never capped.
        /// </summary>
        public static float ApplyPositiveSoftCap(float current, float delta)
        {
            if (delta <= 0f) return delta;
            float normalized = Mathf.Clamp(current, SocialTuning.MinAxis, SocialTuning.MaxAxis) / SocialTuning.MaxAxis;
            return delta * Mathf.Clamp01(1f - normalized);
        }

        /// <summary>
        /// Full modifier pipeline for one axis, in spec order:
        /// personality (sociability scales positive trust; loyalty scales betrayal penalties)
        /// → gang factor (propagation ×0.5 / ×1.0; 1 for direct actions)
        /// → positive soft cap → caller clamps via <see cref="Apply"/>.
        /// </summary>
        public static float ComputeEffectiveDelta(
            float currentValue,
            float baseDelta,
            bool isTrustAxis,
            in PersonalityTraits observerTraits,
            bool isBetrayalClass = false,
            float gangFactor = 1f)
        {
            float d = baseDelta;
            if (isTrustAxis && d > 0f)
                d *= SocialTuning.SociabilityTrustFactor(observerTraits.sociability);
            if (isBetrayalClass && d < 0f)
                d *= SocialTuning.LoyaltyBetrayalFactor(observerTraits.loyalty);
            d *= gangFactor;
            d = ApplyPositiveSoftCap(currentValue, d);
            return d;
        }

        /// <summary>Adds an already-effective delta and clamps to [−100, +100].</summary>
        public static float Apply(float currentValue, float effectiveDelta) =>
            Mathf.Clamp(currentValue + effectiveDelta, SocialTuning.MinAxis, SocialTuning.MaxAxis);

        /// <summary>Reputation tier from average Standing across known inmates + gang rank bonus (v1 tier names kept).</summary>
        public static ReputationTier ComputeTier(float averageStanding, GangRank playerRank)
        {
            float score = averageStanding + TierBonus(playerRank);
            if (score >= SocialTuning.KingpinTierMin) return ReputationTier.Kingpin;
            if (score >= SocialTuning.RespectedTierMin) return ReputationTier.Respected;
            if (score >= SocialTuning.AssociateTierMin) return ReputationTier.Associate;
            return ReputationTier.Outsider;
        }

        public static float TierBonus(GangRank rank)
        {
            switch (rank)
            {
                case GangRank.Associate: return SocialTuning.TierBonusAssociate;
                case GangRank.Member: return SocialTuning.TierBonusMember;
                case GangRank.Trusted: return SocialTuning.TierBonusTrusted;
                default: return 0f;
            }
        }

        /// <summary>
        /// Intimidation roll: your respect with them + Strength stat vs their nerve.
        /// Returns success chance in [0.05, 0.95].
        /// </summary>
        public static float IntimidationChance(float respectTowardPlayer, float playerStrength, int targetNerve)
        {
            float chance = 0.5f
                + respectTowardPlayer / 200f
                + playerStrength / 400f
                - targetNerve / 200f;
            return Mathf.Clamp(chance, 0.05f, 0.95f);
        }
    }
}
