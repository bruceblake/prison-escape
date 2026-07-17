using UnityEngine;
using UnityEngine.AI;

namespace Prison.Visuals
{
    /// <summary>
    /// Drives BlenderKit rigged characters: Mecanim when a valid avatar exists, otherwise procedural bone walk.
    /// </summary>
    public class BlenderKitLocomotionAnimator : MonoBehaviour
    {
        static readonly int SpeedHash = Animator.StringToHash("Speed");
        static readonly int JumpHash = Animator.StringToHash("Jump");

        [SerializeField] private float walkSpeed = 1.6f;
        [SerializeField] private float runSpeed = 4.2f;
        [SerializeField] private float dampTime = 0.18f;
        [SerializeField] private float walkCycleSpeed = 6.5f;
        [SerializeField] private float legSwing = 36f;
        [SerializeField] private float armSwing = 26f;
        [SerializeField] private float kneeBend = 24f;
        [SerializeField] private float idleBobSpeed = 2.2f;
        [SerializeField] private float idleBobAmount = 0.015f;

        [Header("Crouch pose")]
        [Tooltip("How far the hips drop at full crouch, in meters.")]
        [SerializeField] private float crouchHipDrop = 0.32f;
        [SerializeField] private float crouchThighAngle = 32f;
        [SerializeField] private float crouchKneeAngle = 48f;
        [SerializeField] private float crouchChestLean = 14f;

        private Animator _animator;
        private NavMeshAgent _agent;
        private CharacterController _controller;
        private Transform _trackedTransform;
        private Vector3 _lastPosition;
        private float _smoothedAnimSpeed;
        private float _phase;
        private bool _useProcedural;
        private bool _hasJumpParam;
        private bool _wasGrounded = true;
        private float _crouchBlend; // 0 = standing, 1 = crouched (driven by PlayerController)
        // Local X = sagittal swing (forward/back). Local Z was abducting legs sideways.
        private Vector3 _legSwingAxis = Vector3.right;

        private Transform _hips;
        private Transform _chest;
        private Transform _upperLegL;
        private Transform _upperLegR;
        private Transform _lowerLegL;
        private Transform _lowerLegR;
        private Transform _upperArmL;
        private Transform _upperArmR;
        private Quaternion _hipsRest;
        private Quaternion _chestRest;
        private Quaternion _upperLegLRest;
        private Quaternion _upperLegRRest;
        private Quaternion _lowerLegLRest;
        private Quaternion _lowerLegRRest;
        private Quaternion _upperArmLRest;
        private Quaternion _upperArmRRest;
        private Vector3 _hipsRestPos;

        private void Awake()
        {
            _animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
            _agent = GetComponentInParent<NavMeshAgent>();
            _controller = GetComponentInParent<CharacterController>();
            _trackedTransform = transform.root;
            _lastPosition = _trackedTransform.position;

            bool hasController = _animator != null && _animator.runtimeAnimatorController != null;
            bool hasValidAvatar = _animator != null && _animator.avatar != null && _animator.avatar.isValid;

            // Procedural first: BlenderKit Generic avatars often leave Mecanim stuck on the
            // mesh bind pose (mid-stride) even when clips exist. Bone walk always works.
            _useProcedural = BindBones();
            if (_useProcedural)
            {
                TryCaptureStandingRestFromAnimator();
                if (_animator != null)
                    _animator.enabled = false;
                ApplyIdle(0f);
            }
            else
            {
                bool useMecanim = hasValidAvatar && hasController;
                if (_animator != null)
                    _animator.enabled = useMecanim;
                if (useMecanim)
                {
                    foreach (var p in _animator.parameters)
                        if (p.nameHash == JumpHash && p.type == AnimatorControllerParameterType.Trigger)
                            _hasJumpParam = true;
                }
                else
                    Debug.LogWarning($"[BlenderKitLocomotion] No procedural bones and no Mecanim on {name}", this);
            }
        }

        private bool BindBones()
        {
            Transform searchRoot = transform.Find("Mesh") ?? transform;
            _hips = FindBone(searchRoot, "Hips");
            _chest = FindBone(searchRoot, "Chest");
            _upperLegL = FindBone(searchRoot, "UpperLeg_L");
            _upperLegR = FindBone(searchRoot, "UpperLeg_R");
            _lowerLegL = FindBone(searchRoot, "LowerLeg_L");
            _lowerLegR = FindBone(searchRoot, "LowerLeg_R");
            _upperArmL = FindBone(searchRoot, "UpperArm_L");
            _upperArmR = FindBone(searchRoot, "UpperArm_R");

            if (_hips == null || _upperLegL == null || _upperLegR == null)
                return false;

            CaptureCurrentAsRest();
            _legSwingAxis = ChooseSagittalSwingAxis();
            return true;
        }

