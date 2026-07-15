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
        [Tooltip("The local position of the door when fully closed. Captured automatically on Start.")]
        public Vector3 closedLocalPosition;

        [Tooltip("Local-space offset added to the closed position to reach the fully OPEN position. " +
                 "For a barred cell door this should slide it sideways far enough to clear the doorway.")]
        public Vector3 openOffset = new Vector3(0f, 0f, 6.0f);

        [Tooltip("Slide responsiveness (lerp factor per second) between open and closed.")]
        public float slideSpeed = 3.0f;

        private bool isInitialized = false;

        /// <summary>True once the closed position has been captured.</summary>
        public bool IsInitialized => isInitialized;

        /// <summary>Local position the door rests at when closed.</summary>
        public Vector3 ClosedLocalPosition => closedLocalPosition;

        /// <summary>Local position the door rests at when fully open.</summary>
        public Vector3 OpenLocalPosition => closedLocalPosition + openOffset;

        private void Start()
        {
            InitializeClosedPosition();
        }

        /// <summary>
        /// Captures the current local position as the closed position. Called from Start,
        /// and exposed so tools/tests can initialize the controller deterministically.
        /// </summary>
        public void InitializeClosedPosition()
        {
            closedLocalPosition = transform.localPosition;
            isInitialized = true;
        }

        private void Update()
        {
            if (!isInitialized || PrisonTimeManager.Instance == null) return;

            Vector3 targetPos = GetTargetLocalPosition(PrisonTimeManager.Instance.CurrentEvent);
            transform.localPosition = StepToward(transform.localPosition, targetPos, slideSpeed, Time.deltaTime);
        }

        /// <summary>
        /// Returns true when the door should be OPEN for the given schedule phase.
        /// Open during all day phases (05:00-21:00): counts, meals, work, free time.
        /// Closed phases: LightsOut, NightRollCall (the 21:00-05:00 lock-in).
        /// </summary>
        public static bool IsOpenPhase(PrisonEventType evt)
        {
            switch (evt)
            {
                case PrisonEventType.RollCall:
                case PrisonEventType.Breakfast:
                case PrisonEventType.Lunch:
                case PrisonEventType.Dinner:
                case PrisonEventType.FreeTime:
                case PrisonEventType.MorningRollCall:
                case PrisonEventType.WorkProgram:
                case PrisonEventType.MiddayCount:
                case PrisonEventType.EveningCount:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>Target local position for the given phase (open position or closed position).</summary>
        public Vector3 GetTargetLocalPosition(PrisonEventType evt)
        {
            return IsOpenPhase(evt) ? OpenLocalPosition : closedLocalPosition;
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
