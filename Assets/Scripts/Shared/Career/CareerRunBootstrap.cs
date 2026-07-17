using Prison;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Prison.Career
{
    /// <summary>
    /// Spawned automatically in a facility scene entered through the career flow. Re-seeds the
    /// world RNG with the per-visit seed (before GameManager spawns loot), applies the global
    /// carry (cash, stats), drives the day tick at each Morning Count (daily respect at Federal
    /// tiers, day-boundary autosave, County sentence clock), and shows the sentence HUD line.
    /// </summary>
    public class CareerRunBootstrap : MonoBehaviour
    {
        private PrisonEventType _lastMorningEvent = (PrisonEventType)(-1);
        private bool _initialMorningConsumed;
        private bool _sentenceTransferFired;
        private SentenceClockHUD _sentenceHud;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Hook()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            if (!CareerSession.HasActiveRun) return;
            if (CareerSession.ActiveFacility.sceneName != scene.name) return;

            // sceneLoaded fires after Awake but before Start, so seeding here beats
            // GameManager.Start's loot/NPC spawning while overriding GameManager.Awake's seed.
            Random.InitState(CareerSession.ActiveRun.worldSeed);

            var go = new GameObject("CareerRunBootstrap");
            go.AddComponent<CareerRunBootstrap>();
        }

        private void Start()
        {
            var world = CareerSession.ActiveWorld;
            var run = CareerSession.ActiveRun;
            var facility = CareerSession.ActiveFacility;
            if (world == null || run == null || facility == null)
            {
                Destroy(gameObject);
                return;
            }

            // Global carry → live scene systems (GameManager already spawned the player).
            HudBootstrap.EnsureHud();
            if (PlayerWallet.Instance != null)
                PlayerWallet.Instance.SetBalance(world.global.cash);
            PlayerStats.EnsureInstance()
                .ApplyCareerCarry(world.global.mentalHealth, world.global.physicalHealth, world.global.strength);

            if (PrisonTimeManager.Instance != null)
                PrisonTimeManager.Instance.OnEventChanged += OnScheduleEvent;

            if (facility.HasSentenceClock)
            {
                _sentenceHud = SentenceClockHUD.Ensure();
                _sentenceHud.SetLine(SentenceClockMath.HudLine(run.day, facility.sentenceDays));
            }

            Debug.Log($"[CareerRunBootstrap] '{world.displayName}' entered {facility.title} " +
                      $"(visit {run.visitIndex}, seed {run.worldSeed}, ${world.global.cash}, R{world.global.respect:0}).");
        }

        private void OnDestroy()
        {
            if (PrisonTimeManager.Instance != null)
                PrisonTimeManager.Instance.OnEventChanged -= OnScheduleEvent;
        }

        private void OnScheduleEvent(PrisonEventType evt)
        {
            if (!PrisonEventExtensions.IsMorningLineUp(evt))
            {
                _lastMorningEvent = (PrisonEventType)(-1);
                return;
            }
            if (_lastMorningEvent == evt) return;
            _lastMorningEvent = evt;

            // The schedule announces its starting phase on session start; when that phase is
            // already the Morning Count it belongs to day 1 and must not advance the clock.
            if (!_initialMorningConsumed && Time.timeSinceLevelLoad < 5f)
            {
                _initialMorningConsumed = true;
                return;
            }
            _initialMorningConsumed = true;

            OnMorningCount();
        }

        private void OnMorningCount()
        {
            var world = CareerSession.ActiveWorld;
            var run = CareerSession.ActiveRun;
            var facility = CareerSession.ActiveFacility;
            if (world == null || run == null || facility == null) return;

            run.day++;

            if (CareerSession.HasActiveLadderRun)
            {
                // Surviving a full day at Federal tiers is itself respected.
                world.global.respect = CareerRespectMath.Clamp(
                    world.global.respect + CareerRespectMath.DailySurvivalAward(facility.ladderIndex));

                // Day-boundary autosave (spec: save on transfer, quit-to-menu, and Morning Count).
                CareerSession.SyncGlobalsFromScene();
                CareerWorldStore.Save(world);
            }

            if (_sentenceHud != null)
                _sentenceHud.SetLine(SentenceClockMath.HudLine(run.day, facility.sentenceDays));

            if (!_sentenceTransferFired
                && CareerSession.HasActiveLadderRun
                && SentenceClockMath.ShouldTransferAtMorningCount(run.day, facility.sentenceDays))
            {
                _sentenceTransferFired = true;
                CareerTransferFlow.CompleteSentence();
            }
        }
    }
}
