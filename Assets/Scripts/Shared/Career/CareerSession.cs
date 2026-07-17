using UnityEngine;
using UnityEngine.SceneManagement;

namespace Prison.Career
{
    /// <summary>
    /// The active career context: which world is selected and which facility run is in progress.
    /// Static so it survives scene loads without DontDestroyOnLoad plumbing. Gameplay systems read
    /// the difficulty multipliers through here — they all default to 1 outside a career run, so
    /// legacy/sandbox play is untouched.
    /// </summary>
    public static class CareerSession
    {
        public static CareerWorld ActiveWorld { get; private set; }
        public static FacilityDefinition ActiveFacility { get; private set; }
        /// <summary>Sandbox runs get a private run state that is never written into the world file.</summary>
        private static FacilityRunState _sandboxRun;

        /// <summary>Set when the ceremony/pause flow returns to MainMenu so the hub reopens on Prison Select.</summary>
        public static bool ReopenPrisonSelect;

        public static FacilityRunState ActiveRun =>
            ActiveFacility != null && ActiveFacility.IsDevSandbox ? _sandboxRun : ActiveWorld?.activeRun;

        public static bool HasActiveRun =>
            ActiveWorld != null && ActiveFacility != null && ActiveRun != null && ActiveRun.IsActive;

        /// <summary>True for a ladder-facility run that reads AND writes the career world.</summary>
        public static bool HasActiveLadderRun => HasActiveRun && !ActiveFacility.IsDevSandbox;

        // --------------------------------------------------------------
        // Difficulty multipliers (1 when no career run is active)
        // --------------------------------------------------------------

        public static float LootAbundance => ActiveFacility != null ? ActiveFacility.lootAbundance : 1f;
        public static float CashIncomeMult => ActiveFacility != null ? ActiveFacility.cashIncomeMult : 1f;
        public static float TradePriceMult => ActiveFacility != null ? ActiveFacility.tradePriceMult : 1f;
        public static float BribeCostMult => ActiveFacility != null ? ActiveFacility.bribeCostMult : 1f;
        public static float EscapeRouteCostMult => ActiveFacility != null ? ActiveFacility.escapeRouteCostMult : 1f;
        public static float DetectionRangeMult => ActiveFacility != null ? ActiveFacility.detectionRangeMult : 1f;
        public static float ShakedownStrictness => ActiveFacility != null ? ActiveFacility.shakedownStrictness : 1f;

        // --------------------------------------------------------------
        // World / facility flow
        // --------------------------------------------------------------

        public static void SelectWorld(CareerWorld world)
        {
            ActiveWorld = world;
            ActiveFacility = null;
            _sandboxRun = null;
        }

        /// <summary>
        /// Enters an unlocked facility: begins a fresh visit (Day 1, new seed), stamps
        /// lastPlayedUtc, saves (ladder runs only — the sandbox never writes back), and loads the
        /// scene. <see cref="CareerRunBootstrap"/> applies the global carry once the scene is up.
        /// </summary>
        public static bool EnterFacility(CareerWorld world, string facilityId)
        {
            var def = FacilityDirectory.Get(facilityId);
            if (world == null || def == null) return false;
            if (!world.IsUnlocked(facilityId))
            {
                Debug.LogWarning($"[CareerSession] '{facilityId}' is locked in world '{world.displayName}'.");
                return false;
            }
            if (!def.HasScene)
            {
                Debug.LogWarning($"[CareerSession] '{facilityId}' has no playable scene yet (under construction).");
                return false;
            }

            ActiveWorld = world;
            ActiveFacility = def;

            if (def.IsDevSandbox)
            {
                // Reads carry, never writes back — no counters, no save, no lastPlayed bump.
                _sandboxRun = new FacilityRunState
                {
                    facilityId = facilityId,
                    visitIndex = 1,
                    day = 1,
                    worldSeed = CareerSeed.VisitSeed(world.id, facilityId, Random.Range(0, 1_000_000)),
                };
            }
            else
            {
                world.BeginVisit(facilityId);
                world.lastPlayedUtc = CareerWorld.UtcNowString();
                CareerWorldStore.Save(world);
            }

            SceneTransitionScreen.Load(def.sceneName, def.title,
                def.IsDevSandbox ? "Sandbox run — nothing here follows you home"
                                 : $"Visit {ActiveRun.visitIndex} · Day 1 · ${world.global.cash:n0} carried");
            return true;
        }

        /// <summary>Pulls live scene values (wallet, stats) into the world's global carry.</summary>
        public static void SyncGlobalsFromScene()
        {
            if (ActiveWorld == null) return;
            var g = ActiveWorld.global;

            if (PlayerWallet.Instance != null)
                g.cash = Mathf.Max(0, Mathf.RoundToInt(PlayerWallet.Instance.Balance));

            var stats = PlayerStats.Instance;
            if (stats != null)
            {
                g.mentalHealth = Mathf.RoundToInt(stats.MentalHealth);
                g.physicalHealth = Mathf.RoundToInt(stats.PhysicalHealth);
                g.strength = Mathf.RoundToInt(stats.Strength);
            }

            // Gang reputation tag carry is wired when Social M1 lands
            // (feat/social-m1-foundation). Until then leave g.gangId / g.gangRank as saved.
        }

        /// <summary>Caught escaping costs career respect (−2); persisted at the next autosave.</summary>
        public static void ApplyCaughtEscapingPenalty()
        {
            if (!HasActiveLadderRun) return;
            ActiveWorld.global.respect =
                CareerRespectMath.Clamp(ActiveWorld.global.respect + CareerRespectMath.CaughtEscapingPenalty);
        }

        /// <summary>
        /// Quit to Prison Select: saves globals (ladder runs), abandons the local run, and returns
        /// to the hub. The abandoned visit still advanced the visit counter, so re-entry rerolls.
        /// </summary>
        public static void QuitToPrisonSelect()
        {
            if (HasActiveLadderRun)
            {
                SyncGlobalsFromScene();
                ActiveWorld.activeRun = new FacilityRunState();
                CareerWorldStore.Save(ActiveWorld);
            }
            EndRunAndReturnToHub();
        }

        /// <summary>Clears the run context and loads the MainMenu hub on Prison Select.</summary>
        public static void EndRunAndReturnToHub()
        {
            ActiveFacility = null;
            _sandboxRun = null;
            ReopenPrisonSelect = ActiveWorld != null;
            SceneTransitionScreen.Load("MainMenu", "Prison Select", "Processing transfer paperwork…");
        }
    }
}
