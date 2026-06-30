using UnityEngine;

/// <summary>Keeps a transform facing the main camera (for world-space prompts).</summary>
public class CameraBillboard : MonoBehaviour
{
    [SerializeField] private bool flip = true;
    [SerializeField] private bool onlyYaw;

    private void LateUpdate()
    {
        if (Camera.main == null) return;
        Vector3 toCam = (Camera.main.transform.position - transform.position).normalized;
        if (onlyYaw)
        {
            toCam.y = 0f;
            if (toCam.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.LookRotation(-toCam.normalized, Vector3.up);
        }
        else
            transform.forward = flip ? -toCam : Camera.main.transform.forward;
    }
}
