using UnityEngine;

public class WeaponSway : MonoBehaviour
{
    [Header("Sway Settings")]
    [SerializeField] private float smooth = 8f; // Speed of sway (Higher = Snappier, Lower = Heavier)
    [SerializeField] private float swayMultiplier = 2f; // How far it moves

    [Header("Internal References")]
    // The starting position/rotation of the weapon holder
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    // Input storage
    private float mouseX;
    private float mouseY;

    private void Start()
    {
        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation;
    }

    private void Update()
    {
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            mouseX = 0;
            mouseY = 0;
        }
        else
        {
            CalculateSway();
        }

        MoveSway();
        TiltSway();
    }

    private void CalculateSway()
    {
        // 1. Get Mouse Input
        // We multiply by deltaTime to frame-rate independent, but for sway 
        // raw input often feels smoother. Tune this multiplier to taste.
        mouseX = Input.GetAxisRaw("Mouse X") * swayMultiplier;
        mouseY = Input.GetAxisRaw("Mouse Y") * swayMultiplier;
    }

    private void MoveSway()
    {
        // 2. Calculate Position Offset
        // If we look Right (Positive X), gun moves Left (Negative X)
        float moveX = Mathf.Clamp(mouseX, -0.1f, 0.1f);
        float moveY = Mathf.Clamp(mouseY, -0.1f, 0.1f);

        Vector3 finalPosition = new Vector3(moveX, moveY, 0);

        // 3. Apply (Lerp from Current -> Initial + Offset)
        // We target the "Initial Position" minus the movement to create "Drag"
        Vector3 targetPosition = initialPosition + new Vector3(-moveX, -moveY, 0);

        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * smooth);
    }

    private void TiltSway()
    {
        // 4. Calculate Rotation (Tilt)
        // This adds that subtle "twist" when you turn quickly
        float tiltY = Mathf.Clamp(mouseX, -2f, 2f); // Tilt intensity
        float tiltX = Mathf.Clamp(mouseY, -2f, 2f);

        Quaternion targetRotation = Quaternion.Euler(new Vector3(-tiltX, tiltY, 0)); // Inverted X gives better feel

        // Apply rotation relative to the initial rotation
        transform.localRotation = Quaternion.Slerp(transform.localRotation, initialRotation * targetRotation, Time.deltaTime * smooth);
    }
}