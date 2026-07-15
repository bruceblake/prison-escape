using UnityEngine;
using UnityEngine.AI;

namespace Prison
{
    /// <summary>
    /// Resolves objective waypoint label + world position during mandatory schedule phases.
    /// Mirrors <see cref="RoutineNowNextBarUI"/> destination logic.
    /// </summary>
    public static class PrisonRoutineDestination
    {
        public static bool ShouldShowWaypoint(PrisonTimeManager tm, PrisonerController prisoner)
        {
            if (tm == null || prisoner == null)
                return false;

            if (tm.IsHighStakesTransitionWarningActive)
                return true;

            if (tm.IsMandatoryTravelGraceActive)
                return !prisoner.IsAtRequiredLocation;

            if (tm.IsMorningRollCallShakedownGateActive)
            {
                if (MorningRollCallTracker.Instance != null && MorningRollCallTracker.Instance.IsInmateShakedownComplete(prisoner))
                    return !prisoner.IsAtRequiredLocation;
                return true;
            }

            if (!PrisonEventRules.IsMandatory(tm.CurrentEvent))
                return false;

            return !prisoner.IsCompliant || !prisoner.IsAtRequiredLocation;
        }

        public static bool TryGetDestination(PrisonTimeManager tm, PrisonerController prisoner, out Vector3 worldPos, out string label)
        {
            worldPos = Vector3.zero;
            label = string.Empty;

            if (tm == null || prisoner == null)
                return false;

            tm.GetNextEventInfo(out PrisonEventType nextEvent, out _);
            int cellIndex = prisoner.CellIndex;

            string goTo = PrisonRoutineLabels.GetGoToLabel(tm.CurrentEvent, cellIndex);
            string nextGoTo = PrisonRoutineLabels.GetGoToLabel(nextEvent, cellIndex);
            label = ResolveDestinationLabel(tm, prisoner, goTo, nextGoTo);

            PrisonEventType destEvent = ResolveDestinationEvent(tm, prisoner, nextEvent);
            var registry = PrisonLocationRegistry.Instance;
            if (registry == null)
                return false;

            Transform stand = ResolveStandPoint(registry, destEvent, cellIndex, tm, prisoner);
            if (stand == null)
                return false;

            worldPos = stand.position;
            if (NavMesh.SamplePosition(worldPos, out NavMeshHit hit, 2.5f, NavMesh.AllAreas))
                worldPos = hit.position;

            return !string.IsNullOrEmpty(label);
        }

        public static bool IsInCellObjective(PrisonEventType evt)
        {
            return evt is PrisonEventType.MorningRollCall
                or PrisonEventType.RollCall
                or PrisonEventType.LightsOut
                or PrisonEventType.NightRollCall;
        }

        static Transform ResolveStandPoint(
            PrisonLocationRegistry registry,
            PrisonEventType destEvent,
            int cellIndex,
            PrisonTimeManager tm,
            PrisonerController prisoner)
        {
            var cell = registry.GetCell(cellIndex);

            if (destEvent is PrisonEventType.MorningRollCall or PrisonEventType.RollCall)
            {
                bool released = MorningRollCallTracker.IsInmateReleasedFromRollCallStand(prisoner);
                if (!released && cell?.spawnPoint != null)
                    return cell.spawnPoint;
            }

            if (destEvent is PrisonEventType.LightsOut or PrisonEventType.NightRollCall)
            {
                if (cell?.spawnPoint != null)
                    return cell.spawnPoint;
            }

            return registry.GetStandPointForEvent(destEvent, cellIndex);
        }

        private static PrisonEventType ResolveDestinationEvent(PrisonTimeManager tm, PrisonerController prisoner, PrisonEventType nextEvent)
        {
            bool travelGraceNonCompliant = tm.IsMandatoryTravelGraceActive
                && prisoner != null
                && !prisoner.IsCompliant;

            bool enforcement = !tm.IsMandatoryTravelGraceActive
                && PrisonEventRules.IsMandatory(tm.CurrentEvent)
                && prisoner != null
                && !prisoner.IsCompliant
                && !MorningRollCallTracker.IsInmateReleasedFromRollCallStand(prisoner);

            if (tm.IsHighStakesTransitionWarningActive || travelGraceNonCompliant || enforcement)
                return nextEvent;

            if (tm.IsMorningRollCallShakedownGateActive)
            {
                bool released = prisoner != null
                    && MorningRollCallTracker.Instance != null
                    && MorningRollCallTracker.Instance.IsInmateShakedownComplete(prisoner);
                if (released)
                    return nextEvent;
            }

            return tm.CurrentEvent;
        }

        private static string ResolveDestinationLabel(PrisonTimeManager tm, PrisonerController prisoner, string goTo, string nextGoTo)
        {
            bool travelGraceNonCompliant = tm.IsMandatoryTravelGraceActive
                && prisoner != null
                && !prisoner.IsCompliant;

            bool enforcement = !tm.IsMandatoryTravelGraceActive
                && PrisonEventRules.IsMandatory(tm.CurrentEvent)
                && prisoner != null
                && !prisoner.IsCompliant
                && !MorningRollCallTracker.IsInmateReleasedFromRollCallStand(prisoner);

            if (tm.IsHighStakesTransitionWarningActive || travelGraceNonCompliant || enforcement)
                return nextGoTo;

            if (tm.IsMorningRollCallShakedownGateActive)
            {
                bool released = prisoner != null
                    && MorningRollCallTracker.Instance != null
                    && MorningRollCallTracker.Instance.IsInmateShakedownComplete(prisoner);
                if (released)
                    return nextGoTo;
                return PrisonRoutineLabels.GetMorningRollCallLineUpDestinationLabel(prisoner?.CellIndex ?? 0);
            }

            return goTo;
        }
    }
}
