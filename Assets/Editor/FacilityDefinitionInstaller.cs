using System.IO;
using Prison.Career;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Creates/refreshes the 10 <see cref="FacilityDefinition"/> assets (dev sandbox + career ladder
/// 0–8) under Resources/Facilities from the canonical <see cref="FacilityCatalog"/> numbers.
/// Existing assets are overwritten with catalog values — the catalog is the design contract;
/// per-asset tuning happens after the numbers move in the design note first.
/// </summary>
public static class FacilityDefinitionInstaller
{
    private const string Folder = "Assets/Resources/Facilities";

    [MenuItem("Tools/Prison/Career/Install Facility Definitions")]
    public static void Install()
    {
        Directory.CreateDirectory(Folder);

        int created = 0, updated = 0;
        foreach (var info in FacilityCatalog.All)
        {
            string path = $"{Folder}/Facility_{info.id}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<FacilityDefinition>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<FacilityDefinition>();
                asset.PopulateFrom(info);
                AssetDatabase.CreateAsset(asset, path);
                created++;
            }
            else
            {
                var icon = asset.icon;
                var silhouette = asset.silhouette;
                asset.PopulateFrom(info);
                asset.icon = icon;               // art assignments survive a re-install
                asset.silhouette = silhouette;
                EditorUtility.SetDirty(asset);
                updated++;
            }
        }

        AssetDatabase.SaveAssets();
        FacilityDirectory.Reset();
        Debug.Log($"[FacilityDefinitionInstaller] Facility definitions installed: {created} created, {updated} refreshed at {Folder}.");
    }
}
