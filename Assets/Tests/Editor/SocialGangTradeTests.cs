using System.Collections.Generic;
using NUnit.Framework;
using Prison.Social;

namespace Prison.Tests
{
    /// <summary>
    /// Gang membership ladder, propagation, Traitor rules, and trade pricing per the
    /// Social Ecosystem &amp; Gangs v3 test plan.
    /// </summary>
    public class SocialGangTradeTests
    {
        private const float Tol = 1e-3f;

        /// <summary>Roster with 2 gangs × 2 members + 1 independent, ids 1–5 (player 0).</summary>
        private static (SocialRoster roster, GangManager gangs) SmallWorld()
        {
            var roster = new SocialRoster();
            for (int i = 0; i < 5; i++)
            {
                roster.Add(new NPCIdentity
                {
                    actorId = i + 1,
                    archetype = i % 2 == 0 ? PrisonerArchetype.ShotCaller : PrisonerArchetype.Soldier,
                    gangId = i < 2 ? GangCatalog.VipersId : (i < 4 ? GangCatalog.SyndicateId : SocialTuning.IndependentGangId),
                    traits = new PersonalityTraits(50, 50, 50, 50, 50),
                    cellIndex = i,
                });
            }
            return (roster, new GangManager(roster, roster.relationships));
        }

        private static void SetStandingTowardPlayer(SocialRoster roster, int actorId, float standing)
            => roster.relationships.Seed(actorId, SocialTuning.PlayerActorId, standing, standing);

        // ------------------------------------------------------------------ standing & ranks

        [Test]
        public void GangStanding_IsAverageOverMembers()
        {
            var (roster, gangs) = SmallWorld();
            SetStandingTowardPlayer(roster, 1, 40f);
            SetStandingTowardPlayer(roster, 2, 20f);
            Assert.AreEqual(30f, gangs.GangStanding(GangCatalog.VipersId), Tol);
        }

        [Test]
        public void Rank_AssociateAt25Standing()
        {
            var (roster, gangs) = SmallWorld();
            Assert.AreEqual(GangRank.Outsider, gangs.GetRank(GangCatalog.VipersId));
            SetStandingTowardPlayer(roster, 1, 25f);
            SetStandingTowardPlayer(roster, 2, 25f);
            Assert.AreEqual(GangRank.Associate, gangs.GetRank(GangCatalog.VipersId));
        }

        [Test]
        public void Rank_MemberAfterInitiation_TrustedNeedsStandingAndFavors()
        {
            var (roster, gangs) = SmallWorld();
            SetStandingTowardPlayer(roster, 1, 30f);
            SetStandingTowardPlayer(roster, 2, 30f);

            Assert.IsTrue(gangs.CanOfferInitiation(GangCatalog.VipersId, currentDay: 1));
            gangs.OfferInitiation(GangCatalog.VipersId, 1);
            gangs.CompleteInitiation();
            Assert.AreEqual(GangRank.Member, gangs.GetRank(GangCatalog.VipersId));
            Assert.AreEqual(GangCatalog.VipersId, gangs.MemberGangId);

            // Trusted: standing ≥ 60 AND 2 gang favors.
            SetStandingTowardPlayer(roster, 1, 70f);
            SetStandingTowardPlayer(roster, 2, 70f);
            Assert.AreEqual(GangRank.Member, gangs.GetRank(GangCatalog.VipersId));
            gangs.CompleteGangFavor(GangCatalog.VipersId);
            gangs.CompleteGangFavor(GangCatalog.VipersId);
            Assert.AreEqual(GangRank.Trusted, gangs.GetRank(GangCatalog.VipersId));
        }

        [Test]
        public void ExclusiveJoin_MemberOfOneLocksTheOther()
        {
            var (roster, gangs) = SmallWorld();
            SetStandingTowardPlayer(roster, 1, 30f);
            SetStandingTowardPlayer(roster, 2, 30f);
            SetStandingTowardPlayer(roster, 3, 30f);
            SetStandingTowardPlayer(roster, 4, 30f);

            gangs.OfferInitiation(GangCatalog.VipersId, 1);
            gangs.CompleteInitiation();
            Assert.IsFalse(gangs.CanOfferInitiation(GangCatalog.SyndicateId, 1));
        }

        [Test]
        public void Initiation_RefusalHasTwoDayCooldown()
        {
            var (roster, gangs) = SmallWorld();
            SetStandingTowardPlayer(roster, 1, 30f);
            SetStandingTowardPlayer(roster, 2, 30f);

            gangs.OfferInitiation(GangCatalog.VipersId, 1);
            gangs.RefuseOrFailInitiation(1);
            Assert.IsFalse(gangs.CanOfferInitiation(GangCatalog.VipersId, 2));
            Assert.IsTrue(gangs.CanOfferInitiation(GangCatalog.VipersId, 3));
        }

        [Test]
        public void Initiation_DeadlineExpires()
        {
            var (roster, gangs) = SmallWorld();
            SetStandingTowardPlayer(roster, 1, 30f);
            SetStandingTowardPlayer(roster, 2, 30f);
            gangs.OfferInitiation(GangCatalog.VipersId, 1, deadlineDays: 2);
            Assert.IsFalse(gangs.TickInitiationDeadline(3));
            Assert.IsTrue(gangs.TickInitiationDeadline(4));
            Assert.AreEqual(SocialTuning.IndependentGangId, gangs.PendingInitiationGangId);
        }

