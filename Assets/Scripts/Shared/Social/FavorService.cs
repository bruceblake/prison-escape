using System;
using System.Collections.Generic;
using UnityEngine;

namespace Prison.Social
{
    /// <summary>Favors flow both ways (spec §6). Direction is from the player's point of view.</summary>
    public enum FavorDirection
    {
        DoFavor,   // NPC asks, player does it
        AskFavor,  // player asks, NPC does it
    }

    public enum FavorKind
    {
        // Do-favors (NPC → player). No beat-up: combat is out of scope.
        Fetch,          // bring me <item>
        Delivery,       // mule <item> to <target inmate>
        Protection,     // stay near me this phase
        Sabotage,       // get <target inmate> shaken down (snitch on them to a guard)
        Initiation,     // gang initiation task (fetch-flavored, from the Shot-Caller)
        // Ask-favors (player → NPC)
        Lookout,        // warns when a guard is within 20 m, one phase
        Distraction,    // fake argument pulls the nearest guard ~30 s (Veterans immune)
        SourceItem,     // requested category delivered in 1–2 days
        HoldStash,      // holds 2 items through your shakedown
        SilenceSnitch,  // gang Trusted: mute a named snitch for 3 days
    }

    public enum FavorState
    {
        Offered,
        Active,
        Completed,
        Failed,
    }

    /// <summary>A live favor instance.</summary>
    public class FavorInstance
    {
        public FavorDirection direction;
        public FavorKind kind;
        public int npcActorId;              // who asked (DoFavor) or who is doing it (AskFavor)
        public int targetActorId = SocialTuning.NoActor; // delivery/sabotage/silence target
        public ItemData item;               // fetch/delivery/source item
        public ItemCategory itemCategory;   // source-item requests are by category
        public int deadlineDay;             // inclusive; past it → Failed
        public FavorState state = FavorState.Offered;
        public float cashReward;            // payout to the player on completion (DoFavor)
        public bool isGangFavor;            // counts toward Trusted rank
        public int gangId = SocialTuning.IndependentGangId;
        public int deliverOnDay;            // AskFavor SourceItem arrival day
        public List<ItemData> heldStash;    // HoldStash contents

        public string Describe(SocialRoster roster)
        {
            string npc = roster?.Get(npcActorId)?.ShortName ?? "them";
            string target = targetActorId != SocialTuning.NoActor ? roster?.Get(targetActorId)?.ShortName ?? "someone" : "";
            switch (kind)
            {
                case FavorKind.Fetch: return $"Bring {npc} {(item != null ? item.itemName : "an item")}";
                case FavorKind.Delivery: return $"Mule {(item != null ? item.itemName : "a package")} to {target}";
                case FavorKind.Protection: return $"Stay close to {npc} this phase";
                case FavorKind.Sabotage: return $"Get {target} shaken down";
                case FavorKind.Initiation: return $"Initiation: bring {npc} {(item != null ? item.itemName : "proof")}";
                case FavorKind.Lookout: return $"{npc} is watching for guards";
                case FavorKind.Distraction: return $"{npc} is staging a distraction";
                case FavorKind.SourceItem: return $"{npc} is sourcing {itemCategory}";
                case FavorKind.HoldStash: return $"{npc} is holding your stash";
                case FavorKind.SilenceSnitch: return $"{npc} is silencing {target}";
                default: return kind.ToString();
            }
        }
    }

    /// <summary>
    /// Two-way favor tracking (spec §6): NPC offers rolled per phase, ask-favor gates/costs,
    /// deadlines on day tick. World-side effects (lookout warnings, guard distraction,
    /// stash return) are executed by <see cref="SocialWorld"/> and its monitors.
    /// </summary>
    public class FavorService
    {
        private readonly SocialRoster _roster;
        private readonly RelationshipStore _relationships;
        private readonly System.Random _rng;
        private readonly List<FavorInstance> _favors = new List<FavorInstance>();

