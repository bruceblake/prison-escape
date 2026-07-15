using UnityEngine;

namespace Prison
{
    /// <summary>
    /// Watches the midday/evening formal counts (presence-only, no shakedown sweep).
    /// When a cell-count phase ends with an inmate unaccounted for, raises a facility-wide
    /// Lockdown alert — "if the numbers don't match, everything stops."
    /// Morning roll call (shakedown gate) and the night bed check have their own mechanisms.
    /// </summary>
    public class FormalCountMonitor : MonoBehaviour
    {
        public static FormalCountMonitor Instance { get; private set; }

        [Header("Debug")]
        [SerializeField] private bool debugLog;

        private PrisonEventType _previousEvent;
        private bool _hasPreviousEvent;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void OnEnable()
        {
            if (PrisonTimeManager.Instance != null)
            {
                PrisonTimeManager.Instance.OnEventChanged += OnScheduleEventChanged;
                _previousEvent = PrisonTimeManager.Instance.CurrentEvent;
                _hasPreviousEvent = true;
            }
        }

        private void OnDisable()
        {
            if (PrisonTimeManager.Instance != null)
                PrisonTimeManager.Instance.OnEventChanged -= OnScheduleEventChanged;
        }

        private void OnScheduleEventChanged(PrisonEventType evt)
        {
            if (_hasPreviousEvent && _previousEvent != evt)
                EvaluateCountOnPhaseEnd(_previousEvent);

            _previousEvent = evt;
            _hasPreviousEvent = true;
        }

        private void EvaluateCountOnPhaseEnd(PrisonEventType endedPhase)
        {
            if (!PrisonEventExtensions.IsCellCountPhase(endedPhase))
                return;

            int accounted = PrisonerPresence.CountAccountedFor(out int total);
            if (ShouldRaiseLockdown(endedPhase, accounted, total))
            {
                string reason = BuildLockdownReason(endedPhase, accounted, total);
                if (debugLog)
                    Debug.Log($"[FormalCountMonitor] {reason}", this);
                PrisonSecurityAlerts.RaiseLockdown(reason);
            }
            else if (debugLog)
            {
                Debug.Log($"[FormalCountMonitor] {PrisonRoutineLabels.FormatPhaseTitle(endedPhase, false)} cleared: {accounted}/{total} accounted for.", this);
            }
        }

        /// <summary>Pure count rule: a completed cell count with anyone missing triggers lockdown. Empty facility never locks down.</summary>
        public static bool ShouldRaiseLockdown(PrisonEventType endedPhase, int accountedFor, int total)
        {
            return PrisonEventExtensions.IsCellCountPhase(endedPhase) && total > 0 && accountedFor < total;
        }

        public static string BuildLockdownReason(PrisonEventType endedPhase, int accountedFor, int total)
        {
            return $"Count mismatch — {PrisonRoutineLabels.FormatPhaseTitle(endedPhase, false)}: {accountedFor}/{total} accounted for";
        }

        public static void EnsureInstance()
        {
            if (Instance != null) return;
            if (PrisonTimeManager.Instance == null) return;
            var go = PrisonTimeManager.Instance.gameObject;
            if (go.GetComponent<FormalCountMonitor>() == null)
                go.AddComponent<FormalCountMonitor>();
        }
    }
}