        /// <summary>
        /// Pick the local axis that swings the foot most along character forward (walk),
        /// not sideways (the old Vector3.forward bug).
        /// </summary>
        private Vector3 ChooseSagittalSwingAxis()
        {
            Vector3 charForward = _trackedTransform != null ? _trackedTransform.forward : transform.forward;
            charForward.y = 0f;
            if (charForward.sqrMagnitude < 0.01f)
                charForward = Vector3.forward;
            charForward.Normalize();

            Vector3[] candidates = { Vector3.right, Vector3.up, Vector3.forward };
            Vector3 best = Vector3.right;
            float bestScore = -1f;
            Quaternion rest = _upperLegL.localRotation;
            Vector3 footHint = _upperLegL.TransformPoint(Vector3.down);

            foreach (var axis in candidates)
            {
                _upperLegL.localRotation = rest * Quaternion.AngleAxis(25f, axis);
                Vector3 moved = _upperLegL.TransformPoint(Vector3.down);
                Vector3 delta = moved - footHint;
                delta.y = 0f;
                float along = Mathf.Abs(Vector3.Dot(delta, charForward));
                float sideways = (delta - charForward * Vector3.Dot(delta, charForward)).magnitude;
                float score = along - sideways * 0.75f;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = axis;
                }
            }

            _upperLegL.localRotation = rest;
            return best;
        }

        private void CaptureCurrentAsRest()
        {
            _hipsRest = _hips.localRotation;
            _hipsRestPos = _hips.localPosition;
            _chestRest = _chest != null ? _chest.localRotation : Quaternion.identity;
            _upperLegLRest = _upperLegL.localRotation;
            _upperLegRRest = _upperLegR.localRotation;
            _lowerLegLRest = _lowerLegL != null ? _lowerLegL.localRotation : Quaternion.identity;
            _lowerLegRRest = _lowerLegR != null ? _lowerLegR.localRotation : Quaternion.identity;
            _upperArmLRest = _upperArmL != null ? _upperArmL.localRotation : Quaternion.identity;
            _upperArmRRest = _upperArmR != null ? _upperArmR.localRotation : Quaternion.identity;
        }

        private void TryCaptureStandingRestFromAnimator()
        {
            if (_animator == null || _animator.runtimeAnimatorController == null)
            {
                ApproximateStandingRestFromBindPose();
                return;
            }

            bool wasEnabled = _animator.enabled;
            _animator.enabled = true;
            _animator.Rebind();
            _animator.Update(0f);
            _animator.SetFloat(SpeedHash, 0f);
            for (int i = 0; i < 3; i++)
                _animator.Update(0.05f);

            CaptureCurrentAsRest();
            _animator.enabled = wasEnabled;

            float strideSplit = Quaternion.Angle(_upperLegLRest, _upperLegRRest);
            if (strideSplit > 18f)
                ApproximateStandingRestFromBindPose();
        }

        private void ApproximateStandingRestFromBindPose()
        {
            Quaternion upper = Quaternion.Slerp(_upperLegLRest, _upperLegRRest, 0.5f);
            _upperLegLRest = upper;
            _upperLegRRest = upper;

            if (_lowerLegL != null && _lowerLegR != null)
            {
                Quaternion lower = Quaternion.Slerp(_lowerLegLRest, _lowerLegRRest, 0.5f);
                _lowerLegLRest = lower;
                _lowerLegRRest = lower;
            }

            if (_upperArmL != null && _upperArmR != null)
            {
                Quaternion arms = Quaternion.Slerp(_upperArmLRest, _upperArmRRest, 0.5f);
                _upperArmLRest = arms;
                _upperArmRRest = arms;
            }
        }

