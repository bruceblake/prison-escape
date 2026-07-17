using UnityEngine;

namespace Prison.Social
{
    /// <summary>
    /// Publishes player crimes into the social memory web (spec §4: contraband visible,
    /// restricted-zone entry, vent tampering, theft). Vent tampering hooks directly in
    /// <see cref="VentCover"/>; this component polls the rest. One report per crime per phase
    /// keeps memories meaningful instead of spammy.
    /// </summary>
    public class CrimeSignals : MonoBehaviour
    {
        private bool _contrabandReportedThisPhase;
        private bool _restrictedReportedThisPhase;
        private float _nextPoll;
        private PrisonerController _player;
        private PlayerInventory _inventory;

        private void OnEnable()
        {
            WorldItemPickup.OnPlayerPickedUp += OnPickedUp;
            if (Prison.PrisonTimeManager.Instance != null)
                Prison.PrisonTimeManager.Instance.OnEventChanged += OnPhaseChanged;
        }

        private void OnDisable()
        {
            WorldItemPickup.OnPlayerPickedUp -= OnPickedUp;
            if (Prison.PrisonTimeManager.Instance != null)
                Prison.PrisonTimeManager.Instance.OnEventChanged -= OnPhaseChanged;
        }

        private void OnPhaseChanged(PrisonEventType phase)
        {
            _contrabandReportedThisPhase = false;
            _restrictedReportedThisPhase = false;
        }

        private void Update()
        {
            if (Time.time < _nextPoll) return;
            _nextPoll = Time.time + 2f;

            var world = SocialWorld.Instance;
            if (world == null || !world.IsBuilt || world.PlayerTransform == null) return;

            if (_player == null) _player = FindAnyObjectByType<PrisonerController>();
            if (_inventory == null) _inventory = world.PlayerTransform.GetComponentInChildren<PlayerInventory>();

            // Contraband in hand where people can see it.
            if (!_contrabandReportedThisPhase && _inventory != null)
            {
                var held = _inventory.GetEquippedItem();
                if (held != null && held.category == ItemCategory.Contraband)
                {
                    _contrabandReportedThisPhase = true;
                    world.PublishPlayerCrime(world.PlayerTransform.position);
                }
            }

            // Somewhere you're not supposed to be.
            if (!_restrictedReportedThisPhase && _player != null && _player.IsInActiveRestrictedZone)
            {
                _restrictedReportedThisPhase = true;
                world.PublishPlayerCrime(world.PlayerTransform.position);
            }
        }

        /// <summary>Taking things out of someone else's cell is theft — if anyone saw it.</summary>
        private void OnPickedUp(Vector3 position, ItemData item)
        {
            var world = SocialWorld.Instance;
            if (world == null || !world.IsBuilt) return;
            var registry = Prison.PrisonLocationRegistry.Instance;
            if (registry == null) return;

            int playerCell = world.CellOf(SocialTuning.PlayerActorId);
            for (int i = 0; i < registry.CellCount; i++)
            {
                if (i == playerCell) continue;
                var cell = registry.GetCell(i);
                if (cell == null) continue;
                Vector3 center = cell.BedPresenceWorldCenter;
                if (Vector3.Distance(new Vector3(position.x, center.y, position.z), center) > cell.InteriorRadius)
                    continue;

                world.PublishPlayerCrime(position);

                // The cell's owner takes it personally if they know (they hold the memory via witnessing/gossip).
                foreach (var inmate in world.Roster.Inmates())
                {
                    if (inmate.cellIndex != i) continue;
                    var pos = world.PositionOf(inmate.actorId);
                    if (pos != null && Vector3.Distance(pos.Value, position) <= SocialTuning.WitnessRadius)
                        world.ApplyPlayerAct(inmate.actorId, SocialEventType.CaughtStealing);
                }
                break;
            }
        }
    }
}
