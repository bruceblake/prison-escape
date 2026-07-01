using UnityEngine;
using UnityEngine.AI;

namespace Prison.Visuals
{
    /// <summary>
    /// Simple procedural idle / walk animation for the low-poly character rig.
    /// </summary>
    public class LowPolyLocomotionAnimator : MonoBehaviour
    {
        [Header("Rig pivots")]
        [SerializeField] private Transform animRoot;
        [SerializeField] private Transform torso;
        [SerializeField] private Transform head;
        [SerializeField] private Transform leftLegPivot;
        [SerializeField] private Transform rightLegPivot;
        [SerializeField] private Transform leftKneePivot;
        [SerializeField] private Transform rightKneePivot;
        [SerializeField] private Transform leftArmPivot;
        [SerializeField] private Transform rightArmPivot;
        [SerializeField] private Transform leftElbowPivot;
        [SerializeField] private Transform rightElbowPivot;

        [Header("Motion")]
        [SerializeField] private float walkCycleSpeed = 8f;
        [SerializeField] private float idleBobSpeed = 2.2f;
        [SerializeField] private float idleBobAmount = 0.025f;
        [SerializeField] private float legSwing = 38f;
        [SerializeField] private float armSwing = 28f;
        [SerializeField] private float kneeBend = 24f;
        [SerializeField] private float bodyBob = 0.04f;

        private NavMeshAgent _agent;
        private CharacterController _controller;
        private Transform _trackedTransform;
        private Vector3 _lastPosition;
        private float _phase;

        private void Awake()
        {
            _agent = GetComponentInParent<NavMeshAgent>();
            _controller = GetComponentInParent<CharacterController>();
            _trackedTransform = transform.root;
            _lastPosition = _trackedTransform.position;
            AutoBindMissing();
        }

        private void Update()
        {
            float speed = MeasureSpeed();
            bool moving = speed > 0.12f;

            if (moving)
            {
                _phase += Time.deltaTime * walkCycleSpeed * Mathf.Lerp(0.65f, 1.15f, Mathf.InverseLerp(0.12f, 4f, speed));
                ApplyWalk(_phase);
            }
            else
            {
                _phase += Time.deltaTime * idleBobSpeed;
                ApplyIdle(_phase);
            }
        }

        private float MeasureSpeed()
        {
            if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
                return _agent.velocity.magnitude;

            if (_controller != null && _controller.enabled)
            {
                Vector3 delta = _trackedTransform.position - _lastPosition;
                _lastPosition = _trackedTransform.position;
                delta.y = 0f;
                return delta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
            }

            Vector3 fallbackDelta = _trackedTransform.position - _lastPosition;
            _lastPosition = _trackedTransform.position;
            fallbackDelta.y = 0f;
            return fallbackDelta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        }

        private void ApplyIdle(float phase)
        {
            ResetPose();

            if (animRoot != null)
                animRoot.localPosition = new Vector3(0f, Mathf.Sin(phase) * idleBobAmount, 0f);

            if (torso != null)
                torso.localRotation = Quaternion.Euler(Mathf.Sin(phase * 0.5f) * 1.5f, 0f, 0f);

            if (head != null)
                head.localRotation = Quaternion.Euler(Mathf.Sin(phase * 0.7f) * 2f, 0f, 0f);
        }

        private void ApplyWalk(float phase)
        {
            ResetPose();

            float swing = Mathf.Sin(phase);
            float opposite = Mathf.Sin(phase + Mathf.PI);

            if (animRoot != null)
                animRoot.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Cos(phase * 2f)) * bodyBob, 0f);

            SetPivotRotation(leftLegPivot, swing * legSwing, 0f, 0f);
            SetPivotRotation(rightLegPivot, opposite * legSwing, 0f, 0f);
            SetPivotRotation(leftKneePivot, Mathf.Max(0f, swing) * kneeBend, 0f, 0f);
            SetPivotRotation(rightKneePivot, Mathf.Max(0f, opposite) * kneeBend, 0f, 0f);

            SetPivotRotation(leftArmPivot, opposite * armSwing, 0f, 0f);
            SetPivotRotation(rightArmPivot, swing * armSwing, 0f, 0f);
            SetPivotRotation(leftElbowPivot, Mathf.Max(0f, -swing) * (kneeBend * 0.65f), 0f, 0f);
            SetPivotRotation(rightElbowPivot, Mathf.Max(0f, -opposite) * (kneeBend * 0.65f), 0f, 0f);

            if (torso != null)
                torso.localRotation = Quaternion.Euler(Mathf.Sin(phase * 2f) * 3f, 0f, swing * 2f);
        }

        private void ResetPose()
        {
            SetPivotRotation(leftLegPivot, 0f, 0f, 0f);
            SetPivotRotation(rightLegPivot, 0f, 0f, 0f);
            SetPivotRotation(leftKneePivot, 0f, 0f, 0f);
            SetPivotRotation(rightKneePivot, 0f, 0f, 0f);
            SetPivotRotation(leftArmPivot, 0f, 0f, 0f);
            SetPivotRotation(rightArmPivot, 0f, 0f, 0f);
            SetPivotRotation(leftElbowPivot, 0f, 0f, 0f);
            SetPivotRotation(rightElbowPivot, 0f, 0f, 0f);

            if (torso != null)
                torso.localRotation = Quaternion.identity;
            if (head != null)
                head.localRotation = Quaternion.identity;
        }

        private static void SetPivotRotation(Transform pivot, float x, float y, float z)
        {
            if (pivot == null)
                return;
            pivot.localRotation = Quaternion.Euler(x, y, z);
        }

        public void Configure(
            Transform root,
            Transform torsoTransform,
            Transform headTransform,
            Transform leftLeg,
            Transform rightLeg,
            Transform leftKnee,
            Transform rightKnee,
            Transform leftArm,
            Transform rightArm,
            Transform leftElbow,
            Transform rightElbow)
        {
            animRoot = root;
            torso = torsoTransform;
            head = headTransform;
            leftLegPivot = leftLeg;
            rightLegPivot = rightLeg;
            leftKneePivot = leftKnee;
            rightKneePivot = rightKnee;
            leftArmPivot = leftArm;
            rightArmPivot = rightArm;
            leftElbowPivot = leftElbow;
            rightElbowPivot = rightElbow;
        }

        private void AutoBindMissing()
        {
            if (animRoot == null)
                animRoot = transform.Find("AnimRoot");

            if (animRoot == null)
                return;

            torso ??= animRoot.Find("Torso");
            head ??= animRoot.Find("Head");
            leftLegPivot ??= animRoot.Find("LeftLegPivot");
            rightLegPivot ??= animRoot.Find("RightLegPivot");
            leftKneePivot ??= animRoot.Find("LeftLegPivot/LeftKneePivot");
            rightKneePivot ??= animRoot.Find("RightLegPivot/RightKneePivot");
            leftArmPivot ??= animRoot.Find("LeftArmPivot");
            rightArmPivot ??= animRoot.Find("RightArmPivot");
            leftElbowPivot ??= animRoot.Find("LeftArmPivot/LeftElbowPivot");
            rightElbowPivot ??= animRoot.Find("RightArmPivot/RightElbowPivot");
        }
    }
}
