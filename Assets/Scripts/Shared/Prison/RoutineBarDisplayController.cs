using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Prison
{
    /// <summary>
    /// CanvasGroup visibility + transition juice for <see cref="RoutineNowNextBarUI"/>.
    /// Attach to the same GameObject as the routine bar root.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public class RoutineBarDisplayController : MonoBehaviour
    {
        [Header("References")]
        public RoutineNowNextBarUI routineBar;

        [Header("Adaptive visibility (CanvasGroup)")]
        [Range(0.1f, 1f)] public float compliantDimAlpha = 0.4f;
        public float fullAlpha = 1f;
        [Min(1f)] public float fadeInSpeed = 14f;
        [Min(1f)] public float fadeOutSpeed = 5f;
        [Range(0.5f, 0.95f)] public float compliantMinTimeRemaining = 0.5f;
        [Range(0.05f, 0.5f)] public float pressureMaxTimeRemaining = 0.25f;

        [Header("Travel grace juice")]
        [Min(0.01f)] public float graceFlashDuration = 0.1f;
        [Min(0.01f)] public float gracePunchDuration = 0.2f;
        public float gracePunchScale = 1.05f;
        public Color graceFlashColor = Color.white;

        [Header("Enforcement juice")]
        public TMP_Text statusText;
        [Min(0.01f)] public float enforcementShakeDuration = 0.35f;
        public float enforcementShakePixels = 6f;
        [Min(0.01f)] public float enforcementPulseHz = 2.2f;
        [Range(0.35f, 1f)] public float enforcementPulseMinAlpha = 0.55f;

        private CanvasGroup _canvasGroup;
        private RectTransform _rootRect;
        private Vector3 _baseScale = Vector3.one;
        private RoutineNowNextBarUI.RoutineBarVisualState _prevState;
        private Coroutine _juiceRoutine;
        private Coroutine _enforcementRoutine;
        private Vector2 _statusBaseAnchoredPos;
        private bool _statusPosCached;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            _rootRect = transform as RectTransform;
            _baseScale = _rootRect != null ? _rootRect.localScale : Vector3.one;

            if (routineBar == null)
                routineBar = GetComponent<RoutineNowNextBarUI>();

            if (statusText == null && routineBar != null)
                statusText = routineBar.statusText;
        }

        private void LateUpdate()
        {
            if (routineBar == null || PrisonTimeManager.Instance == null)
                return;

            UpdateCanvasGroupAlpha(PrisonTimeManager.Instance);
            DetectStateJuice();
        }

        private void UpdateCanvasGroupAlpha(PrisonTimeManager tm)
        {
            float target = GetTargetAlpha(tm);
            float speed = target < _canvasGroup.alpha ? fadeOutSpeed : fadeInSpeed;
            _canvasGroup.alpha = Mathf.MoveTowards(_canvasGroup.alpha, target, speed * Time.unscaledDeltaTime);
        }

        private float GetTargetAlpha(PrisonTimeManager tm)
        {
            if (UIMenuFocus.IsAnyMenuOpen)
                return 0f;

            if (routineBar == null)
                return fullAlpha;

            var state = routineBar.CurrentVisualState;
            if (state == RoutineNowNextBarUI.RoutineBarVisualState.TravelGrace
                || state == RoutineNowNextBarUI.RoutineBarVisualState.Enforcement
                || state == RoutineNowNextBarUI.RoutineBarVisualState.MandatoryWarning)
                return fullAlpha;

            if (tm.IsMorningRollCallShakedownGateActive)
                return fullAlpha;

            var prisoner = FindFirstObjectByType<PrisonerController>(FindObjectsInactive.Exclude);
            if (prisoner != null && !prisoner.IsCompliant)
                return fullAlpha;

            float timeFrac = tm.IsMandatoryTravelGraceActive
                ? tm.MandatoryTravelGraceRemaining01
                : tm.CurrentPhaseTimeRemainingFraction;

            if (timeFrac <= pressureMaxTimeRemaining)
                return fullAlpha;

            if (prisoner != null && prisoner.IsCompliant && timeFrac > compliantMinTimeRemaining
                && state == RoutineNowNextBarUI.RoutineBarVisualState.Chill)
                return compliantDimAlpha;

            return fullAlpha;
        }

        private void DetectStateJuice()
        {
            var state = routineBar.CurrentVisualState;
            if (state == _prevState)
                return;

            if (state == RoutineNowNextBarUI.RoutineBarVisualState.TravelGrace
                && _prevState != RoutineNowNextBarUI.RoutineBarVisualState.TravelGrace)
            {
                if (_juiceRoutine != null)
                    StopCoroutine(_juiceRoutine);
                _juiceRoutine = StartCoroutine(PlayGraceJuice());
            }

            if (state == RoutineNowNextBarUI.RoutineBarVisualState.Enforcement
                && _prevState != RoutineNowNextBarUI.RoutineBarVisualState.Enforcement)
            {
                if (_enforcementRoutine != null)
                    StopCoroutine(_enforcementRoutine);
                _statusPosCached = false;
                _enforcementRoutine = StartCoroutine(PlayEnforcementJuice());
            }
            else if (state != RoutineNowNextBarUI.RoutineBarVisualState.Enforcement && _enforcementRoutine != null)
            {
                StopCoroutine(_enforcementRoutine);
                _enforcementRoutine = null;
                ResetStatusTransform();
            }

            _prevState = state;
        }

        private IEnumerator PlayGraceJuice()
        {
            Image fill = routineBar != null ? routineBar.phaseProgressFill : null;
            Color settle = routineBar != null ? routineBar.travelGraceFillColor : PrisonUITheme.CautionYellow;
            Color original = fill != null ? fill.color : settle;

            if (fill != null)
                fill.color = graceFlashColor;

            yield return new WaitForSecondsRealtime(graceFlashDuration);

            if (fill != null)
                fill.color = settle;

            if (_rootRect != null)
            {
                float elapsed = 0f;
                while (elapsed < gracePunchDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = gracePunchDuration > 0.001f ? Mathf.Clamp01(elapsed / gracePunchDuration) : 1f;
                    float punch = Mathf.Sin(t * Mathf.PI);
                    _rootRect.localScale = _baseScale * Mathf.Lerp(1f, gracePunchScale, punch);
                    yield return null;
                }
                _rootRect.localScale = _baseScale;
            }

            _juiceRoutine = null;
        }

        private IEnumerator PlayEnforcementJuice()
        {
            if (statusText == null && routineBar != null)
                statusText = routineBar.statusText;

            if (statusText == null)
                yield break;

            yield return null;
            CacheStatusPosition();
            float elapsed = 0f;
            Color baseColor = routineBar != null ? routineBar.enforcementStatusColor : PrisonUITheme.HazardRed;

            while (routineBar != null && routineBar.CurrentVisualState == RoutineNowNextBarUI.RoutineBarVisualState.Enforcement)
            {
                elapsed += Time.unscaledDeltaTime;
                float shakeT = enforcementShakeDuration > 0.001f
                    ? 1f - Mathf.Clamp01(elapsed / enforcementShakeDuration)
                    : 0f;
                float offsetX = Mathf.Sin(elapsed * 42f) * enforcementShakePixels * shakeT;
                statusText.rectTransform.anchoredPosition = _statusBaseAnchoredPos + new Vector2(offsetX, 0f);

                float pulse = (Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f * enforcementPulseHz) * 0.5f) + 0.5f;
                float a = Mathf.Lerp(enforcementPulseMinAlpha, 1f, pulse);
                var c = baseColor;
                c.a = a;
                statusText.color = c;

                yield return null;
            }

            ResetStatusTransform();
            _enforcementRoutine = null;
        }

        private void CacheStatusPosition()
        {
            if (statusText == null)
                return;
            _statusBaseAnchoredPos = statusText.rectTransform.anchoredPosition;
            _statusPosCached = true;
        }

        private void ResetStatusTransform()
        {
            if (statusText == null)
                return;
            statusText.rectTransform.anchoredPosition = _statusBaseAnchoredPos;
        }
    }
}
