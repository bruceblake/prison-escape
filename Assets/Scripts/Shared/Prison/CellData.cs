using UnityEngine;

namespace Prison
{
    [System.Serializable]
    public class CellData
    {
        [Tooltip("Where the prisoner spawns")]
        public Transform spawnPoint;
        [Tooltip("Where to stand during roll call (optional, uses spawnPoint if null)")]
        public Transform rollCallStandPoint;

        [Header("Night bed check")]
        [Tooltip("Where the night verifier guard stands at the cell door (defaults to rollCallStandPoint then spawn)")]
        public Transform nightCheckApproachPoint;
        [Tooltip("Center for bed presence overlap (player or fake dummy). Defaults to spawn position.")]
        public Transform bedPresenceCenter;
        [Tooltip("Radius for bed presence + shakedown pickup sphere")]
        public float interiorCheckRadius = 2.5f;

        [Header("Morning shakedown")]
        [Tooltip("Optional separate center for contraband sweep; if null, uses bedPresenceCenter then spawn")]
        public Transform shakedownSweepCenter;

        public Vector3 SpawnPosition => spawnPoint != null ? spawnPoint.position : Vector3.zero;
        public Quaternion SpawnRotation => spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;
        public Vector3 RollCallPosition => (rollCallStandPoint != null ? rollCallStandPoint : spawnPoint) != null
            ? (rollCallStandPoint != null ? rollCallStandPoint.position : spawnPoint.position)
            : Vector3.zero;

        public Transform NightCheckApproachTransform =>
            nightCheckApproachPoint != null ? nightCheckApproachPoint
            : rollCallStandPoint != null ? rollCallStandPoint
            : spawnPoint;

        public Vector3 BedPresenceWorldCenter =>
            bedPresenceCenter != null ? bedPresenceCenter.position : SpawnPosition;

        public Vector3 ShakedownSweepWorldCenter =>
            shakedownSweepCenter != null ? shakedownSweepCenter.position : BedPresenceWorldCenter;

        public float InteriorRadius => interiorCheckRadius > 0.05f ? interiorCheckRadius : 2.5f;
    }
}
