using System.Collections.Generic;
using UnityEngine;

namespace Prison
{
    /// <summary>
    /// Pure, stateless affinity and reputation math for the prison social system.
    /// Callers (e.g. a SocialManager) own the actual per-prisoner affinity state; this class only
    /// computes deltas, soft-capped gains, clamped results, and reputation tiers from inputs.
    /// </summary>
    public static class SocialMath
    {
        public const float MinAffinity = -100f;
        public const float MaxAffinity = 100f;
        public const float GreetingBase = 2f;
        public const float FavorBase = 15f;
        public const float BetrayalBase = -50f;
        public const float FavoredGiftMultiplier = 2f;
        /// <summary>When you gift the same <see cref="ItemCategory"/> twice in a row, affinity gain is multiplied by this (favored items bypass).</summary>
        public const float GiftSameCategoryRepeatMultiplier = 0.5f;

        /// <summary>True if <paramref name="giftItem"/> appears in <paramref name="personality"/>'s favored items. Null-safe.</summary>
        public static bool IsFavoredGift(NPCPersonalityData personality, ItemData giftItem)
        {
            if (personality == null || giftItem == null || personality.favoredItems == null) return false;
            return personality.favoredItems.Contains(giftItem);
        }

        /// <summary>
        /// If the player gifts the same item <see cref="ItemCategory"/> twice in a row to this NPC, the gain is halved.
        /// Favored gifts are exempt (variety rule does not apply).
        /// </summary>
        public static float ApplyGiftSameCategoryPenalty(
            float giftBaseDelta,
            ItemData giftItem,
            ItemCategory? lastGiftCategory,
            bool isFavoredGift)
        {
            if (giftItem == null) return giftBaseDelta;
            if (isFavoredGift) return giftBaseDelta;
            if (lastGiftCategory.HasValue && lastGiftCategory.Value == giftItem.category)
                return giftBaseDelta * GiftSameCategoryRepeatMultiplier;
            return giftBaseDelta;
        }

        /// <summary>
        /// Base (pre-soft-cap) affinity delta for a given <see cref="SocialActionType"/>.
        /// Gifts use <paramref name="giftBaseAmount"/> (doubled if <paramref name="giftItem"/> is favored by
        /// <paramref name="personality"/>); negative actions use the NPC's <see cref="NPCPersonalityData.betrayalPenalty"/>
        /// when a personality is supplied, otherwise <see cref="BetrayalBase"/>.
        /// </summary>
        public static float GetBaseAffinityDelta(
            SocialActionType actionType,
            NPCPersonalityData personality = null,
            ItemData giftItem = null,
            float giftBaseAmount = 5f)
        {
            switch (actionType)
            {
                case SocialActionType.Greeting:
                    return GreetingBase;
                case SocialActionType.Favor:
                    return FavorBase;
                case SocialActionType.Gift:
                    float giftDelta = giftBaseAmount;
                    if (IsFavoredGift(personality, giftItem))
                    {
                        giftDelta *= FavoredGiftMultiplier;
                    }
                    return giftDelta;
                case SocialActionType.Betrayal:
                case SocialActionType.Theft:
                case SocialActionType.Snitch:
                    return personality != null ? (float)personality.betrayalPenalty : BetrayalBase;
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// Applies the positive-only soft cap: as <paramref name="currentAffinity"/> approaches <see cref="MaxAffinity"/>,
        /// positive deltas shrink toward zero (<c>baseDelta * (1 - currentAffinity / MaxAffinity) * affinityGainMultiplier</c>).
        /// Non-positive <paramref name="baseDelta"/> values (losses) bypass the cap entirely and are returned unchanged.
        /// <paramref name="affinityGainMultiplier"/> is clamped to a minimum of 0 (cannot invert a gain into a loss).
        /// </summary>
        public static float ComputeEffectiveDelta(float currentAffinity, float baseDelta, float affinityGainMultiplier = 1f)
        {
            if (baseDelta <= 0f)
            {
                return baseDelta;
            }

            float normalizedAffinity = Mathf.Clamp(currentAffinity, MinAffinity, MaxAffinity) / MaxAffinity;
            float softCapFactor = Mathf.Clamp01(1f - normalizedAffinity);
            return baseDelta * Mathf.Max(0f, affinityGainMultiplier) * softCapFactor;
        }

        /// <summary>Applies <see cref="ComputeEffectiveDelta"/> to <paramref name="currentAffinity"/> and clamps the result to <see cref="MinAffinity"/>/<see cref="MaxAffinity"/>.</summary>
        public static float ApplyAffinityChange(float currentAffinity, float baseDelta, float affinityGainMultiplier = 1f)
        {
            float effectiveDelta = ComputeEffectiveDelta(currentAffinity, baseDelta, affinityGainMultiplier);
            return Mathf.Clamp(currentAffinity + effectiveDelta, MinAffinity, MaxAffinity);
        }

        /// <summary>Maps an average affinity value to a <see cref="ReputationTier"/> using ascending thresholds (each min is inclusive).</summary>
        public static ReputationTier GetReputationTier(float averageAffinity, float associateMin = 25f, float respectedMin = 50f, float kingpinMin = 75f)
        {
            if (averageAffinity >= kingpinMin) return ReputationTier.Kingpin;
            if (averageAffinity >= respectedMin) return ReputationTier.Respected;
            if (averageAffinity >= associateMin) return ReputationTier.Associate;
            return ReputationTier.Outsider;
        }

        /// <summary>Simple mean of all registered cell affinities (empty = 0).</summary>
        public static float AverageRegisteredAffinities(IReadOnlyDictionary<int, float> prisonerAffinity)
        {
            if (prisonerAffinity == null || prisonerAffinity.Count == 0) return 0f;
            float sum = 0f;
            foreach (var kv in prisonerAffinity)
                sum += kv.Value;
            return sum / prisonerAffinity.Count;
        }
    }
}
