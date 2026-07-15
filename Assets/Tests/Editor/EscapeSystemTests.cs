using NUnit.Framework;
using Prison;

namespace Prison.Tests
{
    /// <summary>
    /// Pure-logic coverage for the escape completion system:
    /// player stats math, the suspicion day window, and restricted-zone rules.
    /// Spec: docs/PrisonEscape/02 Features/Escape Completion System.md
    /// </summary>
    public class EscapeSystemTests
    {
        // ------------------------------------------------------------------
        // PlayerStatsMath
        // ------------------------------------------------------------------

        [Test]
        public void SolitaryPenalty_ReducesMentalHealthBy20()
        {
            Assert.AreEqual(80f, PlayerStatsMath.ApplySolitaryToMentalHealth(100f));
        }

        [Test]
        public void SolitaryPenalty_ReducesStrengthBy10()
        {
            Assert.AreEqual(90f, PlayerStatsMath.ApplySolitaryToStrength(100f));
        }

        [Test]
        public void SolitaryPenalty_ReducesPhysicalHealthBy10()
        {
            Assert.AreEqual(90f, PlayerStatsMath.ApplySolitaryToPhysicalHealth(100f));
        }

        [Test]
        public void SolitaryPenalty_ClampsAtZero()
        {
            Assert.AreEqual(0f, PlayerStatsMath.ApplySolitaryToMentalHealth(10f));
            Assert.AreEqual(0f, PlayerStatsMath.ApplySolitaryToPhysicalHealth(8f));
            Assert.AreEqual(0f, PlayerStatsMath.ApplySolitaryToStrength(5f));
        }

        [Test]
        public void DailyRegen_Adds5()
        {
            Assert.AreEqual(55f, PlayerStatsMath.ApplyDailyRegen(50f));
        }

        [Test]
        public void DailyRegen_ClampsAt100()
        {
            Assert.AreEqual(100f, PlayerStatsMath.ApplyDailyRegen(98f));
            Assert.AreEqual(100f, PlayerStatsMath.ApplyDailyRegen(100f));
        }

        [Test]
        public void SprintMultiplier_NormalAtOrAboveThreshold()
        {
            Assert.AreEqual(PlayerStatsMath.NormalSprintMultiplier, PlayerStatsMath.SprintMultiplierFor(100f));
            Assert.AreEqual(PlayerStatsMath.NormalSprintMultiplier, PlayerStatsMath.SprintMultiplierFor(50f));
        }

        [Test]
        public void SprintMultiplier_WeakBelowThreshold()
        {
            Assert.AreEqual(PlayerStatsMath.WeakSprintMultiplier, PlayerStatsMath.SprintMultiplierFor(49.9f));
            Assert.AreEqual(PlayerStatsMath.WeakSprintMultiplier, PlayerStatsMath.SprintMultiplierFor(0f));
        }

        [Test]
        public void Clamp_StaysWithinBounds()
        {
            Assert.AreEqual(0f, PlayerStatsMath.Clamp(-10f));
            Assert.AreEqual(100f, PlayerStatsMath.Clamp(250f));
            Assert.AreEqual(42f, PlayerStatsMath.Clamp(42f));
        }

        // ------------------------------------------------------------------
        // SuspicionWindow
        // ------------------------------------------------------------------

        [Test]
        public void Suspicion_InactiveByDefault()
        {
            var w = new SuspicionWindow();
            Assert.IsFalse(w.IsActive);
        }

        [Test]
        public void Suspicion_ActiveAfterRaise()
        {
            var w = new SuspicionWindow();
            w.Raise(2);
            Assert.IsTrue(w.IsActive);
            Assert.AreEqual(2, w.RemainingDays);
        }

        [Test]
        public void Suspicion_ExpiresAfterTwoMornings()
        {
            var w = new SuspicionWindow();
            w.Raise(2);
            w.OnMorningRollCall();
            Assert.IsTrue(w.IsActive);
            w.OnMorningRollCall();
            Assert.IsFalse(w.IsActive);
        }

        [Test]
        public void Suspicion_RaiseNeverShortensActiveWindow()
        {
            var w = new SuspicionWindow();
            w.Raise(3);
            w.Raise(1);
            Assert.AreEqual(3, w.RemainingDays);
        }

        [Test]
        public void Suspicion_MorningWithoutSuspicionIsHarmless()
        {
            var w = new SuspicionWindow();
            w.OnMorningRollCall();
            Assert.IsFalse(w.IsActive);
            Assert.AreEqual(0, w.RemainingDays);
        }

        [Test]
        public void Suspicion_NegativeRaiseIgnored()
        {
            var w = new SuspicionWindow();
            w.Raise(-5);
            Assert.IsFalse(w.IsActive);
        }

        // ------------------------------------------------------------------
        // RestrictedZoneRules
        // ------------------------------------------------------------------

        [Test]
        public void AlwaysRestricted_IgnoresPhase()
        {
            Assert.IsTrue(RestrictedZoneRules.IsRestricted(true, null, PrisonEventType.FreeTime));
            Assert.IsTrue(RestrictedZoneRules.IsRestricted(true, new PrisonEventType[0], PrisonEventType.Breakfast));
        }

        [Test]
        public void PhaseRestricted_ActiveOnlyDuringListedPhases()
        {
            var night = new[] { PrisonEventType.LightsOut, PrisonEventType.NightRollCall };
            Assert.IsTrue(RestrictedZoneRules.IsRestricted(false, night, PrisonEventType.LightsOut));
            Assert.IsTrue(RestrictedZoneRules.IsRestricted(false, night, PrisonEventType.NightRollCall));
            Assert.IsFalse(RestrictedZoneRules.IsRestricted(false, night, PrisonEventType.FreeTime));
            Assert.IsFalse(RestrictedZoneRules.IsRestricted(false, night, PrisonEventType.Breakfast));
        }

        [Test]
        public void PhaseRestricted_EmptyOrNullListNeverRestricts()
        {
            Assert.IsFalse(RestrictedZoneRules.IsRestricted(false, null, PrisonEventType.LightsOut));
            Assert.IsFalse(RestrictedZoneRules.IsRestricted(false, new PrisonEventType[0], PrisonEventType.LightsOut));
        }
    }
}
