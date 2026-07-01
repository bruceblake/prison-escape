using System;
using UnityEngine;

namespace Prison
{
    public class PrisonTimeManager : MonoBehaviour
    {
        private static PrisonTimeManager _instance;

        /// <summary>Singleton accessor. Duplicate instances destroy themselves on <see cref="Awake"/>.</summary>
        public static PrisonTimeManager Instance
        {
            get => _instance;
            private set
            {
                if (_instance != null && _instance != value)
                {
                    Destroy(value.gameObject);
                    return;
                }
                _instance = value;
            }
        }

        [Header("Schedule")]
        public PrisonSchedule schedule;

        [Header("Debug")]
        [Tooltip("Log schedule phase changes and OnEventChanged.")]
        public bool debugLogScheduleEvents;

        [Header("Compliance grace")]
        [Tooltip("After each phase change (and on session start), prisoners count as compliant for this many real-time seconds so they can travel to the new area without being arrested mid-route.")]
        public float complianceGraceRealSeconds = 50f;

        [Header("Morning roll call")]
        [Tooltip("Ends morning roll call when every inmate's cell has been shakedown (not after a fixed duration).")]
        public bool endMorningRollCallWhenAllAccounted = true;
        [Tooltip("Safety cap (real seconds). 0 = no cap.")]
        public float morningRollCallMaxRealSeconds = 600f;

        [Header("Runtime (Read Only)")]
        [SerializeField] private float currentTimeMinutes;
        [SerializeField] private PrisonEventType currentEvent;
        [SerializeField] private float eventTimeRemaining;

        /// <summary>The schedule phase currently in effect (e.g. Breakfast, FreeTime, LightsOut).</summary>
        public PrisonEventType CurrentEvent => currentEvent;
        /// <summary>Cosmetic wall-clock time in minutes since midnight (0-1440); wraps daily, decoupled from phase progression.</summary>
        public float CurrentTimeMinutes => currentTimeMinutes;
        /// <summary>Game-minutes remaining in the current schedule phase.</summary>
        public float EventTimeRemaining => eventTimeRemaining;
        /// <summary>Index of the current entry within <see cref="PrisonSchedule.entries"/>.</summary>
        public int CurrentEntryIndex => currentEntryIndex;

        /// <summary>Total game-minutes of all schedule entries (for routine-bar progress).</summary>
        public float TotalScheduleDurationMinutes
        {
            get
            {
                if (schedule == null || schedule.entries == null) return 0f;
                float t = 0f;
                for (int i = 0; i < schedule.entries.Length; i++)
                    t += schedule.entries[i].durationMinutes;
                return t;
            }
        }

        /// <summary>Duration of the current schedule entry (game minutes).</summary>
        public float CurrentEntryDurationMinutes
        {
            get
            {
                if (schedule == null || schedule.entries == null) return 0f;
                if (currentEntryIndex < 0 || currentEntryIndex >= schedule.entries.Length) return 0f;
                return schedule.entries[currentEntryIndex].durationMinutes;
            }
        }

        /// <summary>How far we are into the current phase (0 .. CurrentEntryDurationMinutes).</summary>
        public float ElapsedInCurrentEntryMinutes => Mathf.Max(0f, CurrentEntryDurationMinutes - eventTimeRemaining);

        /// <summary>0–1 progress through the repeating schedule (not wall-clock 24h).</summary>
        public float ScheduleProgress01
        {
            get
            {
                float total = TotalScheduleDurationMinutes;
                if (total < 0.0001f) return 0f;
                float acc = 0f;
                if (schedule != null && schedule.entries != null)
                {
                    for (int i = 0; i < currentEntryIndex && i < schedule.entries.Length; i++)
                        acc += schedule.entries[i].durationMinutes;
                }
                acc += ElapsedInCurrentEntryMinutes;
                return Mathf.Clamp01(acc / total);
            }
        }

        /// <summary>Fraction of the <em>current</em> phase that is still left (0 = at next transition, 1 = just started).</summary>
        public float CurrentPhaseTimeRemainingFraction
        {
            get
            {
                float d = CurrentEntryDurationMinutes;
                if (d < 0.0001f) return 0f;
                return Mathf.Clamp01(eventTimeRemaining / d);
            }
        }

        /// <summary>True when the remaining time in this phase is at or below a fraction of the full phase (e.g. 0.1 = last 10%).</summary>
        public bool IsInLastPortionOfCurrentPhase(float remainingFraction = 0.1f) =>
            CurrentPhaseTimeRemainingFraction <= remainingFraction;

