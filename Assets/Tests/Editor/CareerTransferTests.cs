using NUnit.Framework;
using Prison.Career;

namespace Prison.Tests
{
    /// <summary>
    /// Transfer &amp; graduation logic per the test plan in
    /// docs/PrisonEscape/02 Features/Facility Transfer & Graduation.md: ladder resolution,
    /// respect math, sentence-clock boundaries, state-change ordering, confiscation, and gates.
    /// </summary>
    public class CareerTransferTests
    {
        private static CareerWorld WorldAt(string facilityId, int day = 3)
        {
            var world = CareerWorld.CreateNew("T", includeDevSandbox: false);
            world.Unlock(facilityId);
            var run = world.BeginVisit(facilityId);
            run.day = day;
            return world;
        }

        // ------------------------------------------------------------------
        // Ladder resolution — all 9 tiers; 8 → win, never a 9th facility
        // ------------------------------------------------------------------

        [Test]
        public void Ladder_HasNineTiers_InDesignOrder()
        {
            Assert.AreEqual(9, FacilityIds.LadderOrder.Length);
            Assert.AreEqual(FacilityIds.County, FacilityIds.LadderOrder[0]);
            Assert.AreEqual(FacilityIds.FedAdx, FacilityIds.LadderOrder[8]);
            Assert.AreEqual(-1, FacilityIds.LadderIndexOf(FacilityIds.DevSandbox));
        }

        [Test]
        public void Escape_EachTierBelowTop_TransfersToNextAndUnlocksIt()
        {
            for (int tier = 0; tier < FacilityIds.LadderOrder.Length - 1; tier++)
            {
                string from = FacilityIds.LadderOrder[tier];
                string expectedNext = FacilityIds.LadderOrder[tier + 1];
                var world = WorldAt(from);

                var result = CareerTransfer.Complete(world, escaped: true, itemsConfiscated: 0);

                Assert.AreEqual(TransferKind.Escaped, result.kind, from);
                Assert.AreEqual(expectedNext, result.nextFacilityId, from);
                Assert.IsTrue(world.IsUnlocked(expectedNext), from);
                Assert.AreEqual(expectedNext, world.currentFacilityId, from);
                Assert.IsFalse(world.global.careerWon, from);
            }
        }

        [Test]
        public void Escape_TopFederal_IsCareerWin_NotATransfer()
        {
            var world = WorldAt(FacilityIds.FedAdx);

            var result = CareerTransfer.Complete(world, escaped: true, itemsConfiscated: 0);

            Assert.AreEqual(TransferKind.CareerWin, result.kind);
            Assert.IsNull(result.nextFacilityId);
            Assert.IsTrue(world.global.careerWon);
            Assert.AreEqual(FacilityIds.FedAdx, world.currentFacilityId, "no 9th facility exists");
        }

        [Test]
        public void CareerWon_OnlyFromTier8()
        {
            var world = WorldAt(FacilityIds.FedHigh);
            CareerTransfer.Complete(world, escaped: true, itemsConfiscated: 0);
            Assert.IsFalse(world.global.careerWon, "tier 7 escape must not set the win flag");
        }

        [Test]
        public void Transfer_FromDevSandbox_IsRejected()
        {
            var world = CareerWorld.CreateNew("T", includeDevSandbox: true);
            world.BeginVisit(FacilityIds.DevSandbox);

            Assert.Throws<System.InvalidOperationException>(
                () => CareerTransfer.Complete(world, escaped: true, itemsConfiscated: 0));
        }

        // ------------------------------------------------------------------
        // Respect math
        // ------------------------------------------------------------------

        [Test]
        public void RespectAward_EscapeIs8Plus2PerTier()
        {
            Assert.AreEqual(8f, CareerRespectMath.EscapeAward(0));
            Assert.AreEqual(14f, CareerRespectMath.EscapeAward(3));
            Assert.AreEqual(24f, CareerRespectMath.EscapeAward(8));
        }

        [Test]
        public void RespectAward_SentenceIs5_CaughtIsMinus2_DailyOnlyAtFederalTiers()
        {
            Assert.AreEqual(5f, CareerRespectMath.SentenceServedAward);
            Assert.AreEqual(-2f, CareerRespectMath.CaughtEscapingPenalty);
            Assert.AreEqual(0f, CareerRespectMath.DailySurvivalAward(3), "State Max: no daily award");
            Assert.AreEqual(0.5f, CareerRespectMath.DailySurvivalAward(4), "Fed Camp onward: +0.5/day");
        }

