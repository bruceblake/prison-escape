using NUnit.Framework;
using Prison.Social;

namespace Prison.Tests
{
    /// <summary>
    /// Memory ring buffer + gossip rules per the v3 test plan: decay/eviction/grudges,
    /// gossip weight halving, hop cap, trust/sociability gates, Loner immunity.
    /// </summary>
    public class SocialMemoryGossipTests
    {
        private static SocialEvent Evt(SocialEventType type, float weight, int day = 1,
            int actor = 0, int target = 5, SocialEventSource source = SocialEventSource.Direct, int hops = 0)
            => new SocialEvent { type = type, actor = actor, target = target, day = day, weight = weight, source = source, hops = hops };

        // ------------------------------------------------------------------ ring buffer

        [Test]
        public void Record_EvictsLowestWeightWhenFull()
        {
            var memory = new SocialMemory(3);
            memory.Record(Evt(SocialEventType.Chat, 1f));
            memory.Record(Evt(SocialEventType.Gift, 2f));
            memory.Record(Evt(SocialEventType.FavorForNpc, 4f));
            memory.Record(Evt(SocialEventType.CrimeWitnessed, 6f));

            Assert.AreEqual(3, memory.Count);
            foreach (var e in memory.Events)
                Assert.AreNotEqual(SocialEventType.Chat, e.type); // weight 1 evicted
        }

        [Test]
        public void Record_WeakerThanWeakest_DroppedWhenFull()
        {
            var memory = new SocialMemory(2);
            memory.Record(Evt(SocialEventType.Gift, 2f));
            memory.Record(Evt(SocialEventType.FavorForNpc, 4f));
            memory.Record(Evt(SocialEventType.Chat, 1f));
            Assert.AreEqual(2, memory.Count);
            foreach (var e in memory.Events)
                Assert.AreNotEqual(SocialEventType.Chat, e.type);
        }

        [Test]
        public void Decay_OnePerDay_ForgottenAtZero()
        {
            var memory = new SocialMemory();
            memory.Record(Evt(SocialEventType.Gift, 2f, day: 1));
            memory.DecayOneDay(2);
            Assert.AreEqual(1f, memory.Events[0].weight, 1e-4f);
            memory.DecayOneDay(3);
            Assert.AreEqual(0, memory.Count);
        }

        [Test]
        public void Decay_GrudgesDecayEveryThirdDay()
        {
            var memory = new SocialMemory();
            memory.Record(Evt(SocialEventType.SnitchedOn, 10f, day: 0)); // negative + ≥8 = grudge
            memory.DecayOneDay(1);
            memory.DecayOneDay(2);
            Assert.AreEqual(10f, memory.Events[0].weight, 1e-4f); // days 1,2: untouched
            memory.DecayOneDay(3);
            Assert.AreEqual(9f, memory.Events[0].weight, 1e-4f);  // day 3: −1
            memory.DecayOneDay(4);
            Assert.AreEqual(9f, memory.Events[0].weight, 1e-4f);
        }

        [Test]
        public void Decay_PositiveHeavyEventIsNotAGrudge()
        {
            var memory = new SocialMemory();
            memory.Record(Evt(SocialEventType.Protection, 10f, day: 0));
            memory.DecayOneDay(1);
            Assert.AreEqual(9f, memory.Events[0].weight, 1e-4f);
        }

        [Test]
        public void HighestWeight_And_StrongestAbout_PickCorrectEvents()
        {
            var memory = new SocialMemory();
            memory.Record(Evt(SocialEventType.Chat, 1f, actor: 0, target: 5));
            memory.Record(Evt(SocialEventType.CrimeWitnessed, 6f, actor: 0, target: -1));
            memory.Record(Evt(SocialEventType.Gift, 2f, actor: 9, target: 5));

            Assert.AreEqual(SocialEventType.CrimeWitnessed, memory.HighestWeight().Value.type);
            Assert.AreEqual(SocialEventType.CrimeWitnessed, memory.StrongestAbout(0).Value.type);
            Assert.IsNull(memory.StrongestAbout(0, minWeight: 7f));
        }

        // ------------------------------------------------------------------ gossip

