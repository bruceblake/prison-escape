using UnityEngine;

namespace Prison.Social
{
    /// <summary>
    /// Pure trade pricing (spec §8):
    /// price = base value × greed factor (0.8–1.5) × trust discount (up to −25% at trust ≥ 75)
    ///         × gang modifier (member 0.85) × contraband markup ×2.
    /// Rival trade refusal below gang standing −25 is gated by <see cref="GangManager.RefusesTrade"/>.
    /// </summary>
    public static class TradeMath
    {
        public static float GreedFactor(int greed) =>
            Mathf.Lerp(SocialTuning.GreedPriceMin, SocialTuning.GreedPriceMax, Mathf.Clamp01(greed / 100f));

        /// <summary>1.0 at trust ≤ 0 down to 0.75 at trust ≥ 75.</summary>
        public static float TrustDiscountFactor(float trust)
        {
            float t = Mathf.Clamp01(Mathf.Max(0f, trust) / SocialTuning.TrustDiscountFullAt);
            return 1f - SocialTuning.TrustDiscountMax * t;
        }

        public static float BuyPrice(float baseValue, int sellerGreed, float trustTowardPlayer,
            bool playerIsMemberOfSellerGang, bool isContraband)
        {
            float price = baseValue
                * GreedFactor(sellerGreed)
                * TrustDiscountFactor(trustTowardPlayer)
                * (playerIsMemberOfSellerGang ? SocialTuning.MemberTradePriceFactor : 1f)
                * (isContraband ? SocialTuning.ContrabandMarkup : 1f);
            return Mathf.Max(1f, Mathf.Round(price));
        }

        /// <summary>
        /// What an NPC pays the player for loot: greedier buyers pay less
        /// (50% of value at greed 0 down to 30% at greed 100); contraband fetches ×1.5.
        /// </summary>
        public static float SellPrice(float baseValue, int buyerGreed, bool isContraband)
        {
            float cut = Mathf.Lerp(0.5f, 0.3f, Mathf.Clamp01(buyerGreed / 100f));
            float price = baseValue * cut * (isContraband ? 1.5f : 1f);
            return Mathf.Max(1f, Mathf.Round(price));
        }

        /// <summary>Item value fallback by rarity when the asset has no explicit base value.</summary>
        public static float EffectiveBaseValue(ItemData item)
        {
            if (item == null) return 0f;
            if (item.baseValue > 0f) return item.baseValue;
            switch (item.rarity)
            {
                case ItemRarity.Uncommon: return 15f;
                case ItemRarity.Rare: return 30f;
                case ItemRarity.Legendary: return 60f;
                default: return 8f;
            }
        }
    }
}
