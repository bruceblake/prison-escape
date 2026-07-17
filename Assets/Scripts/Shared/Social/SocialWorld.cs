using System;
using System.Collections.Generic;
using UnityEngine;

namespace Prison.Social
{
    /// <summary>
    /// Central runtime service for the v3 social ecosystem (replaces v1 SocialManager).
    /// Owns the roster, relationship store, per-NPC memories, gangs, trading, favors,
    /// gossip and snitching; subscribes to the prison clock for phase/day ticks.
    /// Built by <see cref="GameManager"/> after spawning; deterministic from worldSeed.
    /// </summary>
    public class SocialWorld : MonoBehaviour
    {
        public static SocialWorld Instance { get; private set; }

        public SocialRoster Roster { get; private set; }
        public RelationshipStore Relationships { get; private set; }
        public GangManager Gangs { get; private set; }
        public TradingService Trading { get; private set; }
        public FavorService Favors { get; private set; }
        public SnitchSystem Snitches { get; private set; }

        /// <summary>In-game day counter; increments at each Morning Roll Call.</summary>
        public int CurrentDay { get; private set; } = 1;

        private readonly Dictionary<int, SocialMemory> _memories = new Dictionary<int, SocialMemory>();
        private readonly Dictionary<int, GameObject> _actorObjects = new Dictionary<int, GameObject>();
        private readonly Dictionary<GameObject, int> _actorIdsByObject = new Dictionary<GameObject, int>();
        private readonly HashSet<int> _metActors = new HashSet<int>();
        private readonly HashSet<int> _heardOfActors = new HashSet<int>();
        private readonly Dictionary<int, PrisonEventType> _lastChatPhase = new Dictionary<int, PrisonEventType>();
        private readonly Dictionary<int, ItemCategory> _lastGiftCategory = new Dictionary<int, ItemCategory>();
        private readonly Dictionary<int, HashSet<ItemCategory>> _knownGiftPrefs = new Dictionary<int, HashSet<ItemCategory>>();
        private readonly HashSet<int> _knownCorruptGuards = new HashSet<int>();
        private readonly HashSet<int> _knownSnitches = new HashSet<int>();
        private readonly HashSet<int> _shakedownSkipCells = new HashSet<int>();
        private readonly HashSet<int> _targetedShakedownCells = new HashSet<int>();

        private Transform _playerTransform;
        private ReputationTier _lastTier = ReputationTier.Outsider;
        private bool _pendingBedDeliveryThisMorning;
        private System.Random _rng = new System.Random();
        private int _seed;

        /// <summary>(actorId, trustDelta, respectDelta, record) — player-perspective changes for popups/UI.</summary>
        public event Action<int, float, float, RelationshipRecord> OnPlayerRelationshipChanged;
        public event Action<ReputationTier, ReputationTier> OnReputationTierChanged;
        public event Action<int> OnDayAdvanced;

        // ------------------------------------------------------------------ lifecycle

        public static SocialWorld EnsureInstance()
        {
            if (Instance != null) return Instance;
            var existing = FindAnyObjectByType<SocialWorld>();
            if (existing != null) { Instance = existing; return existing; }
            var go = new GameObject("SocialWorld");
            Instance = go.AddComponent<SocialWorld>();
            return Instance;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            if (PrisonTimeManager.Instance != null)
                PrisonTimeManager.Instance.OnEventChanged += OnPhaseChanged;
        }

