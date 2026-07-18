using UnityEngine;

namespace Prison.Social
{
    /// <summary>
    /// Blocks the Talk menu when an inmate is mid mandatory travel or the player is
    /// schedule-non-compliant (guards only). Shows a short bark via toast instead.
    /// </summary>
    public static class SocialTalkGate
    {
        public static bool TryGetRefusal(GameObject targetGo, out string toastLine)
        {
            toastLine = null;
            if (targetGo == null)
                return false;

            var world = SocialWorld.Instance;
            if (world == null || !world.IsBuilt)
                return false;

            int actorId = world.GetActorId(targetGo);
            var identity = world.GetIdentity(actorId);
            if (identity == null)
                return false;

            int hash = actorId * 31 + (PrisonTimeManager.Instance?.CurrentEntryIndex ?? 0);

            if (identity.isGuard)
            {
                if (!IsPlayerNonCompliantForGuardTalk())
                    return false;

                toastLine = DialogueLibrary.GuardRefusePlayerNonCompliance(identity, hash);
                return true;
            }

            var inmate = targetGo.GetComponent<PrisonerAI>();
            if (inmate == null || !inmate.IsBusyForTalk)
                return false;

            toastLine = DialogueLibrary.InmateRefuseMandatoryTravel(identity, hash);
            return true;
        }

        public static bool IsPlayerNonCompliantForGuardTalk()
        {
            var player = Object.FindAnyObjectByType<PrisonerController>();
            if (player == null || player.MovementBlocked)
                return true;

            var tm = PrisonTimeManager.Instance;
            if (tm == null)
                return false;

            if (!PrisonEventRules.IsMandatory(tm.CurrentEvent))
                return false;

            if (tm.IsMandatoryTravelGraceActive)
                return false;

            if (player.HasPostEscortImmunity)
                return false;

            return !player.IsCompliant;
        }
    }
}
