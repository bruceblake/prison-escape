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
        inputs[8] = false;
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

        Vector3 moveDirection = Vector3.Normalize(camProxy.right * inputDirection.x + Vector3.Normalize(FlattenVector3(camProxy.forward)) * inputDirection.y);
        moveDirection *= moveSpeed;

        if (sprint)
            moveDirection *= Prison.PlayerStats.SprintMultiplierSafe;

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