using System;
using System.Collections.Generic;
using UnityEngine;

namespace Prison
{
    public class SocialManager : MonoBehaviour
    {
        public const float PersonalityPerkAffinityThreshold = 50f;

        public static SocialManager Instance { get; private set; }

        [Header("Reputation Thresholds (Static)")]
        [SerializeField] private float associateThreshold = 25f;
        [SerializeField] private float respectedThreshold = 50f;
        [SerializeField] private float kingpinThreshold = 75f;

        [Header("Defaults")]
        [SerializeField] private float defaultGiftAmount = 5f;

        [Header("Favor system")]
        [Tooltip("On each schedule phase: pool is cleared and a new valid favor is rolled per registered prisoner. Leave empty to disable.")]
        [SerializeField] private List<FavorOfferDefinition> favorPool = new List<FavorOfferDefinition>();

        private readonly Dictionary<int, float> prisonerAffinity = new Dictionary<int, float>();
        private readonly Dictionary<int, NPCPersonalityData> prisonerPersonality = new Dictionary<int, NPCPersonalityData>();

        private readonly Dictionary<int, PrisonEventType> lastGreetedPhase = new Dictionary<int, PrisonEventType>();

        private readonly Dictionary<int, ItemCategory?> _lastGiftCategory = new Dictionary<int, ItemCategory?>();
        private readonly HashSet<int> _personalityPerkNotified = new HashSet<int>();

        private readonly Dictionary<int, FavorOfferDefinition> currentActiveFavors = new Dictionary<int, FavorOfferDefinition>();

        private ReputationTier _lastTier = ReputationTier.Outsider;

        public event Action<int, float, float> OnAffinityChanged;
        public event Action<ReputationTier, ReputationTier> OnReputationTierChanged;
        public event Action<int, NPCPersonalityData> OnPersonalityPerkUnlocked;

        public IReadOnlyDictionary<int, float> PrisonerAffinity => prisonerAffinity;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            _lastTier = GetReputationTier();
        }

        private void OnEnable()
        {
            if (PrisonTimeManager.Instance != null)
                PrisonTimeManager.Instance.OnEventChanged += OnPrisonSchedulePhase;
        }

        private void OnDisable()
        {
            if (PrisonTimeManager.Instance != null)
                PrisonTimeManager.Instance.OnEventChanged -= OnPrisonSchedulePhase;
        }

        private void Start()
        {
            if (PrisonTimeManager.Instance != null)
                RebuildFavorsForPhase(PrisonTimeManager.Instance.CurrentEvent);
        }

        private void OnPrisonSchedulePhase(PrisonEventType newPhase)
        {
            RebuildFavorsForPhase(newPhase);
        }

        public void RegisterPrisoner(int cellIndex, NPCPersonalityData personality, float initialAffinity = 0f)
        {
            prisonerPersonality[cellIndex] = personality;
            prisonerAffinity[cellIndex] = Mathf.Clamp(initialAffinity, SocialMath.MinAffinity, SocialMath.MaxAffinity);
            if (prisonerAffinity[cellIndex] >= PersonalityPerkAffinityThreshold)
                _personalityPerkNotified.Add(cellIndex);
            else
                _personalityPerkNotified.Remove(cellIndex);
            EvaluateReputationTierChange();
            if (PrisonTimeManager.Instance != null)
                RebuildFavorsForPhase(PrisonTimeManager.Instance.CurrentEvent);
        }

        public bool IsRegistered(int cellIndex)
        {
            return prisonerAffinity.ContainsKey(cellIndex);
        }

        public float GetAffinity(int cellIndex)
        {
            return prisonerAffinity.TryGetValue(cellIndex, out float affinity) ? affinity : 0f;
        }

        public NPCPersonalityData GetPersonality(int cellIndex)
        {
            return prisonerPersonality.TryGetValue(cellIndex, out NPCPersonalityData personality) ? personality : null;
        }

        public bool IsGreetingBlockedByPhaseCooldown(int cellIndex)
        {
            if (PrisonTimeManager.Instance == null) return false;
            if (!lastGreetedPhase.TryGetValue(cellIndex, out var last)) return false;
            return last == PrisonTimeManager.Instance.CurrentEvent;
        }

        public ActiveFavorInfo GetActiveFavorInfo(int cellIndex)
        {
            if (currentActiveFavors.TryGetValue(cellIndex, out FavorOfferDefinition def) && def != null)
                return new ActiveFavorInfo { HasFavor = true, Definition = def };
            return default;
        }

        public bool TryCompleteFavor(int cellIndex, PlayerInventory inventory, out float newAffinity)
        {
            newAffinity = GetAffinity(cellIndex);
            if (inventory == null) return false;
            if (!currentActiveFavors.TryGetValue(cellIndex, out FavorOfferDefinition def) || def == null || def.requiredItem == null)
                return false;
            if (!inventory.HasItem(def.requiredItem, 1)) return false;
            if (!inventory.RemoveItem(def.requiredItem, 1)) return false;

            newAffinity = ChangeAffinityInternal(cellIndex, SocialActionType.Favor, null, def.affinityReward);
            currentActiveFavors.Remove(cellIndex);
            return true;
        }

