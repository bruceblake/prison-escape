using UnityEngine;

public class RecoilController : MonoBehaviour
{
    [Header("Recoil Settings")]
    public float rotationSpeed = 6f;
    public float returnSpeed = 25f;
    public Vector3 recoilAmount = new Vector3(2f, 2f, 0f); // X = Up, Y = Left/Right

    [Header("State")]
    private Vector3 currentRotation;
    private Vector3 targetRotation;

    void Update()
    {
        // 1. Smoothly interpolate current rotation towards the target rotation
        targetRotation = Vector3.Lerp(targetRotation, Vector3.zero, returnSpeed * Time.deltaTime);
        currentRotation = Vector3.Slerp(currentRotation, targetRotation, rotationSpeed * Time.deltaTime);

        // 2. Apply to Camera (Local rotation is key here)
        // We add this on top of your existing Mouse Look script
        transform.localRotation = Quaternion.Euler(currentRotation);
    }

    public void ApplyRecoil()
    {
        // Kick UP (negative X) and Randomly Left/Right (Y)
        float xRecoil = -recoilAmount.x; 
        float yRecoil = Random.Range(-recoilAmount.y, recoilAmount.y);

        targetRotation += new Vector3(xRecoil, yRecoil, 0);
    }
}