using System.Collections.Generic;
using NUnit.Framework;
using Prison.Social;

namespace Prison.Tests
{
    /// <summary>
    /// Deterministic roster generation, NPC↔NPC seeding, snitch propensity, and favor gate
    /// checks per the Social Ecosystem &amp; Gangs v3 test plan.
    /// </summary>
    public class SocialRosterSnitchTests
    {
        private static List<int> Cells(int count)
        {
            var cells = new List<int>();
            for (int i = 0; i < count; i++) cells.Add(i);
            return cells;
        }

        // ------------------------------------------------------------------ roster

        [Test]
        public void Roster_SameSeedSamePrison()
        {
            var a = SocialRosterBuilder.Build(1234, Cells(15), 4);
            var b = SocialRosterBuilder.Build(1234, Cells(15), 4);
            Assert.AreEqual(a.identities.Count, b.identities.Count);
            for (int i = 0; i < a.identities.Count; i++)
            {
                Assert.AreEqual(a.identities[i].DisplayName, b.identities[i].DisplayName);
                Assert.AreEqual(a.identities[i].archetype, b.identities[i].archetype);
                Assert.AreEqual(a.identities[i].gangId, b.identities[i].gangId);
                Assert.AreEqual(a.identities[i].traits.nerve, b.identities[i].traits.nerve);
            }
        }

        [Test]
        public void Roster_DifferentSeedsDiffer()
        {
            var a = SocialRosterBuilder.Build(1, Cells(15), 2);
            var b = SocialRosterBuilder.Build(2, Cells(15), 2);
            bool anyDifferent = false;
            for (int i = 0; i < a.identities.Count && !anyDifferent; i++)
                anyDifferent = a.identities[i].DisplayName != b.identities[i].DisplayName;
            Assert.IsTrue(anyDifferent);
        }

        [Test]
        public void Roster_FifteenInmates_SplitsFiveFiveFive()
        {
            var roster = SocialRosterBuilder.Build(42, Cells(15), 0);
            int vipers = 0, syndicate = 0, independents = 0;
            foreach (var inmate in roster.Inmates())
            {
                if (inmate.gangId == GangCatalog.VipersId) vipers++;
                else if (inmate.gangId == GangCatalog.SyndicateId) syndicate++;
                else independents++;
            }
            Assert.AreEqual(5, vipers);
            Assert.AreEqual(5, syndicate);
            Assert.AreEqual(5, independents);
        }

        [Test]
        public void Roster_EachGangHasExactlyOneShotCaller_SyndicateHasHustler()
        {
            var roster = SocialRosterBuilder.Build(7, Cells(15), 0);
            int vipersCallers = 0, syndicateCallers = 0;
            bool syndicateHustler = false;
            foreach (var inmate in roster.Inmates())
            {
                if (inmate.archetype == PrisonerArchetype.ShotCaller)
                {
                    if (inmate.gangId == GangCatalog.VipersId) vipersCallers++;
                    if (inmate.gangId == GangCatalog.SyndicateId) syndicateCallers++;
                }
                if (inmate.gangId == GangCatalog.SyndicateId && inmate.archetype == PrisonerArchetype.Hustler)
                    syndicateHustler = true;
            }
            Assert.AreEqual(1, vipersCallers);
            Assert.AreEqual(1, syndicateCallers);
            Assert.IsTrue(syndicateHustler);
        }

        [Test]
        public void Roster_IndependentsIncludeOldTimerAndSnitch()
        {
            var roster = SocialRosterBuilder.Build(9, Cells(15), 0);
            bool oldTimer = false, snitch = false;
            foreach (var inmate in roster.Inmates())
            {
                if (inmate.gangId != SocialTuning.IndependentGangId) continue;
                if (inmate.archetype == PrisonerArchetype.OldTimer) oldTimer = true;
                if (inmate.archetype == PrisonerArchetype.Snitch) snitch = true;
            }
            Assert.IsTrue(oldTimer);
            Assert.IsTrue(snitch);
        }

        [Test]
        public void Roster_SmallPopulation_NoGangsBelowFour()
        {
            var roster = SocialRosterBuilder.Build(3, Cells(3), 0);
            foreach (var inmate in roster.Inmates())
                Assert.AreEqual(SocialTuning.IndependentGangId, inmate.gangId);
        }

        [Test]
        public void Seeding_GangMatesStartAtPlus40Trust()
        {
            var roster = SocialRosterBuilder.Build(11, Cells(15), 0);
            foreach (var a in roster.Inmates())
            {
                if (a.gangId == SocialTuning.IndependentGangId) continue;
                foreach (var b in roster.Inmates())
                {
                    if (a.actorId == b.actorId || b.gangId != a.gangId) continue;
                    Assert.GreaterOrEqual(roster.relationships.GetTrust(a.actorId, b.actorId),
                        SocialTuning.GangMateSeedTrust - 0.01f,
                        $"{a.DisplayName} → {b.DisplayName} should be a seeded gang mate");
                }
            }
        }

