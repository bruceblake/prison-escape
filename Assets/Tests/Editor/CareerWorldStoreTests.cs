using System.IO;
using NUnit.Framework;
using Prison.Career;

namespace Prison.Tests
{
    /// <summary>
    /// World-save model and JSON store coverage per the test plan in
    /// docs/PrisonEscape/02 Features/World Saves & Start Screen.md.
    /// </summary>
    public class CareerWorldStoreTests
    {
        private string _root;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "career_store_tests_" + System.Guid.NewGuid().ToString("N"));
            CareerWorldStore.RootOverride = _root;
        }

        [TearDown]
        public void TearDown()
        {
            CareerWorldStore.RootOverride = null;
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }

        // ------------------------------------------------------------------
        // Create / load / save / delete round-trip
        // ------------------------------------------------------------------

        [Test]
        public void Create_UnlocksCountyAndZeroesGlobals()
        {
            var world = CareerWorld.CreateNew("Lifer", includeDevSandbox: false);

            Assert.IsTrue(world.IsUnlocked(FacilityIds.County));
            Assert.IsFalse(world.IsUnlocked(FacilityIds.StateMin));
            Assert.AreEqual(FacilityIds.County, world.currentFacilityId);
            Assert.AreEqual(0, world.global.cash);
            Assert.AreEqual(0f, world.global.respect);
            Assert.AreEqual(100, world.global.mentalHealth);
            Assert.AreEqual(100, world.global.physicalHealth);
            Assert.AreEqual(100, world.global.strength);
            Assert.IsFalse(world.global.careerWon);
        }

        [Test]
        public void Create_DevSandboxOnlyInDevBuilds()
        {
            var withDev = CareerWorld.CreateNew("A", includeDevSandbox: true);
            var withoutDev = CareerWorld.CreateNew("B", includeDevSandbox: false);

            Assert.IsTrue(withDev.IsUnlocked(FacilityIds.DevSandbox));
            Assert.IsFalse(withoutDev.IsUnlocked(FacilityIds.DevSandbox));
        }

        [Test]
        public void SaveLoad_RoundTripsAllCareerFields()
        {
            var world = CareerWorld.CreateNew("Roundtrip", includeDevSandbox: false);
            world.global.cash = 1240;
            world.global.respect = 38.5f;
            world.global.gangId = "los_locos";
            world.global.gangRank = 2;
            world.global.recipesKnown.Add("shiv");
            world.Unlock(FacilityIds.StateMin);
            world.BeginVisit(FacilityIds.County);
            CareerWorldStore.Save(world);

            var loaded = CareerWorldStore.Load(world.id);

            Assert.IsNotNull(loaded);
            Assert.AreEqual(CareerWorld.CurrentSchemaVersion, loaded.schemaVersion);
            Assert.AreEqual("Roundtrip", loaded.displayName);
            Assert.AreEqual(1240, loaded.global.cash);
            Assert.AreEqual(38.5f, loaded.global.respect);
            Assert.AreEqual("los_locos", loaded.global.gangId);
            Assert.AreEqual(2, loaded.global.gangRank);
            CollectionAssert.Contains(loaded.global.recipesKnown, "shiv");
            Assert.IsTrue(loaded.IsUnlocked(FacilityIds.StateMin));
            Assert.AreEqual(FacilityIds.County, loaded.activeRun.facilityId);
            Assert.AreEqual(world.activeRun.worldSeed, loaded.activeRun.worldSeed);
        }

        [Test]
        public void Delete_RemovesTheWorldFile()
        {
            var world = CareerWorldStore.Create("Doomed");
            Assert.IsNotNull(CareerWorldStore.Load(world.id));

            Assert.IsTrue(CareerWorldStore.Delete(world.id));

            Assert.IsNull(CareerWorldStore.Load(world.id));
            Assert.IsFalse(CareerWorldStore.Delete(world.id), "second delete reports nothing to do");
        }

        [Test]
        public void List_ReturnsAllWorlds_MostRecentFirst()
        {
            var older = CareerWorldStore.Create("Older");
            older.lastPlayedUtc = "2026-07-10T10:00:00.0000000Z";
            CareerWorldStore.Save(older);
            var newer = CareerWorldStore.Create("Newer");
            newer.lastPlayedUtc = "2026-07-14T10:00:00.0000000Z";
            CareerWorldStore.Save(newer);

            var list = CareerWorldStore.List();

            Assert.AreEqual(2, list.Count);
            Assert.AreEqual("Newer", list[0].displayName);
        }

        // ------------------------------------------------------------------
        // Continue pick
        // ------------------------------------------------------------------

        [Test]
        public void MostRecentlyPlayed_PicksMaxLastPlayedUtc()
        {
            var a = CareerWorld.CreateNew("A", false);
            a.lastPlayedUtc = "2026-07-01T00:00:00.0000000Z";
            var b = CareerWorld.CreateNew("B", false);
            b.lastPlayedUtc = "2026-07-15T00:00:00.0000000Z";
            var c = CareerWorld.CreateNew("C", false);
            c.lastPlayedUtc = "2026-07-08T00:00:00.0000000Z";

            var pick = CareerWorldStore.MostRecentlyPlayed(new System.Collections.Generic.List<CareerWorld> { a, b, c });

            Assert.AreSame(b, pick);
        }

        // ------------------------------------------------------------------
        // Schema migration
        // ------------------------------------------------------------------

        [Test]
        public void Load_MigratesPreVersionedFileForward()
        {
            Directory.CreateDirectory(_root);
            // A v0 file: no schemaVersion, no lists, no currentFacilityId.
            File.WriteAllText(Path.Combine(_root, "legacy01.json"),
                "{\"id\":\"legacy01\",\"displayName\":\"Old Timer\"}");

            var loaded = CareerWorldStore.Load("legacy01");

            Assert.IsNotNull(loaded);
            Assert.AreEqual(CareerWorld.CurrentSchemaVersion, loaded.schemaVersion);
            Assert.AreEqual(FacilityIds.County, loaded.currentFacilityId);
            Assert.IsTrue(loaded.IsUnlocked(FacilityIds.County), "migration unlocks the ladder's first rung");
            Assert.IsNotNull(loaded.visitLog);
            Assert.IsNotNull(loaded.global.recipesKnown);
            Assert.IsNotEmpty(loaded.lastPlayedUtc);
        }

        [Test]
        public void Load_SkipsCorruptFilesWithoutThrowing()
        {
            Directory.CreateDirectory(_root);
            File.WriteAllText(Path.Combine(_root, "corrupt.json"), "{not json at all");
            CareerWorldStore.Create("Survivor");

            var list = CareerWorldStore.List(); // corrupt file logs a warning and is skipped

            Assert.AreEqual(1, list.Count);
            Assert.AreEqual("Survivor", list[0].displayName);
        }

        // ------------------------------------------------------------------
        // Atomic write
        // ------------------------------------------------------------------

        [Test]
        public void Save_LeavesNoTempFileAndValidJson()
        {
            var world = CareerWorldStore.Create("Atomic");
            world.global.cash = 999;
            CareerWorldStore.Save(world); // second save overwrites via temp + swap

            Assert.IsFalse(File.Exists(CareerWorldStore.PathFor(world.id) + ".tmp"));
            Assert.AreEqual(999, CareerWorldStore.Load(world.id).global.cash);
            Assert.AreEqual(1, Directory.GetFiles(_root, "*.json").Length);
        }

        // ------------------------------------------------------------------
        // Unlock idempotence
        // ------------------------------------------------------------------

        [Test]
        public void Unlock_IsIdempotent()
        {
            var world = CareerWorld.CreateNew("Idem", false);

            Assert.IsTrue(world.Unlock(FacilityIds.StateMin));
            Assert.IsFalse(world.Unlock(FacilityIds.StateMin));

            int count = 0;
            foreach (string id in world.global.unlockedFacilityIds)
                if (id == FacilityIds.StateMin)
                    count++;
            Assert.AreEqual(1, count);
        }

        // ------------------------------------------------------------------
        // Visit seeds
        // ------------------------------------------------------------------

        [Test]
        public void VisitSeed_IsDeterministicPerVisit_AndFreshPerRevisit()
        {
            Assert.AreEqual(
                CareerSeed.VisitSeed("w1", FacilityIds.County, 1),
                CareerSeed.VisitSeed("w1", FacilityIds.County, 1),
                "same world + facility + visitIndex → same seed");

            Assert.AreNotEqual(
                CareerSeed.VisitSeed("w1", FacilityIds.County, 1),
                CareerSeed.VisitSeed("w1", FacilityIds.County, 2),
                "visitIndex+1 → different seed");

            Assert.AreNotEqual(
                CareerSeed.VisitSeed("w1", FacilityIds.County, 1),
                CareerSeed.VisitSeed("w2", FacilityIds.County, 1),
                "different world → different seed");

            Assert.AreNotEqual(
                CareerSeed.VisitSeed("w1", FacilityIds.County, 1),
                CareerSeed.VisitSeed("w1", FacilityIds.StateMin, 1),
                "different facility → different seed");
        }

        [Test]
        public void BeginVisit_StartsDay1_AdvancesVisitIndex_EvenAfterAbandonedRuns()
        {
            var world = CareerWorld.CreateNew("Revisit", false);

            var first = world.BeginVisit(FacilityIds.County);
            Assert.AreEqual(1, first.visitIndex);
            Assert.AreEqual(1, first.day);
            Assert.AreEqual(FacilityIds.County, world.currentFacilityId);

            // Abandon (no visitLog entry) and re-enter: still a fresh visit with a fresh seed.
            var second = world.BeginVisit(FacilityIds.County);
            Assert.AreEqual(2, second.visitIndex);
            Assert.AreEqual(1, second.day);
            Assert.AreNotEqual(first.worldSeed, second.worldSeed);
        }
    }
}