        /// <summary>Chance an eligible NPC rolls a new do-favor offer at a phase change.</summary>
        public float offerChancePerPhase = 0.18f;
        /// <summary>Max simultaneous open do-favor offers across the prison.</summary>
        public int maxOpenOffers = 3;

        public event Action<FavorInstance> OnFavorCompleted;
        public event Action<FavorInstance> OnFavorFailed;

        public FavorService(SocialRoster roster, RelationshipStore relationships, int seed)
        {
            _roster = roster;
            _relationships = relationships;
            _rng = new System.Random(seed);
        }

        public IReadOnlyList<FavorInstance> All => _favors;

        public FavorInstance OpenOfferFor(int actorId) =>
            _favors.Find(f => f.npcActorId == actorId && f.direction == FavorDirection.DoFavor
                              && (f.state == FavorState.Offered || f.state == FavorState.Active));

        public FavorInstance ActiveAskFavor(FavorKind kind) =>
            _favors.Find(f => f.direction == FavorDirection.AskFavor && f.kind == kind && f.state == FavorState.Active);

        public IEnumerable<FavorInstance> ActiveAskFavors()
        {
            foreach (var f in _favors)
                if (f.direction == FavorDirection.AskFavor && f.state == FavorState.Active)
                    yield return f;
        }

        // ---------------------------------------------------------------- do-favors

        /// <summary>Rolls new NPC → player favor offers (call at phase change).</summary>
        public void RollOffersForPhase(int currentDay)
        {
            int open = 0;
            foreach (var f in _favors)
                if (f.direction == FavorDirection.DoFavor && (f.state == FavorState.Offered || f.state == FavorState.Active))
                    open++;

            foreach (var identity in _roster.Inmates())
            {
                if (open >= maxOpenOffers) break;
                if (OpenOfferFor(identity.actorId) != null) continue;
                // Sociable NPCs ask more; Loners barely ever ask.
                float chance = offerChancePerPhase * (0.5f + identity.traits.sociability / 100f);
                if (_rng.NextDouble() > chance) continue;

                var favor = CreateOfferFor(identity, currentDay);
                if (favor == null) continue;
                _favors.Add(favor);
                open++;
            }
        }

        private FavorInstance CreateOfferFor(NPCIdentity identity, int currentDay)
        {
            var kinds = new List<FavorKind> { FavorKind.Fetch };
            var inmates = new List<NPCIdentity>(_roster.Inmates());
            if (inmates.Count > 1) kinds.Add(FavorKind.Delivery);
            if (identity.traits.aggression < 60) kinds.Add(FavorKind.Protection);
            if (identity.gangId != SocialTuning.IndependentGangId && inmates.Count > 2) kinds.Add(FavorKind.Sabotage);

            var kind = kinds[_rng.Next(kinds.Count)];
            var favor = new FavorInstance
            {
                direction = FavorDirection.DoFavor,
                kind = kind,
                npcActorId = identity.actorId,
                deadlineDay = currentDay + 1,
                // Career ladder: favor payouts scale with cashIncomeMult (1 outside a career run).
                cashReward = Mathf.Round((8f + identity.traits.greed * 0.12f + (float)_rng.NextDouble() * 6f)
                    * Prison.Career.CareerSession.CashIncomeMult),
                isGangFavor = identity.gangId != SocialTuning.IndependentGangId
                              && identity.archetype == PrisonerArchetype.ShotCaller,
                gangId = identity.gangId,
            };

            switch (kind)
            {
                case FavorKind.Fetch:
                    favor.item = RandomFetchItem();
                    if (favor.item == null) return null;
                    break;
                case FavorKind.Delivery:
                    favor.item = RandomFetchItem();
                    favor.targetActorId = RandomOtherInmate(identity.actorId);
                    if (favor.item == null || favor.targetActorId == SocialTuning.NoActor) return null;
                    break;
                case FavorKind.Sabotage:
                    favor.targetActorId = RandomRivalOrEnemy(identity);
                    if (favor.targetActorId == SocialTuning.NoActor) return null;
                    break;
            }
            return favor;
        }

