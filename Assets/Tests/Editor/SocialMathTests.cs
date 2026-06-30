using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Prison;

namespace Prison.Tests
{
    /// <summary>
    /// EditMode unit tests for <see cref="SocialMath"/> — the pure affinity / reputation model
    /// behind the social system. Covers base deltas, favored-gift and same-category rules,
    /// the positive-only soft cap, clamping, reputation tiers and average computation.
    /// </summary>
    public class SocialMathTests
    {
        private const float Tol = 1e-4f;
        private readonly List<Object> _created = new List<Object>();

        private NPCPersonalityData NewPersonality(float gainMultiplier = 1f, int betrayalPenalty = -50)
        {
            var p = ScriptableObject.CreateInstance<NPCPersonalityData>();
            p.affinityGainMultiplier = gainMultiplier;
            p.betrayalPenalty = betrayalPenalty;
            p.favoredItems = new List<ItemData>();
            _created.Add(p);
            return p;
        }

        private ItemData NewItem(string itemName, ItemCategory category = ItemCategory.CraftingPart)
        {
            var i = ScriptableObject.CreateInstance<ItemData>();
            i.itemName = itemName;
            i.category = category;
            _created.Add(i);
            return i;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in _created)
                if (o != null) Object.DestroyImmediate(o);
            _created.Clear();
        }

        // -------------------- Base affinity deltas --------------------
        [Test]
        public void GetBaseAffinityDelta_Greeting_IsGreetingBase()
            => Assert.AreEqual(SocialMath.GreetingBase, SocialMath.GetBaseAffinityDelta(SocialActionType.Greeting), Tol);

        [Test]
        public void GetBaseAffinityDelta_Favor_IsFavorBase()
            => Assert.AreEqual(SocialMath.FavorBase, SocialMath.GetBaseAffinityDelta(SocialActionType.Favor), Tol);

        [Test]
        public void GetBaseAffinityDelta_GiftDefault_IsGiftBaseAmount()
            => Assert.AreEqual(5f, SocialMath.GetBaseAffinityDelta(SocialActionType.Gift, null, null, 5f), Tol);

        [Test]
        public void GetBaseAffinityDelta_FavoredGift_IsDoubled()
        {
            var item = NewItem("Cigarettes");
            var pers = NewPersonality();
            pers.favoredItems.Add(item);
            Assert.AreEqual(10f, SocialMath.GetBaseAffinityDelta(SocialActionType.Gift, pers, item, 5f), Tol);
        }

        [Test]
        public void GetBaseAffinityDelta_FavoredGift_UsesCustomBaseTimesMultiplier()
        {
            var item = NewItem("Cigarettes");
            var pers = NewPersonality();
            pers.favoredItems.Add(item);
            Assert.AreEqual(14f, SocialMath.GetBaseAffinityDelta(SocialActionType.Gift, pers, item, 7f), Tol);
        }

        [TestCase(SocialActionType.Betrayal)]
        [TestCase(SocialActionType.Theft)]
        [TestCase(SocialActionType.Snitch)]
        public void GetBaseAffinityDelta_NegativeActions_NoPersonality_UseBetrayalBase(SocialActionType action)
            => Assert.AreEqual(SocialMath.BetrayalBase, SocialMath.GetBaseAffinityDelta(action), Tol);

        [TestCase(SocialActionType.Betrayal)]
        [TestCase(SocialActionType.Theft)]
        [TestCase(SocialActionType.Snitch)]
        public void GetBaseAffinityDelta_NegativeActions_UsePersonalityBetrayalPenalty(SocialActionType action)
        {
            var pers = NewPersonality(betrayalPenalty: -30);
            Assert.AreEqual(-30f, SocialMath.GetBaseAffinityDelta(action, pers), Tol);
        }

        // -------------------- Favored gift detection --------------------
        [Test]
        public void IsFavoredGift_NullPersonality_False()
            => Assert.IsFalse(SocialMath.IsFavoredGift(null, NewItem("X")));

        [Test]
        public void IsFavoredGift_NullItem_False()
            => Assert.IsFalse(SocialMath.IsFavoredGift(NewPersonality(), null));

        [Test]
        public void IsFavoredGift_ItemInList_True()
        {
            var item = NewItem("X");
            var pers = NewPersonality();
            pers.favoredItems.Add(item);
            Assert.IsTrue(SocialMath.IsFavoredGift(pers, item));
        }

        [Test]
        public void IsFavoredGift_ItemNotInList_False()
            => Assert.IsFalse(SocialMath.IsFavoredGift(NewPersonality(), NewItem("X")));

        // -------------------- Same-category gift penalty --------------------
        [Test]
        public void SameCategoryPenalty_RepeatCategory_HalvesGain()
        {
            var item = NewItem("Soap", ItemCategory.CraftingPart);
            float result = SocialMath.ApplyGiftSameCategoryPenalty(5f, item, ItemCategory.CraftingPart, false);
            Assert.AreEqual(2.5f, result, Tol);
        }

        [Test]
        public void SameCategoryPenalty_DifferentCategory_NoChange()
        {
            var item = NewItem("Soap", ItemCategory.CraftingPart);
            float result = SocialMath.ApplyGiftSameCategoryPenalty(5f, item, ItemCategory.Tool, false);
            Assert.AreEqual(5f, result, Tol);
        }

        [Test]
        public void SameCategoryPenalty_FavoredGift_Exempt()
        {
            var item = NewItem("Soap", ItemCategory.CraftingPart);
            float result = SocialMath.ApplyGiftSameCategoryPenalty(10f, item, ItemCategory.CraftingPart, true);
            Assert.AreEqual(10f, result, Tol);
        }

