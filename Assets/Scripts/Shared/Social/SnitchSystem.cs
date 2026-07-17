using System;
using System.Collections.Generic;
using UnityEngine;

namespace Prison.Social
{
    /// <summary>A pending report to the guards about someone's crime.</summary>
    public class SnitchTip
    {
        public int snitchActorId;
        public int targetActorId;   // whose cell gets tossed (player = 0)
        public SocialEventType crime;
        public float weight;        // memory weight backing the tip
        public int day;
        public bool cleared;        // bought off via Corrupt-guard bribe
    }

    /// <summary>
    /// Snitching & tips (spec §9). Propensity = f(low loyalty, low nerve, crime memory weight,
    /// standing toward the player). Tips feed Security (suspicion) and queue a targeted
    /// shakedown of the target's cell next morning. Guards with high trust toward the player
    /// sit on weak tips. Pure rolls; world effects applied by <see cref="SocialWorld"/>.
    /// </summary>
    public class SnitchSystem
    {
        private readonly SocialRoster _roster;
        private readonly RelationshipStore _relationships;
        private readonly System.Random _rng;
        private readonly List<SnitchTip> _pendingTips = new List<SnitchTip>();
        private readonly Dictionary<int, int> _mutedUntilDay = new Dictionary<int, int>();

        public event Action<SnitchTip> OnTipFiled;

        public SnitchSystem(SocialRoster roster, RelationshipStore relationships, int seed)
        {
            _roster = roster;
            _relationships = relationships;
            _rng = new System.Random(seed);
        }

        public IReadOnlyList<SnitchTip> PendingTips => _pendingTips;

        /// <summary>
        /// Propensity that an NPC reports a remembered crime this phase.
        /// Low loyalty + low nerve raise it; positive standing toward the player suppresses it
        /// when the player is the target; heavier memories weigh more.
        /// </summary>
        public static float Propensity(in PersonalityTraits traits, float crimeWeight,
            float standingTowardTarget, float archetypeBase)
        {
            float loyaltyFactor = 1f - traits.loyalty / 100f;   // disloyal → more likely
            float nerveFactor = 1f - traits.nerve / 100f;       // nervous → more likely
            float weightFactor = Mathf.Clamp01(crimeWeight / 10f);
            float standingSuppression = Mathf.Clamp01(1f - Mathf.Max(0f, standingTowardTarget) / 100f);

            float p = archetypeBase
                + 0.25f * loyaltyFactor * nerveFactor
                + 0.15f * weightFactor;
            return Mathf.Clamp01(p * standingSuppression * weightFactor);
        }

        /// <summary>Whether a guard acts on a tip: high trust toward the player shrugs off weak tips.</summary>
        public static bool GuardActsOnTip(float guardTrustTowardPlayer, float tipWeight)
        {
            if (tipWeight >= 6f) return true; // strong tips always land
            return guardTrustTowardPlayer < 25f;
        }

        public bool IsMuted(int actorId, int currentDay) =>
            _mutedUntilDay.TryGetValue(actorId, out int until) && currentDay < until;

        /// <summary>Silence-a-snitch favor result: mute for 3 days.</summary>
        public void Mute(int actorId, int currentDay) =>
            _mutedUntilDay[actorId] = currentDay + SocialTuning.SilenceSnitchMuteDays;

        /// <summary>
        /// Rolls snitch reports over every inmate's crime memories (call at phase change).
        /// Returns newly filed tips.
        /// </summary>
        public List<SnitchTip> RollReports(Func<int, SocialMemory> memoryOf, int currentDay)
        {
            var filed = new List<SnitchTip>();
            foreach (var identity in _roster.Inmates())
            {
                if (IsMuted(identity.actorId, currentDay)) continue;
                var memory = memoryOf(identity.actorId);
                if (memory == null) continue;

                var profile = ArchetypeCatalog.Get(identity.archetype);
                if (profile.snitchBaseChance <= 0f && identity.traits.loyalty >= 30) continue;

                foreach (var evt in memory.Events)
                {
                    if (evt.type != SocialEventType.CrimeWitnessed && evt.type != SocialEventType.BribeWitnessed)
                        continue;
                    int target = evt.actor;
                    if (target == identity.actorId) continue;
                    if (HasPendingTip(identity.actorId, target)) continue;

                    float standing = _relationships.GetStanding(identity.actorId, target);
                    float p = Propensity(identity.traits, evt.weight, standing, profile.snitchBaseChance);
                    if (_rng.NextDouble() >= p) continue;

                    var tip = new SnitchTip
                    {
                        snitchActorId = identity.actorId,
                        targetActorId = target,
                        crime = evt.type,
                        weight = evt.weight,
                        day = currentDay,
                    };
                    _pendingTips.Add(tip);
                    filed.Add(tip);
                    OnTipFiled?.Invoke(tip);
                    break; // one tip per snitch per roll
                }
            }
            return filed;
        }

        /// <summary>Player-bought bribe: clears the oldest pending tip against the player. Returns it, or null.</summary>
        public SnitchTip ClearOneTipAgainstPlayer()
        {
            var tip = _pendingTips.Find(t => t.targetActorId == SocialTuning.PlayerActorId && !t.cleared);
            if (tip != null)
            {
                tip.cleared = true;
                _pendingTips.Remove(tip);
            }
            return tip;
        }

        /// <summary>Drains tips due for action (morning): returns actionable tips and clears the queue.</summary>
        public List<SnitchTip> DrainActionableTips(float guardTrustTowardPlayer)
        {
            var actionable = new List<SnitchTip>();
            for (int i = _pendingTips.Count - 1; i >= 0; i--)
            {
                var tip = _pendingTips[i];
                bool acts = tip.targetActorId != SocialTuning.PlayerActorId
                    || GuardActsOnTip(guardTrustTowardPlayer, tip.weight);
                _pendingTips.RemoveAt(i);
                if (acts && !tip.cleared)
                    actionable.Add(tip);
            }
            return actionable;
        }

        public bool HasPendingTipAgainstPlayer() =>
            _pendingTips.Exists(t => t.targetActorId == SocialTuning.PlayerActorId && !t.cleared);

        private bool HasPendingTip(int snitchId, int targetId) =>
            _pendingTips.Exists(t => t.snitchActorId == snitchId && t.targetActorId == targetId);
    }
}
