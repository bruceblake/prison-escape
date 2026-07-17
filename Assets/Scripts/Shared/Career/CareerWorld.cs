using System;
using System.Collections.Generic;

namespace Prison.Career
{
    /// <summary>Global carry — power and identity are global; physical situation is local.</summary>
    [Serializable]
    public class CareerGlobals
    {
        public int cash;
        /// <summary>Career Respect 0–100. Shared save field with Social v2 when it lands.</summary>
        public float respect;
        /// <summary>Gang affiliation is stored from day one so the schema never migrates for Social v2.</summary>
        public string gangId = "";
        public int gangRank;
        public int mentalHealth = 100;
        public int physicalHealth = 100;
        public int strength = 100;
        public List<string> unlockedFacilityIds = new List<string>();
        public List<string> recipesKnown = new List<string>();
        public bool careerWon;
        public int totalDaysLived;
        public int totalTransfers;
    }

    /// <summary>One completed stay at a facility (appended at transfer / run end).</summary>
    [Serializable]
    public class FacilityVisitRecord
    {
        public string facilityId;
        public int visitIndex;
        public int daysSpent;
        public bool escaped;
        public string endedUtc;
    }

    /// <summary>Started-visit counter per facility so abandoned runs still advance the seed.</summary>
    [Serializable]
    public class FacilityVisitCounter
    {
        public string facilityId;
        public int startedVisits;
    }

    /// <summary>
    /// One named career save ("world"): ladder progression + global carry, JSON-serialized by
    /// <see cref="CareerWorldStore"/>. Pure data + pure logic; EditMode-testable.
    /// Spec: docs/PrisonEscape/01 Game Design/Prison Career Ladder.md § Data model.
    /// </summary>
    [Serializable]
    public class CareerWorld
    {
        public const int CurrentSchemaVersion = 1;
        public const int DisplayNameMaxLength = 24;

        public int schemaVersion = CurrentSchemaVersion;
        public string id;
        public string displayName;
        public string createdUtc;
        public string lastPlayedUtc;
        public string currentFacilityId;
        public CareerGlobals global = new CareerGlobals();
        public List<FacilityVisitRecord> visitLog = new List<FacilityVisitRecord>();
        public List<FacilityVisitCounter> visitCounters = new List<FacilityVisitCounter>();
        /// <summary>The in-progress local run; overwritten on every facility entry.</summary>
        public FacilityRunState activeRun = new FacilityRunState();

        public static string UtcNowString() => DateTime.UtcNow.ToString("o");

        public static DateTime ParseUtc(string iso)
        {
            if (DateTime.TryParse(iso, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
                return dt;
            return DateTime.MinValue;
        }

        /// <summary>New world: County unlocked (+ dev sandbox in development builds), globals zeroed, stats full.</summary>
        public static CareerWorld CreateNew(string name, bool includeDevSandbox)
        {
            var world = new CareerWorld
            {
                id = Guid.NewGuid().ToString("N"),
                displayName = SanitizeDisplayName(name),
                createdUtc = UtcNowString(),
                lastPlayedUtc = UtcNowString(),
                currentFacilityId = FacilityIds.County,
            };
            world.Unlock(FacilityIds.County);
            if (includeDevSandbox)
                world.Unlock(FacilityIds.DevSandbox);
            return world;
        }

        /// <summary>Free text, 1–24 chars; file identity is the guid so duplicates are safe.</summary>
        public static string SanitizeDisplayName(string name)
        {
            name = string.IsNullOrWhiteSpace(name) ? "Unnamed" : name.Trim();
            if (name.Length > DisplayNameMaxLength)
                name = name.Substring(0, DisplayNameMaxLength);
            return name;
        }

        public bool IsUnlocked(string facilityId) =>
            global.unlockedFacilityIds.Contains(facilityId);

        /// <summary>Idempotent; returns true when the facility was newly unlocked.</summary>
        public bool Unlock(string facilityId)
        {
            if (string.IsNullOrEmpty(facilityId) || IsUnlocked(facilityId))
                return false;
            global.unlockedFacilityIds.Add(facilityId);
            return true;
        }

        /// <summary>Completed stays at this facility (transfers/run ends, not abandoned runs).</summary>
        public int CompletedVisitCount(string facilityId)
        {
            int n = 0;
            foreach (var v in visitLog)
                if (v.facilityId == facilityId)
                    n++;
            return n;
        }

        /// <summary>Visits started at this facility, including abandoned ones — drives the seed.</summary>
        public int StartedVisitCount(string facilityId)
        {
            foreach (var c in visitCounters)
                if (c.facilityId == facilityId)
                    return c.startedVisits;
            return 0;
        }

        /// <summary>
        /// Starts a fresh local run at an unlocked facility: Day 1, new deterministic seed,
        /// visitIndex advanced past every previously started visit. Global carry stays intact.
        /// </summary>
        public FacilityRunState BeginVisit(string facilityId)
        {
            FacilityVisitCounter counter = null;
            foreach (var c in visitCounters)
                if (c.facilityId == facilityId)
                {
                    counter = c;
                    break;
                }
            if (counter == null)
            {
                counter = new FacilityVisitCounter { facilityId = facilityId };
                visitCounters.Add(counter);
            }
            counter.startedVisits++;

            activeRun = new FacilityRunState
            {
                facilityId = facilityId,
                visitIndex = counter.startedVisits,
                day = 1,
                worldSeed = CareerSeed.VisitSeed(id, facilityId, counter.startedVisits),
            };
            currentFacilityId = facilityId;
            return activeRun;
        }
    }
}
