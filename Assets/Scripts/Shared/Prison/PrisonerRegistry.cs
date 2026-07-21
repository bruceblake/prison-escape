using System.Collections.Generic;
using UnityEngine;

namespace Prison
{
    /// <summary>
    /// Live registry of active prisoners, maintained by the instances themselves in
    /// OnEnable/OnDisable. Replaces per-frame <c>FindObjectsByType</c> scans in guard
    /// detection and roll-call headcounts (those were O(guards × prisoners) per frame
    /// plus a fresh array allocation on every call).
    /// </summary>
    public static class PrisonerRegistry
    {
        private static readonly List<PrisonerController> _players = new List<PrisonerController>(4);
        private static readonly List<PrisonerAI> _npcs = new List<PrisonerAI>(32);

        /// <summary>Active player-driven prisoners. Do not mutate; may contain destroyed entries mid-frame.</summary>
        public static IReadOnlyList<PrisonerController> Players => _players;

        /// <summary>Active NPC prisoners. Do not mutate; may contain destroyed entries mid-frame.</summary>
        public static IReadOnlyList<PrisonerAI> Npcs => _npcs;

        public static void Register(PrisonerController prisoner)
        {
            if (prisoner != null && !_players.Contains(prisoner))
                _players.Add(prisoner);
        }

        public static void Unregister(PrisonerController prisoner)
        {
            _players.Remove(prisoner);
        }

        public static void Register(PrisonerAI prisoner)
        {
            if (prisoner != null && !_npcs.Contains(prisoner))
                _npcs.Add(prisoner);
        }

        public static void Unregister(PrisonerAI prisoner)
        {
            _npcs.Remove(prisoner);
        }

        /// <summary>
        /// Statics survive domain reloads being disabled (Enter Play Mode Options) and would
        /// otherwise carry destroyed instances from the previous play session into the next.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlayMode()
        {
            _players.Clear();
            _npcs.Clear();
        }
    }
}