        [Test]
        public void Respect_ClampsToZeroTo100()
        {
            Assert.AreEqual(0f, CareerRespectMath.Clamp(-5f));
            Assert.AreEqual(100f, CareerRespectMath.Clamp(140f));

            var world = WorldAt(FacilityIds.FedAdx);
            world.global.respect = 95f;
            var result = CareerTransfer.Complete(world, escaped: true, itemsConfiscated: 0);
            Assert.AreEqual(100f, result.respectAfter);
        }

        [Test]
        public void ArrivalAffinitySeed_MatchesDesignBands()
        {
            Assert.AreEqual(0f, CareerRespectMath.ArrivalAffinitySeed(24f));
            Assert.AreEqual(10f, CareerRespectMath.ArrivalAffinitySeed(25f));
            Assert.AreEqual(20f, CareerRespectMath.ArrivalAffinitySeed(50f));
            Assert.AreEqual(30f, CareerRespectMath.ArrivalAffinitySeed(75f));
        }

        // ------------------------------------------------------------------
        // Sentence clock — day 7 vs day 8, no double fire
        // ------------------------------------------------------------------

        [Test]
        public void SentenceClock_TransfersAtDay8MorningCount_NotDay7()
        {
            const int sentence = 7;
            Assert.IsFalse(SentenceClockMath.ShouldTransferAtMorningCount(7, sentence), "day 7 count = 6 served");
            Assert.IsTrue(SentenceClockMath.ShouldTransferAtMorningCount(8, sentence), "day 8 count = 7 served");
        }

        [Test]
        public void SentenceClock_NeverFiresWithoutASentence()
        {
            Assert.IsFalse(SentenceClockMath.ShouldTransferAtMorningCount(100, 0), "only County has a clock");
        }

        [Test]
        public void SentenceClock_HudLineCountsServedDays_AndClampsAtSentence()
        {
            Assert.AreEqual("Days served: 0 / 7", SentenceClockMath.HudLine(1, 7));
            Assert.AreEqual("Days served: 3 / 7", SentenceClockMath.HudLine(4, 7));
            Assert.AreEqual("Days served: 7 / 7", SentenceClockMath.HudLine(9, 7));
        }

        [Test]
        public void SentenceServed_TransfersWithSentenceFramingAndAward()
        {
            var world = WorldAt(FacilityIds.County, day: 8);

            var result = CareerTransfer.Complete(world, escaped: false, itemsConfiscated: 2);

            Assert.AreEqual(TransferKind.SentenceServed, result.kind);
            Assert.AreEqual(FacilityIds.StateMin, result.nextFacilityId);
            Assert.AreEqual(5f, result.respectAwarded);
            Assert.IsFalse(world.visitLog[0].escaped);
        }

        // ------------------------------------------------------------------
        // State-change ordering & confiscation
        // ------------------------------------------------------------------

        [Test]
        public void Transfer_AppendsVisitLog_BeforeUnlock_ThenConfiscatesRunWholesale()
        {
            var world = WorldAt(FacilityIds.County, day: 4);
            world.global.cash = 300;
            int seedBefore = world.activeRun.worldSeed;

            var result = CareerTransfer.Complete(world, escaped: true, itemsConfiscated: 5);

            // Visit log recorded the completed stay.
            Assert.AreEqual(1, world.visitLog.Count);
            var visit = world.visitLog[0];
            Assert.AreEqual(FacilityIds.County, visit.facilityId);
            Assert.AreEqual(1, visit.visitIndex);
            Assert.AreEqual(4, visit.daysSpent);
            Assert.IsTrue(visit.escaped);
            Assert.IsNotEmpty(visit.endedUtc);

            // Totals moved with it.
            Assert.AreEqual(1, world.global.totalTransfers);
            Assert.AreEqual(4, world.global.totalDaysLived);

            // Confiscation discards the run state wholesale — inventory AND stash die with it.
            Assert.IsFalse(world.activeRun.IsActive, "fresh empty run state replaces the completed one");
            Assert.AreEqual(0, world.activeRun.worldSeed);
            Assert.AreNotEqual(0, seedBefore);
            Assert.AreEqual(5, result.itemsConfiscated);

            // Cash is global and survived.
            Assert.AreEqual(300, result.cashCarried);
            Assert.AreEqual(300, world.global.cash);
        }