        /// <summary>
        /// Fired whenever the schedule advances to a new phase, including the very first phase on session start.
        /// Subscribers (guard/prisoner AI, routine HUD) should use this to learn the initial phase rather than
        /// assuming a default.
        /// </summary>
        public event Action<PrisonEventType> OnEventChanged;

        private int currentEntryIndex;
        private float _complianceGraceEndUnscaled = float.NegativeInfinity;
        private float _morningRollCallStartedUnscaled;
        private bool _scheduleInitialized;

        /// <summary>True while the post-phase-change travel grace is active (guards should not treat prisoners as out-of-position).</summary>
        public bool IsComplianceGraceActive =>
            complianceGraceRealSeconds > 0.01f && Time.unscaledTime < _complianceGraceEndUnscaled;

        /// <summary>Travel grace only runs when entering a mandatory phase (not when rolling into yard time, etc.).</summary>
        public bool IsMandatoryTravelGraceActive =>
            IsComplianceGraceActive && PrisonEventRules.IsMandatory(currentEvent);

        /// <summary>Still in a flexible phase, but the upcoming block is mandatory — show a low-stakes warning on "Next".</summary>
        public bool IsHighStakesTransitionWarningActive
        {
            get
            {
                GetNextEventInfo(out PrisonEventType next, out _);
                return PrisonEventRules.IsHighStakesUpcoming(currentEvent, next);
            }
        }

        /// <summary>Real-time seconds left in the travel grace window.</summary>
        public float ComplianceGraceSecondsRemaining =>
            Mathf.Max(0f, _complianceGraceEndUnscaled - Time.unscaledTime);

        /// <summary>0–1 fraction of mandatory travel grace remaining (for progress bars).</summary>
        public float MandatoryTravelGraceRemaining01
        {
            get
            {
                if (!IsMandatoryTravelGraceActive || complianceGraceRealSeconds < 0.01f)
                    return 0f;
                return Mathf.Clamp01(ComplianceGraceSecondsRemaining / complianceGraceRealSeconds);
            }
        }

        /// <summary>Approximate real-time seconds until the current schedule phase ends and the next begins.</summary>
        public float SecondsRemainingInCurrentPhaseRealTime
        {
            get
            {
                if (schedule == null || schedule.entries == null || schedule.entries.Length == 0) return 0f;
                float mps = schedule.minutesPerRealSecond;
                if (mps < 0.00001f) return 0f;
                return eventTimeRemaining / mps;
            }
        }

        public bool IsMorningRollCallShakedownGateActive =>
            endMorningRollCallWhenAllAccounted && PrisonEventExtensions.IsMorningLineUp(currentEvent);

        /// <summary>Alias for UI — morning roll call waits on per-inmate shakedown.</summary>
        public bool IsMorningRollCallHeadcountActive => IsMorningRollCallShakedownGateActive;

        /// <summary>Looks up the schedule entry that follows the current one without advancing the schedule.</summary>
        /// <param name="nextEvent">The upcoming phase, or the current phase if no schedule is assigned.</param>
        /// <param name="timeUntilNext">Game-minutes remaining until that phase begins.</param>
        public void GetNextEventInfo(out PrisonEventType nextEvent, out float timeUntilNext)
        {
            nextEvent = currentEvent;
            timeUntilNext = eventTimeRemaining;
            if (schedule == null || schedule.entries == null || schedule.entries.Length == 0) return;
            int nextIdx = (currentEntryIndex + 1) % schedule.entries.Length;
            nextEvent = schedule.entries[nextIdx].eventType;
            timeUntilNext = eventTimeRemaining;
        }

        private void StartComplianceGraceWindow()
        {
            if (complianceGraceRealSeconds <= 0.01f)
            {
                _complianceGraceEndUnscaled = float.NegativeInfinity;
                return;
            }
            _complianceGraceEndUnscaled = Time.unscaledTime + complianceGraceRealSeconds;
        }

        private void StartComplianceGraceForEvent(PrisonEventType enteringEvent, bool isInitialScheduleSetup)
        {
            if (isInitialScheduleSetup
                || !PrisonEventRules.IsMandatory(enteringEvent)
                || PrisonEventExtensions.IsMorningLineUp(enteringEvent))
            {
                _complianceGraceEndUnscaled = float.NegativeInfinity;
                return;
            }
            StartComplianceGraceWindow();
        }

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            MorningRollCallTracker.EnsureInstance();

            if (schedule == null || schedule.entries == null || schedule.entries.Length == 0)
            {
                Debug.LogWarning("[PrisonTimeManager] No schedule assigned or empty schedule.");
                return;
            }

