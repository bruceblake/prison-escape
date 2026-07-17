using System.Collections.Generic;
using UnityEngine;

namespace Prison.Social
{
    /// <summary>
    /// World-side execution of ask-favors (spec §6): lookout guard warnings, staged
    /// distractions, protection proximity tracking, sourced-item delivery, and stash return
    /// after the morning shakedown. Added to the <see cref="SocialWorld"/> object at build time.
    /// </summary>
    public class SocialFavorRuntime : MonoBehaviour
    {
        private const float LookoutWarnCooldown = 6f;
        private const float ProtectionRadius = 6f;
        private const float ProtectionRequiredSeconds = 45f;

        private float _lastLookoutWarn = -99f;
        private PrisonEventType _lastPhase;
        private readonly Dictionary<FavorInstance, float> _protectionProgress = new Dictionary<FavorInstance, float>();

        private void OnEnable()
        {
            if (Prison.PrisonTimeManager.Instance != null)
            {
                Prison.PrisonTimeManager.Instance.OnEventChanged += OnPhaseChanged;
                _lastPhase = Prison.PrisonTimeManager.Instance.CurrentEvent;
            }
            var world = SocialWorld.Instance;
            if (world != null)
                world.OnDayAdvanced += OnDayAdvanced;
        }

        private void OnDisable()
        {
            if (Prison.PrisonTimeManager.Instance != null)
                Prison.PrisonTimeManager.Instance.OnEventChanged -= OnPhaseChanged;
            var world = SocialWorld.Instance;
            if (world != null)
                world.OnDayAdvanced -= OnDayAdvanced;
        }

        private void Update()
        {
            var world = SocialWorld.Instance;
            if (world == null || !world.IsBuilt || world.PlayerTransform == null) return;

            foreach (var favor in world.Favors.ActiveAskFavors())
            {
                switch (favor.kind)
                {
                    case FavorKind.Lookout: TickLookout(world); break;
                    case FavorKind.Distraction: ExecuteDistraction(world, favor); break;
                }
            }

            TickProtection(world);
        }

        // ---------------------------------------------------------------- lookout

        private void TickLookout(SocialWorld world)
        {
            if (Time.realtimeSinceStartup - _lastLookoutWarn < LookoutWarnCooldown) return;
            Vector3 playerPos = world.PlayerTransform.position;
            foreach (var guardIdentity in world.Roster.Guards())
            {
                var go = world.GetActorObject(guardIdentity.actorId);
                if (go == null || !go.activeInHierarchy) continue;
                if (Vector3.Distance(playerPos, go.transform.position) > SocialTuning.LookoutWarnRadius) continue;
                _lastLookoutWarn = Time.realtimeSinceStartup;
                SocialToastUI.Show($"Psst — Ofc. {guardIdentity.lastName} is close. ({Vector3.Distance(playerPos, go.transform.position):0} m)");
                return;
            }
        }

        // ---------------------------------------------------------------- distraction

        public static bool CanDistractAnyGuard()
        {
            var world = SocialWorld.Instance;
            if (world == null || world.PlayerTransform == null) return false;
            foreach (var guardIdentity in world.Roster.Guards())
            {
                var go = world.GetActorObject(guardIdentity.actorId);
                if (go == null || !go.activeInHierarchy) continue;
                var fsm = go.GetComponent<GuardFSM>();
                if (fsm == null || fsm.State != GuardFSM.GuardState.Patrol) continue;
                var profile = go.GetComponent<GuardSocialProfile>();
                if (profile != null && profile.ImmuneToDistraction) continue;
                if (Vector3.Distance(world.PlayerTransform.position, go.transform.position) <= 30f)
                    return true;
            }
            return false;
        }

        private void ExecuteDistraction(SocialWorld world, FavorInstance favor)
        {
            var askerGo = world.GetActorObject(favor.npcActorId);
            Vector3 stage = askerGo != null ? askerGo.transform.position : world.PlayerTransform.position;

            GuardFSM nearest = null;
            float best = float.MaxValue;
            foreach (var guardIdentity in world.Roster.Guards())
            {
                var go = world.GetActorObject(guardIdentity.actorId);
                if (go == null || !go.activeInHierarchy) continue;
                var profile = go.GetComponent<GuardSocialProfile>();
                if (profile != null && profile.ImmuneToDistraction) continue; // Veterans don't bite
                var fsm = go.GetComponent<GuardFSM>();
                if (fsm == null || fsm.State != GuardFSM.GuardState.Patrol) continue;
                float d = Vector3.Distance(world.PlayerTransform.position, go.transform.position);
                if (d < best) { best = d; nearest = fsm; }
            }

            if (nearest != null && nearest.TryApplyDistraction(stage, SocialTuning.DistractionSeconds))
                SocialToastUI.Show("A shouting match breaks out — the guard turns to look.");
            else
                SocialToastUI.Show("Nobody took the bait.");
            world.Favors.Complete(favor);
        }

