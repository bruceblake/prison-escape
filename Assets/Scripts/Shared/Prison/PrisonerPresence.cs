using System.Collections.Generic;
using UnityEngine;

namespace Prison
{
    /// <summary>Collects local <see cref="IPrisoner"/> instances for roll call / headcount.</summary>
    public static class PrisonerPresence
    {
        public static void GetAllPrisoners(List<IPrisoner> buffer)
        {
            buffer.Clear();
            var players = PrisonerRegistry.Players;
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i] != null)
                    buffer.Add(players[i]);
            }

            var npcs = PrisonerRegistry.Npcs;
            for (int i = 0; i < npcs.Count; i++)
            {
                if (npcs[i] != null)
                    buffer.Add(npcs[i]);
            }
        }

        public static int CountAccountedFor(out int total)
        {
            var list = new List<IPrisoner>(16);
            GetAllPrisoners(list);
            total = list.Count;
            int ok = 0;
            foreach (var p in list)
            {
                if (p != null && p.IsAtRequiredLocation)
                    ok++;
            }
            return ok;
        }

        public static bool AreAllAccountedFor()
        {
            int ok = CountAccountedFor(out int total);
            return total > 0 && ok == total;
        }
    }
}
