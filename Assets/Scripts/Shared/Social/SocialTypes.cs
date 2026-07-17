using System;
using System.Collections.Generic;

namespace Prison.Social
{
    /// <summary>Player-facing relationship band derived from Standing (0.6·trust + 0.4·respect).</summary>
    public enum StandingBand
    {
        Enemy,      // ≤ −50
        Hostile,    // −49…−25
        Neutral,    // −24…+24
        Friendly,   // +25…+49
        Ally,       // +50…+74
        Confidant,  // ≥ +75
    }

    public enum PrisonerArchetype
    {
        ShotCaller,
        Soldier,
        Hustler,
        OldTimer,
        Bruiser,
        Snitch,
        Loner,
    }

    public enum GuardArchetype
    {
        ByTheBook,
        Corrupt,
        Rookie,
        Veteran,
    }

    /// <summary>Exclusive gang membership ladder. Traitor lockout is tracked separately per gang.</summary>
    public enum GangRank
    {
        Outsider,
        Associate,
        Member,
        Trusted,
    }

    public enum SocialEventSource
    {
        Direct,
        Witnessed,
        Heard,
    }

    public enum SocialEventType
    {
        Chat,
        Gift,
        FavorForNpc,        // player completed a favor for them
        FavorForPlayer,     // they did a favor for the player
        RiskyFavor,         // mule / lookout class
        Protection,
        IntimidationSuccess,
        IntimidationFail,
        CaughtStealing,
        SnitchedOn,         // actor snitched on target
        CrimeWitnessed,     // contraband visible, restricted zone, vent tampering, theft
        BribeWitnessed,
        GangBetrayal,
        Trade,
        Argument,
    }

    /// <summary>Five trait axes, 0–100, rolled per NPC inside archetype ranges.</summary>
    [Serializable]
    public struct PersonalityTraits
    {
        public int aggression;   // escalation speed, intimidation attempts, ambient arguments
        public int loyalty;      // gang stickiness, betrayal resistance, stash honesty
        public int greed;        // trade prices, bribability, favor payment demands
        public int sociability;  // chat frequency, gossip spread, trust build speed
        public int nerve;        // rule-breaking; low nerve + low loyalty = snitch risk

        public PersonalityTraits(int aggression, int loyalty, int greed, int sociability, int nerve)
        {
            this.aggression = Clamp(aggression);
            this.loyalty = Clamp(loyalty);
            this.greed = Clamp(greed);
            this.sociability = Clamp(sociability);
            this.nerve = Clamp(nerve);
        }

        private static int Clamp(int v) => v < 0 ? 0 : (v > 100 ? 100 : v);
    }

    /// <summary>Sparse two-axis relationship record: how <c>observer</c> feels about <c>subject</c>.</summary>
    [Serializable]
    public struct RelationshipRecord
    {
        public float trust;
        public float respect;

        public RelationshipRecord(float trust, float respect)
        {
            this.trust = trust;
            this.respect = respect;
        }
    }

    /// <summary>One remembered social/crime event in an NPC's ring buffer.</summary>
    [Serializable]
    public struct SocialEvent
    {
        public SocialEventType type;
        public int actor;      // who did it
        public int target;     // who it was done to (or -1)
        public int day;        // in-game day it happened
        public PrisonEventType phase;
        public float weight;   // 1–10; decays daily, grudges (negative, ≥8) decay per 3 days
        public SocialEventSource source;
        public int hops;       // gossip hop count (0 = original)

        /// <summary>Negative events with original weight ≥ 8 decay at one third the normal rate.</summary>
        public bool IsGrudge => IsNegative && weight >= SocialTuning.GrudgeWeightThreshold;

        public bool IsNegative =>
            type == SocialEventType.IntimidationFail ||
            type == SocialEventType.CaughtStealing ||
            type == SocialEventType.SnitchedOn ||
            type == SocialEventType.CrimeWitnessed ||
            type == SocialEventType.BribeWitnessed ||
            type == SocialEventType.GangBetrayal ||
            type == SocialEventType.Argument;
    }

    /// <summary>Generated identity for every social actor (prisoner and guard). Player uses actorId 0.</summary>
    [Serializable]
    public class NPCIdentity
    {
        public int actorId;
        public bool isGuard;
        public string firstName = "";
        public string nickname = "";
        public string lastName = "";
        public PrisonerArchetype archetype;
        public GuardArchetype guardArchetype;
        public PersonalityTraits traits;
        /// <summary>Gang index into the gang list, or -1 for Independent. Guards are always -1.</summary>
        public int gangId = SocialTuning.IndependentGangId;
        /// <summary>Cell index for prisoners; -1 for guards.</summary>
        public int cellIndex = -1;
        /// <summary>Guard role/shift label (e.g. "Standard Patrol"), empty for prisoners.</summary>
        public string roleLabel = "";
        /// <summary>Gift categories this NPC favors (hidden from the player until discovered).</summary>
        public List<ItemCategory> favoredGiftCategories = new List<ItemCategory>();

        public string DisplayName =>
            string.IsNullOrEmpty(nickname)
                ? $"{firstName} {lastName}".Trim()
                : $"{firstName} \"{nickname}\" {lastName}".Trim();

        public string ShortName => string.IsNullOrEmpty(nickname) ? firstName : nickname;
    }

    /// <summary>Central tunable constants for the social ecosystem (design of record: Social Ecosystem &amp; Gangs v3).</summary>
    public static class SocialTuning
    {
        public const int PlayerActorId = 0;
        public const int NoActor = -1;
        public const int IndependentGangId = -1;

        // Relationship axes
        public const float MinAxis = -100f;
        public const float MaxAxis = 100f;
        public const float StandingTrustWeight = 0.6f;
        public const float StandingRespectWeight = 0.4f;