        /// <summary>Gang initiation favor from the Shot-Caller (spec §5): fetch-flavored, 2-day timer.</summary>
        public FavorInstance CreateInitiationFavor(NPCIdentity shotCaller, int currentDay)
        {
            var item = RandomFetchItem();
            if (item == null) return null;
            var favor = new FavorInstance
            {
                direction = FavorDirection.DoFavor,
                kind = FavorKind.Initiation,
                npcActorId = shotCaller.actorId,
                item = item,
                deadlineDay = currentDay + SocialTuning.InitiationCooldownDays,
                isGangFavor = true,
                gangId = shotCaller.gangId,
                state = FavorState.Active,
            };
            _favors.Add(favor);
            return favor;
        }

        public void AcceptOffer(FavorInstance favor) => favor.state = FavorState.Active;

        /// <summary>Declining a regular offer carries no penalty; it just goes away.</summary>
        public void Decline(FavorInstance favor) => _favors.Remove(favor);

        public void Complete(FavorInstance favor)
        {
            favor.state = FavorState.Completed;
            OnFavorCompleted?.Invoke(favor);
            _favors.Remove(favor);
        }

        public void Fail(FavorInstance favor)
        {
            favor.state = FavorState.Failed;
            OnFavorFailed?.Invoke(favor);
            _favors.Remove(favor);
        }

        // ---------------------------------------------------------------- ask-favors

        /// <summary>Gate check for asking a favor of an NPC (spec §6 table). Returns a reason when blocked.</summary>
        public bool CanAsk(FavorKind kind, NPCIdentity npc, GangManager gangs, out string blockedReason)
        {
            blockedReason = null;
            float trust = _relationships.GetTrust(npc.actorId, SocialTuning.PlayerActorId);
            float respect = _relationships.GetRespect(npc.actorId, SocialTuning.PlayerActorId);

            switch (kind)
            {
                case FavorKind.Lookout:
                    if (trust < SocialTuning.LookoutMinTrust) blockedReason = $"Needs trust {SocialTuning.LookoutMinTrust}+";
                    break;
                case FavorKind.Distraction:
                    if (respect < SocialTuning.DistractionMinRespect) blockedReason = $"Needs respect {SocialTuning.DistractionMinRespect}+";
                    break;
                case FavorKind.SourceItem:
                    if (trust < SocialTuning.SourceItemMinTrust)
                        blockedReason = $"Needs trust {SocialTuning.SourceItemMinTrust}+";
                    else if (npc.archetype != PrisonerArchetype.Hustler && npc.gangId == SocialTuning.IndependentGangId)
                        blockedReason = "Only hustlers or gang members can source";
                    break;
                case FavorKind.HoldStash:
                    if (trust < SocialTuning.HoldStashMinTrust) blockedReason = $"Needs trust {SocialTuning.HoldStashMinTrust}+";
                    else if (npc.archetype == PrisonerArchetype.Snitch) blockedReason = "You don't trust their mouth";
                    break;
                case FavorKind.SilenceSnitch:
                    if (npc.gangId == SocialTuning.IndependentGangId
                        || gangs == null
                        || !gangs.IsMemberOf(npc.gangId)
                        || gangs.GetRank(npc.gangId) != GangRank.Trusted)
                        blockedReason = "Needs gang Trusted rank";
                    break;
                default:
                    blockedReason = "Not askable";
                    break;
            }
            if (ActiveAskFavor(kind) != null) blockedReason = "Already running";
            return blockedReason == null;
        }

        public FavorInstance StartAskFavor(FavorKind kind, NPCIdentity npc, int currentDay,
            int targetActorId = SocialTuning.NoActor, ItemCategory category = ItemCategory.CraftingPart)
        {
            var favor = new FavorInstance
            {
                direction = FavorDirection.AskFavor,
                kind = kind,
                npcActorId = npc.actorId,
                targetActorId = targetActorId,
                itemCategory = category,
                state = FavorState.Active,
                deadlineDay = currentDay + 2,
                deliverOnDay = currentDay + 1 + _rng.Next(2), // source item: 1–2 days
            };
            _favors.Add(favor);
            return favor;
        }