        [Test]
        public void SameCategoryPenalty_NoPreviousCategory_NoChange()
        {
            var item = NewItem("Soap", ItemCategory.CraftingPart);
            float result = SocialMath.ApplyGiftSameCategoryPenalty(5f, item, null, false);
            Assert.AreEqual(5f, result, Tol);
        }

        [Test]
        public void SameCategoryPenalty_NullItem_NoChange()
            => Assert.AreEqual(5f, SocialMath.ApplyGiftSameCategoryPenalty(5f, null, ItemCategory.CraftingPart, false), Tol);

        // -------------------- Soft cap (ComputeEffectiveDelta) --------------------
        [Test]
        public void ComputeEffectiveDelta_AtZeroAffinity_FullDelta()
            => Assert.AreEqual(2f, SocialMath.ComputeEffectiveDelta(0f, 2f, 1f), Tol);

        [Test]
        public void ComputeEffectiveDelta_AtHalfAffinity_HalfDelta()
            => Assert.AreEqual(1f, SocialMath.ComputeEffectiveDelta(50f, 2f, 1f), Tol);

        [Test]
        public void ComputeEffectiveDelta_AtMaxAffinity_Zero()
            => Assert.AreEqual(0f, SocialMath.ComputeEffectiveDelta(100f, 2f, 1f), Tol);

        [Test]
        public void ComputeEffectiveDelta_NegativeDelta_BypassesSoftCap()
            => Assert.AreEqual(-50f, SocialMath.ComputeEffectiveDelta(50f, -50f, 1f), Tol);

        [Test]
        public void ComputeEffectiveDelta_AppliesGainMultiplier()
            => Assert.AreEqual(4f, SocialMath.ComputeEffectiveDelta(0f, 2f, 2f), Tol);

        [Test]
        public void ComputeEffectiveDelta_NegativeGainMultiplier_ClampedToZero()
            => Assert.AreEqual(0f, SocialMath.ComputeEffectiveDelta(0f, 10f, -2f), Tol);

        [Test]
        public void ComputeEffectiveDelta_AffinityAboveMax_ClampedSoftCapZero()
            => Assert.AreEqual(0f, SocialMath.ComputeEffectiveDelta(200f, 10f, 1f), Tol);

        // -------------------- ApplyAffinityChange (with clamp) --------------------
        [Test]
        public void ApplyAffinityChange_Greeting_AtZero()
            => Assert.AreEqual(2f, SocialMath.ApplyAffinityChange(0f, 2f, 1f), Tol);

        [Test]
        public void ApplyAffinityChange_Greeting_NearMax_RespectsSoftCap()
            => Assert.AreEqual(51f, SocialMath.ApplyAffinityChange(50f, 2f, 1f), Tol);

        [Test]
        public void ApplyAffinityChange_ClampsAtMax()
            => Assert.AreEqual(100f, SocialMath.ApplyAffinityChange(100f, 2f, 1f), Tol);

        [Test]
        public void ApplyAffinityChange_ClampsAtMin()
            => Assert.AreEqual(-100f, SocialMath.ApplyAffinityChange(-100f, -50f, 1f), Tol);

        [Test]
        public void ApplyAffinityChange_PersonalityMultiplierDoublesGain()
            => Assert.AreEqual(4f, SocialMath.ApplyAffinityChange(0f, 2f, 2f), Tol);

        // -------------------- Reputation tiers --------------------
        [TestCase(-100f, ReputationTier.Outsider)]
        [TestCase(0f, ReputationTier.Outsider)]
        [TestCase(24.9f, ReputationTier.Outsider)]
        [TestCase(25f, ReputationTier.Associate)]
        [TestCase(49.9f, ReputationTier.Associate)]
        [TestCase(50f, ReputationTier.Respected)]
        [TestCase(74.9f, ReputationTier.Respected)]
        [TestCase(75f, ReputationTier.Kingpin)]
        [TestCase(100f, ReputationTier.Kingpin)]
        public void GetReputationTier_Boundaries(float avg, ReputationTier expected)
            => Assert.AreEqual(expected, SocialMath.GetReputationTier(avg));

        // -------------------- Average affinities --------------------
        [Test]
        public void AverageRegisteredAffinities_Mean()
        {
            var dict = new Dictionary<int, float> { { 0, 10f }, { 1, 20f }, { 2, 30f } };
            Assert.AreEqual(20f, SocialMath.AverageRegisteredAffinities(dict), Tol);
        }

        [Test]
        public void AverageRegisteredAffinities_Empty_Zero()
            => Assert.AreEqual(0f, SocialMath.AverageRegisteredAffinities(new Dictionary<int, float>()), Tol);

        [Test]
        public void AverageRegisteredAffinities_Null_Zero()
            => Assert.AreEqual(0f, SocialMath.AverageRegisteredAffinities(null), Tol);

        // -------------------- End-to-end scenario --------------------
        [Test]
        public void Scenario_FavoredGiftFromZero_RaisesAffinityByTen()
        {
            var item = NewItem("Cigarettes");
            var pers = NewPersonality();
            pers.favoredItems.Add(item);
            float baseDelta = SocialMath.GetBaseAffinityDelta(SocialActionType.Gift, pers, item, 5f);
            float next = SocialMath.ApplyAffinityChange(0f, baseDelta, pers.affinityGainMultiplier);
            Assert.AreEqual(10f, next, Tol);
        }
    }
}
