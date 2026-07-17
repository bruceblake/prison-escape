using System;
using UnityEngine;

namespace Prison
{
    /// <summary>
    /// The player's mental health, physical health, and strength (0–100). Solitary confinement lowers all three;
    /// all regenerate a little each day (applied at Morning Roll Call).
    /// Low strength slows sprinting (see <see cref="PlayerStatsMath"/>).
    /// </summary>
    public class PlayerStats : MonoBehaviour
    {
        private static PlayerStats _instance;
        public static PlayerStats Instance => _instance;

        [Header("Stats (0-100)")]
        [SerializeField] private float mentalHealth = PlayerStatsMath.MaxStat;
        [SerializeField] private float physicalHealth = PlayerStatsMath.MaxStat;
        [SerializeField] private float strength = PlayerStatsMath.MaxStat;

        public float MentalHealth => mentalHealth;
        public float PhysicalHealth => physicalHealth;
        public float Strength => strength;

        /// <summary>(mentalHealth, physicalHealth, strength) after any change.</summary>
        public event Action<float, float, float> StatsChanged;

        private PrisonEventType _lastRegenEvent = (PrisonEventType)(-1);

        /// <summary>Sprint multiplier honoring strength; safe default when no instance exists.</summary>
        public static float SprintMultiplierSafe =>
            _instance != null
                ? PlayerStatsMath.SprintMultiplierFor(_instance.strength)
                : PlayerStatsMath.NormalSprintMultiplier;

        public static PlayerStats EnsureInstance()
        {
            if (_instance != null) return _instance;
            var go = new GameObject("PlayerStats");
            return go.AddComponent<PlayerStats>();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }
            _instance = this;
        }

        private void Start()
        {
            if (PrisonTimeManager.Instance != null)
                PrisonTimeManager.Instance.OnEventChanged += OnScheduleEvent;
        }

        private void OnDestroy()
        {
            if (PrisonTimeManager.Instance != null)
                PrisonTimeManager.Instance.OnEventChanged -= OnScheduleEvent;
            if (_instance == this)
                _instance = null;
        }

        private void OnScheduleEvent(PrisonEventType evt)
        {
            if (!PrisonEventExtensions.IsMorningLineUp(evt))
            {
                _lastRegenEvent = (PrisonEventType)(-1);
                return;
            }
            if (_lastRegenEvent == evt) return;
            _lastRegenEvent = evt;

            mentalHealth = PlayerStatsMath.ApplyDailyRegen(mentalHealth);
            physicalHealth = PlayerStatsMath.ApplyDailyRegen(physicalHealth);
            strength = PlayerStatsMath.ApplyDailyRegen(strength);
            RaiseStatsChanged();
        }

        /// <summary>
        /// Career transfer/revisit carry: stat current values are global saveables and arrive
        /// with the player (Prison Career Ladder § Global vs local persistence).
        /// </summary>
        public void ApplyCareerCarry(float mental, float physical, float str)
        {
            mentalHealth = Mathf.Clamp(mental, 0f, PlayerStatsMath.MaxStat);
            physicalHealth = Mathf.Clamp(physical, 0f, PlayerStatsMath.MaxStat);
            strength = Mathf.Clamp(str, 0f, PlayerStatsMath.MaxStat);
            RaiseStatsChanged();
        }

        /// <summary>Applies the solitary-confinement penalty (-20 MH, -10 BODY, -10 STR).</summary>
        public void ApplySolitaryPenalty()
        {
            mentalHealth = PlayerStatsMath.ApplySolitaryToMentalHealth(mentalHealth);
            physicalHealth = PlayerStatsMath.ApplySolitaryToPhysicalHealth(physicalHealth);
            strength = PlayerStatsMath.ApplySolitaryToStrength(strength);
            RaiseStatsChanged();
        }

        private void RaiseStatsChanged() =>
            StatsChanged?.Invoke(mentalHealth, physicalHealth, strength);
    }
}