            currentTimeMinutes = schedule.entries[0].startTimeMinutes;
            currentEntryIndex = 0;
            if (debugLogScheduleEvents)
                Debug.Log($"[PrisonTimeManager] Start: entryCount={schedule.entries.Length}, minutesPerRealSecond={schedule.minutesPerRealSecond}, startGameTime={currentTimeMinutes} min", this);
            SetCurrentEvent(schedule.entries[0].eventType, schedule.entries[0].durationMinutes);
        }

        private void Update()
        {
            if (schedule == null || schedule.entries == null || schedule.entries.Length == 0) return;

            float minutesPerSecond = schedule.minutesPerRealSecond;
            currentTimeMinutes += minutesPerSecond * Time.deltaTime;

            if (currentTimeMinutes >= 1440f)
                currentTimeMinutes -= 1440f;

            if (TryCompleteMorningRollCallAfterShakedown())
                return;

            eventTimeRemaining -= minutesPerSecond * Time.deltaTime;
            if (eventTimeRemaining <= 0f)
                AdvanceToNextEvent();
        }

        /// <summary>Called when the last inmate's cell has been shakedown — ends roll call immediately.</summary>
        public void AdvanceMorningRollCallWhenComplete()
        {
            if (!IsMorningRollCallShakedownGateActive)
                return;
            if (debugLogScheduleEvents)
                Debug.Log("[PrisonTimeManager] Morning roll call complete — all inmates shakedown.", this);
            AdvanceToNextEvent();
        }

        private bool TryCompleteMorningRollCallAfterShakedown()
        {
            if (!IsMorningRollCallShakedownGateActive)
                return false;

            MorningRollCallTracker.EnsureInstance();
            var tracker = MorningRollCallTracker.Instance;
            if (tracker != null && tracker.AreAllInmatesShakedownComplete())
            {
                AdvanceMorningRollCallWhenComplete();
                return true;
            }

            if (morningRollCallMaxRealSeconds > 0.01f
                && Time.unscaledTime - _morningRollCallStartedUnscaled >= morningRollCallMaxRealSeconds)
            {
                if (debugLogScheduleEvents)
                    Debug.LogWarning("[PrisonTimeManager] Morning roll call max time reached — advancing schedule.", this);
                AdvanceToNextEvent();
                return true;
            }

            return false;
        }

        private void AdvanceToNextEvent()
        {
            if (schedule == null || schedule.entries == null || schedule.entries.Length == 0)
                return;

            currentEntryIndex = (currentEntryIndex + 1) % schedule.entries.Length;
            var entry = schedule.entries[currentEntryIndex];
            if (debugLogScheduleEvents)
                Debug.Log($"[PrisonTimeManager] AdvanceToNextEvent -> entryIndex={currentEntryIndex} event={entry.eventType} duration={entry.durationMinutes}", this);
            SetCurrentEvent(entry.eventType, entry.durationMinutes);
        }

        private void SetCurrentEvent(PrisonEventType evt, float duration)
        {
            var previous = currentEvent;
            bool isInitialSetup = !_scheduleInitialized;
            bool willInvoke = currentEvent != evt;
            if (debugLogScheduleEvents)
                Debug.Log($"[PrisonTimeManager] SetCurrentEvent: entryIdx={currentEntryIndex} previous={previous} next={evt} duration={duration} min (game) willInvokeOnEventChanged={willInvoke} initialSetup={isInitialSetup}", this);

            if (PrisonEventExtensions.IsMorningLineUp(evt) && (willInvoke || isInitialSetup))
            {
                _morningRollCallStartedUnscaled = Time.unscaledTime;
                MorningRollCallTracker.EnsureInstance();
                MorningRollCallTracker.Instance?.BeginPhase();
            }

            if (willInvoke || isInitialSetup)
            {
                currentEvent = evt;
                StartComplianceGraceForEvent(evt, isInitialSetup);

                // Always notify on the very first SetCurrentEvent, even if evt matches the
                // PrisonEventType default (RollCall = 0): otherwise schedules whose first entry
                // is RollCall never announce the starting phase, and subscribers (guard/prisoner
                // AI, HUD) that rely on OnEventChanged to learn the initial phase are never told.
                OnEventChanged?.Invoke(evt);
                if (debugLogScheduleEvents)
                    Debug.Log($"[PrisonTimeManager] OnEventChanged subscribers notified: {evt} (initialSetup={isInitialSetup})", this);
            }
            else if (debugLogScheduleEvents)
            {
                Debug.Log($"[PrisonTimeManager] Event type unchanged ({evt}); OnEventChanged NOT fired.", this);
            }

            _scheduleInitialized = true;
            eventTimeRemaining = duration;
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}
