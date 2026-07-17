using System.Collections.Generic;

namespace Prison.Social
{
    /// <summary>
    /// The prison telephone (spec §7). Pure spread rules — proximity pairs are supplied by
    /// <see cref="SocialWorld"/>'s ambient ticker during meals and Free Time:
    /// sharer needs sociability ≥ 50; listener needs trust ≥ 25 toward the sharer;
    /// copies land at half weight (quarter after a second hop), source Heard, max 2 hops.
    /// Loners never absorb gossip about the player.
    /// </summary>
    public static class GossipSystem
    {
        /// <summary>Halves the weight per hop and stamps the Heard source. Returns false when the event can't spread.</summary>
        public static bool TryCreateGossipCopy(in SocialEvent original, out SocialEvent copy)
        {
            copy = original;
            if (original.hops >= SocialTuning.GossipMaxHops) return false;

            copy.weight = original.weight * SocialTuning.GossipWeightFactor;
            copy.source = SocialEventSource.Heard;
            copy.hops = original.hops + 1;
            return copy.weight >= 0.5f;
        }

        /// <summary>
        /// One gossip exchange between two nearby NPCs. The sharer passes its highest-weight
        /// memory. Returns true when something spread.
        /// </summary>
        public static bool TryShare(
            NPCIdentity sharer,
            NPCIdentity listener,
            SocialMemory sharerMemory,
            SocialMemory listenerMemory,
            float listenerTrustTowardSharer)
        {
            if (sharer == null || listener == null || sharerMemory == null || listenerMemory == null) return false;
            if (sharer.traits.sociability < SocialTuning.GossipMinSociability) return false;
            if (listenerTrustTowardSharer < SocialTuning.GossipMinTrust) return false;

            var best = sharerMemory.HighestWeight();
            if (best == null) return false;

            var evt = best.Value;
            // Loners are immune to gossip about the player.
            if (listener.archetype == PrisonerArchetype.Loner
                && (evt.actor == SocialTuning.PlayerActorId || evt.target == SocialTuning.PlayerActorId))
                return false;

            if (!TryCreateGossipCopy(evt, out var copy)) return false;
            if (listenerMemory.ContainsSimilar(copy)) return false;

            listenerMemory.Record(copy);
            return true;
        }

        /// <summary>Phases in which gossip spreads (meals + free time).</summary>
        public static bool IsGossipPhase(PrisonEventType phase) =>
            phase == PrisonEventType.Breakfast
            || phase == PrisonEventType.Lunch
            || phase == PrisonEventType.Dinner
            || phase == PrisonEventType.FreeTime;
    }
}
