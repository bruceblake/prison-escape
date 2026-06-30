using UnityEngine;

public class BulletTracer : MonoBehaviour {
    public void Init(Vector3 start, Vector3 end) {
        var lr = GetComponent<LineRenderer>();
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        Destroy(gameObject, 0.5f); // Disappear after 0.5s
    }
}