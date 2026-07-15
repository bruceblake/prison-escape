using UnityEngine;
using UnityEngine.UI;

namespace Prison
{
    /// <summary>“The Heat” stealth eye: escort = red, danger band = yellow, hidden = fully transparent.</summary>
    public class PrisonHeatUI : MonoBehaviour
    {
        [Header("Eyes (assign one graphic per state; others disabled when not used)")]
        public Graphic eyeClosed;
        public Graphic eyeHalf;
        public Graphic eyeOpen;
        [Tooltip("If true, the numeric state eases in over ~0.2s (less snap).")]
        public bool useSmoothing;
        [Min(0.01f)] public float smoothSpeed = 6f;

        [Header("Optional CanvasGroup for whole control")]
        public CanvasGroup rootGroup;

        private PrisonerController _prisoner;
        private int _state; // 0 hidden 1 half 2 open
        private float _smoothedState;

        private void Start()
        {
            _prisoner = FindFirstObjectByType<PrisonerController>(FindObjectsInactive.Exclude);
        }

        private void Update()
        {
            if (_prisoner == null) _prisoner = FindFirstObjectByType<PrisonerController>(FindObjectsInactive.Exclude);
            if (_prisoner == null) { SetViewInstant(0); return; }

            if (_prisoner.MovementBlocked)
                _state = 2; // escort / caught
            else
            {
                bool attention = IsAnyGuardAttentive();
                var tm = PrisonTimeManager.Instance;
                bool mandatoryEnforcement = tm != null
                    && PrisonEventRules.IsMandatory(tm.CurrentEvent)
                    && !tm.IsMandatoryTravelGraceActive
                    && !_prisoner.IsCompliant;
                if (mandatoryEnforcement)
                    _state = 2; // lockdown — eye open
                else if (attention || (tm != null && tm.IsMandatoryTravelGraceActive))
                    _state = 1;
                else
                    _state = 0;
            }

            if (PrisonSuspicion.IsSuspicionActive && _state < 1)
                _state = 1; // suspicion floor — half eye minimum

            if (useSmoothing)
            {
                _smoothedState = Mathf.MoveTowards(_smoothedState, _state, Time.deltaTime * smoothSpeed);
                int shown = Mathf.Clamp(Mathf.RoundToInt(_smoothedState + 0.15f), 0, 2);
                if (Mathf.Approximately(_smoothedState, _state) && _state < shown) shown = _state;
                SetViewInstant(shown);
            }
            else
            {
                _smoothedState = _state;
                SetViewInstant(_state);
            }

            if (rootGroup != null)
            {
                if (UIMenuFocus.IsAnyMenuOpen)
                {
                    rootGroup.alpha = Mathf.MoveTowards(rootGroup.alpha, 0f, Time.unscaledDeltaTime * 10f);
                }
                else
                {
                    bool hidden = _state == 0 && !PrisonSuspicion.IsSuspicionActive;
                    if (useSmoothing)
                        hidden = _smoothedState < 0.04f && !PrisonSuspicion.IsSuspicionActive;
                    rootGroup.alpha = hidden ? 0f : 1f;
                }
            }
        }

        private bool IsAnyGuardAttentive()
        {
            var tr = _prisoner.transform;
            var guards = FindObjectsByType<GuardDetection>(FindObjectsSortMode.None);
            Vector3 p = tr.position;
            for (int i = 0; i < guards.Length; i++)
            {
                if (guards[i] != null && guards[i].IsPositionInAttentionZone(p))
                    return true;
            }
            return false;
        }

        private void SetViewInstant(int s)
        {
            if (eyeClosed != null) eyeClosed.gameObject.SetActive(s == 0);
            if (eyeHalf != null) eyeHalf.gameObject.SetActive(s == 1);
            if (eyeOpen != null) eyeOpen.gameObject.SetActive(s == 2);
        }
    }
}
