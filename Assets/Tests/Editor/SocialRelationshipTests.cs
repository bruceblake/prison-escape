using NUnit.Framework;
using Prison.Social;

namespace Prison.Tests
{
    /// <summary>
    /// Relationship math per the Social Ecosystem &amp; Gangs v3 test plan: delta pipeline +
    /// soft cap, Standing formula + band thresholds, tier computation, intimidation odds.
    /// </summary>
    public class SocialRelationshipTests
    {
        private const float Tol = 1e-4f;
        private static readonly PersonalityTraits Neutral = new PersonalityTraits(50, 50, 50, 50, 50);

        // ------------------------------------------------------------------ standing & bands

        [Test]
        public void Standing_IsWeightedSixtyFortyTrustRespect()
            => Assert.AreEqual(0.6f * 40f + 0.4f * 20f, RelationshipMath.Standing(40f, 20f), Tol);

        [TestCase(-100f, StandingBand.Enemy)]
        [TestCase(-50f, StandingBand.Enemy)]
        [TestCase(-49.9f, StandingBand.Hostile)]
        [TestCase(-25f, StandingBand.Hostile)]
        [TestCase(-24.9f, StandingBand.Neutral)]
        [TestCase(0f, StandingBand.Neutral)]
        [TestCase(24.9f, StandingBand.Neutral)]
        [TestCase(25f, StandingBand.Friendly)]
        [TestCase(49.9f, StandingBand.Friendly)]
        [TestCase(50f, StandingBand.Ally)]
        [TestCase(74.9f, StandingBand.Ally)]
        [TestCase(75f, StandingBand.Confidant)]
        [TestCase(100f, StandingBand.Confidant)]
        public void Bands_MatchSpecThresholds(float standing, StandingBand expected)
            => Assert.AreEqual(expected, RelationshipMath.GetBand(standing));

        [Test]
        public void FriendAndEnemyFilters_CoverSpecBands()
        {
            Assert.IsTrue(RelationshipMath.IsFriendBand(StandingBand.Friendly));
            Assert.IsTrue(RelationshipMath.IsFriendBand(StandingBand.Ally));
            Assert.IsTrue(RelationshipMath.IsFriendBand(StandingBand.Confidant));
            Assert.IsFalse(RelationshipMath.IsFriendBand(StandingBand.Neutral));
            Assert.IsTrue(RelationshipMath.IsEnemyBand(StandingBand.Hostile));
            Assert.IsTrue(RelationshipMath.IsEnemyBand(StandingBand.Enemy));
            Assert.IsFalse(RelationshipMath.IsEnemyBand(StandingBand.Neutral));
        }

        // ------------------------------------------------------------------ soft cap (kept from v1)

        [Test]
        public void SoftCap_FullGainAtZero()
            => Assert.AreEqual(2f, RelationshipMath.ApplyPositiveSoftCap(0f, 2f), Tol);

        [Test]
        public void SoftCap_HalfGainAtFifty()
            => Assert.AreEqual(1f, RelationshipMath.ApplyPositiveSoftCap(50f, 2f), Tol);

        [Test]
        public void SoftCap_ZeroGainAtMax()
            => Assert.AreEqual(0f, RelationshipMath.ApplyPositiveSoftCap(100f, 2f), Tol);

        [Test]
        public void SoftCap_NegativesNeverCapped()
            => Assert.AreEqual(-50f, RelationshipMath.ApplyPositiveSoftCap(90f, -50f), Tol);

        // ------------------------------------------------------------------ pipeline

        [Test]
        public void Pipeline_SociabilityScalesPositiveTrust()
        {
            var social = new PersonalityTraits(50, 50, 50, 100, 50);
            var shy = new PersonalityTraits(50, 50, 50, 0, 50);
            Assert.AreEqual(2f * 1.25f, RelationshipMath.ComputeEffectiveDelta(0f, 2f, true, social), Tol);
            Assert.AreEqual(2f * 0.75f, RelationshipMath.ComputeEffectiveDelta(0f, 2f, true, shy), Tol);
        }

        [Test]
        public void Pipeline_SociabilityDoesNotTouchRespectOrLosses()
        {
            var social = new PersonalityTraits(50, 50, 50, 100, 50);
            Assert.AreEqual(5f, RelationshipMath.ComputeEffectiveDelta(0f, 5f, false, social), Tol);
            Assert.AreEqual(-10f, RelationshipMath.ComputeEffectiveDelta(0f, -10f, true, social), Tol);
        }

