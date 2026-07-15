using UnityEngine;
using UnityEngine.AI;

namespace Prison
{
    /// <summary>
    /// Single source of truth for "where must the player be right now" — used by both the
    /// objective waypoint (<see cref="ObjectiveWaypointUI"/>) and the top routine bar
    /// (<see cref="RoutineNowNextBarUI"/>) so they never disagree on the destination.
    /// </summary>
    public static class PrisonRoutineDestination
    {
        /// <summary>Resolved "go here now" objective for the current schedule state.</summary>
        public struct RoutineObjective
        {
            /// <summary>Whether the player currently has somewhere they must go.</summary>
            public bool Show;
            /// <summary>The phase whose venue is being targeted (current, next, or cell line-up).</summary>
            public PrisonEventType Event;
            /// <summary>The world stand point to walk to (may be null if unresolved).</summary>
            public Transform Stand;
            /// <summary>Venue label, e.g. "CAFETERIA" or "CELL 1".</summary>
            public string Label;
            /// <summary>Destination is the player's own cell (roll call / counts / night).</summary>
            public bool InCell;
        }

        /// <summary>
        /// Resolves the active objective. Travel grace targets the CURRENT mandatory phase's
        /// venue (that's where the grace is letting you walk to); a high-stakes warning during
        /// free time targets the NEXT phase's venue. Free time with no warning has no objective.
        /// </summary>
        public static RoutineObjective ResolveActiveDestination(PrisonTimeManager tm, PrisonerController prisoner)
        {
            var obj = new RoutineObjective { Event = tm != null ? tm.CurrentEvent : default };
            if (tm == null || prisoner == null)
                return obj;

            var registry = PrisonLocationRegistry.Instance;
            int cellIndex = prisoner.CellIndex;
            tm.GetNextEventInfo(out PrisonEventType nextEvent, out _);

            // Morning roll call: line up / wait in cell until this cell is shakedown-cleared,
            // then release toward the next phase's venue.
            if (tm.IsMorningRollCallShakedownGateActive)
            {
                bool released = MorningRollCallTracker.Instance != null
                    && MorningRollCallTracker.Instance.IsInmateShakedownComplete(prisoner);
                if (released)
                {
                    FillVenue(ref obj, registry, nextEvent, cellIndex);
                    obj.Show = !prisoner.IsAtRequiredLocation;
                }
                else
                {
                    FillCell(ref obj, registry, cellIndex);
                    obj.Label = PrisonRoutineLabels.GetMorningRollCallLineUpDestinationLabel(cellIndex);
                    obj.Show = true;
                }
                return obj;
            }

            // Free time now, mandatory next: point at the upcoming venue so the player can pre-position.
            if (tm.IsHighStakesTransitionWarningActive)
            {
                FillVenue(ref obj, registry, nextEvent, cellIndex);
                obj.Show = true;
                return obj;
            }

            // Flexible phase (free time) with no warning: nowhere you must be.
            if (!PrisonEventRules.IsMandatory(tm.CurrentEvent))
            {
                obj.Show = false;
                return obj;
            }

            // Mandatory phase (meals, work, counts, night, or travel grace into one):
            // the destination is THIS phase's venue.
            FillVenue(ref obj, registry, tm.CurrentEvent, cellIndex);
            obj.Show = !prisoner.IsCompliant || !prisoner.IsAtRequiredLocation;
            return obj;
        }

        private static void FillVenue(ref RoutineObjective obj, PrisonLocationRegistry registry, PrisonEventType evt, int cellIndex)
        {
            obj.Event = evt;
            if (IsInCellObjective(evt))
            {
                FillCell(ref obj, registry, cellIndex);
                return;
            }

            obj.InCell = false;
            obj.Label = PrisonRoutineLabels.GetGoToLabel(evt, cellIndex);
            obj.Stand = registry != null ? registry.GetStandPointForEvent(evt, cellIndex) : null;
        }

        private static void FillCell(ref RoutineObjective obj, PrisonLocationRegistry registry, int cellIndex)
        {
            obj.InCell = true;
            var cell = registry != null ? registry.GetCell(cellIndex) : null;
            obj.Stand = cell?.rollCallStandPoint != null ? cell.rollCallStandPoint : cell?.spawnPoint;
            obj.Label = registry != null ? registry.GetCellHudLabel(cellIndex) : $"CELL {cellIndex}";
        }

        public static bool ShouldShowWaypoint(PrisonTimeManager tm, PrisonerController prisoner)
            => ResolveActiveDestination(tm, prisoner).Show;

        public static bool TryGetDestination(PrisonTimeManager tm, PrisonerController prisoner, out Vector3 worldPos, out string label)
        {
            worldPos = Vector3.zero;
            label = string.Empty;

            var obj = ResolveActiveDestination(tm, prisoner);
            if (!obj.Show || obj.Stand == null)
                return false;

            label = obj.Label;
            worldPos = obj.Stand.position;
            if (NavMesh.SamplePosition(worldPos, out NavMeshHit hit, 2.5f, NavMesh.AllAreas))
                worldPos = hit.position;

            return !string.IsNullOrEmpty(label);
        }

        public static bool IsInCellObjective(PrisonEventType evt)
        {
            return evt is PrisonEventType.MorningRollCall
                or PrisonEventType.RollCall
                or PrisonEventType.LightsOut
                or PrisonEventType.NightRollCall
                or PrisonEventType.MiddayCount
                or PrisonEventType.EveningCount;
        }
    }
}