        [Test]
        public void Traitor_LockoutBlocksRejoinForever()
        {
            var (roster, gangs) = SmallWorld();
            SetStandingTowardPlayer(roster, 1, 30f);
            SetStandingTowardPlayer(roster, 2, 30f);
            gangs.OfferInitiation(GangCatalog.VipersId, 1);
            gangs.CompleteInitiation();

            gangs.MarkTraitor(GangCatalog.VipersId);
            Assert.IsTrue(gangs.IsTraitorLocked(GangCatalog.VipersId));
            Assert.AreEqual(SocialTuning.IndependentGangId, gangs.MemberGangId);
            Assert.IsFalse(gangs.CanOfferInitiation(GangCatalog.VipersId, 99));
            // Rival stays joinable.
            SetStandingTowardPlayer(roster, 3, 30f);
            SetStandingTowardPlayer(roster, 4, 30f);
            Assert.IsTrue(gangs.CanOfferInitiation(GangCatalog.SyndicateId, 99));
        }

        [Test]
        public void Propagation_HalfForOutsiders_FullForMembers()
        {
            var (roster, gangs) = SmallWorld();
            Assert.AreEqual(0.5f, gangs.PropagationFactor(GangCatalog.VipersId), Tol);

            SetStandingTowardPlayer(roster, 1, 30f);
            SetStandingTowardPlayer(roster, 2, 30f);
            gangs.OfferInitiation(GangCatalog.VipersId, 1);
            gangs.CompleteInitiation();
            Assert.AreEqual(1f, gangs.PropagationFactor(GangCatalog.VipersId), Tol);
        }

        [Test]
        public void RivalTradeRefusal_BelowMinus25Standing()
        {
            var (roster, gangs) = SmallWorld();
            SetStandingTowardPlayer(roster, 3, -30f);
            SetStandingTowardPlayer(roster, 4, -30f);
            Assert.IsTrue(gangs.RefusesTrade(GangCatalog.SyndicateId));
            SetStandingTowardPlayer(roster, 3, -20f);
            SetStandingTowardPlayer(roster, 4, -20f);
            Assert.IsFalse(gangs.RefusesTrade(GangCatalog.SyndicateId));
            Assert.IsFalse(gangs.RefusesTrade(SocialTuning.IndependentGangId));
        }

        // ------------------------------------------------------------------ trade math

        [Test]
        public void GreedFactor_MapsToSpecRange()
        {
            Assert.AreEqual(0.8f, TradeMath.GreedFactor(0), Tol);
            Assert.AreEqual(1.5f, TradeMath.GreedFactor(100), Tol);
        }

        [Test]
        public void TrustDiscount_Caps25PercentAt75Trust()
        {
            Assert.AreEqual(1f, TradeMath.TrustDiscountFactor(0f), Tol);
            Assert.AreEqual(0.75f, TradeMath.TrustDiscountFactor(75f), Tol);
            Assert.AreEqual(0.75f, TradeMath.TrustDiscountFactor(100f), Tol);
            Assert.AreEqual(1f, TradeMath.TrustDiscountFactor(-40f), Tol);
        }

        [Test]
        public void BuyPrice_AppliesAllFactors()
        {
            // 10 × 1.5 (greed 100) × 0.75 (trust 75) × 0.85 (member) × 2 (contraband) = 19.125 → 19
            Assert.AreEqual(19f, TradeMath.BuyPrice(10f, 100, 75f, true, true), Tol);
            // Neutral everything: 10 × 1.15 (greed 50) = 11.5 → 12
            Assert.AreEqual(12f, TradeMath.BuyPrice(10f, 50, 0f, false, false), Tol);
        }

        [Test]
        public void SellPrice_GreedierBuyersPayLess_ContrabandPremium()
        {
            Assert.AreEqual(5f, TradeMath.SellPrice(10f, 0, false), Tol);
            Assert.AreEqual(3f, TradeMath.SellPrice(10f, 100, false), Tol);
            Assert.Greater(TradeMath.SellPrice(10f, 50, true), TradeMath.SellPrice(10f, 50, false));
        }

        [Test]
        public void BuyPrice_NeverBelowOneDollar()
            => Assert.GreaterOrEqual(TradeMath.BuyPrice(0.5f, 0, 100f, true, false), 1f);

        // ------------------------------------------------------------------ career facility scaling

        [Test]
        public void FacilityPriceMult_ScalesRoundsAndFloors()
        {
            Assert.AreEqual(10f, TradeMath.ApplyFacilityPriceMult(10f, 1f), Tol);       // sandbox: untouched
            Assert.AreEqual(9f, TradeMath.ApplyFacilityPriceMult(10f, 0.9f), Tol);      // County discount
            Assert.AreEqual(26f, TradeMath.ApplyFacilityPriceMult(10f, 2.6f), Tol);     // Fed ADX markup
            Assert.AreEqual(12f, TradeMath.ApplyFacilityPriceMult(11.5f, 1f), Tol);     // rounds
            Assert.AreEqual(1f, TradeMath.ApplyFacilityPriceMult(0.4f, 0.9f), Tol);     // floors at $1
        }

        [Test]
        public void FacilityPriceMult_DefaultsToOneOutsideCareerRun()
        {
            // No facility entered in EditMode → all CareerSession multipliers must read 1
            // so sandbox/legacy pricing is untouched.
            Assert.AreEqual(1f, Prison.Career.CareerSession.TradePriceMult, Tol);
            Assert.AreEqual(1f, Prison.Career.CareerSession.BribeCostMult, Tol);
            Assert.AreEqual(1f, Prison.Career.CareerSession.CashIncomeMult, Tol);
        }
    }
}
