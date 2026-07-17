using System.Collections.Generic;

namespace Prison.Social
{
    /// <summary>
    /// Per-NPC ring buffer of 16 remembered <see cref="SocialEvent"/>s with daily decay.
    /// Grudges (negative, weight ≥ 8 at record time) decay at −1 per 3 in-game days.
    /// Pure logic — no UnityEngine — so decay/eviction stays EditMode-testable.
    /// </summary>
    public class SocialMemory
    {
        private readonly List<SocialEvent> _events = new List<SocialEvent>(SocialTuning.MemoryCapacity);
        private readonly int _capacity;

        public SocialMemory(int capacity = SocialTuning.MemoryCapacity)
        {
            _capacity = capacity < 1 ? 1 : capacity;
        }

        public IReadOnlyList<SocialEvent> Events => _events;
        public int Count => _events.Count;

        /// <summary>Records an event; when full, evicts the lowest-weight event first.</summary>
        public void Record(SocialEvent evt)
        {
            if (evt.weight <= 0f) return;

            if (_events.Count >= _capacity)
            {
                int lowest = 0;
                for (int i = 1; i < _events.Count; i++)
                {
                    if (_events[i].weight < _events[lowest].weight)
                        lowest = i;
                }
                // Only evict if the incoming event matters at least as much as the weakest memory.
                if (_events[lowest].weight > evt.weight) return;
                _events.RemoveAt(lowest);
            }

            _events.Add(evt);
        }

        /// <summary>
        /// Daily decay (run at Morning Roll Call): −1 weight per day; grudges −1 per
        /// <see cref="SocialTuning.GrudgeDecayDays"/> days. Events at 0 are forgotten.
        /// </summary>
        public void DecayOneDay(int currentDay)
        {
            for (int i = _events.Count - 1; i >= 0; i--)
            {
                var e = _events[i];
                if (e.IsGrudge)
                {
                    int age = currentDay - e.day;
                    if (age <= 0 || age % SocialTuning.GrudgeDecayDays != 0)
                        continue;
                }
                e.weight -= 1f;
                if (e.weight <= 0f)
                    _events.RemoveAt(i);
                else
                    _events[i] = e;
            }
        }

        /// <summary>Highest-weight memory (what an NPC gossips about), or null when empty.</summary>
        public SocialEvent? HighestWeight()
        {
            if (_events.Count == 0) return null;
            int best = 0;
            for (int i = 1; i < _events.Count; i++)
            {
                if (_events[i].weight > _events[best].weight)
                    best = i;
            }
            return _events[best];
        }

        /// <summary>Strongest memory about a specific actor with weight ≥ <paramref name="minWeight"/> (dialogue hook), or null.</summary>
        public SocialEvent? StrongestAbout(int actorId, float minWeight = 0f)
        {
            SocialEvent? best = null;
            for (int i = 0; i < _events.Count; i++)
            {
                var e = _events[i];
                if (e.actor != actorId && e.target != actorId) continue;
                if (e.weight < minWeight) continue;
                if (best == null || e.weight > best.Value.weight)
                    best = e;
            }
            return best;
        }

        /// <summary>All memories involving an actor, strongest first.</summary>
        public List<SocialEvent> AllAbout(int actorId)
        {
            var result = new List<SocialEvent>();
            for (int i = 0; i < _events.Count; i++)
            {
                var e = _events[i];
                if (e.actor == actorId || e.target == actorId)
                    result.Add(e);
            }
            result.Sort((a, b) => b.weight.CompareTo(a.weight));
            return result;
        }

        /// <summary>True if this memory already contains a same-type event by the same actor about the same target (dedup for gossip).</summary>
        public bool ContainsSimilar(in SocialEvent evt)
        {
            for (int i = 0; i < _events.Count; i++)
            {
                var e = _events[i];
                if (e.type == evt.type && e.actor == evt.actor && e.target == evt.target && e.day == evt.day)
                    return true;
            }
            return false;
        }
    }
}
