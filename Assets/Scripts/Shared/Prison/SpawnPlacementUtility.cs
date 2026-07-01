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
    }
}
