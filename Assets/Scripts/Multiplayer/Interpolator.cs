using System.Collections.Generic;
using UnityEngine;

public class Interpolator : MonoBehaviour
{
    [SerializeField] private float timeElapsed = 0f;
    [SerializeField] private float timeToReachTarget = 0.05f;
    [SerializeField] private float movementThreshold = 0.05f;

    private readonly List<TransformUpdate> futureTransformUpdates = new List<TransformUpdate>();

    private float squareMovementThreshold;
    private TransformUpdate to;
    private TransformUpdate from;
    private TransformUpdate previous;

    private void Start()
    {
        squareMovementThreshold = movementThreshold * movementThreshold;
        to = new TransformUpdate(NetworkManager.Singleton.ServerTick, false, transform.position);
        from = new TransformUpdate(NetworkManager.Singleton.InterpolationTick, false, transform.position);
        previous = new TransformUpdate(NetworkManager.Singleton.InterpolationTick, false, transform.position);
    }

    private void Update()
    {
        for (int i = 0; i < futureTransformUpdates.Count; i++)
        {
            if (NetworkManager.Singleton.ServerTick >= futureTransformUpdates[i].Tick)
            {
                if (futureTransformUpdates[i].IsTeleport)
                {
                    to = futureTransformUpdates[i];
                    from = to;
                    previous = to;
                    transform.position = to.Position;
                }
                else
                {
                    previous = to;
                    to = futureTransformUpdates[i];
                    from = previous;
                }

                futureTransformUpdates.RemoveAt(i);
                i--;
                timeElapsed = 0f;
                timeToReachTarget = NetworkManager.Singleton.TicksBetweenPositionUpdates * Time.fixedDeltaTime;
            }
        }

        timeElapsed += Time.deltaTime;
        InterpolatePosition(timeElapsed / timeToReachTarget);
    }

    private void InterpolatePosition(float lerpAmount)
    {
        transform.position = Vector3.Lerp(from.Position, to.Position, Mathf.Clamp01(lerpAmount));
    }

    public void NewUpdate(ushort tick, bool isTeleport, Vector3 position)
    {
        if (tick <= NetworkManager.Singleton.InterpolationTick && !isTeleport)
            return;

        for (int i = 0; i < futureTransformUpdates.Count; i++)
        {
            if (tick < futureTransformUpdates[i].Tick)
            {
                futureTransformUpdates.Insert(i, new TransformUpdate(tick, isTeleport, position));
                return;
            }
        }

        futureTransformUpdates.Add(new TransformUpdate(tick, isTeleport, position));
    }
}