        [Test]
        public void Seeding_NoSeededEnemiesInsideOwnGang()
        {
            var roster = SocialRosterBuilder.Build(23, Cells(15), 0);
            foreach (var a in roster.Inmates())
            {
                if (a.gangId == SocialTuning.IndependentGangId) continue;
                foreach (var b in roster.Inmates())
                {
                    if (a.actorId == b.actorId || b.gangId != a.gangId) continue;
                    Assert.GreaterOrEqual(roster.relationships.GetTrust(a.actorId, b.actorId), 0f);
                }
            }
        }

        [Test]
        public void Roster_TraitsRollinsideArchetypeRanges()
        {
            var roster = SocialRosterBuilder.Build(5, Cells(15), 0);
            foreach (var inmate in roster.Inmates())
            {
                var profile = ArchetypeCatalog.CreateDefault(inmate.archetype);
                Assert.GreaterOrEqual(inmate.traits.nerve, profile.nerveMin);
                Assert.LessOrEqual(inmate.traits.nerve, profile.nerveMax);
                Assert.GreaterOrEqual(inmate.traits.loyalty, profile.loyaltyMin);
                Assert.LessOrEqual(inmate.traits.loyalty, profile.loyaltyMax);
            }
        }

        [Test]
        public void Guards_ArchetypeCycleGuaranteesOneCorruptFromTwoGuards()
        {
            Assert.AreEqual(GuardArchetype.ByTheBook, SocialRosterBuilder.GuardArchetypeForIndex(0));
            Assert.AreEqual(GuardArchetype.Corrupt, SocialRosterBuilder.GuardArchetypeForIndex(1));
            Assert.AreEqual(GuardArchetype.Rookie, SocialRosterBuilder.GuardArchetypeForIndex(2));
            Assert.AreEqual(GuardArchetype.Veteran, SocialRosterBuilder.GuardArchetypeForIndex(3));
            Assert.AreEqual(GuardArchetype.ByTheBook, SocialRosterBuilder.GuardArchetypeForIndex(4));
        }

        // ------------------------------------------------------------------ snitch propensity

        [Test]
        public void Propensity_LowLoyaltyLowNerveSnitchesMore()
        {
            var rat = new PersonalityTraits(20, 10, 50, 60, 10);
            var soldier = new PersonalityTraits(60, 90, 30, 40, 80);
            float ratP = SnitchSystem.Propensity(rat, 6f, 0f, 0.35f);
            float soldierP = SnitchSystem.Propensity(soldier, 6f, 0f, 0f);
            Assert.Greater(ratP, soldierP);
        }

        [Test]
        public void Propensity_PositiveStandingSuppresses()
        {
            var traits = new PersonalityTraits(20, 10, 50, 60, 10);
            Assert.Greater(
                SnitchSystem.Propensity(traits, 6f, 0f, 0.35f),
                SnitchSystem.Propensity(traits, 6f, 80f, 0.35f));
        }

        [Test]
        public void Propensity_HeavierCrimesWeighMore()
        {
            var traits = new PersonalityTraits(20, 10, 50, 60, 10);
            Assert.Greater(
                SnitchSystem.Propensity(traits, 10f, 0f, 0.35f),
                SnitchSystem.Propensity(traits, 2f, 0f, 0.35f));
        }

        [Test]
        public void GuardTips_StrongTipsAlwaysLand_WeakTipsNeedLowTrust()
        {
            Assert.IsTrue(SnitchSystem.GuardActsOnTip(90f, 8f));
            Assert.IsFalse(SnitchSystem.GuardActsOnTip(50f, 3f));
            Assert.IsTrue(SnitchSystem.GuardActsOnTip(10f, 3f));
        }

        [Test]
        public void Mute_SilencesForThreeDays()
        {
            var roster = SocialRosterBuilder.Build(5, Cells(6), 0);
            var snitches = new SnitchSystem(roster, roster.relationships, 1);
            snitches.Mute(3, currentDay: 5);
            Assert.IsTrue(snitches.IsMuted(3, 5));
            Assert.IsTrue(snitches.IsMuted(3, 7));
            Assert.IsFalse(snitches.IsMuted(3, 8));
        }

        // ------------------------------------------------------------------ ask-favor gates

        private static (SocialRoster roster, FavorService favors, GangManager gangs, NPCIdentity npc) FavorWorld(
            PrisonerArchetype archetype = PrisonerArchetype.Hustler)
        {
            var roster = new SocialRoster();
            var npc = new NPCIdentity
            {
                actorId = 1,
                archetype = archetype,
                gangId = SocialTuning.IndependentGangId,
                traits = new PersonalityTraits(50, 50, 50, 50, 50),
            };
            roster.Add(npc);
            var gangs = new GangManager(roster, roster.relationships);
            var favors = new FavorService(roster, roster.relationships, 1);
            return (roster, favors, gangs, npc);
        }