        [Test]
        public void Pipeline_LoyaltyScalesBetrayalPenalty()
        {
            var loyal = new PersonalityTraits(50, 100, 50, 50, 50);
            Assert.AreEqual(-80f, RelationshipMath.ComputeEffectiveDelta(0f, -40f, true, loyal, isBetrayalClass: true), Tol);
            var disloyal = new PersonalityTraits(50, 0, 50, 50, 50);
            Assert.AreEqual(-40f, RelationshipMath.ComputeEffectiveDelta(0f, -40f, true, disloyal, isBetrayalClass: true), Tol);
        }

        [Test]
        public void Pipeline_GangFactorScalesBeforeSoftCap()
            => Assert.AreEqual(10f * 0.5f * 0.5f, // ×0.5 propagation then ×0.5 soft cap at 50
                RelationshipMath.ComputeEffectiveDelta(50f, 10f, false, Neutral, false, 0.5f), Tol);

        [Test]
        public void Apply_ClampsToAxisRange()
        {
            Assert.AreEqual(100f, RelationshipMath.Apply(95f, 20f), Tol);
            Assert.AreEqual(-100f, RelationshipMath.Apply(-95f, -20f), Tol);
        }

        // ------------------------------------------------------------------ store

        [Test]
        public void Store_UnknownPairsReadZero()
        {
            var store = new RelationshipStore();
            Assert.AreEqual(0f, store.GetTrust(1, 2), Tol);
            Assert.AreEqual(StandingBand.Neutral, store.GetBand(1, 2));
        }

        [Test]
        public void Store_ApplyDeltas_RunsPipelineAndRaisesEvent()
        {
            var store = new RelationshipStore();
            int events = 0;
            store.OnChanged += (o, s, td, rd, r) => events++;
            var (trustDelta, _) = store.ApplyDeltas(1, 0, 2f, 0f, Neutral);
            Assert.AreEqual(2f, trustDelta, Tol);
            Assert.AreEqual(2f, store.GetTrust(1, 0), Tol);
            Assert.AreEqual(1, events);
        }

        [Test]
        public void Store_SeedBypassesPipelineButClamps()
        {
            var store = new RelationshipStore();
            store.Seed(1, 0, 150f, -150f);
            Assert.AreEqual(100f, store.GetTrust(1, 0), Tol);
            Assert.AreEqual(-100f, store.GetRespect(1, 0), Tol);
        }

        // ------------------------------------------------------------------ tier

        [TestCase(0f, GangRank.Outsider, ReputationTier.Outsider)]
        [TestCase(25f, GangRank.Outsider, ReputationTier.Associate)]
        [TestCase(50f, GangRank.Outsider, ReputationTier.Respected)]
        [TestCase(75f, GangRank.Outsider, ReputationTier.Kingpin)]
        [TestCase(20f, GangRank.Associate, ReputationTier.Associate)] // +5 bonus
        [TestCase(40f, GangRank.Member, ReputationTier.Respected)]    // +10 bonus
        [TestCase(55f, GangRank.Trusted, ReputationTier.Kingpin)]     // +20 bonus
        public void Tier_UsesAverageStandingPlusRankBonus(float avg, GangRank rank, ReputationTier expected)
            => Assert.AreEqual(expected, RelationshipMath.ComputeTier(avg, rank));

        // ------------------------------------------------------------------ intimidation

        [Test]
        public void Intimidation_ChanceClampedToSaneRange()
        {
            Assert.LessOrEqual(RelationshipMath.IntimidationChance(100f, 200f, 0), 0.95f);
            Assert.GreaterOrEqual(RelationshipMath.IntimidationChance(-100f, 0f, 100), 0.05f);
        }

        [Test]
        public void Intimidation_RespectAndStrengthHelp_NerveHurts()
        {
            float baseline = RelationshipMath.IntimidationChance(0f, 100f, 50);
            Assert.Greater(RelationshipMath.IntimidationChance(50f, 100f, 50), baseline);
            Assert.Greater(RelationshipMath.IntimidationChance(0f, 150f, 50), baseline);
            Assert.Less(RelationshipMath.IntimidationChance(0f, 100f, 90), baseline);
        }
    }
}
