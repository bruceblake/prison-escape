using System;
using System.Collections.Generic;

namespace Prison.Social
{
    /// <summary>
    /// Sparse directional relationship records: how <c>observer</c> feels about <c>subject</c>.
    /// Pure data structure (no UnityEngine) so it stays EditMode-testable.
    /// Unrecorded pairs read as (0, 0).
    /// </summary>
    public class RelationshipStore
    {
        private readonly Dictionary<long, RelationshipRecord> _records = new Dictionary<long, RelationshipRecord>();

        /// <summary>Raised after any record changes: (observer, subject, trustDelta, respectDelta, newRecord).</summary>
        public event Action<int, int, float, float, RelationshipRecord> OnChanged;

        private static long Key(int observer, int subject) => ((long)observer << 32) | (uint)subject;

        public RelationshipRecord Get(int observer, int subject) =>
            _records.TryGetValue(Key(observer, subject), out var r) ? r : default;

        public float GetTrust(int observer, int subject) => Get(observer, subject).trust;
        public float GetRespect(int observer, int subject) => Get(observer, subject).respect;
        public float GetStanding(int observer, int subject) => RelationshipMath.Standing(Get(observer, subject));
        public StandingBand GetBand(int observer, int subject) => RelationshipMath.GetBand(GetStanding(observer, subject));

        /// <summary>Sets a record directly (world-gen seeding). Does not run the modifier pipeline.</summary>
        public void Seed(int observer, int subject, float trust, float respect)
        {
            var record = new RelationshipRecord(
                RelationshipMath.Apply(0f, trust),
                RelationshipMath.Apply(0f, respect));
            _records[Key(observer, subject)] = record;
        }

        /// <summary>
        /// Applies base deltas through the full modifier pipeline
        /// (personality → gang factor → soft cap → clamp) and raises <see cref="OnChanged"/>.
        /// Returns the applied effective deltas.
        /// </summary>
        public (float trustDelta, float respectDelta) ApplyDeltas(
            int observer,
            int subject,
            float baseTrustDelta,
            float baseRespectDelta,
            in PersonalityTraits observerTraits,
            bool isBetrayalClass = false,
            float gangFactor = 1f)
        {
            long key = Key(observer, subject);
            _records.TryGetValue(key, out var record);

            float trustDelta = RelationshipMath.ComputeEffectiveDelta(
                record.trust, baseTrustDelta, true, observerTraits, isBetrayalClass, gangFactor);
            float respectDelta = RelationshipMath.ComputeEffectiveDelta(
                record.respect, baseRespectDelta, false, observerTraits, isBetrayalClass, gangFactor);

            if (trustDelta == 0f && respectDelta == 0f)
                return (0f, 0f);

            record.trust = RelationshipMath.Apply(record.trust, trustDelta);
            record.respect = RelationshipMath.Apply(record.respect, respectDelta);
            _records[key] = record;

            OnChanged?.Invoke(observer, subject, trustDelta, respectDelta, record);
            return (trustDelta, respectDelta);
        }

        /// <summary>All observers that hold a record about <paramref name="subject"/>.</summary>
        public IEnumerable<int> ObserversOf(int subject)
        {
            foreach (var kv in _records)
            {
                int obs = (int)(kv.Key >> 32);
                int subj = (int)kv.Key;
                if (subj == subject)
                    yield return obs;
            }
        }

        public int RecordCount => _records.Count;
    }
}
