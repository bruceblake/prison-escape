using System.Collections.Generic;
using UnityEngine;

namespace Prison
{
    /// <summary>
    /// Live registry of active guards, maintained by the instances themselves in
    /// OnEnable/OnDisable. Mirrors <see cref="PrisonerRegistry"/> and replaces the
    /// per-frame <c>FindObjectsByType&lt;GuardDetection&gt;</c> scan the heat HUD used
    /// to run (a full scene walk plus a fresh array allocation every frame).
    /// </summary>
    public static class GuardRegistry
    {
        private static readonly List<GuardDetection> _guards = new List<GuardDetection>(16);

        /// <summary>Active guard detectors. Do not mutate; may contain destroyed entries mid-frame.</summary>
        public static IReadOnlyList<GuardDetection> Guards => _guards;

        public static void Register(GuardDetection guard)
        {
            if (guard != null && !_guards.Contains(guard))
                _guards.Add(guard);
        }

        public static void Unregister(GuardDetection guard)
        {
            _guards.Remove(guard);
        }

        /// <summary>
        /// Statics survive domain reloads being disabled (Enter Play Mode Options) and would
        /// otherwise carry destroyed instances from the previous play session into the next.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlayMode()
        {
            _guards.Clear();
        }
    }
}
