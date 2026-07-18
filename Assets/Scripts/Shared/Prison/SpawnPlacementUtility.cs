using UnityEngine;
using UnityEngine.AI;

namespace Prison
{
    /// <summary>
    /// Raycast / NavMesh helpers for placing characters and world pickups on walkable floors.
    /// </summary>
    public static class SpawnPlacementUtility
    {
        public const float PickupFloorOffset = 0.12f;
        public const float CharacterFloorOffset = 0.02f;

        /// <summary>Target longest axis (meters) for world pickups after spawn scaling.</summary>
        public const float WorldPickupTargetSize = 0.4f;

        public static bool TrySnapToFloor(Vector3 near, out Vector3 snapped, float rayStartHeight = 8f, float maxRayDistance = 30f)
        {
            if (TrySnapToLowestFloor(near, rayStartHeight, maxRayDistance, out snapped))
                return true;

            if (NavMesh.SamplePosition(near, out NavMeshHit navHit, 6f, NavMesh.AllAreas))
            {
                snapped = navHit.position;
                return true;
            }

            snapped = near;
            return false;
        }

        /// <summary>
        /// Picks the lowest physics hit below <paramref name="near"/> so roof/deck geometry
        /// above cell floors is not chosen by mistake.
        /// </summary>
        public static bool TrySnapToLowestFloor(Vector3 near, float rayStartHeight, float maxRayDistance, out Vector3 snapped)
        {
            Vector3 origin = near + Vector3.up * rayStartHeight;
            float maxDist = maxRayDistance + rayStartHeight;
            RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, maxDist, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (hits == null || hits.Length == 0)
            {
                snapped = near;
                return false;
            }

            RaycastHit best = hits[0];
            for (int i = 1; i < hits.Length; i++)
            {
                if (hits[i].point.y < best.point.y)
                    best = hits[i];
            }

            snapped = best.point;
            return true;
        }

        public static bool TrySnapToLowestFloor(Vector3 near, out Vector3 snapped)
        {
            return TrySnapToLowestFloor(near, 2f, 12f, out snapped);
        }

        public static void WarpNavMeshAgent(NavMeshAgent agent, Vector3 near)
        {
            if (agent == null) return;

            // Cell-floor spawns (~Y0.72): never warp onto the roof deck navmesh (~Y7.5) above.
            if (near.y <= 1.25f)
            {
                if (NavMesh.SamplePosition(near, out NavMeshHit floorHit, 2f, NavMesh.AllAreas)
                    && floorHit.position.y <= near.y + 1f)
                {
                    agent.Warp(floorHit.position);
                    return;
                }

                agent.Warp(near);
                return;
            }

            float sampleRadius = 8f;
            if (NavMesh.SamplePosition(near, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas))
                agent.Warp(hit.position);
        }

        public static Vector3 SnapPickupPosition(Vector3 near)
        {
            // Layout anchors sit near floor Y (~0.6–0.72). Prefer NavMesh / nearest-to-anchor
            // hits — TrySnapToLowestFloor can bury pickups under slabs or basements.
            if (near.y <= 1.5f)
            {
                if (NavMesh.SamplePosition(near, out NavMeshHit navHit, 5f, NavMesh.AllAreas)
                    && navHit.position.y <= 2.5f
                    && Mathf.Abs(navHit.position.y - near.y) <= 2f)
                {
                    return navHit.position + Vector3.up * PickupFloorOffset;
                }

                Vector3 origin = near + Vector3.up * 2.5f;
                RaycastHit[] hits = Physics.RaycastAll(
                    origin, Vector3.down, 6f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                if (hits != null && hits.Length > 0)
                {
                    RaycastHit best = hits[0];
                    float bestDelta = Mathf.Abs(best.point.y - near.y);
                    for (int i = 1; i < hits.Length; i++)
                    {
                        float y = hits[i].point.y;
                        if (y > 2.5f || y < -0.5f) continue;
                        float d = Mathf.Abs(y - near.y);
                        if (d < bestDelta)
                        {
                            best = hits[i];
                            bestDelta = d;
                        }
                    }

                    if (best.point.y >= -0.5f && best.point.y <= 2.5f)
                        return best.point + Vector3.up * PickupFloorOffset;
                }

                float floorY = Mathf.Max(near.y, PrisonLayoutAnchors.FloorY);
                return new Vector3(near.x, floorY, near.z) + Vector3.up * PickupFloorOffset;
            }

            if (TrySnapToFloor(near, out Vector3 floor))
                return floor + Vector3.up * PickupFloorOffset;
            return near;
        }

        public static Vector3 SnapCharacterPosition(Vector3 near)
        {
            // Spawn points placed on cell floors (~0.72). Trust them — raycasts from +8m hit the roof deck (~7.5).
            if (near.y <= 1.25f)
                return near;

            if (TrySnapToLowestFloor(near, out Vector3 floor))
                return floor + Vector3.up * CharacterFloorOffset;
            return near;
        }

        /// <summary>
        /// Normalizes BlenderKit props to a readable floor footprint, then seats mesh bottom on <paramref name="floorY"/>.
        /// Replaces a flat multiplier so bottles are not huge and paperclips stay visible.
        /// </summary>
        public static void FitWorldPickupOnFloor(GameObject instance, float floorY)
        {
            if (instance == null) return;

            float factor = ComputePickupScaleFactor(instance, WorldPickupTargetSize);
            if (factor > 0f)
                instance.transform.localScale *= factor;

            AlignPickupBottom(instance, floorY);
        }

        public static float ComputePickupScaleFactor(GameObject instance, float targetMaxDimension)
        {
            Bounds bounds = GetRendererBounds(instance);
            if (bounds.size.sqrMagnitude < 1e-8f)
                return 3f;

            float maxExtent = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            return Mathf.Clamp(targetMaxDimension / maxExtent, 1.2f, 4.5f);
        }

        public static Bounds GetRendererBounds(GameObject instance)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
                return new Bounds(instance.transform.position, Vector3.zero);

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        static void AlignPickupBottom(GameObject instance, float targetBottomY)
        {
            Bounds bounds = GetRendererBounds(instance);
            if (bounds.size.sqrMagnitude < 1e-8f) return;

            float delta = targetBottomY - bounds.min.y;
            if (Mathf.Abs(delta) > 0.0001f)
                instance.transform.position += Vector3.up * delta;
        }
    }
}
