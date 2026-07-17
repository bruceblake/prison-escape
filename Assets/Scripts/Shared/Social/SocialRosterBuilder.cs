using System.Collections.Generic;

namespace Prison.Social
{
    /// <summary>Result of deterministic world-gen: identities plus seeded NPC↔NPC relationships.</summary>
    public class SocialRoster
    {
        public readonly List<NPCIdentity> identities = new List<NPCIdentity>();
        public readonly RelationshipStore relationships = new RelationshipStore();

        private readonly Dictionary<int, NPCIdentity> _byActor = new Dictionary<int, NPCIdentity>();

        public NPCIdentity Get(int actorId) => _byActor.TryGetValue(actorId, out var id) ? id : null;

        public void Add(NPCIdentity identity)
        {
            identities.Add(identity);
            _byActor[identity.actorId] = identity;
        }

        public IEnumerable<NPCIdentity> Inmates()
        {
            foreach (var id in identities)
                if (!id.isGuard) yield return id;
        }

        public IEnumerable<NPCIdentity> Guards()
        {
            foreach (var id in identities)
                if (id.isGuard) yield return id;
        }

        public IEnumerable<NPCIdentity> MembersOf(int gangId)
        {
            foreach (var id in identities)
                if (!id.isGuard && id.gangId == gangId) yield return id;
        }
    }

    /// <summary>
    /// Pure, deterministic roster generation (same seed = same prison):
    /// names, archetypes, traits, gang splits, and the pre-existing relationship web
    /// (gang mates +40 trust, 1–2 friends +30, 0–1 enemy −30, never across allied lines).
    /// Actor ids: player = 0, then inmates and guards in registration order.
    /// </summary>
    public static class SocialRosterBuilder
    {
        public static SocialRoster Build(int seed, IReadOnlyList<int> inmateCellIndices, int guardCount)
        {
            var roster = new SocialRoster();
            var rng = new System.Random(seed);
            var names = new SocialNameGenerator.Pool(seed ^ 0x5EED);

            int inmateCount = inmateCellIndices?.Count ?? 0;
            int nextActorId = SocialTuning.PlayerActorId + 1;

            // --- Gang split -------------------------------------------------------------
            // Target (15 inmates): 2 gangs × 5 + 5 independents. Scales down for smaller
            // populations: each gang gets ~N/3 (min 2 to field a shot-caller + a soldier);
            // below 4 inmates there are no functioning gangs.
            int perGang = inmateCount >= 4 ? System.Math.Max(2, inmateCount / 3) : 0;

            var archetypeSlots = BuildArchetypeSlots(inmateCount, perGang);

            for (int i = 0; i < inmateCount; i++)
            {
                var slot = archetypeSlots[i];
                var profile = ArchetypeCatalog.Get(slot.archetype);
                names.NextPrisonerName(out string first, out string nick, out string last);

                var identity = new NPCIdentity
                {
                    actorId = nextActorId++,
                    isGuard = false,
                    firstName = first,
                    nickname = nick,
                    lastName = last,
                    archetype = slot.archetype,
                    gangId = slot.gangId,
                    cellIndex = inmateCellIndices[i],
                    traits = profile.RollTraits(rng),
                };
                identity.favoredGiftCategories.AddRange(profile.favoredGiftCategories);
                roster.Add(identity);
            }

            for (int g = 0; g < guardCount; g++)
            {
                var guardArchetype = GuardArchetypeForIndex(g);
                names.NextGuardName(out string first, out string last);
                var identity = new NPCIdentity
                {
                    actorId = nextActorId++,
                    isGuard = true,
                    firstName = first,
                    lastName = last,
                    guardArchetype = guardArchetype,
                    gangId = SocialTuning.IndependentGangId,
                    traits = ArchetypeCatalog.RollGuardTraits(guardArchetype, rng),
                };
                roster.Add(identity);
            }

            SeedRelationships(roster, rng);
            return roster;
        }

        private struct ArchetypeSlot
        {
            public PrisonerArchetype archetype;
            public int gangId;
        }