        [Test]
        public void Gossip_CopyHalvesWeightAndMarksHeard()
        {
            var original = Evt(SocialEventType.CrimeWitnessed, 6f);
            Assert.IsTrue(GossipSystem.TryCreateGossipCopy(original, out var copy));
            Assert.AreEqual(3f, copy.weight, 1e-4f);
            Assert.AreEqual(SocialEventSource.Heard, copy.source);
            Assert.AreEqual(1, copy.hops);
        }

        [Test]
        public void Gossip_SecondHopQuarterWeight_ThirdHopBlocked()
        {
            var original = Evt(SocialEventType.CrimeWitnessed, 6f);
            GossipSystem.TryCreateGossipCopy(original, out var hop1);
            Assert.IsTrue(GossipSystem.TryCreateGossipCopy(hop1, out var hop2));
            Assert.AreEqual(1.5f, hop2.weight, 1e-4f); // quarter of 6
            Assert.AreEqual(2, hop2.hops);
            Assert.IsFalse(GossipSystem.TryCreateGossipCopy(hop2, out _)); // max 2 hops
        }

        private static NPCIdentity Npc(int id, int sociability = 80, PrisonerArchetype archetype = PrisonerArchetype.Hustler)
            => new NPCIdentity
            {
                actorId = id,
                archetype = archetype,
                traits = new PersonalityTraits(50, 50, 50, sociability, 50),
            };

        [Test]
        public void Gossip_RequiresSociabilityAndTrust()
        {
            var sharerMemory = new SocialMemory();
            sharerMemory.Record(Evt(SocialEventType.CrimeWitnessed, 6f));
            var listenerMemory = new SocialMemory();

            // Low sociability sharer: nothing spreads.
            Assert.IsFalse(GossipSystem.TryShare(Npc(1, sociability: 20), Npc(2), sharerMemory, listenerMemory, 50f));
            // Low trust listener: nothing spreads.
            Assert.IsFalse(GossipSystem.TryShare(Npc(1), Npc(2), sharerMemory, listenerMemory, 10f));
            // Both gates pass:
            Assert.IsTrue(GossipSystem.TryShare(Npc(1), Npc(2), sharerMemory, listenerMemory, 30f));
            Assert.AreEqual(1, listenerMemory.Count);
        }

        [Test]
        public void Gossip_LonerImmuneToPlayerGossip()
        {
            var sharerMemory = new SocialMemory();
            sharerMemory.Record(Evt(SocialEventType.CrimeWitnessed, 6f, actor: SocialTuning.PlayerActorId));
            var listenerMemory = new SocialMemory();
            var loner = Npc(2, sociability: 80, archetype: PrisonerArchetype.Loner);

            Assert.IsFalse(GossipSystem.TryShare(Npc(1), loner, sharerMemory, listenerMemory, 50f));
        }

        [Test]
        public void Gossip_DoesNotDuplicateSameStory()
        {
            var sharerMemory = new SocialMemory();
            sharerMemory.Record(Evt(SocialEventType.CrimeWitnessed, 6f));
            var listenerMemory = new SocialMemory();

            Assert.IsTrue(GossipSystem.TryShare(Npc(1), Npc(2), sharerMemory, listenerMemory, 50f));
            Assert.IsFalse(GossipSystem.TryShare(Npc(1), Npc(2), sharerMemory, listenerMemory, 50f));
            Assert.AreEqual(1, listenerMemory.Count);
        }

        [Test]
        public void Gossip_PhasesAreMealsAndFreeTime()
        {
            Assert.IsTrue(GossipSystem.IsGossipPhase(PrisonEventType.Breakfast));
            Assert.IsTrue(GossipSystem.IsGossipPhase(PrisonEventType.Lunch));
            Assert.IsTrue(GossipSystem.IsGossipPhase(PrisonEventType.Dinner));
            Assert.IsTrue(GossipSystem.IsGossipPhase(PrisonEventType.FreeTime));
            Assert.IsFalse(GossipSystem.IsGossipPhase(PrisonEventType.LightsOut));
            Assert.IsFalse(GossipSystem.IsGossipPhase(PrisonEventType.MorningRollCall));
        }
    }
}
