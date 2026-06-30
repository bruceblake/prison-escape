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
            foreach (var pc in Object.FindObjectsByType<PrisonerController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                buffer.Add(pc);
            foreach (var ai in Object.FindObjectsByType<PrisonerAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                buffer.Add(ai);
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