        [Test]
        public void CanAsk_LookoutNeedsTrust25()
        {
            var (roster, favors, gangs, npc) = FavorWorld();
            Assert.IsFalse(favors.CanAsk(FavorKind.Lookout, npc, gangs, out _));
            roster.relationships.Seed(npc.actorId, SocialTuning.PlayerActorId, 30f, 0f);
            Assert.IsTrue(favors.CanAsk(FavorKind.Lookout, npc, gangs, out _));
        }

        [Test]
        public void CanAsk_DistractionNeedsRespect25()
        {
            var (roster, favors, gangs, npc) = FavorWorld();
            roster.relationships.Seed(npc.actorId, SocialTuning.PlayerActorId, 50f, 10f);
            Assert.IsFalse(favors.CanAsk(FavorKind.Distraction, npc, gangs, out _));
            roster.relationships.Seed(npc.actorId, SocialTuning.PlayerActorId, 50f, 30f);
            Assert.IsTrue(favors.CanAsk(FavorKind.Distraction, npc, gangs, out _));
        }

        [Test]
        public void CanAsk_HoldStash_NeverTrustASnitch()
        {
            var (roster, favors, gangs, npc) = FavorWorld(PrisonerArchetype.Snitch);
            roster.relationships.Seed(npc.actorId, SocialTuning.PlayerActorId, 80f, 0f);
            Assert.IsFalse(favors.CanAsk(FavorKind.HoldStash, npc, gangs, out string reason));
            StringAssert.Contains("mouth", reason);
        }

        [Test]
        public void CanAsk_SilenceSnitchNeedsGangTrusted()
        {
            var (roster, favors, gangs, npc) = FavorWorld(PrisonerArchetype.Soldier);
            Assert.IsFalse(favors.CanAsk(FavorKind.SilenceSnitch, npc, gangs, out _));
        }

        [Test]
        public void Favors_AskFavorsDoNotExpireOnDayTick()
        {
            var (roster, favors, gangs, npc) = FavorWorld();
            roster.relationships.Seed(npc.actorId, SocialTuning.PlayerActorId, 30f, 0f);
            favors.StartAskFavor(FavorKind.Lookout, npc, currentDay: 1);
            var failed = favors.TickDay(10);
            Assert.AreEqual(0, failed.Count);
            Assert.IsNotNull(favors.ActiveAskFavor(FavorKind.Lookout));
        }

        [Test]
        public void Favors_InitiationRequiresItemPool()
        {
            // No ItemDatabase exists in EditMode → the fetch-flavored initiation favor cannot
            // roll an item and must fail null-safely rather than create a broken favor.
            var (roster, favors, gangs, npc) = FavorWorld();
            var shotCaller = new NPCIdentity
            {
                actorId = 9,
                archetype = PrisonerArchetype.ShotCaller,
                gangId = GangCatalog.VipersId,
                traits = new PersonalityTraits(70, 80, 40, 50, 70),
            };
            roster.Add(shotCaller);
            Assert.IsNull(favors.CreateInitiationFavor(shotCaller, 1));
        }

        // ------------------------------------------------------------------ guard trust modifiers (M6)

        [Test]
        public void GuardTrust_DetectionMult_WidensOnlyBelowDistrustThreshold()
        {
            // Guard AI note: trust ≤ −25 → +2 m on the 10 m base cone (×1.2); neutral otherwise.
            Assert.AreEqual(1f, GuardTrustMath.DetectionRangeMultiplier(0f), 0.001f);
            Assert.AreEqual(1f, GuardTrustMath.DetectionRangeMultiplier(75f), 0.001f);
            Assert.AreEqual(1f, GuardTrustMath.DetectionRangeMultiplier(-24f), 0.001f);
            Assert.AreEqual(1.2f, GuardTrustMath.DetectionRangeMultiplier(-25f), 0.001f);
            Assert.AreEqual(1.2f, GuardTrustMath.DetectionRangeMultiplier(-100f), 0.001f);
        }

        [Test]
        public void GuardTrust_ComplianceGrace_TenSecondsAtTrustFiftyPlus()
        {
            // Guard AI note: trust ≥ 50 → +10 s compliance tolerance; none below.
            Assert.AreEqual(0f, GuardTrustMath.ComplianceGraceSeconds(0f), 0.001f);
            Assert.AreEqual(0f, GuardTrustMath.ComplianceGraceSeconds(49f), 0.001f);
            Assert.AreEqual(10f, GuardTrustMath.ComplianceGraceSeconds(50f), 0.001f);
            Assert.AreEqual(10f, GuardTrustMath.ComplianceGraceSeconds(100f), 0.001f);
            Assert.AreEqual(0f, GuardTrustMath.ComplianceGraceSeconds(-60f), 0.001f); // never negative
        }

        // ------------------------------------------------------------------ names

        [Test]
        public void Names_UniqueAcrossFullRoster()
        {
            var roster = SocialRosterBuilder.Build(77, Cells(15), 6);
            var seen = new HashSet<string>();
            foreach (var identity in roster.identities)
                Assert.IsTrue(seen.Add(identity.DisplayName), $"duplicate name {identity.DisplayName}");
        }
    }
}
