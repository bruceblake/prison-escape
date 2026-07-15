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
        [SerializeField] private float dampTime = 0.12f;
        [SerializeField] private float walkCycleSpeed = 8f;
        [SerializeField] private float legSwing = 42f;
        [SerializeField] private float armSwing = 30f;
        [SerializeField] private float kneeBend = 28f;
        [SerializeField] private float idleBobSpeed = 2.2f;
        [SerializeField] private float idleBobAmount = 0.02f;

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
        private Vector3 _legSwingAxis = Vector3.forward;

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

        private void Awake()
        {
            _animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
            _agent = GetComponentInParent<NavMeshAgent>();
            _controller = GetComponentInParent<CharacterController>();
            _trackedTransform = transform.root;
            _lastPosition = _trackedTransform.position;

            bool hasValidAvatar = _animator != null && _animator.avatar != null && _animator.avatar.isValid;
            if (!hasValidAvatar && _animator != null)
                _animator.enabled = false;

            if (hasValidAvatar)
            {
                foreach (var p in _animator.parameters)
                    if (p.nameHash == JumpHash && p.type == AnimatorControllerParameterType.Trigger)
                        _hasJumpParam = true;
            }

            _useProcedural = !hasValidAvatar;
            if (_useProcedural && !BindBones())
                Debug.LogWarning($"[BlenderKitLocomotion] Procedural bones not found on {name}", this);
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

            _hipsRest = _hips.localRotation;
            _chestRest = _chest != null ? _chest.localRotation : Quaternion.identity;
            _upperLegLRest = _upperLegL.localRotation;
            _upperLegRRest = _upperLegR.localRotation;
            _lowerLegLRest = _lowerLegL != null ? _lowerLegL.localRotation : Quaternion.identity;
            _lowerLegRRest = _lowerLegR != null ? _lowerLegR.localRotation : Quaternion.identity;
            _upperArmLRest = _upperArmL != null ? _upperArmL.localRotation : Quaternion.identity;
            _upperArmRRest = _upperArmR != null ? _upperArmR.localRotation : Quaternion.identity;

            // BlenderKit export: leg swing reads best on local Z for these rigs.
            _legSwingAxis = Vector3.forward;
            return true;
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
            if (_useProcedural)
                return;

            if (_animator == null || !_animator.isActiveAndEnabled)
                return;

            // Feed the continuous measured speed so the blend tree eases through
            // idle <-> walk <-> run instead of snapping between fixed thresholds.
            float speed = Mathf.Min(MeasureSpeed(), runSpeed * 1.25f);
            if (speed <= 0.12f) speed = 0f;
            _smoothedAnimSpeed = Mathf.Lerp(_smoothedAnimSpeed, speed, dampTime > 0f ? Time.deltaTime / dampTime : 1f);
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

        private void LateUpdate()
        {
            if (!_useProcedural || _upperLegL == null || _upperLegR == null)
                return;

            float speed = MeasureSpeed();
            if (speed > 0.12f)
            {
                _phase += Time.deltaTime * walkCycleSpeed * Mathf.Lerp(0.7f, 1.2f, Mathf.InverseLerp(0.12f, runSpeed, speed));
                ApplyWalk(_phase);
            }
            else
            {
                _phase += Time.deltaTime * idleBobSpeed;
                ApplyIdle(_phase);
            }
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
                _lowerLegR.localRotation = _lowerLegRRest * Quaternion.AngleAxis(knee, _legSwingAxis);

            if (_upperArmL != null)
                _upperArmL.localRotation = _upperArmLRest * Quaternion.AngleAxis(-arm, _legSwingAxis);
            if (_upperArmR != null)
                _upperArmR.localRotation = _upperArmRRest * Quaternion.AngleAxis(arm, _legSwingAxis);

            if (_hips != null)
            {
                float bob = Mathf.Sin(phase * 2f) * 0.02f;
                _hips.localRotation = _hipsRest * Quaternion.Euler(bob * 20f, 0f, 0f);
            }
        }

        private void ApplyIdle(float phase)
        {
            float bob = Mathf.Sin(phase) * idleBobAmount;
            if (_hips != null)
                _hips.localRotation = _hipsRest * Quaternion.Euler(bob * 30f, 0f, 0f);
            if (_chest != null)
                _chest.localRotation = _chestRest * Quaternion.Euler(-bob * 15f, 0f, 0f);

            _upperLegL.localRotation = _upperLegLRest;
            _upperLegR.localRotation = _upperLegRRest;
            if (_lowerLegL != null) _lowerLegL.localRotation = _lowerLegLRest;
            if (_lowerLegR != null) _lowerLegR.localRotation = _lowerLegRRest;
            if (_upperArmL != null) _upperArmL.localRotation = _upperArmLRest;
            if (_upperArmR != null) _upperArmR.localRotation = _upperArmRRest;
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

            Vector3 fallback = _trackedTransform.position - _lastPosition;
            _lastPosition = _trackedTransform.position;
            fallback.y = 0f;
            return fallback.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        }
    }
}