        [Test]
        public void Revisit_AfterTransfer_ProducesNextVisitIndexAndDifferentSeed()
        {
            var world = WorldAt(FacilityIds.County);
            int firstSeed = world.activeRun.worldSeed;
            CareerTransfer.Complete(world, escaped: true, itemsConfiscated: 0);

            var revisit = world.BeginVisit(FacilityIds.County);

            Assert.AreEqual(2, revisit.visitIndex);
            Assert.AreEqual(1, revisit.day);
            Assert.AreNotEqual(firstSeed, revisit.worldSeed);
        }

        // ------------------------------------------------------------------
        // Soft transfer gates
        // ------------------------------------------------------------------

        [Test]
        public void Gates_LowTiersHaveNone()
        {
            var globals = new CareerGlobals(); // broke, no respect
            foreach (string id in new[] { FacilityIds.County, FacilityIds.StateMax, FacilityIds.FedCamp, FacilityIds.FedLow })
                Assert.IsTrue(CareerGates.CanAttemptEscape(globals, FacilityCatalog.Get(id)), id);
        }

        [Test]
        public void Gates_FedMedAndHigh_AcceptCashOrRespect()
        {
            var fedMed = FacilityCatalog.Get(FacilityIds.FedMed);
            Assert.IsFalse(CareerGates.CanAttemptEscape(new CareerGlobals { cash = 2499, respect = 39f }, fedMed));
            Assert.IsTrue(CareerGates.CanAttemptEscape(new CareerGlobals { cash = 2500, respect = 0f }, fedMed));
            Assert.IsTrue(CareerGates.CanAttemptEscape(new CareerGlobals { cash = 0, respect = 40f }, fedMed));

            var fedHigh = FacilityCatalog.Get(FacilityIds.FedHigh);
            Assert.IsTrue(CareerGates.CanAttemptEscape(new CareerGlobals { cash = 5000, respect = 0f }, fedHigh));
            Assert.IsFalse(CareerGates.CanAttemptEscape(new CareerGlobals { cash = 4999, respect = 59f }, fedHigh));
        }

        [Test]
        public void Gates_FedAdx_RequiresCashAndRespect()
        {
            var adx = FacilityCatalog.Get(FacilityIds.FedAdx);
            Assert.IsFalse(CareerGates.CanAttemptEscape(new CareerGlobals { cash = 10000, respect = 74f }, adx));
            Assert.IsFalse(CareerGates.CanAttemptEscape(new CareerGlobals { cash = 9999, respect = 75f }, adx));
            Assert.IsTrue(CareerGates.CanAttemptEscape(new CareerGlobals { cash = 10000, respect = 75f }, adx));
        }

        // ------------------------------------------------------------------
        // Catalog sanity — the curve shapes are the design contract
        // ------------------------------------------------------------------

        [Test]
        public void Catalog_HasAllTenSlots_WithMonotonicDifficultyCurves()
        {
            Assert.AreEqual(10, FacilityCatalog.All.Length);

            for (int tier = 1; tier < FacilityIds.LadderOrder.Length; tier++)
            {
                var prev = FacilityCatalog.Get(FacilityIds.LadderOrder[tier - 1]);
                var cur = FacilityCatalog.Get(FacilityIds.LadderOrder[tier]);
                Assert.Less(cur.lootAbundance, prev.lootAbundance, "lootAbundance falls up the ladder");
                Assert.Greater(cur.cashIncomeMult, prev.cashIncomeMult, "income rises up the ladder");
                Assert.Greater(cur.bribeCostMult, prev.bribeCostMult, "bribe costs rise up the ladder");
                Assert.Greater(cur.escapeRouteCostMult, prev.escapeRouteCostMult, "route costs rise up the ladder");
                Assert.GreaterOrEqual(cur.detectionRangeMult, prev.detectionRangeMult, "detection tightens up the ladder");
                Assert.Greater(cur.shakedownStrictness, prev.shakedownStrictness, "shakedowns tighten up the ladder");
            }

            Assert.AreEqual(7, FacilityCatalog.Get(FacilityIds.County).sentenceDays, "County waits out a 7-day sentence");
            foreach (string id in FacilityIds.LadderOrder)
                if (id != FacilityIds.County)
                    Assert.AreEqual(0, FacilityCatalog.Get(id).sentenceDays, $"{id}: sentence clocks are County-only");
        }
    }
}
