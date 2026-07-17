using UnityEngine;

namespace Prison.Social
{
    /// <summary>
    /// Binds a spawned guard to its social identity and applies guard-archetype modifiers
    /// (spec §2): Rookie vision cone 90° → 75°, Veteran proximity spot 6 → 8 m and immune to
    /// the Distraction favor, Corrupt guards are bribable once discovered.
    /// Added at runtime by <see cref="SocialWorld.BuildWorld"/>.
    /// </summary>
    public class GuardSocialProfile : MonoBehaviour
    {
        public NPCIdentity Identity { get; private set; }
        public int ActorId => Identity?.actorId ?? SocialTuning.NoActor;
        public GuardArchetype Archetype => Identity?.guardArchetype ?? GuardArchetype.ByTheBook;

        public bool IsCorrupt => Archetype == GuardArchetype.Corrupt;
        public bool ImmuneToDistraction => Archetype == GuardArchetype.Veteran;

        /// <summary>Blind-eye bribe: this guard's detection is off until the schedule phase changes.</summary>
        public bool BlindEyeActive { get; private set; }

        public void Bind(NPCIdentity identity)
        {
            Identity = identity;
            ApplyDetectionModifiers();
        }

        private void OnEnable()
        {
            if (Prison.PrisonTimeManager.Instance != null)
                Prison.PrisonTimeManager.Instance.OnEventChanged += OnPhaseChanged;
        }

        private void OnDisable()
        {
            if (Prison.PrisonTimeManager.Instance != null)
                Prison.PrisonTimeManager.Instance.OnEventChanged -= OnPhaseChanged;
        }

        private void OnPhaseChanged(Prison.PrisonEventType phase)
        {
            BlindEyeActive = false;
        }

        public void ActivateBlindEye()
        {
            BlindEyeActive = true;
        }

        private void ApplyDetectionModifiers()
        {
            var detection = GetComponent<GuardDetection>();
            if (detection == null) return;
            switch (Archetype)
            {
                case GuardArchetype.Rookie:
                    detection.coneAngle = SocialTuning.RookieConeAngle;
                    break;
                case GuardArchetype.Veteran:
                    detection.proximitySpotDistance = SocialTuning.VeteranProximitySpot;
                    break;
            }
        }
    }
}