        /// <summary>
        /// Deterministic composition. Per gang: Shot-Caller first, Syndicate gets the Hustler,
        /// rest Soldiers. Independents in priority order: Old-Timer, Snitch, Bruiser, Hustler
        /// (if Syndicate too small to have one), then Loners.
        /// </summary>
        private static List<ArchetypeSlot> BuildArchetypeSlots(int inmateCount, int perGang)
        {
            var slots = new List<ArchetypeSlot>(inmateCount);

            for (int gang = 0; gang < GangCatalog.GangCount && perGang > 0; gang++)
            {
                for (int m = 0; m < perGang && slots.Count < inmateCount; m++)
                {
                    PrisonerArchetype archetype;
                    if (m == 0) archetype = PrisonerArchetype.ShotCaller;
                    else if (m == 1 && gang == GangCatalog.SyndicateId) archetype = PrisonerArchetype.Hustler;
                    else archetype = PrisonerArchetype.Soldier;
                    slots.Add(new ArchetypeSlot { archetype = archetype, gangId = gang });
                }
            }

            var independentOrder = new[]
            {
                PrisonerArchetype.OldTimer,
                PrisonerArchetype.Snitch,
                PrisonerArchetype.Bruiser,
                PrisonerArchetype.Hustler,
                PrisonerArchetype.Loner,
            };
            int nextIndependent = 0;
            while (slots.Count < inmateCount)
            {
                var archetype = nextIndependent < independentOrder.Length
                    ? independentOrder[nextIndependent]
                    : PrisonerArchetype.Loner;
                nextIndependent++;
                slots.Add(new ArchetypeSlot { archetype = archetype, gangId = SocialTuning.IndependentGangId });
            }

            return slots;
        }

        /// <summary>First guard is By-the-Book; a Corrupt guard appears from the second onward, then Rookie, Veteran, repeat.</summary>
        public static GuardArchetype GuardArchetypeForIndex(int index)
        {
            switch (index % 4)
            {
                case 1: return GuardArchetype.Corrupt;
                case 2: return GuardArchetype.Rookie;
                case 3: return GuardArchetype.Veteran;
                default: return GuardArchetype.ByTheBook;
            }
        }

        /// <summary>
        /// NPC↔NPC seeding (spec §3): gang mates +40 trust both ways; each inmate gets 1–2
        /// seeded friends (+30) and 0–1 enemy (−30). Enemies never inside the same gang.
        /// </summary>
        private static void SeedRelationships(SocialRoster roster, System.Random rng)
        {
            var inmates = new List<NPCIdentity>(roster.Inmates());

            foreach (var a in inmates)
            {
                foreach (var b in inmates)
                {
                    if (a.actorId == b.actorId) continue;
                    if (a.gangId != SocialTuning.IndependentGangId && a.gangId == b.gangId)
                        roster.relationships.Seed(a.actorId, b.actorId, SocialTuning.GangMateSeedTrust, 0f);
                }
            }

            foreach (var inmate in inmates)
            {
                int friendCount = 1 + rng.Next(2); // 1–2
                for (int f = 0; f < friendCount; f++)
                {
                    var friend = PickOther(inmates, inmate, rng, candidate =>
                        roster.relationships.GetTrust(inmate.actorId, candidate.actorId) <= 0f);
                    if (friend != null)
                    {
                        roster.relationships.Seed(inmate.actorId, friend.actorId, SocialTuning.SeededFriendTrust, 0f);
                        roster.relationships.Seed(friend.actorId, inmate.actorId, SocialTuning.SeededFriendTrust, 0f);
                    }
                }

                if (rng.Next(2) == 0) // 0–1 enemy
                {
                    var enemy = PickOther(inmates, inmate, rng, candidate =>
                        // never across allied lines: no seeded enemies inside your own gang,
                        // and only pick someone you have no positive record with
                        (inmate.gangId == SocialTuning.IndependentGangId || candidate.gangId != inmate.gangId)
                        && roster.relationships.GetTrust(inmate.actorId, candidate.actorId) <= 0f);
                    if (enemy != null)
                    {
                        roster.relationships.Seed(inmate.actorId, enemy.actorId, SocialTuning.SeededEnemyTrust, -10f);
                        roster.relationships.Seed(enemy.actorId, inmate.actorId, SocialTuning.SeededEnemyTrust, -10f);
                    }
                }
            }
        }

        private static NPCIdentity PickOther(
            List<NPCIdentity> pool, NPCIdentity self, System.Random rng, System.Predicate<NPCIdentity> filter)
        {
            var candidates = new List<NPCIdentity>();
            foreach (var c in pool)
            {
                if (c.actorId == self.actorId) continue;
                if (filter != null && !filter(c)) continue;
                candidates.Add(c);
            }
            if (candidates.Count == 0) return null;
            return candidates[rng.Next(candidates.Count)];
        }
    }
}