        /// <summary>Hold stash: NPC keeps the items; low loyalty (&lt; 40) → 25% they keep one on return.</summary>
        public List<ItemData> ReturnStash(FavorInstance favor, NPCIdentity holder, out ItemData stolen)
        {
            stolen = null;
            var items = favor.heldStash ?? new List<ItemData>();
            if (holder != null
                && holder.traits.loyalty < SocialTuning.HoldStashLowLoyalty
                && items.Count > 0
                && _rng.NextDouble() < SocialTuning.HoldStashTheftChance)
            {
                stolen = items[_rng.Next(items.Count)];
                items.Remove(stolen);
            }
            Complete(favor);
            return items;
        }

        /// <summary>Expires deadlines (call on day tick). Returns favors that just failed.</summary>
        public List<FavorInstance> TickDay(int currentDay)
        {
            var failed = new List<FavorInstance>();
            for (int i = _favors.Count - 1; i >= 0; i--)
            {
                var f = _favors[i];
                bool expirable = f.direction == FavorDirection.DoFavor
                                 && (f.state == FavorState.Active || f.state == FavorState.Offered);
                if (expirable && currentDay > f.deadlineDay)
                {
                    f.state = FavorState.Failed;
                    _favors.RemoveAt(i);
                    failed.Add(f);
                    OnFavorFailed?.Invoke(f);
                }
            }
            return failed;
        }

        // ---------------------------------------------------------------- helpers

        private ItemData RandomFetchItem()
        {
            var db = ItemDatabase.Singleton;
            if (db == null || db.allItemsInGame == null || db.allItemsInGame.Count == 0) return null;
            var pool = new List<ItemData>();
            foreach (var item in db.allItemsInGame)
            {
                if (item == null) continue;
                if (item.category == ItemCategory.Weapon) continue; // no weapon runs
                if (item.rarity == ItemRarity.Legendary) continue;
                pool.Add(item);
            }
            return pool.Count > 0 ? pool[_rng.Next(pool.Count)] : null;
        }

        public ItemData RandomItemOfCategory(ItemCategory category)
        {
            var db = ItemDatabase.Singleton;
            if (db == null || db.allItemsInGame == null) return null;
            var pool = new List<ItemData>();
            foreach (var item in db.allItemsInGame)
                if (item != null && item.category == category)
                    pool.Add(item);
            return pool.Count > 0 ? pool[_rng.Next(pool.Count)] : null;
        }

        private int RandomOtherInmate(int exceptActorId)
        {
            var pool = new List<int>();
            foreach (var inmate in _roster.Inmates())
                if (inmate.actorId != exceptActorId)
                    pool.Add(inmate.actorId);
            return pool.Count > 0 ? pool[_rng.Next(pool.Count)] : SocialTuning.NoActor;
        }

        /// <summary>Sabotage targets: rival gang members first, else the asker's seeded enemies.</summary>
        private int RandomRivalOrEnemy(NPCIdentity asker)
        {
            var pool = new List<int>();
            int rivalGang = GangCatalog.RivalOf(asker.gangId);
            foreach (var inmate in _roster.Inmates())
            {
                if (inmate.actorId == asker.actorId) continue;
                if (rivalGang != SocialTuning.IndependentGangId && inmate.gangId == rivalGang)
                    pool.Add(inmate.actorId);
            }
            if (pool.Count == 0)
            {
                foreach (var inmate in _roster.Inmates())
                {
                    if (inmate.actorId == asker.actorId) continue;
                    if (_relationships.GetStanding(asker.actorId, inmate.actorId) < 0f)
                        pool.Add(inmate.actorId);
                }
            }
            return pool.Count > 0 ? pool[_rng.Next(pool.Count)] : SocialTuning.NoActor;
        }
    }
}
