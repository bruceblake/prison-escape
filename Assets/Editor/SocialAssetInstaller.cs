using System.IO;
using Prison.Social;
using UnityEditor;
using UnityEngine;

namespace Prison.EditorTools
{
    /// <summary>
    /// Authors the designer-facing social tuning assets (spec "Data &amp; tuning"):
    /// ArchetypeDefinitions and GangDefinitions under Resources/Social so designers can
    /// tweak trait ranges, stock sizes, and territory without touching code. Runtime falls
    /// back to the code catalogs when these assets are absent, so this is optional.
    /// Menu: Tools → Prison → Social → Install Social Assets.
    /// </summary>
    public static class SocialAssetInstaller
    {
        private const string ArchetypeDir = "Assets/Resources/Social/Archetypes";
        private const string GangDir = "Assets/Resources/Social/Gangs";

        [MenuItem("Tools/Prison/Social/Install Social Assets")]
        public static void Install()
        {
            EnsureFolder(ArchetypeDir);
            EnsureFolder(GangDir);

            int created = 0;
            foreach (PrisonerArchetype archetype in System.Enum.GetValues(typeof(PrisonerArchetype)))
            {
                string path = $"{ArchetypeDir}/{archetype}.asset";
                if (AssetDatabase.LoadAssetAtPath<ArchetypeDefinition>(path) != null) continue;

                var profile = ArchetypeCatalog.CreateDefault(archetype);
                var asset = ScriptableObject.CreateInstance<ArchetypeDefinition>();
                asset.archetype = archetype;
                asset.aggressionRange = new Vector2Int(profile.aggressionMin, profile.aggressionMax);
                asset.loyaltyRange = new Vector2Int(profile.loyaltyMin, profile.loyaltyMax);
                asset.greedRange = new Vector2Int(profile.greedMin, profile.greedMax);
                asset.sociabilityRange = new Vector2Int(profile.sociabilityMin, profile.sociabilityMax);
                asset.nerveRange = new Vector2Int(profile.nerveMin, profile.nerveMax);
                asset.blurb = profile.blurb;
                asset.snitchBaseChance = profile.snitchBaseChance;
                asset.stockCountRange = new Vector2Int(profile.stockMin, profile.stockMax);
                asset.favoredGiftCategories.AddRange(profile.favoredGiftCategories);
                AssetDatabase.CreateAsset(asset, path);
                created++;
            }

            created += InstallGang("Vipers", GangCatalog.VipersId);
            created += InstallGang("Syndicate", GangCatalog.SyndicateId);

            AssetDatabase.SaveAssets();
            ArchetypeCatalog.ResetCache();
            GangCatalog.ResetCache();
            Debug.Log($"[SocialAssetInstaller] Done — {created} asset(s) created (existing assets left untouched).");
        }

        private static int InstallGang(string name, int gangId)
        {
            string path = $"{GangDir}/{name}.asset";
            if (AssetDatabase.LoadAssetAtPath<GangDefinition>(path) != null) return 0;

            GangCatalog.ResetCache();
            var profile = GangCatalog.All()[gangId];
            var asset = ScriptableObject.CreateInstance<GangDefinition>();
            asset.displayName = profile.displayName;
            asset.flavor = profile.flavor;
            asset.territoryZone = profile.territoryZone;
            asset.territoryLabel = profile.territoryLabel;
            asset.hasStore = profile.hasStore;
            AssetDatabase.CreateAsset(asset, path);
            return 1;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            Directory.CreateDirectory(path);
            AssetDatabase.Refresh();
        }
    }
}
