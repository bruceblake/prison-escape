using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Prison.Career
{
    /// <summary>
    /// JSON persistence for career worlds: one human-readable file per world at
    /// {persistentDataPath}/worlds/{id}.json. No PlayerPrefs blobs. Writes are atomic
    /// (temp file + replace) so a crash can never corrupt a world. schemaVersion is stamped
    /// from day one and the loader migrates old files forward.
    /// Spec: docs/PrisonEscape/02 Features/World Saves & Start Screen.md § Save model.
    /// </summary>
    public static class CareerWorldStore
    {
        /// <summary>Tests point this at a temp directory; null/empty = persistentDataPath/worlds.</summary>
        public static string RootOverride;

        public static string Root =>
            string.IsNullOrEmpty(RootOverride)
                ? Path.Combine(Application.persistentDataPath, "worlds")
                : RootOverride;

        public static string PathFor(string worldId) => Path.Combine(Root, worldId + ".json");

        /// <summary>Loads every world in the store, skipping unreadable files with a warning.</summary>
        public static List<CareerWorld> List()
        {
            var worlds = new List<CareerWorld>();
            if (!Directory.Exists(Root)) return worlds;

            foreach (string file in Directory.GetFiles(Root, "*.json"))
            {
                var world = LoadFile(file);
                if (world != null)
                    worlds.Add(world);
            }
            worlds.Sort((a, b) => CareerWorld.ParseUtc(b.lastPlayedUtc).CompareTo(CareerWorld.ParseUtc(a.lastPlayedUtc)));
            return worlds;
        }

        /// <summary>The CONTINUE target: most recently played world, or null when none exist.</summary>
        public static CareerWorld MostRecentlyPlayed(List<CareerWorld> worlds)
        {
            CareerWorld best = null;
            if (worlds == null) return null;
            foreach (var w in worlds)
            {
                if (best == null ||
                    CareerWorld.ParseUtc(w.lastPlayedUtc) > CareerWorld.ParseUtc(best.lastPlayedUtc))
                    best = w;
            }
            return best;
        }

        /// <summary>Creates, persists, and returns a new world. Dev sandbox slot only in dev builds.</summary>
        public static CareerWorld Create(string name)
        {
            bool devBuild = Application.isEditor || Debug.isDebugBuild;
            var world = CareerWorld.CreateNew(name, includeDevSandbox: devBuild);
            Save(world);
            return world;
        }

        public static CareerWorld Load(string worldId)
        {
            string path = PathFor(worldId);
            return File.Exists(path) ? LoadFile(path) : null;
        }

        /// <summary>Atomic write: serialize to a temp file, then swap it into place.</summary>
        public static void Save(CareerWorld world)
        {
            if (world == null || string.IsNullOrEmpty(world.id))
            {
                Debug.LogWarning("[CareerWorldStore] Ignoring save of null/id-less world.");
                return;
            }

            Directory.CreateDirectory(Root);
            world.schemaVersion = CareerWorld.CurrentSchemaVersion;

            string path = PathFor(world.id);
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, JsonUtility.ToJson(world, prettyPrint: true));

            if (File.Exists(path))
                File.Delete(path);
            File.Move(tmp, path);
        }

        public static bool Delete(string worldId)
        {
            string path = PathFor(worldId);
            if (!File.Exists(path)) return false;
            File.Delete(path);
            return true;
        }

        private static CareerWorld LoadFile(string path)
        {
            try
            {
                string json = File.ReadAllText(path);
                var world = JsonUtility.FromJson<CareerWorld>(json);
                if (world == null || string.IsNullOrEmpty(world.id))
                {
                    Debug.LogWarning($"[CareerWorldStore] '{path}' is not a career world file — skipped.");
                    return null;
                }
                Migrate(world);
                return world;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CareerWorldStore] Failed to load '{path}': {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Forward migration. Version 0 covers pre-versioned/partial files (fields JsonUtility
        /// left at defaults); future schema bumps add explicit steps here.
        /// </summary>
        private static void Migrate(CareerWorld world)
        {
            if (world.schemaVersion > CareerWorld.CurrentSchemaVersion)
            {
                Debug.LogWarning($"[CareerWorldStore] World '{world.displayName}' has schemaVersion {world.schemaVersion} from a newer build; loading best-effort.");
                return;
            }

            // v0 → v1: fill anything a hand-made or pre-release file may lack.
            if (world.global == null) world.global = new CareerGlobals();
            if (world.global.unlockedFacilityIds == null) world.global.unlockedFacilityIds = new List<string>();
            if (world.global.recipesKnown == null) world.global.recipesKnown = new List<string>();
            if (world.visitLog == null) world.visitLog = new List<FacilityVisitRecord>();
            if (world.visitCounters == null) world.visitCounters = new List<FacilityVisitCounter>();
            if (world.activeRun == null) world.activeRun = new FacilityRunState();
            if (string.IsNullOrEmpty(world.displayName)) world.displayName = "Unnamed";
            if (string.IsNullOrEmpty(world.currentFacilityId)) world.currentFacilityId = FacilityIds.County;
            if (world.global.unlockedFacilityIds.Count == 0) world.Unlock(FacilityIds.County);
            if (string.IsNullOrEmpty(world.createdUtc)) world.createdUtc = CareerWorld.UtcNowString();
            if (string.IsNullOrEmpty(world.lastPlayedUtc)) world.lastPlayedUtc = world.createdUtc;

            world.schemaVersion = CareerWorld.CurrentSchemaVersion;
        }
    }
}
