using Unity.AI.Navigation;
using UnityEngine;

namespace Prison
{
    /// <summary>
    /// Gates a doorway's <see cref="NavMeshLink"/> on the cell-door schedule: the link (the only
    /// NavMesh connection between a cell interior and the corridor) is active exactly when the
    /// door's schedule phase is OPEN, so agents path through open doorways and never through
    /// closed ones. Installed next to each <see cref="CellDoorController"/> by the collision
    /// fixer pass; the link object stays static while the door panel slides.
    /// </summary>
    [RequireComponent(typeof(NavMeshLink))]
    public class CellDoorNavMeshLink : MonoBehaviour
    {
        [Tooltip("The scheduled door that owns this doorway. Auto-found on the parent if null.")]
        public CellDoorController door;

        private NavMeshLink _link;

        private void Awake()
        {
            _link = GetComponent<NavMeshLink>();
            if (door == null)
                door = GetComponentInParent<CellDoorController>();
        }

        private void Update()
        {
            if (_link == null) return;

            bool open = true; // no schedule (menu/test scenes) → never trap agents
            var tm = PrisonTimeManager.Instance;
            if (door != null)
                open = door.ShouldBeOpen(tm != null ? tm.CurrentEvent : PrisonEventType.Breakfast);
            else if (tm != null)
                open = CellDoorController.IsOpenPhase(tm.CurrentEvent);

            if (_link.enabled != open)
                _link.enabled = open;
        }
    }
}