        private void OnDisable()
        {
            if (PrisonTimeManager.Instance != null)
                PrisonTimeManager.Instance.OnEventChanged -= OnPhaseChanged;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        // ------------------------------------------------------------------ world building

        /// <summary>
        /// Builds the deterministic roster and binds spawned objects to identities.
        /// <paramref name="inmates"/> pair spawned prisoner objects with their cell index,
        /// in spawn order; identity assignment follows the same order.
        /// </summary>
        public void BuildWorld(int seed, IReadOnlyList<(GameObject go, int cellIndex)> inmates,
            IReadOnlyList<GameObject> guards, float arrivalStandingSeed)
        {
            _seed = seed;
            _rng = new System.Random(seed ^ 0x50C1A1);

            var cellIndices = new List<int>(inmates.Count);
            foreach (var (_, cellIndex) in inmates)
                cellIndices.Add(cellIndex);

            Roster = SocialRosterBuilder.Build(seed, cellIndices, guards.Count);
            Relationships = Roster.relationships;
            Gangs = new GangManager(Roster, Relationships);
            Trading = new TradingService(Roster, seed ^ 0x7EADE);
            Favors = new FavorService(Roster, Relationships, seed ^ 0xFA40);
            Snitches = new SnitchSystem(Roster, Relationships, seed ^ 0x5417C);

            Relationships.OnChanged += HandleRelationshipChanged;
            Favors.OnFavorCompleted += HandleFavorCompleted;

            int i = 0;
            foreach (var identity in Roster.Inmates())
            {
                if (i < inmates.Count)
                    BindActor(identity.actorId, inmates[i].go);
                i++;
            }
            int g = 0;
            foreach (var identity in Roster.Guards())
            {
                if (g < guards.Count)
                {
                    BindActor(identity.actorId, guards[g]);
                    var profile = guards[g].GetComponent<GuardSocialProfile>();
                    if (profile == null) profile = guards[g].AddComponent<GuardSocialProfile>();
                    profile.Bind(identity);
                    var label = guards[g].GetComponent<Prison.Visuals.CharacterNameLabel>();
                    if (label != null) label.SetDisplayName($"Ofc. {identity.lastName}");
                    // Guards join the social layer: Talk entry ([F]) + band tint.
                    if (guards[g].GetComponent<PrisonerSocialPresenter>() == null)
                        guards[g].AddComponent<PrisonerSocialPresenter>();
                }
                g++;
            }

            // Career arrival seed (spec §10): seeds the starting band with non-rivals, never individual history.
            if (Mathf.Abs(arrivalStandingSeed) > 0.01f)
            {
                foreach (var identity in Roster.Inmates())
                    Relationships.Seed(identity.actorId, SocialTuning.PlayerActorId, arrivalStandingSeed, arrivalStandingSeed);
            }

            Trading.RefreshDailyStock();
            Favors.RollOffersForPhase(CurrentDay);
            _lastTier = GetReputationTier();

            if (GetComponent<SocialFavorRuntime>() == null)
                gameObject.AddComponent<SocialFavorRuntime>();
            if (GetComponent<SocialSimulationTicker>() == null)
                gameObject.AddComponent<SocialSimulationTicker>();
            if (GetComponent<GangTerritoryMonitor>() == null)
                gameObject.AddComponent<GangTerritoryMonitor>();
            if (GetComponent<PrisonJobPaymaster>() == null)
                gameObject.AddComponent<PrisonJobPaymaster>();
            if (GetComponent<CrimeSignals>() == null)
                gameObject.AddComponent<CrimeSignals>();
            SocialToastUI.EnsureInstance();

            Debug.Log($"[SocialWorld] Built roster: {cellIndices.Count} inmates, {guards.Count} guards, seed {seed}.");
        }

        /// <summary>
        /// Arrival application of the career gang tag (spec §10): a "Traitor — X" tag makes
        /// the matching gang's Shot-Caller start Hostile in this facility's fresh cast.
        /// </summary>
        public void ApplyCareerGangTag(string tag)
        {
            if (string.IsNullOrEmpty(tag) || !tag.StartsWith("Traitor — ")) return;
            string gangName = tag.Substring("Traitor — ".Length);
            foreach (var gang in GangCatalog.All())
            {
                if (gang.displayName != gangName) continue;
                foreach (var member in Roster.MembersOf(gang.gangId))
                {
                    if (member.archetype != PrisonerArchetype.ShotCaller) continue;
                    Relationships.Seed(member.actorId, SocialTuning.PlayerActorId, -35f, -25f); // Hostile band
                }
            }
        }

        public void RegisterPlayer(GameObject player)
        {
            _playerTransform = player != null ? player.transform : null;
            if (player != null)
                BindActor(SocialTuning.PlayerActorId, player);
        }

        private void BindActor(int actorId, GameObject go)
        {
            if (go == null) return;
            _actorObjects[actorId] = go;
            _actorIdsByObject[go] = actorId;
        }

        // ------------------------------------------------------------------ lookups

        public bool IsBuilt => Roster != null;

        public NPCIdentity GetIdentity(int actorId) => Roster?.Get(actorId);

        public int GetActorId(GameObject go)
        {
            if (go == null) return SocialTuning.NoActor;
            var current = go.transform;
            while (current != null)
            {
                if (_actorIdsByObject.TryGetValue(current.gameObject, out int id))
                    return id;
                current = current.parent;
            }
            return SocialTuning.NoActor;
        }

        public GameObject GetActorObject(int actorId) =>
            _actorObjects.TryGetValue(actorId, out var go) ? go : null;

        public Transform PlayerTransform => _playerTransform;

        public SocialMemory GetMemory(int actorId)
        {
            if (!_memories.TryGetValue(actorId, out var memory))
            {
                memory = new SocialMemory();
                _memories[actorId] = memory;
            }
            return memory;
        }

        public bool HasMet(int actorId) => _metActors.Contains(actorId);
        public bool HasHeardOf(int actorId) => _heardOfActors.Contains(actorId) || _metActors.Contains(actorId);
        public void MarkMet(int actorId) => _metActors.Add(actorId);
        public void MarkHeardOf(int actorId) => _heardOfActors.Add(actorId);

        public bool IsKnownCorrupt(int actorId) => _knownCorruptGuards.Contains(actorId);
        public void MarkKnownCorrupt(int actorId) => _knownCorruptGuards.Add(actorId);
        public bool IsKnownSnitch(int actorId) => _knownSnitches.Contains(actorId);
        public void MarkKnownSnitch(int actorId) => _knownSnitches.Add(actorId);

        public IReadOnlyCollection<ItemCategory> KnownGiftPrefs(int actorId) =>
            _knownGiftPrefs.TryGetValue(actorId, out var set) ? set : (IReadOnlyCollection<ItemCategory>)System.Array.Empty<ItemCategory>();

        // ------------------------------------------------------------------ reputation

        public ReputationTier GetReputationTier()
        {
            if (Roster == null) return ReputationTier.Outsider;
            float sum = 0f;
            int count = 0;
            foreach (var identity in Roster.Inmates())
            {
                if (!HasMet(identity.actorId)) continue;
                sum += Relationships.GetStanding(identity.actorId, SocialTuning.PlayerActorId);
                count++;
            }
            float avg = count > 0 ? sum / count : 0f;
            return RelationshipMath.ComputeTier(avg, Gangs?.PlayerBestRank() ?? GangRank.Outsider);
        }

        // ------------------------------------------------------------------ player acts

        /// <summary>True once the player already chatted with this NPC in the current phase.</summary>
        public bool ChatUsedThisPhase(int actorId)
        {
            if (PrisonTimeManager.Instance == null) return false;
            return _lastChatPhase.TryGetValue(actorId, out var phase)
                   && phase == PrisonTimeManager.Instance.CurrentEvent;
        }

        /// <summary>Chat: +2 trust, 1/phase/NPC. Returns the dialogue line (intel by trust band).</summary>
        public string Chat(int actorId)
        {
            var identity = GetIdentity(actorId);
            if (identity == null || ChatUsedThisPhase(actorId)) return null;

            if (PrisonTimeManager.Instance != null)
                _lastChatPhase[actorId] = PrisonTimeManager.Instance.CurrentEvent;
            MarkMet(actorId);

            ApplyPlayerAct(actorId, SocialEventType.Chat);

            float trust = Relationships.GetTrust(actorId, SocialTuning.PlayerActorId);
            int hash = actorId * 31 + CurrentDay * 7 + (int)(PrisonTimeManager.Instance != null ? PrisonTimeManager.Instance.CurrentEvent : 0);

            // Memory-driven line beats generic intel/greeting when they hold a strong memory of you.
            var strongest = GetMemory(actorId).StrongestAbout(SocialTuning.PlayerActorId, 4f);
            if (strongest != null)
            {
                string reaction = DialogueLibrary.MemoryReaction(strongest.Value, hash);
                if (!string.IsNullOrEmpty(reaction)) return reaction;
            }

            // Old-Timer names known snitches at trust ≥ 50 (spec §7, gossip-first snitch discovery).
            if (identity.archetype == PrisonerArchetype.OldTimer && trust >= SocialTuning.OldTimerSnitchRevealTrust)
            {
                var namedSnitch = FindSnitchToReveal();
                if (namedSnitch != null)
                {
                    MarkKnownSnitch(namedSnitch.actorId);
                    MarkHeardOf(namedSnitch.actorId);
                    return $"Watch yourself around {namedSnitch.DisplayName}. That one talks to the badges.";
                }
            }

            // Hustlers and gossips can out the corrupt guard at friendly trust.
            if (trust >= SocialTuning.IntelLootRouteTrust && identity.traits.sociability >= 50)
            {
                var corrupt = FindCorruptGuardToReveal();
                if (corrupt != null)
                {
                    MarkKnownCorrupt(corrupt.actorId);
                    MarkHeardOf(corrupt.actorId);
                    return $"Ofc. {corrupt.lastName}? Palm open, eyes closed — you follow me.";
                }
            }

            string intel = DialogueLibrary.Intel(trust, hash);
            if (!string.IsNullOrEmpty(intel)) return intel;
            var band = Relationships.GetBand(actorId, SocialTuning.PlayerActorId);
            return DialogueLibrary.Greeting(identity, band, hash);
        }

        /// <summary>Gift (spec §3): favored ×2, liked ×1.5, repeat category ×0.5; discovery reveals prefs.</summary>
        public float Gift(int actorId, ItemData item)
        {
            var identity = GetIdentity(actorId);
            if (identity == null || item == null) return 0f;
            MarkMet(actorId);

            float baseTrust = SocialTuning.GiftBaseTrust;
            bool favored = identity.favoredGiftCategories.Count > 0 && identity.favoredGiftCategories[0] == item.category;
            bool liked = !favored && identity.favoredGiftCategories.Contains(item.category);
            if (favored) baseTrust *= SocialTuning.FavoredGiftMultiplier;
            else if (liked) baseTrust *= SocialTuning.LikedGiftMultiplier;

            if (!favored && !liked
                && _lastGiftCategory.TryGetValue(actorId, out var lastCat) && lastCat == item.category)
                baseTrust *= SocialTuning.RepeatGiftCategoryMultiplier;
            _lastGiftCategory[actorId] = item.category;

            if (favored || liked)
            {
                if (!_knownGiftPrefs.TryGetValue(actorId, out var known))
                {
                    known = new HashSet<ItemCategory>();
                    _knownGiftPrefs[actorId] = known;
                }
                known.Add(item.category);
            }

            ApplyPlayerAct(actorId, SocialEventType.Gift, baseTrust, 0f);
            return baseTrust;
        }

        /// <summary>
        /// Applies a player act on a target through the full pipeline: relationship deltas,
        /// gang standing propagation (spec §5), target memory, and nearby witnesses (spec §4).
        /// </summary>
        public void ApplyPlayerAct(int targetActorId, SocialEventType type,
            float? baseTrustOverride = null, float? baseRespectOverride = null)
        {
            var identity = GetIdentity(targetActorId);
            if (identity == null) return;

            SocialActs.GetBaseDeltas(type, out float baseTrust, out float baseRespect);
            if (baseTrustOverride.HasValue) baseTrust = baseTrustOverride.Value;
            if (baseRespectOverride.HasValue) baseRespect = baseRespectOverride.Value;

            bool betrayal = SocialActs.IsBetrayalClass(type);
            Relationships.ApplyDeltas(targetActorId, SocialTuning.PlayerActorId,
                baseTrust, baseRespect, identity.traits, betrayal);

            // Gang propagation: helping/hurting a member shifts the rest of the gang.
            if (!identity.isGuard && identity.gangId != SocialTuning.IndependentGangId)
            {
                float factor = Gangs.PropagationFactor(identity.gangId);
                foreach (var mate in Roster.MembersOf(identity.gangId))
                {
                    if (mate.actorId == targetActorId) continue;
                    Relationships.ApplyDeltas(mate.actorId, SocialTuning.PlayerActorId,
                        baseTrust, baseRespect, mate.traits, betrayal, factor);
                }
            }

            RecordWithWitnesses(new SocialEvent
            {
                type = type,
                actor = SocialTuning.PlayerActorId,
                target = targetActorId,
                day = CurrentDay,
                phase = PrisonTimeManager.Instance != null ? PrisonTimeManager.Instance.CurrentEvent : PrisonEventType.FreeTime,
                weight = SocialActs.MemoryWeight(type),
                source = SocialEventSource.Direct,
            });

            EvaluateTier();
        }

        /// <summary>Records the event in the target's memory and as Witnessed for NPCs within 12 m + LOS.</summary>
        public void RecordWithWitnesses(SocialEvent evt)
        {
            if (evt.target != SocialTuning.NoActor && evt.target != SocialTuning.PlayerActorId)
                GetMemory(evt.target).Record(evt);

            Vector3? origin = PositionOf(evt.target != SocialTuning.NoActor ? evt.target : evt.actor);
            if (origin == null) return;

            foreach (var identity in Roster.identities)
            {
                if (identity.actorId == evt.target || identity.actorId == evt.actor) continue;
                var pos = PositionOf(identity.actorId);
                if (pos == null) continue;
                if (Vector3.Distance(origin.Value, pos.Value) > SocialTuning.WitnessRadius) continue;
                if (!HasLineOfSight(origin.Value, pos.Value)) continue;

                var witnessed = evt;
                witnessed.source = SocialEventSource.Witnessed;
                GetMemory(identity.actorId).Record(witnessed);
            }
        }

        /// <summary>Publishes a player crime (contraband visible, restricted zone, vent tampering, theft) to bystanders.</summary>
        public void PublishPlayerCrime(Vector3 position)
        {
            var evt = new SocialEvent
            {
                type = SocialEventType.CrimeWitnessed,
                actor = SocialTuning.PlayerActorId,
                target = SocialTuning.NoActor,
                day = CurrentDay,
                phase = PrisonTimeManager.Instance != null ? PrisonTimeManager.Instance.CurrentEvent : PrisonEventType.FreeTime,
                weight = SocialTuning.CrimeWitnessedWeight,
                source = SocialEventSource.Witnessed,
            };

            foreach (var identity in Roster.identities)
            {
                var pos = PositionOf(identity.actorId);
                if (pos == null) continue;
                if (Vector3.Distance(position, pos.Value) > SocialTuning.WitnessRadius) continue;
                if (!HasLineOfSight(position, pos.Value)) continue;
                GetMemory(identity.actorId).Record(evt);
            }
        }

        public Vector3? PositionOf(int actorId)
        {
            if (actorId == SocialTuning.PlayerActorId)
                return _playerTransform != null ? _playerTransform.position : (Vector3?)null;
            var go = GetActorObject(actorId);
            return go != null ? go.transform.position : (Vector3?)null;
        }

        private bool HasLineOfSight(Vector3 from, Vector3 to)
        {
            Vector3 eyeFrom = from + Vector3.up * 1.6f;
            Vector3 eyeTo = to + Vector3.up * 1.6f;
            // Walls block gossip-worthy sightlines; characters themselves shouldn't.
            if (Physics.Linecast(eyeFrom, eyeTo, out var hit))
                return hit.collider == null || hit.collider.GetComponentInParent<IInteractable>() != null
                       || hit.collider.GetComponentInParent<PrisonerAI>() != null
                       || hit.collider.GetComponentInParent<GuardFSM>() != null;
            return true;
        }

        // ------------------------------------------------------------------ player actions

        /// <summary>
        /// Intimidation (spec §6): respect + Strength vs their nerve. Success: +15 respect,
        /// −10 trust. Fail: −10 respect, −15 trust, and they may report you (no fight stub).
        /// </summary>
        public bool Intimidate(int actorId, out float chance)
        {
            chance = 0f;
            var identity = GetIdentity(actorId);
            if (identity == null) return false;

            float respect = Relationships.GetRespect(actorId, SocialTuning.PlayerActorId);
            float strength = PlayerStats.Instance != null ? PlayerStats.Instance.Strength : 0f;
            chance = RelationshipMath.IntimidationChance(respect, strength, identity.traits.nerve);

            bool success = _rng.NextDouble() < chance;
            ApplyPlayerAct(actorId, success ? SocialEventType.IntimidationSuccess : SocialEventType.IntimidationFail);

            if (!success)
            {
                // Report risk scales with their nerve — shaken cowards stay quiet, hard cases talk.
                float reportChance = 0.25f + identity.traits.nerve / 250f;
                if (_rng.NextDouble() < reportChance)
                {
                    PrisonSecurityAlerts.RaiseSuspicion($"{identity.ShortName} told the guards you threatened them.");
                    QueueTargetedShakedown(CellOf(SocialTuning.PlayerActorId));
                }
            }
            return success;
        }

        /// <summary>
        /// Player snitches on an inmate to a guard (spec §6): the guard tosses the target's
        /// cell, +10 guard trust, and the target's friends turn on you (−40 trust). Completes
        /// any matching sabotage favor.
        /// </summary>
        public void PlayerSnitchOn(int targetActorId, int guardActorId)
        {
            var target = GetIdentity(targetActorId);
            var guard = GetIdentity(guardActorId);
            if (target == null || guard == null || !guard.isGuard) return;

            Relationships.ApplyDeltas(guardActorId, SocialTuning.PlayerActorId, 10f, 0f, guard.traits);
            QueueTargetedShakedown(target.cellIndex);

            var evt = new SocialEvent
            {
                type = SocialEventType.SnitchedOn,
                actor = SocialTuning.PlayerActorId,
                target = targetActorId,
                day = CurrentDay,
                phase = PrisonTimeManager.Instance != null ? PrisonTimeManager.Instance.CurrentEvent : PrisonEventType.FreeTime,
                weight = SocialTuning.BetrayalMemoryWeight,
                source = SocialEventSource.Direct,
            };
            RecordWithWitnesses(evt);

            // The target and their friends wreck your trust (spec: −40 with target's friends).
            Relationships.ApplyDeltas(targetActorId, SocialTuning.PlayerActorId,
                SocialTuning.CaughtStealingTrust, SocialTuning.CaughtStealingRespect, target.traits, true);
            foreach (var identity in Roster.Inmates())
            {
                if (identity.actorId == targetActorId) continue;
                if (Relationships.GetStanding(identity.actorId, targetActorId) < SocialTuning.FriendlyMin) continue;
                Relationships.ApplyDeltas(identity.actorId, SocialTuning.PlayerActorId,
                    SocialTuning.CaughtStealingTrust, 0f, identity.traits, true);
            }

            // Snitching on a member of your own gang is betrayal → Traitor lockout.
            if (!target.isGuard && target.gangId != SocialTuning.IndependentGangId && Gangs.IsMemberOf(target.gangId))
                ApplyTraitorFallout(target.gangId);

            // Sabotage favor: "get X shaken down" completes through this exact path.
            foreach (var favor in new List<FavorInstance>(Favors.All))
            {
                if (favor.kind == FavorKind.Sabotage && favor.state == FavorState.Active
                    && favor.targetActorId == targetActorId)
                    Favors.Complete(favor);
            }

            EvaluateTier();
        }

        /// <summary>Bribe options for a discovered Corrupt guard (spec §8). Returns false when it can't be paid.</summary>
        public bool BribeCorrupt(int guardActorId, float price, string effect)
        {
            var guard = GetIdentity(guardActorId);
            if (guard == null || guard.guardArchetype != GuardArchetype.Corrupt) return false;
            var wallet = PlayerWallet.Instance;
            if (wallet == null || wallet.Balance < price) return false;

            wallet.Add(-price);

            switch (effect)
            {
                case "cleartip":
                    Snitches.ClearOneTipAgainstPlayer();
                    SocialToastUI.Show($"Ofc. {guard.lastName} loses some paperwork. (${price:0})");
                    break;
                case "skipcell":
                    QueueShakedownSkip(CellOf(SocialTuning.PlayerActorId));
                    SocialToastUI.Show($"Your cell gets 'already searched' tomorrow. (${price:0})");
                    break;
                case "blindeye":
                    var go = GetActorObject(guardActorId);
                    var profile = go != null ? go.GetComponent<GuardSocialProfile>() : null;
                    if (profile != null) profile.ActivateBlindEye();
                    SocialToastUI.Show($"Ofc. {guard.lastName} suddenly finds the ceiling fascinating. (${price:0})");
                    break;
                default:
                    return false;
            }

            Relationships.ApplyDeltas(guardActorId, SocialTuning.PlayerActorId, 5f, 0f, guard.traits);

            // Bribing in view of witnesses creates leverage for snitches (spec §8).
            RecordWithWitnesses(new SocialEvent
            {
                type = SocialEventType.BribeWitnessed,
                actor = SocialTuning.PlayerActorId,
                target = guardActorId,
                day = CurrentDay,
                phase = PrisonTimeManager.Instance != null ? PrisonTimeManager.Instance.CurrentEvent : PrisonEventType.FreeTime,
                weight = SocialTuning.CrimeWitnessedWeight,
                source = SocialEventSource.Witnessed,
            });
            return true;
        }

        // ------------------------------------------------------------------ shakedown hooks

        public void QueueTargetedShakedown(int cellIndex)
        {
            if (cellIndex >= 0) _targetedShakedownCells.Add(cellIndex);
        }

        public void QueueShakedownSkip(int cellIndex)
        {
            if (cellIndex >= 0) _shakedownSkipCells.Add(cellIndex);
        }

        /// <summary>Consumed by the morning sweeper: cells with pending snitch-tip searches.</summary>
        public bool ConsumeTargetedShakedown(int cellIndex) => _targetedShakedownCells.Remove(cellIndex);

        /// <summary>Consumed by the morning sweeper: bribe-bought skip for this cell.</summary>
        public bool ConsumeShakedownSkip(int cellIndex) => _shakedownSkipCells.Remove(cellIndex);

        public bool HasTargetedShakedowns => _targetedShakedownCells.Count > 0;

        // ------------------------------------------------------------------ clock ticks

        private void OnPhaseChanged(PrisonEventType phase)
        {
            if (Roster == null) return;

            if (phase == PrisonEventType.MorningRollCall)
                AdvanceDay();

            Favors.RollOffersForPhase(CurrentDay);

            var tips = Snitches.RollReports(GetMemory, CurrentDay);
            foreach (var tip in tips)
            {
                if (tip.targetActorId == SocialTuning.PlayerActorId)
                    PrisonSecurityAlerts.RaiseSuspicion("An inmate tipped the guards about you.");
            }

            // Bed deliveries land after the morning headcount (the phase after MorningRollCall).
            if (_pendingBedDeliveryThisMorning && phase != PrisonEventType.MorningRollCall)
            {
                _pendingBedDeliveryThisMorning = false;
                var inventory = FindPlayerInventory();
                int delivered = Trading.DeliverPendingToBed(inventory);
                if (delivered > 0)
                    SocialToastUI.Show($"Something was left under your bed. ({delivered} item{(delivered > 1 ? "s" : "")})");
            }
        }

        private void AdvanceDay()
        {
            CurrentDay++;

            foreach (var memory in _memories.Values)
                memory.DecayOneDay(CurrentDay);

            Trading.RefreshDailyStock();

            foreach (var failed in Favors.TickDay(CurrentDay))
            {
                if (failed.kind == FavorKind.Initiation)
                {
                    Gangs.RefuseOrFailInitiation(CurrentDay);
                    ApplyShotCallerRespectHit(failed.gangId);
                    SocialToastUI.Show("You blew the initiation window.");
                }
            }
            Gangs.TickInitiationDeadline(CurrentDay);

            // Snitch tips go actionable in the morning: queue targeted cell searches.
            float guardTrust = AverageGuardTrustTowardPlayer();
            foreach (var tip in Snitches.DrainActionableTips(guardTrust))
            {
                int cell = CellOf(tip.targetActorId);
                if (cell >= 0)
                {
                    QueueTargetedShakedown(cell);
                    if (tip.targetActorId == SocialTuning.PlayerActorId)
                        SocialToastUI.Show("The guards are heading for YOUR cell...");
                }
            }

            if (Trading.PendingBedDeliveryCount > 0)
                _pendingBedDeliveryThisMorning = true;

            OnDayAdvanced?.Invoke(CurrentDay);
        }

        public int CellOf(int actorId)
        {
            if (actorId == SocialTuning.PlayerActorId)
            {
                var pc = FindAnyObjectByType<PrisonerController>();
                return pc != null ? pc.cellIndex : -1;
            }
            var identity = GetIdentity(actorId);
            return identity != null ? identity.cellIndex : -1;
        }

        public float AverageGuardTrustTowardPlayer()
        {
            float sum = 0f;
            int count = 0;
            foreach (var guard in Roster.Guards())
            {
                sum += Relationships.GetTrust(guard.actorId, SocialTuning.PlayerActorId);
                count++;
            }
            return count > 0 ? sum / count : 0f;
        }

        public void ApplyShotCallerRespectHit(int gangId)
        {
            foreach (var member in Roster.MembersOf(gangId))
            {
                if (member.archetype != PrisonerArchetype.ShotCaller) continue;
                Relationships.ApplyDeltas(member.actorId, SocialTuning.PlayerActorId,
                    0f, SocialTuning.InitiationRefusalRespect, member.traits);
            }
        }

        /// <summary>Traitor lockout fallout (spec §5): −80/−80 with ex-gang, +20 standing with rivals.</summary>
        public void ApplyTraitorFallout(int betrayedGangId)
        {
            Gangs.MarkTraitor(betrayedGangId);
            foreach (var member in Roster.MembersOf(betrayedGangId))
            {
                Relationships.ApplyDeltas(member.actorId, SocialTuning.PlayerActorId,
                    SocialTuning.TraitorTrustPenalty, SocialTuning.TraitorRespectPenalty, member.traits, true);
                GetMemory(member.actorId).Record(new SocialEvent
                {
                    type = SocialEventType.GangBetrayal,
                    actor = SocialTuning.PlayerActorId,
                    target = member.actorId,
                    day = CurrentDay,
                    phase = PrisonTimeManager.Instance != null ? PrisonTimeManager.Instance.CurrentEvent : PrisonEventType.FreeTime,
                    weight = SocialTuning.BetrayalMemoryWeight,
                    source = SocialEventSource.Direct,
                });
            }
            int rival = GangCatalog.RivalOf(betrayedGangId);
            if (rival != SocialTuning.IndependentGangId)
            {
                foreach (var member in Roster.MembersOf(rival))
                    Relationships.ApplyDeltas(member.actorId, SocialTuning.PlayerActorId,
                        SocialTuning.TraitorRivalStandingBonus, SocialTuning.TraitorRivalStandingBonus, member.traits);
            }
            EvaluateTier();
        }

        // ------------------------------------------------------------------ internals

        private void HandleRelationshipChanged(int observer, int subject, float trustDelta, float respectDelta, RelationshipRecord record)
        {
            if (subject == SocialTuning.PlayerActorId)
                OnPlayerRelationshipChanged?.Invoke(observer, trustDelta, respectDelta, record);
        }

        private void HandleFavorCompleted(FavorInstance favor)
        {
            if (favor.direction != FavorDirection.DoFavor) return;

            var type = favor.kind == FavorKind.Delivery || favor.kind == FavorKind.Sabotage
                ? SocialEventType.RiskyFavor
                : SocialEventType.FavorForNpc;
            ApplyPlayerAct(favor.npcActorId, type);

            if (favor.cashReward > 0f && PlayerWallet.Instance != null)
                PlayerWallet.Instance.Add(favor.cashReward);

            if (favor.kind == FavorKind.Initiation)
            {
                Gangs.CompleteInitiation();
                var gang = GangCatalog.Get(favor.gangId);
                SocialToastUI.Show($"You're in. {gang?.displayName ?? "The gang"} calls you one of theirs now.");
            }
            else if (favor.isGangFavor && favor.gangId != SocialTuning.IndependentGangId
                     && Gangs.IsMemberOf(favor.gangId))
            {
                Gangs.CompleteGangFavor(favor.gangId);
            }

            EvaluateTier();
        }

        private void EvaluateTier()
        {
            var tier = GetReputationTier();
            if (tier == _lastTier) return;
            var prev = _lastTier;
            _lastTier = tier;
            OnReputationTierChanged?.Invoke(prev, tier);
        }

        private NPCIdentity FindSnitchToReveal()
        {
            foreach (var identity in Roster.Inmates())
            {
                if (identity.archetype != PrisonerArchetype.Snitch) continue;
                if (IsKnownSnitch(identity.actorId)) continue;
                if (!HasHeardOf(identity.actorId) && !HasMet(identity.actorId)) continue;
                return identity;
            }
            return null;
        }

        private NPCIdentity FindCorruptGuardToReveal()
        {
            foreach (var identity in Roster.Guards())
            {
                if (identity.guardArchetype != GuardArchetype.Corrupt) continue;
                if (IsKnownCorrupt(identity.actorId)) continue;
                return identity;
            }
            return null;
        }

        private PlayerInventory FindPlayerInventory()
        {
            if (_playerTransform != null)
            {
                var inv = _playerTransform.GetComponentInChildren<PlayerInventory>();
                if (inv != null) return inv;
            }
            return FindAnyObjectByType<PlayerInventory>();
        }
    }
}
