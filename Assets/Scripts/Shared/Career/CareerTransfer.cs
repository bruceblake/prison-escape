using System;

namespace Prison.Career
{
    public enum TransferKind
    {
        /// <summary>Crossed the boundary at tier &lt; 8: "CAUGHT — TRANSFERRED".</summary>
        Escaped,
        /// <summary>Served out the County sentence: "SENTENCE COMPLETE".</summary>
        SentenceServed,
        /// <summary>Escaped the top Federal facility: "CAREER CLEARED". World stays playable.</summary>
        CareerWin,
    }

    /// <summary>What a transfer did to the world — feeds the ceremony screen's ledger beat.</summary>
    public class TransferResult
    {
        public TransferKind kind;
        public string fromFacilityId;
        /// <summary>Facility unlocked and set current; null on career win.</summary>
        public string nextFacilityId;
        public float respectAwarded;
        public float respectAfter;
        public int cashCarried;
        public int daysSpent;
        public int itemsConfiscated;
        public bool careerWon;
        /// <summary>True when nextFacilityId was newly unlocked (false when re-climbing the ladder).</summary>
        public bool unlockedNewFacility;
    }

    /// <summary>
    /// The graduation state machine: escape = transfer, not freedom. Pure logic over
    /// <see cref="CareerWorld"/> so the ordering contract is EditMode-testable
    /// (visitLog → respect/totals → unlock/current → confiscation; the caller saves after).
    /// Spec: docs/PrisonEscape/02 Features/Facility Transfer & Graduation.md § State changes.
    /// </summary>
    public static class CareerTransfer
    {
        /// <summary>
        /// Completes the active run at a ladder facility, by escape or served sentence.
        /// Mutates the world in spec order and returns the ledger; caller persists via the store.
        /// </summary>
        public static TransferResult Complete(CareerWorld world, bool escaped, int itemsConfiscated)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            var run = world.activeRun;
            if (run == null || !run.IsActive)
                throw new InvalidOperationException("No active facility run to complete.");

            string facilityId = run.facilityId;
            int tier = FacilityIds.LadderIndexOf(facilityId);
            if (tier < 0)
                throw new InvalidOperationException($"Facility '{facilityId}' is not on the career ladder (dev sandbox runs never transfer).");

            int daysSpent = Math.Max(1, run.day);

            // 1. Visit log first — the career record exists even if a later step ever fails.
            world.visitLog.Add(new FacilityVisitRecord
            {
                facilityId = facilityId,
                visitIndex = run.visitIndex,
                daysSpent = daysSpent,
                escaped = escaped,
                endedUtc = CareerWorld.UtcNowString(),
            });

            // 2. Respect award + career totals.
            float award = escaped ? CareerRespectMath.EscapeAward(tier) : CareerRespectMath.SentenceServedAward;
            world.global.respect = CareerRespectMath.Clamp(world.global.respect + award);
            world.global.totalTransfers++;
            world.global.totalDaysLived += daysSpent;

            // 3. Unlock next / career win.
            bool top = FacilityIds.IsTopOfLadder(facilityId);
            string next = top ? null : FacilityIds.NextOnLadder(facilityId);
            bool newlyUnlocked = false;
            if (top)
            {
                world.global.careerWon = true;
            }
            else
            {
                newlyUnlocked = world.Unlock(next);
                world.currentFacilityId = next;
            }

            // 4. Confiscate: the run state is discarded wholesale (inventory AND pillow stash —
            //    World Rules 24's stash-survives rule applies to solitary, not transfer).
            world.activeRun = new FacilityRunState();

            return new TransferResult
            {
                kind = top ? TransferKind.CareerWin : (escaped ? TransferKind.Escaped : TransferKind.SentenceServed),
                fromFacilityId = facilityId,
                nextFacilityId = next,
                respectAwarded = award,
                respectAfter = world.global.respect,
                cashCarried = world.global.cash,
                daysSpent = daysSpent,
                itemsConfiscated = itemsConfiscated,
                careerWon = world.global.careerWon,
                unlockedNewFacility = newlyUnlocked,
            };
        }
    }

    /// <summary>
    /// Soft transfer gates: whether the boundary *route* is affordable, expressed in-fiction
    /// before the boundary. The boundary trigger itself never rejects a player who reaches it.
    /// </summary>
    public static class CareerGates
    {
        public static bool CanAttemptEscape(CareerGlobals globals, FacilityInfo facility)
        {
            if (globals == null || facility == null) return true;
            switch (facility.gateMode)
            {
                case TransferGateMode.Any:
                    return globals.cash >= facility.gateCash || globals.respect >= facility.gateRespect;
                case TransferGateMode.All:
                    return globals.cash >= facility.gateCash && globals.respect >= facility.gateRespect;
                default:
                    return true;
            }
        }
    }
}
