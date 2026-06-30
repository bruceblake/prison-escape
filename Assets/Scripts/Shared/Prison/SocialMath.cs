using System.Collections.Generic;
using UnityEngine;

namespace Prison
{
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

        /// <summary>Positive gains only: <c>rawDelta * (1 - (currentAffinity / 100)) * gainMultiplier</c>.</summary>
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

        public static float ApplyAffinityChange(float currentAffinity, float baseDelta, float affinityGainMultiplier = 1f)
        {
            float effectiveDelta = ComputeEffectiveDelta(currentAffinity, baseDelta, affinityGainMultiplier);
            return Mathf.Clamp(currentAffinity + effectiveDelta, MinAffinity, MaxAffinity);
        }

        public static ReputationTier GetReputationTier(float averageAffinity, float tier1Min = 25f, float tier2Min = 50f, float tier3Min = 75f)
        {
            if (averageAffinity >= tier3Min) return ReputationTier.Kingpin;
            if (averageAffinity >= tier2Min) return ReputationTier.Respected;
            if (averageAffinity >= tier1Min) return ReputationTier.Associate;
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
