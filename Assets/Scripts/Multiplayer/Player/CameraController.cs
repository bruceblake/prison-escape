using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Player player;
    // Note: Because we removed Time.deltaTime, you will likely need to 
    // lower your sensitivity in the Inspector (e.g., from 100 to around 2 or 3)
    [SerializeField] private float sensitivity = 2f; 
    [SerializeField] private float clampAngle = 85f;

    private float verticalRotation = 0f;
    private float horizontalRotation = 0f;

    private void OnValidate()
    {
        if (player == null)
            player = GetComponentInParent<Player>();
    }

    private void Start()
    {
        verticalRotation = transform.localEulerAngles.x;
        horizontalRotation = player.transform.eulerAngles.y;

        // Lock the cursor right as the game starts
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            // Ensure cursor is hidden when locked
            if (Cursor.visible) Cursor.visible = false;
            Look();
        }
    }

    private void Look()
    {
        // Removed Time.deltaTime so mouse movement is smooth and raw
        float mouseX = Input.GetAxis("Mouse X") * sensitivity;
        float mouseY = -Input.GetAxis("Mouse Y") * sensitivity;

        verticalRotation += mouseY;
        horizontalRotation += mouseX;

        verticalRotation = Mathf.Clamp(verticalRotation, -clampAngle, clampAngle);

        transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        player.transform.rotation = Quaternion.Euler(0f, horizontalRotation, 0f);
    }
    }