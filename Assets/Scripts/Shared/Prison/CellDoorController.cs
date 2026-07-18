using UnityEngine;

namespace Prison
{
    /// <summary>
    /// Slides a barred cell door open/closed based on the prison schedule phase.
    /// Open during social phases (roll call, meals, free time); closed at night.
    /// The closed position is captured at runtime from the door's authored local position,
    /// and the open position is that plus <see cref="openOffset"/>.
    /// </summary>
    public class CellDoorController : MonoBehaviour
    {
        [Tooltip("The local position of the door when fully closed. Set by facility install / door fixer.")]
        public Vector3 closedLocalPosition;

        [Tooltip("Local-space offset added to the closed position to reach the fully OPEN position. " +
                 "For a barred cell door this should slide it sideways far enough to clear the doorway.")]
        public Vector3 openOffset = new Vector3(0f, 0f, 1.35f);

        [Tooltip("Slide responsiveness (lerp factor per second) between open and closed.")]
        public float slideSpeed = 3.0f;

        [SerializeField]
        [Tooltip("When true, Start keeps closedLocalPosition instead of re-capturing from the live transform " +
                 "(avoids treating a left-open door as the new closed pose).")]
        private bool hasAuthoredClosedPosition;

        private bool isInitialized;
        private bool forcedOpen;

        /// <summary>When true, this door stays open regardless of schedule (e.g. after morning shakedown).</summary>
        public bool IsForcedOpen => forcedOpen;

        /// <summary>True once the closed position has been captured.</summary>
        public bool IsInitialized => isInitialized;

        /// <summary>Local position the door rests at when closed.</summary>
        public Vector3 ClosedLocalPosition => closedLocalPosition;

        /// <summary>Local position the door rests at when fully open.</summary>
        public Vector3 OpenLocalPosition => closedLocalPosition + openOffset;

        private void Start()
        {
            // Never re-capture from a door that was left slid open in the scene — that made
            // Start treat the open pose as closed, then slide further and block the doorway.
            if (!hasAuthoredClosedPosition)
                InitializeClosedPosition();
            else
                isInitialized = true;

            SnapToScheduleTarget(immediate: true);
        }

        /// <summary>
        /// Captures the current local position as the closed position. Called from Start,
        /// and exposed so tools/tests can initialize the controller deterministically.
        /// </summary>
        public void InitializeClosedPosition()
        {
            closedLocalPosition = transform.localPosition;
            hasAuthoredClosedPosition = true;
            isInitialized = true;
        }

        /// <summary>Override schedule and hold the door open until cleared.</summary>
        public void SetForcedOpen(bool open)
        {
            forcedOpen = open;
        }

        /// <summary>Whether this door should be open for the given phase (schedule or forced override).</summary>
        public bool ShouldBeOpen(PrisonEventType evt) => forcedOpen || IsOpenPhase(evt);

        /// <summary>Moves the door to the open or closed pose for the current schedule phase.</summary>
        public void SnapToScheduleTarget(bool immediate)
        {
            PrisonEventType evt = PrisonTimeManager.Instance != null
                ? PrisonTimeManager.Instance.CurrentEvent
                : PrisonEventType.Breakfast;
            Vector3 target = GetTargetLocalPosition(evt);
            if (immediate)
                transform.localPosition = target;
            else
                transform.localPosition = StepToward(transform.localPosition, target, slideSpeed, Time.deltaTime);
        }

        private void Update()
        {
            if (!isInitialized) return;

            PrisonEventType evt = PrisonTimeManager.Instance != null
                ? PrisonTimeManager.Instance.CurrentEvent
                : PrisonEventType.Breakfast;
            Vector3 targetPos = GetTargetLocalPosition(evt);
            transform.localPosition = StepToward(transform.localPosition, targetPos, slideSpeed, Time.deltaTime);
        }

        /// <summary>
        /// Clears forced-open overrides on every door in the scene (e.g. when morning count ends).
        /// </summary>
        public static void ClearAllForcedOpens()
        {
            var doors = Object.FindObjectsByType<CellDoorController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < doors.Length; i++)
                doors[i].SetForcedOpen(false);
        }

        /// <summary>
        /// Returns true when the door should be OPEN for the given schedule phase.
        /// Open for movement blocks (meals, work, free time) and for midday/evening
        /// presence counts (inmates must walk into their cells). Closed for night lock-in
        /// and morning roll call / shakedown only.
        /// </summary>
        public static bool IsOpenPhase(PrisonEventType evt)
        {
            switch (evt)
            {
                case PrisonEventType.Breakfast:
                case PrisonEventType.Lunch:
                case PrisonEventType.Dinner:
                case PrisonEventType.FreeTime:
                case PrisonEventType.WorkProgram:
                case PrisonEventType.MiddayCount:
                case PrisonEventType.EveningCount:
                    return true;
                default:
                    // LightsOut, NightRollCall, MorningRollCall, RollCall
                    return false;
            }
        }

        /// <summary>Target local position for the given phase (open position or closed position).</summary>
        public Vector3 GetTargetLocalPosition(PrisonEventType evt)
        {
            return ShouldBeOpen(evt) ? OpenLocalPosition : closedLocalPosition;
        }

        /// <summary>
        /// Pure, deterministic single slide step toward <paramref name="target"/>.
        /// Mirrors the runtime <see cref="Vector3.Lerp(Vector3, Vector3, float)"/> behaviour; the
        /// lerp factor is clamped to [0,1] so the door never overshoots its target.
        /// </summary>
        public static Vector3 StepToward(Vector3 current, Vector3 target, float slideSpeed, float deltaTime)
        {
            float t = Mathf.Clamp01(slideSpeed * deltaTime);
            return Vector3.Lerp(current, target, t);
        }
    }
}
