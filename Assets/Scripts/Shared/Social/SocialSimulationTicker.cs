using System.Collections.Generic;
using UnityEngine;

namespace Prison.Social
{
    /// <summary>
    /// Ambient social simulation (spec §7 gossip + M6 ambient life): during meals and Free
    /// Time, nearby NPC pairs exchange gossip on a slow tick, and high-aggression inmates
    /// occasionally spark visible arguments that feed the memory/gossip web.
    /// </summary>
    public class SocialSimulationTicker : MonoBehaviour
    {
        [Tooltip("Seconds between ambient simulation ticks.")]
        public float tickSeconds = 15f;
        [Tooltip("Max gossip exchanges per tick.")]
        public int gossipPairsPerTick = 3;
        [Tooltip("Chance per tick that two hot-headed inmates near each other start an argument.")]
        [Range(0f, 1f)] public float argumentChance = 0.08f;
        [Tooltip("NPCs must be this close to gossip.")]
        public float gossipRadius = 8f;

        private float _nextTick;
        private System.Random _rng = new System.Random();

        private void Update()
        {
            if (Time.time < _nextTick) return;
            _nextTick = Time.time + tickSeconds;

            var world = SocialWorld.Instance;
            if (world == null || !world.IsBuilt) return;
            var tm = Prison.PrisonTimeManager.Instance;
            if (tm == null || !GossipSystem.IsGossipPhase(tm.CurrentEvent)) return;

            TickGossip(world);
            TickArguments(world);
        }

        private void TickGossip(SocialWorld world)
        {
            var inmates = new List<NPCIdentity>(world.Roster.Inmates());
            int exchanges = 0;

            Shuffle(inmates);
            foreach (var sharer in inmates)
            {
                if (exchanges >= gossipPairsPerTick) break;
                if (sharer.traits.sociability < SocialTuning.GossipMinSociability) continue;
                var sharerPos = world.PositionOf(sharer.actorId);
                if (sharerPos == null) continue;

                foreach (var listener in inmates)
                {
                    if (listener.actorId == sharer.actorId) continue;
                    var listenerPos = world.PositionOf(listener.actorId);
                    if (listenerPos == null) continue;
                    if (Vector3.Distance(sharerPos.Value, listenerPos.Value) > gossipRadius) continue;

                    float trust = world.Relationships.GetTrust(listener.actorId, sharer.actorId);
                    if (GossipSystem.TryShare(sharer, listener,
                            world.GetMemory(sharer.actorId), world.GetMemory(listener.actorId), trust))
                    {
                        // Hearing someone's name spreads it (dossier fog-of-war).
                        var heard = world.GetMemory(listener.actorId).HighestWeight();
                        if (heard != null && heard.Value.actor == SocialTuning.PlayerActorId)
                            world.MarkHeardOf(listener.actorId);
                        exchanges++;
                        break;
                    }
                }
            }
        }

        private void TickArguments(SocialWorld world)
        {
            if (_rng.NextDouble() > argumentChance) return;

            var inmates = new List<NPCIdentity>(world.Roster.Inmates());
            Shuffle(inmates);
            foreach (var a in inmates)
            {
                if (a.traits.aggression < 60) continue;
                var aPos = world.PositionOf(a.actorId);
                if (aPos == null) continue;

                foreach (var b in inmates)
                {
                    if (b.actorId == a.actorId) continue;
                    if (world.Relationships.GetStanding(a.actorId, b.actorId) > 10f) continue; // no beef
                    var bPos = world.PositionOf(b.actorId);
                    if (bPos == null) continue;
                    if (Vector3.Distance(aPos.Value, bPos.Value) > 6f) continue;

                    // Visible argument: both remember it; bystanders witness it.
                    world.RecordWithWitnesses(new SocialEvent
                    {
                        type = SocialEventType.Argument,
                        actor = a.actorId,
                        target = b.actorId,
                        day = world.CurrentDay,
                        phase = Prison.PrisonTimeManager.Instance != null
                            ? Prison.PrisonTimeManager.Instance.CurrentEvent : PrisonEventType.FreeTime,
                        weight = 5f,
                        source = SocialEventSource.Direct,
                    });
                    world.Relationships.ApplyDeltas(a.actorId, b.actorId, -4f, -2f, a.traits);
                    world.Relationships.ApplyDeltas(b.actorId, a.actorId, -4f, -2f, b.traits);

                    var playerPos = world.PositionOf(SocialTuning.PlayerActorId);
                    if (playerPos != null && Vector3.Distance(playerPos.Value, aPos.Value) < 14f)
                        SocialToastUI.Show($"{a.ShortName} and {b.ShortName} are getting loud...");
                    return;
                }
            }
        }

        private void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
