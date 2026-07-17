using UnityEngine;

namespace Prison.Career
{
    /// <summary>
    /// Designer-tunable asset for one facility slot (10 assets: dev sandbox + ladder 0–8).
    /// Created/refreshed from <see cref="FacilityCatalog"/> by Tools ▸ Prison ▸ Career ▸
    /// Install Facility Definitions; numbers are design targets, the curve shapes are the contract.
    /// </summary>
    [CreateAssetMenu(fileName = "Facility", menuName = "Prison/Facility Definition")]
    public class FacilityDefinition : ScriptableObject
    {
        [Header("Identity (permanent save keys)")]
        public string id;
        public string system;
        public string securityLabel;
        [Tooltip("Placeholder display title — rename in the Prison Career Ladder note first.")]
        public string title;
        [TextArea] public string description;

        [Header("Presentation")]
        public Sprite icon;
        [Tooltip("Locked-slot art. Null = the select screen draws a plain black silhouette block.")]
        public Sprite silhouette;

        [Header("Scene")]
        [Tooltip("Scene loaded on ENTER. If missing from Build Settings the slot shows UNDER CONSTRUCTION even when unlocked.")]
        public string sceneName;

        [Header("Ladder")]
        [Tooltip("0–8 on the career ladder; -1 for the dev sandbox.")]
        public int ladderIndex = -1;

        [Header("Difficulty & pacing (multipliers on base systems)")]
        public float lootAbundance = 1f;
        public float cashIncomeMult = 1f;
        public float tradePriceMult = 1f;
        public float bribeCostMult = 1f;
        public float escapeRouteCostMult = 1f;
        public float detectionRangeMult = 1f;
        public float shakedownStrictness = 1f;
        public int recommendedStayDaysMin;
        public int recommendedStayDaysMax;

        [Header("Sentence clock (County only)")]
        [Tooltip("In-game days until auto-graduation without escaping. 0 = no sentence clock.")]
        public int sentenceDays;

        [Header("Soft transfer gate (on attempting the boundary route, never on entry)")]
        public TransferGateMode gateMode = TransferGateMode.None;
        public int gateCash;
        public float gateRespect;

        public bool IsDevSandbox => id == FacilityIds.DevSandbox;
        public bool HasSentenceClock => sentenceDays > 0;
        public bool HasScene => !string.IsNullOrEmpty(sceneName) && Application.CanStreamedLevelBeLoaded(sceneName);

        public string RecommendedStayHint =>
            recommendedStayDaysMax > 0
                ? $"Recommended stay: {recommendedStayDaysMin}–{recommendedStayDaysMax} days"
                : "";

        public void PopulateFrom(FacilityInfo info)
        {
            id = info.id;
            system = info.system;
            securityLabel = info.securityLabel;
            title = info.title;
            description = info.description;
            sceneName = info.sceneName;
            ladderIndex = info.ladderIndex;
            lootAbundance = info.lootAbundance;
            cashIncomeMult = info.cashIncomeMult;
            tradePriceMult = info.tradePriceMult;
            bribeCostMult = info.bribeCostMult;
            escapeRouteCostMult = info.escapeRouteCostMult;
            detectionRangeMult = info.detectionRangeMult;
            shakedownStrictness = info.shakedownStrictness;
            recommendedStayDaysMin = info.recommendedStayDaysMin;
            recommendedStayDaysMax = info.recommendedStayDaysMax;
            sentenceDays = info.sentenceDays;
            gateMode = info.gateMode;
            gateCash = info.gateCash;
            gateRespect = info.gateRespect;
        }
    }
}
