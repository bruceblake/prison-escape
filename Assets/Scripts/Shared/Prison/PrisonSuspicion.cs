using UnityEngine;

namespace Prison
{
    /// <summary>
    /// Global suspicion after a caught escape attempt: guards see farther for a few days.
    /// Day boundaries are Morning Roll Calls (see <see cref="SuspicionWindow"/>).
    /// </summary>
    public class PrisonSuspicion : MonoBehaviour
    {
        private static PrisonSuspicion _instance;
        public static PrisonSuspicion Instance => _instance;

        [Header("Tuning")]
        [Tooltip("In-game days (Morning Roll Calls) suspicion lasts after being caught escaping.")]
        [SerializeField] private int suspicionDays = 2;
        [Tooltip("Guard detection range / proximity multiplier while suspicion is active (10 m -> 14 m at 1.4).")]
        [SerializeField] private float detectionRangeMultiplier = 1.4f;

        [Header("Runtime (Read Only)")]
        [SerializeField] private SuspicionWindow window;

        public bool IsActive => window.IsActive;
        public int RemainingDays => window.RemainingDays;

        /// <summary>Multiplier guards apply to detection geometry against the player (1 when calm).</summary>
        public static float GlobalDetectionRangeMultiplier =>
            _instance != null && _instance.window.IsActive ? _instance.detectionRangeMultiplier : 1f;

        public static bool IsSuspicionActive => _instance != null && _instance.window.IsActive;

        public static PrisonSuspicion EnsureInstance()
        {
            if (_instance != null) return _instance;
            var go = new GameObject("PrisonSuspicion");
            return go.AddComponent<PrisonSuspicion>();
        }

        public void RaiseSuspicion()
        {
            window.Raise(suspicionDays);
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
            if (PrisonEventExtensions.IsMorningLineUp(evt))
                window.OnMorningRollCall();
        }
    }
}
