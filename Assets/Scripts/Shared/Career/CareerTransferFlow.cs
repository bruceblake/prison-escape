using UnityEngine;

namespace Prison.Career
{
    /// <summary>Run stats snapshot shown on the ceremony screen (kept from the v1 end screen).</summary>
    public struct EscapeRunStats
    {
        public int daysInside;
        public string playTime;
        public int timesInSolitary;
        public int itemsCrafted;
        public string reputation;
    }

    /// <summary>
    /// Orchestrates what happens when a run at a facility *succeeds* — boundary cross or served
    /// sentence: sync carry, apply the transfer to the world, save atomically, then hand the
    /// result to the ceremony screen. Sandbox and non-career runs fall through to a ceremony
    /// with no world writes (the sandbox reads carry but never writes back).
    /// </summary>
    public static class CareerTransferFlow
    {
        /// <summary>Called by EscapeManager when the player crosses the outer boundary.</summary>
        public static void OnBoundaryCrossed(EscapeRunStats stats)
        {
            if (!CareerSession.HasActiveLadderRun)
            {
                EscapeEndScreenUI.ShowSandboxEscape(stats, backToHub: CareerSession.HasActiveRun);
                return;
            }
            CompleteActiveRun(escaped: true, stats);
        }

        /// <summary>Called by the sentence clock at the Morning Count that completes the sentence.</summary>
        public static void CompleteSentence()
        {
            if (!CareerSession.HasActiveLadderRun) return;

            FreezeForCeremony();
            var stats = EscapeManager.Instance != null
                ? EscapeManager.Instance.BuildRunStats()
                : new EscapeRunStats { daysInside = CareerSession.ActiveRun.day - 1, playTime = "—", reputation = "—" };
            CompleteActiveRun(escaped: false, stats);
        }

        private static void CompleteActiveRun(bool escaped, EscapeRunStats stats)
        {
            var world = CareerSession.ActiveWorld;

            CareerSession.SyncGlobalsFromScene();
            int itemsConfiscated = CountConfiscatedItems();

            var result = CareerTransfer.Complete(world, escaped, itemsConfiscated);
            world.lastPlayedUtc = CareerWorld.UtcNowString();
            CareerWorldStore.Save(world);

            Debug.Log($"[CareerTransferFlow] {result.kind}: {result.fromFacilityId} → " +
                      $"{result.nextFacilityId ?? "(career win)"} | +{result.respectAwarded} respect, " +
                      $"${result.cashCarried} carried, {result.itemsConfiscated} items confiscated.");

            EscapeEndScreenUI.ShowTransfer(result, stats);
        }

        /// <summary>Everything on the player at transfer time — carried slots and pillow stashes.</summary>
        private static int CountConfiscatedItems()
        {
            int count = 0;

            var inventory = Object.FindAnyObjectByType<PlayerInventory>();
            if (inventory != null)
                foreach (var slot in inventory.inventorySlots)
                    if (slot != null && !slot.IsEmpty)
                        count += slot.quantity;

            foreach (var stash in Object.FindObjectsByType<PillowStash>(FindObjectsSortMode.None))
                if (stash != null && stash.StoredItem != null)
                    count++;

            return count;
        }

        private static void FreezeForCeremony()
        {
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
