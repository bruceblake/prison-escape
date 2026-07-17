using System.Collections.Generic;

namespace Prison.Career
{
    /// <summary>How a facility's soft transfer gate combines its cash and respect thresholds.</summary>
    public enum TransferGateMode
    {
        None,
        /// <summary>Either threshold satisfies the gate (Fed Med / Fed High).</summary>
        Any,
        /// <summary>Both thresholds required (Fed ADX).</summary>
        All,
    }

    /// <summary>
    /// Canonical design data for one facility. All numbers are design targets from
    /// docs/PrisonEscape/01 Game Design/Prison Career Ladder.md § Difficulty &amp; pacing curves;
    /// the <see cref="FacilityDefinition"/> ScriptableObjects created from this stay tunable per asset.
    /// </summary>
    public class FacilityInfo
    {
        public string id;
        /// <summary>"Dev", "County", "State", or "Federal".</summary>
        public string system;
        /// <summary>Security tier label shown on the select card ("Minimum", "ADX", ...). Empty for County/Dev.</summary>
        public string securityLabel;
        /// <summary>Placeholder display title — rename in the design note first, then here.</summary>
        public string title;
        public string description;
        /// <summary>
        /// Scene to load when entering. Facilities whose scene is not yet in the build show as
        /// "UNDER CONSTRUCTION" on the select screen even when unlocked (§ Data model).
        /// </summary>
        public string sceneName;
        /// <summary>0–8 on the career ladder; -1 for the dev sandbox.</summary>
        public int ladderIndex;

        // Difficulty & pacing multipliers (the shape of each curve is the design contract).
        public float lootAbundance = 1f;
        public float cashIncomeMult = 1f;
        public float tradePriceMult = 1f;
        public float bribeCostMult = 1f;
        public float escapeRouteCostMult = 1f;
        public float detectionRangeMult = 1f;
        public float shakedownStrictness = 1f;
        public int recommendedStayDaysMin;
        public int recommendedStayDaysMax;

        /// <summary>In-game days to auto-graduate without escaping. County = 7; 0 = no sentence clock.</summary>
        public int sentenceDays;

        // Soft transfer gates — on *attempting* the boundary route, never on unlocking/entering.
        public TransferGateMode gateMode = TransferGateMode.None;
        public int gateCash;
        public float gateRespect;

        public bool IsDevSandbox => id == FacilityIds.DevSandbox;
        public bool HasSentenceClock => sentenceDays > 0;
    }

    /// <summary>
    /// The 10 canonical facilities (dev sandbox + ladder 0–8) with the locked design numbers.
    /// <see cref="FacilityDirectory"/> serves <see cref="FacilityDefinition"/> assets built from
    /// this data and falls back to it when no asset exists yet.
    /// </summary>
    public static class FacilityCatalog
    {
        // Every facility is its own scene: MainMenu is the hub, the dev sandbox keeps the
        // original dev layout, and each prison loads (or will load) a dedicated scene asset.
        public const string DevLayoutScene = "PrisonLevel1";
        public const string CountyScene = "CountyJail";