        // Standing band thresholds
        public const float EnemyMax = -50f;      // standing ≤ −50 → Enemy
        public const float HostileMax = -25f;    // −49…−25 → Hostile
        public const float FriendlyMin = 25f;
        public const float AllyMin = 50f;
        public const float ConfidantMin = 75f;

        // Base deltas (before modifiers) — spec §3
        public const float ChatTrust = 2f;
        public const float GiftBaseTrust = 5f;
        public const float FavoredGiftMultiplier = 2f;
        public const float LikedGiftMultiplier = 1.5f;
        public const float RepeatGiftCategoryMultiplier = 0.5f;
        public const float FavorTrustMin = 10f;
        public const float FavorTrustMax = 20f;
        public const float FavorRespect = 5f;
        public const float RiskyFavorTrust = 15f;
        public const float RiskyFavorRespect = 10f;
        public const float IntimidateSuccessTrust = -10f;
        public const float IntimidateSuccessRespect = 15f;
        public const float IntimidateFailTrust = -15f;
        public const float IntimidateFailRespect = -10f;
        public const float CaughtStealingTrust = -40f;
        public const float CaughtStealingRespect = -20f;
        public const float ProtectionTrust = 10f;
        public const float ProtectionRespect = 20f;

        // Memory — spec §4
        public const int MemoryCapacity = 16;
        public const float GrudgeWeightThreshold = 8f;
        public const int GrudgeDecayDays = 3;
        public const float WitnessRadius = 12f;
        public const float CrimeWitnessedWeight = 6f;
        public const float GiftMemoryWeight = 2f;
        public const float FavorMemoryWeight = 4f;
        public const float BetrayalMemoryWeight = 10f;

        // Gossip — spec §7
        public const int GossipMinSociability = 50;
        public const float GossipMinTrust = 25f;
        public const float GossipWeightFactor = 0.5f;
        public const int GossipMaxHops = 2;

        // NPC↔NPC seeding — spec §3
        public const float GangMateSeedTrust = 40f;
        public const float SeededFriendTrust = 30f;
        public const float SeededEnemyTrust = -30f;

        // Gangs — spec §5
        public const float AssociateMinStanding = 25f;
        public const float TrustedMinStanding = 60f;
        public const int TrustedMinGangFavors = 2;
        public const float RivalTradeRefusalStanding = -25f;
        public const float PropagationOutsider = 0.5f;
        public const float PropagationMember = 1f;
        public const float MemberTradePriceFactor = 0.85f;
        public const float TraitorTrustPenalty = -80f;
        public const float TraitorRespectPenalty = -80f;
        public const float TraitorRivalStandingBonus = 20f;
        public const int InitiationCooldownDays = 2;
        public const float InitiationRefusalRespect = -5f;

        // Chat intel bands — spec §6
        public const float IntelScheduleTrust = 25f;
        public const float IntelLootRouteTrust = 50f;
        public const float IntelEscapeLoreTrust = 75f;
        public const float OldTimerSnitchRevealTrust = 50f;

        // Ask-favor gates — spec §6
        public const float LookoutMinTrust = 25f;
        public const float LookoutCost = 10f;
        public const float DistractionMinRespect = 25f;
        public const float DistractionCost = 15f;
        public const float SourceItemMinTrust = 40f;
        public const float SourceItemPriceFactor = 1.5f;
        public const float HoldStashMinTrust = 60f;
        public const int HoldStashMaxItems = 2;
        public const int HoldStashLowLoyalty = 40;
        public const float HoldStashTheftChance = 0.25f;
        public const float SilenceSnitchGangStandingCost = 10f;
        public const int SilenceSnitchMuteDays = 3;
        public const float LookoutWarnRadius = 20f;
        public const float DistractionSeconds = 30f;

        // Trade & bribes — spec §8
        public const float GreedPriceMin = 0.8f;
        public const float GreedPriceMax = 1.5f;
        public const float TrustDiscountMax = 0.25f;
        public const float TrustDiscountFullAt = 75f;
        public const float ContrabandMarkup = 2f;
        public const float BribeClearTip = 25f;
        public const float BribeSkipShakedown = 40f;
        public const float BribeBlindEye = 60f;

        // Reputation tier (kept v1 names/thresholds; new math = avg standing + gang rank bonus)
        public const float AssociateTierMin = 25f;
        public const float RespectedTierMin = 50f;
        public const float KingpinTierMin = 75f;
        public const float TierBonusAssociate = 5f;
        public const float TierBonusMember = 10f;
        public const float TierBonusTrusted = 20f;

        // Personality modifiers — spec §3 (applied before gang factor and soft cap)
        /// <summary>Sociability scales positive trust ±25% (0 → ×0.75, 50 → ×1.0, 100 → ×1.25).</summary>
        public static float SociabilityTrustFactor(int sociability) =>
            0.75f + 0.5f * (Clamp01to100(sociability) / 100f);

        /// <summary>Loyalty scales betrayal penalties ×1–2 (0 → ×1, 100 → ×2).</summary>
        public static float LoyaltyBetrayalFactor(int loyalty) =>
            1f + Clamp01to100(loyalty) / 100f;

        private static int Clamp01to100(int v) => v < 0 ? 0 : (v > 100 ? 100 : v);

        // Guard archetype modifiers — spec §2
        public const float RookieConeAngle = 75f;
        public const float VeteranProximitySpot = 8f;

        // Per-player guard Trust modifiers — Guard AI note, M6 (see GuardTrustMath)
        public const float GuardTrustGraceAtTrust = 50f;
        public const float GuardTrustGraceSeconds = 10f;
        public const float GuardDistrustDetectAtTrust = -25f;
        public const float GuardDistrustDetectRangeMult = 1.2f;
    }
}
