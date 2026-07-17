using Prison;
using Prison.Social;
using UnityEditor;
using UnityEngine;

namespace Prison.EditorTools
{
    /// <summary>
    /// Balance simulator for the v3 social math (rebuilt per the Social Ecosystem &amp; Gangs
    /// teardown table): preview the relationship delta pipeline (personality → gang factor
    /// → soft cap), Standing bands, trade prices, intimidation odds, and reputation tiers.
    /// Window: Tools → Prison → Social → Balance Simulator.
    /// </summary>
    public class SocialBalanceSimulatorWindow : EditorWindow
    {
        private float _currentTrust;
        private float _currentRespect;
        private SocialEventType _eventType = SocialEventType.Chat;
        private int _sociability = 50;
        private int _loyalty = 50;
        private float _gangFactor = 1f;

        private float _tradeBaseValue = 10f;
        private int _greed = 50;
        private float _tradeTrust;
        private bool _member;
        private bool _contraband;

        private float _intRespect;
        private float _strength = 100f;
        private int _nerve = 50;

        private float _avgStanding;
        private GangRank _rank = GangRank.Outsider;

        [MenuItem("Tools/Prison/Social/Balance Simulator")]
        public static void ShowWindow() =>
            GetWindow<SocialBalanceSimulatorWindow>("Social Balance");

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Relationship delta pipeline", EditorStyles.boldLabel);
            _currentTrust = EditorGUILayout.Slider("Current trust", _currentTrust, -100f, 100f);
            _currentRespect = EditorGUILayout.Slider("Current respect", _currentRespect, -100f, 100f);
            _eventType = (SocialEventType)EditorGUILayout.EnumPopup("Event", _eventType);
            _sociability = EditorGUILayout.IntSlider("Sociability", _sociability, 0, 100);
            _loyalty = EditorGUILayout.IntSlider("Loyalty", _loyalty, 0, 100);
            _gangFactor = EditorGUILayout.Slider("Gang factor", _gangFactor, 0f, 1f);

            SocialActs.GetBaseDeltas(_eventType, out float baseTrust, out float baseRespect);
            var traits = new PersonalityTraits(50, _loyalty, 50, _sociability, 50);
            bool betrayal = SocialActs.IsBetrayalClass(_eventType);

            float trustDelta = RelationshipMath.ComputeEffectiveDelta(_currentTrust, baseTrust, true, traits, betrayal, _gangFactor);
            float respectDelta = RelationshipMath.ComputeEffectiveDelta(_currentRespect, baseRespect, false, traits, betrayal, _gangFactor);
            float newTrust = RelationshipMath.Apply(_currentTrust, trustDelta);
            float newRespect = RelationshipMath.Apply(_currentRespect, respectDelta);
            float standing = RelationshipMath.Standing(newTrust, newRespect);

            EditorGUILayout.HelpBox(
                $"Base Δ: trust {baseTrust:+0.#;-0.#;0}, respect {baseRespect:+0.#;-0.#;0}\n" +
                $"Effective Δ: trust {trustDelta:+0.##;-0.##;0}, respect {respectDelta:+0.##;-0.##;0}\n" +
                $"Result: trust {newTrust:0.#}, respect {newRespect:0.#} → standing {standing:0.#} " +
                $"({RelationshipMath.GetBand(standing)})",
                MessageType.Info);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Trade price (spec §8)", EditorStyles.boldLabel);
            _tradeBaseValue = EditorGUILayout.FloatField("Base value", _tradeBaseValue);
            _greed = EditorGUILayout.IntSlider("Seller greed", _greed, 0, 100);
            _tradeTrust = EditorGUILayout.Slider("Their trust in you", _tradeTrust, -100f, 100f);
            _member = EditorGUILayout.Toggle("You are gang member", _member);
            _contraband = EditorGUILayout.Toggle("Contraband", _contraband);
            float buy = TradeMath.BuyPrice(_tradeBaseValue, _greed, _tradeTrust, _member, _contraband);
            float sell = TradeMath.SellPrice(_tradeBaseValue, _greed, _contraband);
            EditorGUILayout.HelpBox($"Buy: ${buy:0}   ·   They pay you: ${sell:0}", MessageType.Info);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Intimidation", EditorStyles.boldLabel);
            _intRespect = EditorGUILayout.Slider("Their respect for you", _intRespect, -100f, 100f);
            _strength = EditorGUILayout.Slider("Your strength", _strength, 0f, 200f);
            _nerve = EditorGUILayout.IntSlider("Their nerve", _nerve, 0, 100);
            EditorGUILayout.HelpBox(
                $"Success chance: {RelationshipMath.IntimidationChance(_intRespect, _strength, _nerve) * 100f:0}%",
                MessageType.Info);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Reputation tier", EditorStyles.boldLabel);
            _avgStanding = EditorGUILayout.Slider("Avg standing (known inmates)", _avgStanding, -100f, 100f);
            _rank = (GangRank)EditorGUILayout.EnumPopup("Gang rank", _rank);
            EditorGUILayout.HelpBox(
                $"Tier: {RelationshipMath.ComputeTier(_avgStanding, _rank)} " +
                $"(bonus +{RelationshipMath.TierBonus(_rank):0})",
                MessageType.Info);
        }
    }
}
