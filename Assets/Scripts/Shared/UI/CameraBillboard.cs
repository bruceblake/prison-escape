using UnityEngine;

/// <summary>Keeps a transform facing the main camera (for world-space prompts).</summary>
public class CameraBillboard : MonoBehaviour
{
    [SerializeField] private bool flip = true;
    [SerializeField] private bool onlyYaw;

    private Camera _cam;

    private void LateUpdate()
    {
        if (_cam == null)
            _cam = Camera.main;
        if (_cam == null) return;

        Transform camTransform = _cam.transform;
        Vector3 toCam = (camTransform.position - transform.position).normalized;
        if (onlyYaw)
        {
            toCam.y = 0f;
            if (toCam.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.LookRotation(-toCam.normalized, Vector3.up);
        }
        else
            transform.forward = flip ? -toCam : camTransform.forward;
    }
}
