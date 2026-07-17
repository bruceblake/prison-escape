using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Prison;

namespace Prison.Tests
{
    /// <summary>
    /// EditMode unit tests for the pure rule/label/data helpers:
    /// <see cref="PrisonEventRules"/>, <see cref="PrisonEventExtensions"/>,
    /// <see cref="PrisonRoutineLabels"/>, <see cref="PrisonLocationZone.GetHudLabel"/>,
    /// <see cref="CellData.InteriorRadius"/> and enum contracts.
    /// </summary>
    public class PrisonRulesAndLabelsTests
    {
        private readonly List<UnityEngine.Object> _created = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var o in _created)
                if (o != null) UnityEngine.Object.DestroyImmediate(o);
            _created.Clear();
        }

        // ============ PrisonEventRules ============
        [Test]
        public void IsMandatory_OnlyFreeTimeIsFlexible()
        {
            foreach (PrisonEventType evt in Enum.GetValues(typeof(PrisonEventType)))
            {
                bool mandatory = evt != PrisonEventType.FreeTime;
                Assert.AreEqual(mandatory, PrisonEventRules.IsMandatory(evt), $"IsMandatory wrong for {evt}");
                Assert.AreEqual(!mandatory, PrisonEventRules.IsFlexible(evt), $"IsFlexible wrong for {evt}");
            }
        }

        [Test]
        public void IsHighStakesUpcoming_FlexibleNowMandatoryNext_True()
            => Assert.IsTrue(PrisonEventRules.IsHighStakesUpcoming(PrisonEventType.FreeTime, PrisonEventType.Breakfast));

        [Test]
        public void IsHighStakesUpcoming_FlexibleNowFlexibleNext_False()
            => Assert.IsFalse(PrisonEventRules.IsHighStakesUpcoming(PrisonEventType.FreeTime, PrisonEventType.FreeTime));

        [Test]
        public void IsHighStakesUpcoming_MandatoryNow_False()
            => Assert.IsFalse(PrisonEventRules.IsHighStakesUpcoming(PrisonEventType.Breakfast, PrisonEventType.Lunch));

        // ============ PrisonEventExtensions ============
        [Test]
        public void IsMorningLineUp_OnlyRollCallAndMorningRollCall()
        {
            foreach (PrisonEventType evt in Enum.GetValues(typeof(PrisonEventType)))
            {
                bool expected = evt == PrisonEventType.RollCall || evt == PrisonEventType.MorningRollCall;
                Assert.AreEqual(expected, PrisonEventExtensions.IsMorningLineUp(evt), $"IsMorningLineUp wrong for {evt}");
            }
        }

        [Test]
        public void IsNightBedPhase_OnlyNightRollCallAndLightsOut()
        {
            foreach (PrisonEventType evt in Enum.GetValues(typeof(PrisonEventType)))
            {
                bool expected = evt == PrisonEventType.NightRollCall || evt == PrisonEventType.LightsOut;
                Assert.AreEqual(expected, PrisonEventExtensions.IsNightBedPhase(evt), $"IsNightBedPhase wrong for {evt}");
            }
        }

        // ============ PrisonEventExtensions — formal counts ============
        [Test]
        public void IsCellCountPhase_OnlyMiddayAndEveningCount()
        {
            foreach (PrisonEventType evt in Enum.GetValues(typeof(PrisonEventType)))
            {
                bool expected = evt == PrisonEventType.MiddayCount || evt == PrisonEventType.EveningCount;
                Assert.AreEqual(expected, PrisonEventExtensions.IsCellCountPhase(evt), $"IsCellCountPhase wrong for {evt}");
            }
        }

        [Test]
        public void IsFormalCount_AllFourDailyCounts()
        {
            foreach (PrisonEventType evt in Enum.GetValues(typeof(PrisonEventType)))
            {
                bool expected = evt == PrisonEventType.RollCall
                    || evt == PrisonEventType.MorningRollCall
                    || evt == PrisonEventType.MiddayCount
                    || evt == PrisonEventType.EveningCount
                    || evt == PrisonEventType.NightRollCall;
                Assert.AreEqual(expected, PrisonEventExtensions.IsFormalCount(evt), $"IsFormalCount wrong for {evt}");
            }
        }

        // ============ FormalCountMonitor rules ============
        [Test]
        public void FormalCountMonitor_MissingInmateOnCellCount_RaisesLockdown()
        {
            Assert.IsTrue(FormalCountMonitor.ShouldRaiseLockdown(PrisonEventType.MiddayCount, 15, 16));
            Assert.IsTrue(FormalCountMonitor.ShouldRaiseLockdown(PrisonEventType.EveningCount, 0, 16));
        }

        [Test]
        public void FormalCountMonitor_AllAccountedFor_NoLockdown()
        {
            Assert.IsFalse(FormalCountMonitor.ShouldRaiseLockdown(PrisonEventType.MiddayCount, 16, 16));
            Assert.IsFalse(FormalCountMonitor.ShouldRaiseLockdown(PrisonEventType.EveningCount, 16, 16));
        }

        [Test]
        public void FormalCountMonitor_EmptyFacilityOrNonCountPhase_NoLockdown()
        {
            Assert.IsFalse(FormalCountMonitor.ShouldRaiseLockdown(PrisonEventType.MiddayCount, 0, 0));
            Assert.IsFalse(FormalCountMonitor.ShouldRaiseLockdown(PrisonEventType.FreeTime, 3, 16));
            Assert.IsFalse(FormalCountMonitor.ShouldRaiseLockdown(PrisonEventType.WorkProgram, 3, 16));
        }

        [Test]
        public void FormalCountMonitor_LockdownReason_NamesPhaseAndCount()
        {
            string reason = FormalCountMonitor.BuildLockdownReason(PrisonEventType.MiddayCount, 15, 16);
            StringAssert.Contains("Midday Count", reason);
            StringAssert.Contains("15/16", reason);
        }

        // ============ PrisonRoutineLabels.FormatPhaseTitle ============
        [TestCase(PrisonEventType.MorningRollCall, "MORNING ROLL CALL")]
        [TestCase(PrisonEventType.NightRollCall, "NIGHT ROLL CALL")]
        [TestCase(PrisonEventType.RollCall, "ROLL CALL")]
        [TestCase(PrisonEventType.Breakfast, "BREAKFAST")]
        [TestCase(PrisonEventType.Lunch, "LUNCH")]
        [TestCase(PrisonEventType.Dinner, "DINNER")]
        [TestCase(PrisonEventType.FreeTime, "FREE TIME")]
        [TestCase(PrisonEventType.LightsOut, "LIGHTS OUT")]
        [TestCase(PrisonEventType.WorkProgram, "WORK / PROGRAMS")]
        [TestCase(PrisonEventType.MiddayCount, "MIDDAY COUNT")]
        [TestCase(PrisonEventType.EveningCount, "EVENING COUNT")]
        public void FormatPhaseTitle_Uppercase(PrisonEventType evt, string expected)
            => Assert.AreEqual(expected, PrisonRoutineLabels.FormatPhaseTitle(evt));

        [Test]
        public void FormatPhaseTitle_NonUppercase_TitleCase()
            => Assert.AreEqual("Morning Roll Call", PrisonRoutineLabels.FormatPhaseTitle(PrisonEventType.MorningRollCall, false));

        // ============ PrisonRoutineLabels.FormatPlayerLocation ============
        [Test]
        public void FormatPlayerLocation_Null_Dash()
            => Assert.AreEqual("\u2014", PrisonRoutineLabels.FormatPlayerLocation(null));

        [Test]
        public void FormatPlayerLocation_Empty_Dash()
            => Assert.AreEqual("\u2014", PrisonRoutineLabels.FormatPlayerLocation(""));

        [Test]
        public void FormatPlayerLocation_StripsLocPrefix()
            => Assert.AreEqual("CELL 3", PrisonRoutineLabels.FormatPlayerLocation("LOC: CELL 3"));

        [Test]
        public void FormatPlayerLocation_NoPrefix_ReturnedAsIs()
            => Assert.AreEqual("YARD", PrisonRoutineLabels.FormatPlayerLocation("YARD"));

        // ============ PrisonRoutineLabels.GetGoToLabel (registry-null fallbacks) ============
        [Test]
        public void GetGoToLabel_FallbacksWhenNoRegistry()
        {
            Assume.That(PrisonLocationRegistry.Instance == null,
                "A PrisonLocationRegistry singleton is present; fallback labels can't be asserted.");
            Assert.AreEqual("CAFETERIA", PrisonRoutineLabels.GetGoToLabel(PrisonEventType.Breakfast, 0));
            Assert.AreEqual("CAFETERIA", PrisonRoutineLabels.GetGoToLabel(PrisonEventType.Lunch, 0));
            Assert.AreEqual("CAFETERIA", PrisonRoutineLabels.GetGoToLabel(PrisonEventType.Dinner, 0));
            Assert.AreEqual("YARD", PrisonRoutineLabels.GetGoToLabel(PrisonEventType.FreeTime, 0));
            Assert.AreEqual("ROLL CALL AREA", PrisonRoutineLabels.GetGoToLabel(PrisonEventType.MorningRollCall, 0));
            Assert.AreEqual("ROLL CALL AREA", PrisonRoutineLabels.GetGoToLabel(PrisonEventType.RollCall, 0));
            Assert.AreEqual("CELL 3", PrisonRoutineLabels.GetGoToLabel(PrisonEventType.LightsOut, 3));
            Assert.AreEqual("CELL 3", PrisonRoutineLabels.GetGoToLabel(PrisonEventType.MiddayCount, 3));
            Assert.AreEqual("CELL 3", PrisonRoutineLabels.GetGoToLabel(PrisonEventType.EveningCount, 3));
            Assert.AreEqual("WORKSHOP", PrisonRoutineLabels.GetGoToLabel(PrisonEventType.WorkProgram, 0));
        }

        // ============ PrisonLocationZone.GetHudLabel ============
        private PrisonLocationZone NewZone(ZoneType type, int cellIndex = 0, string hud = null)
        {
            var go = new GameObject("Zone");
            _created.Add(go);
            var z = go.AddComponent<PrisonLocationZone>();
            z.zoneType = type;
            z.cellIndex = cellIndex;
            z.hudDisplayName = hud;
            return z;
        }

        [Test]
        public void GetHudLabel_CustomNameWins()
            => Assert.AreEqual("CELL BLOCK B", NewZone(ZoneType.Cell, 1, "CELL BLOCK B").GetHudLabel());

        [Test]
        public void GetHudLabel_Cell_DefaultUsesIndex()
            => Assert.AreEqual("CELL 3", NewZone(ZoneType.Cell, 3).GetHudLabel());

        [Test]
        public void GetHudLabel_Cafeteria_Default()
            => Assert.AreEqual("CAFETERIA", NewZone(ZoneType.Cafeteria).GetHudLabel());

        [Test]
        public void GetHudLabel_Yard_Default()
            => Assert.AreEqual("YARD", NewZone(ZoneType.Yard).GetHudLabel());

        [Test]
        public void GetHudLabel_RollCallArea_Default()
            => Assert.AreEqual("ROLL CALL", NewZone(ZoneType.RollCallArea).GetHudLabel());

        // ============ CellData.InteriorRadius ============
        [TestCase(2.5f, 2.5f)]
        [TestCase(3f, 3f)]
        [TestCase(0f, 2.5f)]
        [TestCase(0.04f, 2.5f)]
        [TestCase(0.05f, 2.5f)]
        [TestCase(0.06f, 0.06f)]
        public void InteriorRadius_FallbackThreshold(float input, float expected)
        {
            var cell = new CellData { interiorCheckRadius = input };
            Assert.AreEqual(expected, cell.InteriorRadius, 1e-4f);
        }

        // ============ Enum contracts (guard against accidental reorder) ============
        [Test]
        public void ReputationTier_IntValues_Pinned()
        {
            Assert.AreEqual(0, (int)ReputationTier.Outsider);
            Assert.AreEqual(1, (int)ReputationTier.Associate);
            Assert.AreEqual(2, (int)ReputationTier.Respected);
            Assert.AreEqual(3, (int)ReputationTier.Kingpin);
        }

        [Test]
        public void PrisonEventType_IntValues_Pinned()
        {
            // Serialized as ints in PrisonSchedule.asset, guard onDutyDuring arrays,
            // and RestrictedZone phase lists — reordering silently remaps them all.
            Assert.AreEqual(0, (int)PrisonEventType.RollCall);
            Assert.AreEqual(1, (int)PrisonEventType.Breakfast);
            Assert.AreEqual(2, (int)PrisonEventType.Lunch);
            Assert.AreEqual(3, (int)PrisonEventType.Dinner);
            Assert.AreEqual(4, (int)PrisonEventType.FreeTime);
            Assert.AreEqual(5, (int)PrisonEventType.LightsOut);
            Assert.AreEqual(6, (int)PrisonEventType.MorningRollCall);
            Assert.AreEqual(7, (int)PrisonEventType.NightRollCall);
            Assert.AreEqual(8, (int)PrisonEventType.WorkProgram);
            Assert.AreEqual(9, (int)PrisonEventType.MiddayCount);
            Assert.AreEqual(10, (int)PrisonEventType.EveningCount);
        }
    }
}
