using UnityEngine;

namespace Prison.Social
{
    /// <summary>
    /// Designer-facing gang tuning: identity, territory, and store behavior.
    /// Runtime falls back to <see cref="GangCatalog"/> code defaults (Vipers &amp; Syndicate,
    /// working names per spec) when no assets exist. Assets live in <c>Resources/Social/Gangs</c>.
    /// </summary>
    [CreateAssetMenu(fileName = "Gang", menuName = "Prison/Social/Gang Definition")]
    public class GangDefinition : ScriptableObject
    {
        public string displayName = "Gang";
        [TextArea] public string flavor = "";
        [Tooltip("Zone this gang claims (warn-offs for non-members with standing < 0).")]
        public ZoneType territoryZone = ZoneType.Yard;
        public string territoryLabel = "";
        [Tooltip("Members can buy from the gang store (delivers under the bed after morning count).")]
        public bool hasStore;

        public GangProfile ToProfile(int gangId)
        {
            return new GangProfile
            {
                gangId = gangId,
                displayName = displayName,
                flavor = flavor,
                territoryZone = territoryZone,
                territoryLabel = territoryLabel,
                hasStore = hasStore,
            };
        }
    }

    /// <summary>Plain (asset-free, testable) gang data.</summary>
    public class GangProfile
    {
        public int gangId;
        public string displayName = "";
        public string flavor = "";
        public ZoneType territoryZone;
        public string territoryLabel = "";
        public bool hasStore;
    }

    /// <summary>Code-default gangs for Minimum Security / Dev Sandbox (spec §5).</summary>
    public static class GangCatalog
    {
        public const int VipersId = 0;
        public const int SyndicateId = 1;
        public const int GangCount = 2;

        private static GangProfile[] _profiles;

        public static GangProfile Get(int gangId)
        {
            var all = All();
            return gangId >= 0 && gangId < all.Length ? all[gangId] : null;
        }

        public static GangProfile[] All()
        {
            if (_profiles != null) return _profiles;

            GangDefinition vipersAsset = null;
            GangDefinition syndicateAsset = null;
            try
            {
                vipersAsset = Resources.Load<GangDefinition>("Social/Gangs/Vipers");
                syndicateAsset = Resources.Load<GangDefinition>("Social/Gangs/Syndicate");
            }
            catch (System.Exception)
            {
                // Unity icall unavailable outside the engine (pure unit tests) — use code defaults.
            }

            _profiles = new[]
            {
                vipersAsset != null ? vipersAsset.ToProfile(VipersId) : new GangProfile
                {
                    gangId = VipersId,
                    displayName = "Vipers",
                    flavor = "Muscle. Respect-first culture — earn it or stay out of the pit.",
                    territoryZone = ZoneType.Yard,
                    territoryLabel = "Yard — weight pit corner",
                    hasStore = false,
                },
                syndicateAsset != null ? syndicateAsset.ToProfile(SyndicateId) : new GangProfile
                {
                    gangId = SyndicateId,
                    displayName = "Syndicate",
                    flavor = "Smugglers and traders. Trust-and-greed culture — everything has a price.",
                    territoryZone = ZoneType.Cafeteria,
                    territoryLabel = "Cafeteria — back corner tables",
                    hasStore = true,
                },
            };
            return _profiles;
        }

        public static void ResetCache() => _profiles = null;

        public static int RivalOf(int gangId) =>
            gangId == VipersId ? SyndicateId : (gangId == SyndicateId ? VipersId : SocialTuning.IndependentGangId);
    }
}
