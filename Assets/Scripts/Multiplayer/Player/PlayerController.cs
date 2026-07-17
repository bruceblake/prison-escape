using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Riptide;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private CharacterController controller;
    [SerializeField] private Transform camProxy;
    [SerializeField] private float gravity;
    [SerializeField] private float movementSpeed;
    [SerializeField] private float jumpHeight;

    [Header("Crouch")]
    [Tooltip("Hold to crouch. C toggles as an alternative.")]
    [SerializeField] private KeyCode crouchHoldKey = KeyCode.LeftControl;
    [SerializeField] private KeyCode crouchToggleKey = KeyCode.C;
    [SerializeField] private float crouchSpeedMultiplier = 0.45f;
    [Tooltip("CharacterController height while crouched (standing height is captured at startup).")]
    [SerializeField] private float crouchedHeight = 1.2f;
    [Tooltip("How far the camera/eye drops while crouched, in meters.")]
    [SerializeField] private float crouchEyeDrop = 0.55f;
    [SerializeField] private float crouchLerpSpeed = 9f;

    /// <summary>True while the player is crouched (movement, camera, animation, and guard detection all key off this).</summary>
    public bool IsCrouched { get; private set; }

    private bool _crouchToggled;
    private float _standingHeight = 2f;
    private Vector3 _standingCenter;
    private Vector3 _standingCamLocalPos;
    private float _crouchBlend; // 0 = standing, 1 = crouched
    private Prison.Visuals.BlenderKitLocomotionAnimator _visualAnimator;

    [SerializeField] private GameObject serverPositionPrefab;
    private Transform serverPositionVisualizer;

    private float gravityAcceleration;
    private float moveSpeed;
    private float jumpSeed;
    private float yVelocity;

    [SerializeField] private GameObject hitmarker;

    private bool[] inputs = new bool[10];

    // A struct to hold a snapshot of input and state
    private struct InputState
    {
        public ushort Tick;
        public bool[] Inputs;
        public Vector3 Forward;
        public Vector3 Position; // Store the position after this input was applied
    }

    private List<InputState> inputBuffer = new List<InputState>();

    private void OnValidate()
    {
        if (controller == null) controller = GetComponent<CharacterController>();
        if (player == null) player = GetComponent<Player>();
        Initialize();
    }

    private void Start()
    {
        Initialize();

        if (controller != null)
        {
            _standingHeight = controller.height;
            _standingCenter = controller.center;
        }
        if (camProxy != null)
            _standingCamLocalPos = camProxy.localPosition;
        _visualAnimator = GetComponentInChildren<Prison.Visuals.BlenderKitLocomotionAnimator>(true);
        // if (serverPositionPrefab != null)
        // {
        //     serverPositionVisualizer = Instantiate(serverPositionPrefab, transform.position, Quaternion.identity).transform;
        // }
    }

    private void Initialize()
    {
        gravityAcceleration = gravity * Time.fixedDeltaTime * Time.fixedDeltaTime;
        moveSpeed = movementSpeed * Time.fixedDeltaTime;
        jumpSeed = Mathf.Sqrt(jumpHeight * -2f * gravityAcceleration);
    }

    private void FixedUpdate()
    {
       
        
        // 2. Predict Movement
        ProcessInputs(inputs, camProxy.forward);
        // 3. Store State and Send Input
        ushort currentTick = NetworkManager.CurrentTick;
        inputBuffer.Add(new InputState
        {
            Tick = currentTick,
            Inputs = inputs,
            Forward = camProxy.forward,
            Position = transform.position // Store the result of the prediction
        });
        SendPlayerInput(currentTick, inputs, camProxy.forward);
    }

    public void Update()
    {
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            // Clear inputs so we don't keep moving/shooting if menu is opened while holding keys
            for (int i = 0; i < inputs.Length; i++) inputs[i] = false;
            return;
        }

         // 1. Get Input
        inputs[0] = Input.GetKey(KeyCode.W);
        inputs[1] = Input.GetKey(KeyCode.S);
        inputs[2] = Input.GetKey(KeyCode.A);
        inputs[3] = Input.GetKey(KeyCode.D);
        inputs[4] = Input.GetKey(KeyCode.Space);
        inputs[5] = Input.GetKey(KeyCode.LeftShift);
        inputs[6] = !PlayerInteractor.IsHoldInteracting && Input.GetMouseButtonDown(0);
        inputs[7] = !PlayerInteractor.IsHoldInteracting && Input.GetMouseButton(0);
        if (Input.GetKeyDown(crouchToggleKey))
            _crouchToggled = !_crouchToggled;
        if (Input.GetKey(crouchHoldKey))
            _crouchToggled = false; // hold key takes over; releasing it stands up
        inputs[8] = Input.GetKey(crouchHoldKey) || _crouchToggled;
        inputs[9] = false;

         if (inputs[6] && player.WeaponController?.currentGun != null && player.WeaponController.currentGun.fireMode == Gun.FireMode.Single)
        {
            if (player.WeaponController.currentGun != null && !PlayerInteractor.IsHoldInteracting)
            {
                Debug.Log($"Player {player.Id} is attempting to shoot with {player.WeaponController.currentGun.name}");
                player.WeaponController.currentGun.Shoot(NetworkManager.CurrentTick, player);
            }
        }
        if (inputs[7] && player.WeaponController?.currentGun != null && player.WeaponController.currentGun.fireMode == Gun.FireMode.Automatic)
        {
            Debug.Log($"Player {player.Id} is attempting to shoot (automatic) with {player.WeaponController.currentGun.name}");
            player.WeaponController.currentGun.Shoot(NetworkManager.CurrentTick, player);
        }
    }

    private void ProcessInputs(bool[] inputs, Vector3 forward)
    {
        Vector2 inputDirection = Vector2.zero;
        if (inputs[0]) inputDirection.y += 1;
        if (inputs[1]) inputDirection.y -= 1;
        if (inputs[2]) inputDirection.x -= 1;
        if (inputs[3]) inputDirection.x += 1;

        bool jump = inputs[4];
        bool sprint = inputs[5];

        bool wantCrouch = inputs[8];
        if (!wantCrouch && IsCrouched && !HasHeadroomToStand())
            wantCrouch = true; // stay down until there's ceiling clearance (vents, under beds)
        IsCrouched = wantCrouch;

        Vector3 moveDirection = Vector3.Normalize(camProxy.right * inputDirection.x + Vector3.Normalize(FlattenVector3(camProxy.forward)) * inputDirection.y);
        moveDirection *= moveSpeed;

        if (IsCrouched)
        {
            moveDirection *= crouchSpeedMultiplier;
            jump = false; // no crouch-jumping
        }
        else if (sprint)
        {
            moveDirection *= Prison.PlayerStats.SprintMultiplierSafe;
        }

        if (controller.isGrounded)
        {
            yVelocity = 0f;
            if (jump)
                yVelocity = jumpSeed;
        }
        yVelocity += gravityAcceleration;

        moveDirection.y = yVelocity;
        controller.Move(moveDirection);
    }

    private bool HasHeadroomToStand()
    {
        if (controller == null) return true;
        float radius = Mathf.Max(0.05f, controller.radius - 0.02f);
        Vector3 bottom = transform.position + controller.center - Vector3.up * (controller.height * 0.5f - radius);
        float castUp = (_standingHeight - controller.height) + 0.05f;
        if (castUp <= 0f) return true;
        return !Physics.SphereCast(bottom, radius, Vector3.up,
            out _, controller.height - radius * 2f + castUp, ~0, QueryTriggerInteraction.Ignore);
    }

    /// <summary>Smoothly applies crouch to the capsule, camera, and character visual.</summary>
    private void LateUpdate()
    {
        float target = IsCrouched ? 1f : 0f;
        _crouchBlend = Mathf.MoveTowards(_crouchBlend, target, crouchLerpSpeed * Time.deltaTime);

        if (controller != null)
        {
            float height = Mathf.Lerp(_standingHeight, crouchedHeight, _crouchBlend);
            controller.height = height;
            controller.center = new Vector3(_standingCenter.x,
                _standingCenter.y - (_standingHeight - height) * 0.5f, _standingCenter.z);
        }

        if (camProxy != null)
            camProxy.localPosition = _standingCamLocalPos + Vector3.down * (crouchEyeDrop * _crouchBlend);

        if (_visualAnimator == null)
            _visualAnimator = GetComponentInChildren<Prison.Visuals.BlenderKitLocomotionAnimator>(true);
        if (_visualAnimator != null)
            _visualAnimator.SetCrouched(_crouchBlend);
    }

    private Vector3 FlattenVector3(Vector3 vector)
    {
        vector.y = 0;
        return vector;
    }

    public void ForceTeleport(Vector3 position)
    {
        controller.enabled = false;
        transform.position = position;
        yVelocity = 0f;
        controller.enabled = true;
        inputBuffer.Clear();
    }

    public void OnServerStateReceived(ushort tick, bool didTeleport, Vector3 serverPosition)
    {
        if (serverPositionVisualizer != null)
        {
            serverPositionVisualizer.position = serverPosition;
        }

        if (didTeleport)
        {
            transform.position = serverPosition;
            yVelocity = 0f;
            inputBuffer.Clear();
            Debug.Log("[Client] Teleport detected, snapping to server position and clearing input buffer.");
            return;
        }


        int bufferIndex = -1;
        for (int i = 0; i < inputBuffer.Count; i++)
        {
            if (inputBuffer[i].Tick == tick)
            {
                bufferIndex = i;
                break;
            }
        }

        if (bufferIndex == -1) return;

        Vector3 predictedPosition = inputBuffer[bufferIndex].Position;
        float positionError = Vector3.Distance(predictedPosition, serverPosition);

        if (positionError > 0.001f)
        {
            Debug.LogWarning($"[Client] Reconciliation needed for Tick {tick}! Snapping state and clearing buffer. Error: {positionError:F3}");      

            // 1. Snap position and velocity to the authoritative server state.
            controller.enabled = false;
            transform.position = serverPosition;
            yVelocity = 0f;
            controller.enabled = true;

            // 2. Clear the entire input buffer. This is a simpler and more stable
            // approach than re-simulating, as it avoids running physics logic
            // multiple times within a single frame.
            inputBuffer.Clear();
        }
        else
        {
            // Prediction was accurate. Just clear the old buffer entries that the server has now processed.
            inputBuffer.RemoveRange(0, bufferIndex + 1);
        }
    }

    #region Messages
    private void SendPlayerInput(ushort tick, bool[] inputs, Vector3 forward)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || nm.Client == null || !nm.IsConnected)
            return;

        Message message = Message.Create(MessageSendMode.Unreliable, ClientToServerId.PlayerInput);
        message.AddUShort(tick);
        message.AddBools(inputs, false);
        message.AddVector3(forward);
        nm.Client.Send(message);
    }
    #endregion
}