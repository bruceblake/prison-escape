using UnityEngine;

namespace Prison.Social
{
    /// <summary>
    /// The one light cash job (spec §8, locked decision): working the Workshop during the
    /// Work Program phase pays a stipend at phase end — enough for bribes and trade without
    /// a full job career sim. Pays proportionally to time spent in the zone.
    /// </summary>
    public class PrisonJobPaymaster : MonoBehaviour
    {
        [Tooltip("Full-phase stipend for working the whole Work Program block.")]
        public float fullStipend = 18f;
        [Tooltip("Fraction of the phase you must be in the Workshop before any pay.")]
        [Range(0f, 1f)] public float minimumFraction = 0.25f;

        private float _secondsWorked;
        private float _phaseSeconds;
        private bool _inWorkPhase;
        private PrisonerController _player;

        private void OnEnable()
        {
            if (Prison.PrisonTimeManager.Instance != null)
            {
                Prison.PrisonTimeManager.Instance.OnEventChanged += OnPhaseChanged;
                _inWorkPhase = Prison.PrisonTimeManager.Instance.CurrentEvent == PrisonEventType.WorkProgram;
            }
        }

        private void OnDisable()
        {
            if (Prison.PrisonTimeManager.Instance != null)
                Prison.PrisonTimeManager.Instance.OnEventChanged -= OnPhaseChanged;
        }

        private void Update()
        {
            if (!_inWorkPhase) return;
            _phaseSeconds += Time.deltaTime;

            if (_player == null)
                _player = FindAnyObjectByType<PrisonerController>();
            if (_player != null && _player.IsInZoneType(ZoneType.Workshop))
                _secondsWorked += Time.deltaTime;
        }

        private void OnPhaseChanged(PrisonEventType phase)
        {
            if (_inWorkPhase && phase != PrisonEventType.WorkProgram)
                PayOut();

            _inWorkPhase = phase == PrisonEventType.WorkProgram;
            if (_inWorkPhase)
            {
                _secondsWorked = 0f;
                _phaseSeconds = 0f;
            }
        }

        private void PayOut()
        {
            if (_phaseSeconds <= 1f) return;
            float fraction = Mathf.Clamp01(_secondsWorked / _phaseSeconds);
            if (fraction < minimumFraction) return;

            // Career ladder: harder prisons pay better (cashIncomeMult — work wages).
            float pay = Mathf.Round(fullStipend * fraction * Prison.Career.CareerSession.CashIncomeMult);
            if (pay <= 0f || Prison.PlayerWallet.Instance == null) return;
            Prison.PlayerWallet.Instance.Add(pay);
            SocialToastUI.Show($"Work stipend: ${pay:0}.");
        }
    }
}