        /// <summary>Apply a social action and update affinity. Gift uses <paramref name="giftItem"/>; favor rewards use the favor table via <c>TryCompleteFavor</c>.</summary>
        public void ChangeAffinity(int cellIndex, SocialActionType action, ItemData giftItem = null)
        {
            ChangeAffinityInternal(cellIndex, action, giftItem, 0f);
        }

        private float ChangeAffinityInternal(int cellIndex, SocialActionType actionType, ItemData giftItem, float customBaseAmount)
        {
            EnsurePrisonerEntry(cellIndex);
            if (actionType == SocialActionType.Greeting && IsGreetingBlockedByPhaseCooldown(cellIndex))
                return prisonerAffinity[cellIndex];

            float current = prisonerAffinity[cellIndex];
            NPCPersonalityData personality = GetPersonality(cellIndex);

            if (actionType == SocialActionType.Greeting)
            {
                if (PrisonTimeManager.Instance == null)
                    return RunAffinityApply(cellIndex, current, actionType, giftItem, customBaseAmount, personality, out float nextG, out _);

                PrisonEventType phase = PrisonTimeManager.Instance.CurrentEvent;
                float n = RunAffinityApply(cellIndex, current, actionType, giftItem, customBaseAmount, personality, out float next, out _);
                lastGreetedPhase[cellIndex] = phase;
                return n;
            }

            RunAffinityApply(cellIndex, current, actionType, giftItem, customBaseAmount, personality, out float n2, out _);
            return n2;
        }

        private float RunAffinityApply(
            int cellIndex,
            float current,
            SocialActionType actionType,
            ItemData giftItem,
            float customBaseAmount,
            NPCPersonalityData personality,
            out float next,
            out float effectiveDelta)
        {
            bool useCustom = !Mathf.Approximately(customBaseAmount, 0f);
            float baseDelta = useCustom
                ? customBaseAmount
                : SocialMath.GetBaseAffinityDelta(actionType, personality, giftItem, defaultGiftAmount);

            if (actionType == SocialActionType.Gift && giftItem != null)
            {
                bool isFav = SocialMath.IsFavoredGift(personality, giftItem);
                _lastGiftCategory.TryGetValue(cellIndex, out ItemCategory? lastCat);
                baseDelta = SocialMath.ApplyGiftSameCategoryPenalty(
                    baseDelta, giftItem, lastCat, isFav);
                _lastGiftCategory[cellIndex] = giftItem.category;
            }

            float gainMultiplier = personality != null ? personality.affinityGainMultiplier : 1f;
            next = SocialMath.ApplyAffinityChange(current, baseDelta, gainMultiplier);
            effectiveDelta = next - current;

            prisonerAffinity[cellIndex] = next;
            OnAffinityChanged?.Invoke(cellIndex, next, effectiveDelta);
            EvaluatePerkIfNeeded(cellIndex, current, next, personality);
            EvaluateReputationTierChange();
            return next;
        }

        private void EvaluatePerkIfNeeded(int cellIndex, float before, float after, NPCPersonalityData personality)
        {
            if (personality == null) return;
            if (before < PersonalityPerkAffinityThreshold && after >= PersonalityPerkAffinityThreshold)
            {
                if (_personalityPerkNotified.Add(cellIndex))
                    OnPersonalityPerkUnlocked?.Invoke(cellIndex, personality);
            }
        }

        public float GetAverageAffinity()
        {
            return SocialMath.AverageRegisteredAffinities(PrisonerAffinity);
        }

        public ReputationTier GetReputationTier()
        {
            return SocialMath.GetReputationTier(GetAverageAffinity(), associateThreshold, respectedThreshold, kingpinThreshold);
        }

        private void EnsurePrisonerEntry(int cellIndex)
        {
            if (!prisonerAffinity.ContainsKey(cellIndex))
            {
                prisonerAffinity[cellIndex] = 0f;
            }
        }

        private void EvaluateReputationTierChange()
        {
            ReputationTier currentTier = GetReputationTier();
            if (currentTier == _lastTier) return;
            ReputationTier previous = _lastTier;
            _lastTier = currentTier;
            OnReputationTierChanged?.Invoke(previous, currentTier);
        }

        private void RebuildFavorsForPhase(PrisonEventType phase)
        {
            currentActiveFavors.Clear();

            if (favorPool == null || favorPool.Count == 0) return;

            foreach (int cellIndex in prisonerAffinity.Keys)
            {
                NPCPersonalityData p = GetPersonality(cellIndex);
                var candidates = new List<FavorOfferDefinition>();
                for (int i = 0; i < favorPool.Count; i++)
                {
                    FavorOfferDefinition d = favorPool[i];
                    if (d == null || d.requiredItem == null) continue;
                    if (!d.IsValidFor(phase, p)) continue;
                    candidates.Add(d);
                }
                if (candidates.Count == 0) continue;
                currentActiveFavors[cellIndex] = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            }
        }
    }

    [Serializable]
    public struct ActiveFavorInfo
    {
        public bool HasFavor;
        public FavorOfferDefinition Definition;
    }
}
