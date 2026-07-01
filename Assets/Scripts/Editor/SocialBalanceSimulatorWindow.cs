using Prison;
using UnityEditor;
using UnityEngine;

public class SocialBalanceSimulatorWindow : EditorWindow
{
    private float currentAffinity = 0f;
    private float affinityGainMultiplier = 1f;
    private float giftBaseAmount = 5f;
    private bool isFavoredGift;
    private SocialActionType actionType = SocialActionType.Greeting;

    private float otherPrisonersAverage = 0f;
    private int totalPrisoners = 8;

    private float tier1Threshold = 25f;
    private float tier2Threshold = 50f;
    private float tier3Threshold = 75f;

    [MenuItem("Tools/Prison/Social Balance Simulator")]
    public static void OpenWindow()
    {
        GetWindow<SocialBalanceSimulatorWindow>("Social Simulator");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("NPC Affinity Simulation", EditorStyles.boldLabel);
        currentAffinity = EditorGUILayout.Slider("Current Affinity", currentAffinity, -100f, 100f);
        affinityGainMultiplier = EditorGUILayout.FloatField("Gain Multiplier", affinityGainMultiplier);
        actionType = (SocialActionType)EditorGUILayout.EnumPopup("Action", actionType);

        if (actionType == SocialActionType.Gift)
        {
            giftBaseAmount = EditorGUILayout.FloatField("Gift Base Amount", giftBaseAmount);
            isFavoredGift = EditorGUILayout.Toggle("Favored Item", isFavoredGift);
        }

        float actionBase = GetActionBaseForPreview(actionType);
        float effectiveDelta = SocialMath.ComputeEffectiveDelta(currentAffinity, actionBase, affinityGainMultiplier);
        float nextAffinity = Mathf.Clamp(currentAffinity + effectiveDelta, SocialMath.MinAffinity, SocialMath.MaxAffinity);

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Action Outcome", EditorStyles.boldLabel);
        EditorGUILayout.FloatField("Base Delta", actionBase);
        EditorGUILayout.FloatField("Effective Delta", effectiveDelta);
        EditorGUILayout.FloatField("Next Affinity", nextAffinity);

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Reputation Preview", EditorStyles.boldLabel);
        totalPrisoners = EditorGUILayout.IntSlider("Total Prisoners", totalPrisoners, 1, 32);
        otherPrisonersAverage = EditorGUILayout.Slider("Other Prisoners Avg", otherPrisonersAverage, -100f, 100f);

        tier1Threshold = EditorGUILayout.FloatField("Associate Threshold", tier1Threshold);
        tier2Threshold = EditorGUILayout.FloatField("Respected Threshold", tier2Threshold);
        tier3Threshold = EditorGUILayout.FloatField("Kingpin Threshold", tier3Threshold);

        float combinedAverage = GetCombinedAverage(nextAffinity, otherPrisonersAverage, totalPrisoners);
        ReputationTier tier = SocialMath.GetReputationTier(combinedAverage, tier1Threshold, tier2Threshold, tier3Threshold);

        EditorGUILayout.FloatField("Projected Global Average", combinedAverage);
        EditorGUILayout.LabelField("Projected Tier", tier.ToString());

        EditorGUILayout.Space(8f);
        if (GUILayout.Button("Apply Next Affinity As Current"))
        {
            currentAffinity = nextAffinity;
        }
    }

    private float GetActionBaseForPreview(SocialActionType type)
    {
        if (type == SocialActionType.Gift)
        {
            return isFavoredGift ? giftBaseAmount * SocialMath.FavoredGiftMultiplier : giftBaseAmount;
        }

        return SocialMath.GetBaseAffinityDelta(type, null, null, giftBaseAmount);
    }

    private static float GetCombinedAverage(float focusedNpcAffinity, float othersAverage, int totalNpcCount)
    {
        if (totalNpcCount <= 1)
        {
            return focusedNpcAffinity;
        }

        float total = focusedNpcAffinity + (othersAverage * (totalNpcCount - 1));
        return total / totalNpcCount;
    }
}