        public static readonly FacilityInfo[] All =
        {
            new FacilityInfo
            {
                id = FacilityIds.DevSandbox, system = "Dev", securityLabel = "Sandbox",
                title = "Development Prison",
                description = "Layout, tooling and playtest facility. Reads your career carry but never writes back.",
                sceneName = DevLayoutScene, ladderIndex = -1,
                lootAbundance = 1f, cashIncomeMult = 1f, tradePriceMult = 1f, bribeCostMult = 1f,
                escapeRouteCostMult = 1f, detectionRangeMult = 1f, shakedownStrictness = 1f,
                recommendedStayDaysMin = 0, recommendedStayDaysMax = 0,
            },
            new FacilityInfo
            {
                id = FacilityIds.County, system = "County", securityLabel = "",
                title = "County Detention Center",
                description = "Overcrowded intake block. Contraband everywhere, guards who barely look — and a sentence you can simply serve out.",
                sceneName = CountyScene, ladderIndex = 0,
                lootAbundance = 1.30f, cashIncomeMult = 1.00f, tradePriceMult = 0.90f, bribeCostMult = 1.00f,
                escapeRouteCostMult = 1.00f, detectionRangeMult = 0.90f, shakedownStrictness = 0.75f,
                recommendedStayDaysMin = 3, recommendedStayDaysMax = 7,
                sentenceDays = 7,
            },
            new FacilityInfo
            {
                id = FacilityIds.StateMin, system = "State", securityLabel = "Minimum",
                title = "State — Minimum",
                description = "Work-farm fences and trusting counts. A gentle introduction to state time.",
                sceneName = "StateMin", ladderIndex = 1,
                lootAbundance = 1.15f, cashIncomeMult = 1.20f, tradePriceMult = 1.00f, bribeCostMult = 1.20f,
                escapeRouteCostMult = 1.20f, detectionRangeMult = 1.00f, shakedownStrictness = 0.90f,
                recommendedStayDaysMin = 5, recommendedStayDaysMax = 8,
            },
            new FacilityInfo
            {
                id = FacilityIds.StateMed, system = "State", securityLabel = "Medium",
                title = "State — Medium",
                description = "Double fences, real patrol patterns, and a yard economy that charges what it can.",
                sceneName = "StateMed", ladderIndex = 2,
                lootAbundance = 1.00f, cashIncomeMult = 1.35f, tradePriceMult = 1.15f, bribeCostMult = 1.45f,
                escapeRouteCostMult = 1.45f, detectionRangeMult = 1.10f, shakedownStrictness = 1.00f,
                recommendedStayDaysMin = 6, recommendedStayDaysMax = 10,
            },
            new FacilityInfo
            {
                id = FacilityIds.StateMax, system = "State", securityLabel = "Maximum",
                title = "State — Maximum",
                description = "The state's hard yard. Every route costs favors, and the guards re-check their work.",
                sceneName = "StateMax", ladderIndex = 3,
                lootAbundance = 0.90f, cashIncomeMult = 1.50f, tradePriceMult = 1.30f, bribeCostMult = 1.75f,
                escapeRouteCostMult = 1.75f, detectionRangeMult = 1.20f, shakedownStrictness = 1.15f,
                recommendedStayDaysMin = 8, recommendedStayDaysMax = 12,
            },
            new FacilityInfo
            {
                id = FacilityIds.FedCamp, system = "Federal", securityLabel = "Camp",
                title = "Federal Camp",
                description = "Federal time starts polite: low walls, high wages, and paperwork that follows you forever.",
                sceneName = "FedCamp", ladderIndex = 4,
                lootAbundance = 0.85f, cashIncomeMult = 1.70f, tradePriceMult = 1.45f, bribeCostMult = 2.10f,
                escapeRouteCostMult = 2.10f, detectionRangeMult = 1.25f, shakedownStrictness = 1.20f,
                recommendedStayDaysMin = 8, recommendedStayDaysMax = 12,
            },
            new FacilityInfo
            {
                id = FacilityIds.FedLow, system = "Federal", securityLabel = "Low",
                title = "Federal Low",
                description = "Cameras in the corners and prices to match. Arriving broke here starts to hurt.",
                sceneName = "FedLow", ladderIndex = 5,
                lootAbundance = 0.75f, cashIncomeMult = 1.90f, tradePriceMult = 1.65f, bribeCostMult = 2.50f,
                escapeRouteCostMult = 2.55f, detectionRangeMult = 1.30f, shakedownStrictness = 1.30f,
                recommendedStayDaysMin = 10, recommendedStayDaysMax = 14,
            },
            new FacilityInfo
            {
                id = FacilityIds.FedMed, system = "Federal", securityLabel = "Medium",
                title = "Federal Medium",
                description = "Fixers here don't work for strangers. Bring cash or a reputation.",
                sceneName = "FedMed", ladderIndex = 6,
                lootAbundance = 0.65f, cashIncomeMult = 2.15f, tradePriceMult = 1.90f, bribeCostMult = 3.00f,
                escapeRouteCostMult = 3.10f, detectionRangeMult = 1.35f, shakedownStrictness = 1.40f,
                recommendedStayDaysMin = 12, recommendedStayDaysMax = 16,
                gateMode = TransferGateMode.Any, gateCash = 2500, gateRespect = 40f,
            },
            new FacilityInfo
            {
                id = FacilityIds.FedHigh, system = "Federal", securityLabel = "High",
                title = "Federal High",
                description = "A penitentiary that assumes you will try. Routes exist — for people who can pay.",
                sceneName = "FedHigh", ladderIndex = 7,
                lootAbundance = 0.55f, cashIncomeMult = 2.40f, tradePriceMult = 2.20f, bribeCostMult = 3.60f,
                escapeRouteCostMult = 3.80f, detectionRangeMult = 1.45f, shakedownStrictness = 1.55f,
                recommendedStayDaysMin = 14, recommendedStayDaysMax = 18,
                gateMode = TransferGateMode.Any, gateCash = 5000, gateRespect = 60f,
            },
            new FacilityInfo
            {
                id = FacilityIds.FedAdx, system = "Federal", securityLabel = "ADX",
                title = "Federal Administrative Max",
                description = "The last box. Nobody has ever left it early — which is exactly why you're going to.",
                sceneName = "FedAdx", ladderIndex = 8,
                lootAbundance = 0.45f, cashIncomeMult = 2.70f, tradePriceMult = 2.60f, bribeCostMult = 4.50f,
                escapeRouteCostMult = 4.60f, detectionRangeMult = 1.55f, shakedownStrictness = 1.75f,
                recommendedStayDaysMin = 16, recommendedStayDaysMax = 22,
                gateMode = TransferGateMode.All, gateCash = 10000, gateRespect = 75f,
            },
        };

        private static Dictionary<string, FacilityInfo> _byId;

        public static FacilityInfo Get(string facilityId)
        {
            if (_byId == null)
            {
                _byId = new Dictionary<string, FacilityInfo>(All.Length);
                foreach (var info in All)
                    _byId[info.id] = info;
            }
            return facilityId != null && _byId.TryGetValue(facilityId, out var f) ? f : null;
        }
    }
}