        private static Transform FindBone(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name)
                    return t;
            }

            return null;
        }

        private void Update()
        {
            float raw = MeasureSpeed();
            float target = raw <= 0.12f ? 0f : Mathf.Min(raw, runSpeed * 1.25f);
            float lerp = dampTime > 0f ? Time.deltaTime / dampTime : 1f;
            _smoothedAnimSpeed = Mathf.Lerp(_smoothedAnimSpeed, target, Mathf.Clamp01(lerp));

            if (_useProcedural)
                return;

            if (_animator == null || !_animator.isActiveAndEnabled)
                return;

            _animator.SetFloat(SpeedHash, _smoothedAnimSpeed);
            UpdateJumpTrigger();
        }

        private void UpdateJumpTrigger()
        {
            if (!_hasJumpParam || _controller == null || !_controller.enabled)
                return;

            bool grounded = _controller.isGrounded;
            if (_wasGrounded && !grounded && _controller.velocity.y > 0.5f)
                _animator.SetTrigger(JumpHash);
            _wasGrounded = grounded;
        }

        /// <summary>
        /// Crouch amount, 0–1, from the player controller. In procedural mode the crouch pose
        /// overlays the walk/idle cycle; Mecanim-only rigs still get collider/camera crouch
        /// from <see cref="PlayerController"/> even though the clip pose is unchanged.
        /// </summary>
        public void SetCrouched(float blend01)
        {
            _crouchBlend = Mathf.Clamp01(blend01);
        }

        private void LateUpdate()
        {
            if (!_useProcedural || _upperLegL == null || _upperLegR == null)
                return;

            if (_smoothedAnimSpeed > 0.15f)
            {
                float cycle = Mathf.Lerp(0.75f, 1.15f, Mathf.InverseLerp(0.15f, runSpeed, _smoothedAnimSpeed));
                _phase += Time.deltaTime * walkCycleSpeed * cycle;
                ApplyWalk(_phase);
            }
            else
            {
                _phase += Time.deltaTime * idleBobSpeed;
                ApplyIdle(_phase);
            }

            ApplyCrouchOverlay();
        }

        /// <summary>
        /// Sneak pose layered on top of walk/idle: hips sink, thighs and knees fold (slight
        /// stagger like a creep), chest leans in. Blend-weighted so entering/leaving crouch eases.
        /// </summary>
        private void ApplyCrouchOverlay()
        {
            // Rotations above already reset to rest each frame, so restore the rest position
            // first and re-apply the world-space drop — never accumulate.
            _hips.localPosition = _hipsRestPos;
            if (_crouchBlend <= 0.001f)
                return;

            float b = _crouchBlend;
            _hips.position += Vector3.down * (crouchHipDrop * b);
            _hips.localRotation = _hips.localRotation * Quaternion.AngleAxis(crouchChestLean * 0.4f * b, _legSwingAxis);
            if (_chest != null)
                _chest.localRotation = _chest.localRotation * Quaternion.AngleAxis(crouchChestLean * b, _legSwingAxis);

            // Same sign convention as the walk cycle (L +, R −) → a slight stagger-stance creep.
            _upperLegL.localRotation = _upperLegL.localRotation * Quaternion.AngleAxis(crouchThighAngle * b, _legSwingAxis);
            _upperLegR.localRotation = _upperLegR.localRotation * Quaternion.AngleAxis(-crouchThighAngle * 0.7f * b, _legSwingAxis);
            if (_lowerLegL != null)
                _lowerLegL.localRotation = _lowerLegL.localRotation * Quaternion.AngleAxis(crouchKneeAngle * b, _legSwingAxis);
            if (_lowerLegR != null)
                _lowerLegR.localRotation = _lowerLegR.localRotation * Quaternion.AngleAxis(-crouchKneeAngle * 0.8f * b, _legSwingAxis);
        }

        private void ApplyWalk(float phase)
        {
            float swing = Mathf.Sin(phase) * legSwing;
            float arm = Mathf.Sin(phase) * armSwing;
            float knee = Mathf.Max(0f, Mathf.Sin(phase)) * kneeBend;

            _upperLegL.localRotation = _upperLegLRest * Quaternion.AngleAxis(swing, _legSwingAxis);
            _upperLegR.localRotation = _upperLegRRest * Quaternion.AngleAxis(-swing, _legSwingAxis);

            if (_lowerLegL != null)
                _lowerLegL.localRotation = _lowerLegLRest * Quaternion.AngleAxis(knee, _legSwingAxis);
            if (_lowerLegR != null)
                _lowerLegR.localRotation = _lowerLegRRest * Quaternion.AngleAxis(-knee, _legSwingAxis);

            if (_upperArmL != null)
                _upperArmL.localRotation = _upperArmLRest * Quaternion.AngleAxis(-arm, _legSwingAxis);
            if (_upperArmR != null)
                _upperArmR.localRotation = _upperArmRRest * Quaternion.AngleAxis(arm, _legSwingAxis);

            if (_hips != null)
            {
                float bob = Mathf.Sin(phase * 2f) * 0.015f;
                _hips.localRotation = _hipsRest * Quaternion.AngleAxis(bob * 12f, _legSwingAxis);
            }
        }

        private void ApplyIdle(float phase)
        {
            float bob = Mathf.Sin(phase) * idleBobAmount;
            if (_hips != null)
                _hips.localRotation = _hipsRest * Quaternion.AngleAxis(bob * 20f, _legSwingAxis);
            if (_chest != null)
                _chest.localRotation = _chestRest * Quaternion.AngleAxis(-bob * 12f, _legSwingAxis);

            _upperLegL.localRotation = _upperLegLRest;
            _upperLegR.localRotation = _upperLegRRest;
            if (_lowerLegL != null) _lowerLegL.localRotation = _lowerLegLRest;
            if (_lowerLegR != null) _lowerLegR.localRotation = _lowerLegRRest;
            if (_upperArmL != null) _upperArmL.localRotation = _upperArmLRest;
            if (_upperArmR != null) _upperArmR.localRotation = _upperArmRRest;
        }

        private float MeasureSpeed()
        {
            float transformSpeed = TransformDeltaSpeed();

            float agentSpeed = 0f;
            if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
                agentSpeed = _agent.velocity.magnitude;

            // Prefer agent velocity when moving on mesh — transform deltas jitter on slopes/rebases.
            float speed = agentSpeed > 0.05f ? agentSpeed : Mathf.Max(agentSpeed, transformSpeed);
            return Mathf.Min(speed, runSpeed * 2f);
        }

        private float TransformDeltaSpeed()
        {
            Vector3 delta = _trackedTransform.position - _lastPosition;
            _lastPosition = _trackedTransform.position;
            delta.y = 0f;
            return delta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        }
    }
}
