using UnityEngine;

namespace Prison.Social
{
    /// <summary>
    /// Territory warn-offs (spec §5): entering a gang's claim zone as a non-member with
    /// gang standing &lt; 0 triggers an ambient bark once per phase; a nearby Soldier may
    /// intimidate once per phase. Members use the territory freely.
    /// </summary>
    public class GangTerritoryMonitor : MonoBehaviour
    {
        private readonly bool[] _warnedThisPhase = new bool[GangCatalog.GangCount];
        private readonly bool[] _soldierPushThisPhase = new bool[GangCatalog.GangCount];

        private void OnEnable()
        {
            PrisonerController.OnPlayerZoneChanged += OnZoneChanged;
            if (Prison.PrisonTimeManager.Instance != null)
                Prison.PrisonTimeManager.Instance.OnEventChanged += OnPhaseChanged;
        }

        private void OnDisable()
        {
            PrisonerController.OnPlayerZoneChanged -= OnZoneChanged;
            if (Prison.PrisonTimeManager.Instance != null)
                Prison.PrisonTimeManager.Instance.OnEventChanged -= OnPhaseChanged;
        }

        private void OnPhaseChanged(PrisonEventType phase)
        {
            for (int i = 0; i < GangCatalog.GangCount; i++)
            {
                _warnedThisPhase[i] = false;
                _soldierPushThisPhase[i] = false;
            }
        }

        private void OnZoneChanged(PrisonLocationZone zone, bool entered)
        {
            if (!entered || zone == null) return;
            var world = SocialWorld.Instance;
            if (world == null || !world.IsBuilt) return;

            foreach (var gang in GangCatalog.All())
            {
                if (gang.territoryZone != zone.zoneType) continue;
                if (world.Gangs.IsMemberOf(gang.gangId)) continue;
                if (world.Gangs.GangStanding(gang.gangId) >= 0f) continue;

                if (!_warnedThisPhase[gang.gangId])
                {
                    _warnedThisPhase[gang.gangId] = true;
                    int hash = world.CurrentDay * 13 + gang.gangId;
                    SocialToastUI.Show(DialogueLibrary.TerritoryWarnOff(gang.displayName, hash));
                }
                else if (!_soldierPushThisPhase[gang.gangId])
                {
                    // Second incursion in the same phase: a Soldier steps to you.
                    _soldierPushThisPhase[gang.gangId] = true;
                    TrySoldierIntimidation(world, gang.gangId);
                }
            }
        }

        private void TrySoldierIntimidation(SocialWorld world, int gangId)
        {
            foreach (var member in world.Roster.MembersOf(gangId))
            {
                if (member.archetype != PrisonerArchetype.Soldier) continue;
                var go = world.GetActorObject(member.actorId);
                if (go == null || !go.activeInHierarchy) continue;

                SocialToastUI.Show($"{member.DisplayName} squares up: \"Last warning.\"");
                // Their move, not yours: your respect toward the gang takes the hit.
                world.Relationships.ApplyDeltas(member.actorId, SocialTuning.PlayerActorId,
                    0f, -5f, member.traits);
                return;
            }
        }
    }
}