        // ---------------------------------------------------------------- protection (do-favor)

        private void TickProtection(SocialWorld world)
        {
            foreach (var favor in new List<FavorInstance>(world.Favors.All))
            {
                if (favor.kind != FavorKind.Protection || favor.state != FavorState.Active) continue;
                var askerGo = world.GetActorObject(favor.npcActorId);
                if (askerGo == null) continue;
                if (Vector3.Distance(world.PlayerTransform.position, askerGo.transform.position) > ProtectionRadius)
                    continue;

                _protectionProgress.TryGetValue(favor, out float progress);
                progress += Time.deltaTime;
                _protectionProgress[favor] = progress;

                if (progress >= ProtectionRequiredSeconds)
                {
                    _protectionProgress.Remove(favor);
                    // Protection completion carries its own delta profile (spec §3 table).
                    world.ApplyPlayerAct(favor.npcActorId, SocialEventType.Protection);
                    if (favor.cashReward > 0f && Prison.PlayerWallet.Instance != null)
                        Prison.PlayerWallet.Instance.Add(favor.cashReward);
                    var identity = world.GetIdentity(favor.npcActorId);
                    SocialToastUI.Show($"{identity?.ShortName ?? "They"} nods. You had their back.");
                    world.Favors.Decline(favor); // remove without double-applying completion deltas
                }
            }
        }

        // ---------------------------------------------------------------- phase / day hooks

        private void OnPhaseChanged(PrisonEventType phase)
        {
            var world = SocialWorld.Instance;
            if (world == null || !world.IsBuilt) { _lastPhase = phase; return; }

            // Lookout covers one phase; it ends when the phase does.
            foreach (var favor in new List<FavorInstance>(world.Favors.All))
            {
                if (favor.direction != FavorDirection.AskFavor || favor.state != FavorState.Active) continue;
                if (favor.kind == FavorKind.Lookout)
                    world.Favors.Complete(favor);
            }

            // Stash comes back after the morning shakedown phase ends.
            if (_lastPhase == PrisonEventType.MorningRollCall)
                ReturnStashes(world);

            _lastPhase = phase;
        }

        private void ReturnStashes(SocialWorld world)
        {
            var inventory = world.PlayerTransform != null
                ? world.PlayerTransform.GetComponentInChildren<PlayerInventory>()
                : null;
            if (inventory == null) inventory = FindAnyObjectByType<PlayerInventory>();

            foreach (var favor in new List<FavorInstance>(world.Favors.All))
            {
                if (favor.kind != FavorKind.HoldStash || favor.state != FavorState.Active) continue;
                if (favor.heldStash == null || favor.heldStash.Count == 0) { world.Favors.Decline(favor); continue; }

                var holder = world.GetIdentity(favor.npcActorId);
                var returned = world.Favors.ReturnStash(favor, holder, out var stolen);
                foreach (var item in returned)
                {
                    if (inventory == null || !inventory.AddItem(item, 1))
                        SocialToastUI.Show($"No room — {holder?.ShortName ?? "they"} keeps the {item.itemName} for now.");
                }
                if (stolen != null)
                    SocialToastUI.Show($"{holder?.ShortName ?? "They"} swears the {stolen.itemName} \"got confiscated\". Sure it did.");
                else if (returned.Count > 0)
                    SocialToastUI.Show($"{holder?.ShortName ?? "They"} slips your stash back. Clean.");
            }
        }

        private void OnDayAdvanced(int day)
        {
            var world = SocialWorld.Instance;
            if (world == null) return;

            foreach (var favor in new List<FavorInstance>(world.Favors.All))
            {
                if (favor.kind != FavorKind.SourceItem || favor.state != FavorState.Active) continue;
                if (day < favor.deliverOnDay) continue;

                var item = favor.item != null ? favor.item : world.Favors.RandomItemOfCategory(favor.itemCategory);
                if (item != null)
                {
                    world.Trading.QueueBedDelivery(item, 1);
                    var identity = world.GetIdentity(favor.npcActorId);
                    SocialToastUI.Show($"{identity?.ShortName ?? "Your source"} came through — check under your bed after count.");
                }
                world.Favors.Complete(favor);
            }
        }
    }
}
