using System.Collections.Generic;
using System.Linq;
using Prison;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Generates BlenderKit prefabs, assigns URP materials, and wires item worldPrefab fields.
/// Menu: Prison → Assets → Setup BlenderKit
/// </summary>
public static class BlenderKitAssetSetup
{
    static readonly HashSet<string> StructuralAssets = new()
    {
        "SM_Wall_Straight_1m", "SM_Wall_Straight_2m", "SM_Wall_Straight_4m",
        "SM_Wall_Doorway_4m", "SM_Wall_Corner_1m", "SM_Wall_TFiller",
        "SM_Floor_Tile_2m", "SM_Floor_Tile_4m",
        "SM_Roof_Slab_4m", "SM_Roof_Edge_4m",
        "SM_Cell_Shell_4x5-5m", "SM_Solitary_Shell_3x4m",
        "SM_Fence_Panel_4m", "SM_Fence_Panel_4m_Cut", "SM_Fence_Gate_4m",
        "SM_Fence_Post", "SM_Fence_CornerPost",
        "SM_Pillar_Concrete",
    };

    static readonly Dictionary<string, string> TextureToMaterial = new()
    {
        { "T_ConcreteWall", "Assets/Materials/Prison/PrisonWall_Concrete.mat" },
        { "T_ConcreteCeiling", "Assets/Materials/Prison/PrisonCeiling_Concrete.mat" },
        { "T_FloorTile", "Assets/Materials/Prison/PrisonFloor_Tile.mat" },
        { "T_CafeteriaTile", "Assets/Materials/Prison/PrisonFloor_CafeteriaTile.mat" },
        { "T_MetalShelf", "Assets/Materials/Prison/PrisonMetal_Shelf.mat" },
        { "T_MetalBars", "Assets/Materials/Prison/PrisonBars_Metal.mat" },
        { "T_Porcelain", "Assets/Materials/Prison/PrisonToilet_Porcelain.mat" },
        { "T_Wood", "Assets/Materials/Prison/PrisonBed_Mattress.mat" },
    };

    static readonly Dictionary<string, string> BlenderMaterialToAsset = new()
    {
        { "M_ConcreteWall", "Assets/Materials/Prison/PrisonWall_Concrete.mat" },
        { "M_Concrete", "Assets/Materials/Prison/PrisonWall_Concrete.mat" },
        { "M_Wall", "Assets/Materials/Prison/PrisonWall_Concrete.mat" },
        { "M_ConcreteCeiling", "Assets/Materials/Prison/PrisonCeiling_Concrete.mat" },
        { "M_Ceiling", "Assets/Materials/Prison/PrisonCeiling_Concrete.mat" },
        { "M_FloorTile", "Assets/Materials/Prison/PrisonFloor_Tile.mat" },
        { "M_Floor", "Assets/Materials/Prison/PrisonFloor_Tile.mat" },
        { "M_CafeteriaTile", "Assets/Materials/Prison/PrisonFloor_CafeteriaTile.mat" },
        { "M_Cafeteria", "Assets/Materials/Prison/PrisonFloor_CafeteriaTile.mat" },
        { "M_MetalShelf", "Assets/Materials/Prison/PrisonMetal_Shelf.mat" },
        { "M_Metal", "Assets/Materials/Prison/PrisonMetal_Shelf.mat" },
        { "M_Sink", "Assets/Materials/Prison/PrisonSink_Metal.mat" },
        { "M_MetalBars", "Assets/Materials/Prison/PrisonBars_Metal.mat" },
        { "M_Bars", "Assets/Materials/Prison/PrisonBars_Metal.mat" },
        { "M_Porcelain", "Assets/Materials/Prison/PrisonToilet_Porcelain.mat" },
        { "M_Wood", "Assets/Materials/Prison/PrisonBed_Mattress.mat" },
        { "M_Mattress", "Assets/Materials/Prison/PrisonBed_Mattress.mat" },
        { "M_Blanket", "Assets/Materials/Prison/PrisonBed_Blanket.mat" },
        { "M_Pillow", "Assets/Materials/Prison/PrisonBed_Pillow.mat" },
        { "M_Emissive", "Assets/Materials/Prison/PrisonLight_Emissive.mat" },
        { "M_Light", "Assets/Materials/Prison/PrisonLight_Emissive.mat" },
        { "M_Char_Skin", "Assets/Materials/Characters/Char_Skin.mat" },
        { "M_Char_Prisoner", "Assets/Materials/Characters/Char_Clothing_Prisoner.mat" },
        { "M_Char_Prisoner_Clothing", "Assets/Materials/Characters/Char_Clothing_Prisoner.mat" },
        { "M_Char_Prisoner_Accent", "Assets/Materials/Characters/Char_Accent_Prisoner.mat" },
        { "M_Char_Prisoner_Boots", "Assets/Materials/Characters/Char_Boots_Prisoner.mat" },
        { "M_Char_Guard", "Assets/Materials/Characters/Char_Clothing_Guard.mat" },
        { "M_Char_Guard_Clothing", "Assets/Materials/Characters/Char_Clothing_Guard.mat" },
        { "M_Char_Guard_Accent", "Assets/Materials/Characters/Char_Accent_Guard.mat" },
        { "M_Char_Guard_Boots", "Assets/Materials/Characters/Char_Boots_Guard.mat" },
        { "M_Grass", "Assets/Materials/Prison/PrisonFloor_Tile.mat" },
        { "M_Outdoor", "Assets/Materials/Prison/PrisonFloor_Tile.mat" },
        { "M_Pipe", "Assets/Materials/Prison/PrisonMetal_Shelf.mat" },
        { "M_Duct", "Assets/Materials/Prison/PrisonMetal_Shelf.mat" },
        { "M_Fence", "Assets/Materials/Prison/PrisonBars_Metal.mat" },
        { "M_Glass", "Assets/Materials/Prison/Accents/Accent_PanelWhite.mat" },
        { "M_Workshop", "Assets/Materials/Prison/PrisonFloor_Tile.mat" },
        { "M_Shower", "Assets/Materials/Prison/PrisonFloor_Tile.mat" },
    };

    [MenuItem("Prison/Assets/Setup BlenderKit")]
    public static void SetupAll()
    {
        EnsureFolders();
        EnsureTexturedMaterials();
        int prefabs = GenerateKitPrefabs();
        int facility = GeneratePrisonFacilityPrefab();
        int items = GenerateItemPrefabsAndWireData();
        int chars = BlenderKitCharacterSetup.GenerateCharacterPrefabs();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[BlenderKit] Setup complete: {prefabs} kit prefabs, facility={facility}, {items} items, {chars} characters.");
    }

    public static void EnsureTexturedMaterials()
    {
        AssignTextureToMaterial("Assets/Models/BlenderKit/Textures/T_ConcreteWall.png",
            "Assets/Materials/Prison/PrisonWall_Concrete.mat", smoothness: 0.08f, metallic: 0f);
        AssignTextureToMaterial("Assets/Models/BlenderKit/Textures/T_ConcreteCeiling.png",
            "Assets/Materials/Prison/PrisonCeiling_Concrete.mat", smoothness: 0.06f, metallic: 0f);
        AssignTextureToMaterial("Assets/Models/BlenderKit/Textures/T_FloorTile.png",
            "Assets/Materials/Prison/PrisonFloor_Tile.mat", smoothness: 0.12f, metallic: 0f);
        AssignTextureToMaterial("Assets/Models/BlenderKit/Textures/T_CafeteriaTile.png",
            "Assets/Materials/Prison/PrisonFloor_CafeteriaTile.mat", smoothness: 0.14f, metallic: 0f);
        AssignTextureToMaterial("Assets/Models/BlenderKit/Textures/T_MetalBars.png",
            "Assets/Materials/Prison/PrisonBars_Metal.mat", smoothness: 0.35f, metallic: 0.65f);
        AssignTextureToMaterial("Assets/Models/BlenderKit/Textures/T_MetalShelf.png",
            "Assets/Materials/Prison/PrisonMetal_Shelf.mat", smoothness: 0.4f, metallic: 0.55f);
        AssignTextureToMaterial("Assets/Models/BlenderKit/Textures/T_MetalShelf.png",
            "Assets/Materials/Prison/PrisonSink_Metal.mat", smoothness: 0.45f, metallic: 0.6f);
        AssignTextureToMaterial("Assets/Models/BlenderKit/Textures/T_Porcelain.png",
            "Assets/Materials/Prison/PrisonToilet_Porcelain.mat", smoothness: 0.25f, metallic: 0f);
        AssignTextureToMaterial("Assets/Models/BlenderKit/Textures/T_Wood.png",
            "Assets/Materials/Prison/PrisonBed_Mattress.mat", smoothness: 0.1f, metallic: 0f);
    }

    static void AssignTextureToMaterial(string texturePath, string materialPath, float smoothness, float metallic)
    {
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        var mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (tex == null || mat == null) return;

        mat.SetTexture("_BaseMap", tex);
        mat.SetTexture("_MainTex", tex);
        mat.SetFloat("_Smoothness", smoothness);
        mat.SetFloat("_Glossiness", smoothness);
        mat.SetFloat("_Metallic", metallic);
        EditorUtility.SetDirty(mat);
    }

    static int GeneratePrisonFacilityPrefab()
    {
        if (!AssetDatabase.LoadAssetAtPath<GameObject>(PrisonFacilityInstaller.FbxPath))
            return 0;

        var source = AssetDatabase.LoadAssetAtPath<GameObject>(PrisonFacilityInstaller.FbxPath);
        var instance = Object.Instantiate(source);
        instance.name = "PrisonFacility";
        RemapMaterials(instance);
        BlenderKitAssetSetup.RemapMaterialsByObjectNamePublic(instance);

        var prefab = PrefabUtility.SaveAsPrefabAsset(instance, PrisonFacilityInstaller.PrefabPath);
        Object.DestroyImmediate(instance);
        return prefab != null ? 1 : 0;
    }

    [MenuItem("Prison/Assets/Wire Item World Prefabs")]
    public static void WireItemPrefabsOnly()
    {
        int items = WireItemScriptableObjects();
        AssetDatabase.SaveAssets();
        Debug.Log($"[BlenderKit] Wired {items} ItemData.worldPrefab references.");
    }

    static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder(BlenderKitCatalog.PrefabRoot))
            AssetDatabase.CreateFolder("Assets/Prefabs", "BlenderKit");
        if (!AssetDatabase.IsValidFolder(BlenderKitCatalog.ItemPrefabRoot))
            AssetDatabase.CreateFolder(BlenderKitCatalog.PrefabRoot, "Items");
    }

    static int GenerateKitPrefabs()
    {
        int count = 0;
        var guids = AssetDatabase.FindAssets("t:Model", new[] { BlenderKitCatalog.FbxRoot });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase)) continue;
            if (path.Contains("/Items/")) continue;
            if (path.Contains("/Characters/")) continue;
            if (path.EndsWith("PrisonFacility.fbx", System.StringComparison.OrdinalIgnoreCase)) continue;

            var name = System.IO.Path.GetFileNameWithoutExtension(path);
            try
            {
                if (CreatePrefabFromFbx(name, path, BlenderKitCatalog.PrefabPath(name), structural: StructuralAssets.Contains(name)))
                    count++;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[BlenderKit] Skipped prefab {name}: {ex.Message}");
            }
        }

        return count;
    }

    static int GenerateItemPrefabsAndWireData()
    {
        int count = 0;
        var guids = AssetDatabase.FindAssets("t:Model", new[] { $"{BlenderKitCatalog.FbxRoot}/Items" });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase)) continue;
            var name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (CreateItemPrefab(name, path))
                count++;
        }

        WireItemScriptableObjects();
        return count;
    }

    static int WireItemScriptableObjects()
    {
        int wired = 0;
        foreach (var kv in BlenderKitCatalog.ItemPrefabToScriptableObject)
        {
            var so = AssetDatabase.LoadAssetAtPath<ItemData>(kv.Value);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BlenderKitCatalog.PrefabPath(kv.Key));
            if (so == null || prefab == null) continue;
            if (so.worldPrefab == prefab) continue;
            so.worldPrefab = prefab;
            EditorUtility.SetDirty(so);
            wired++;
        }

        // Legacy duplicate assets (catalog hygiene debt).
        WireDuplicateItem("Assets/ScriptableObjects/Items/MetalScrap.asset", "SM_Item_MetalScrap", ref wired);
        WireDuplicateItem("Assets/ScriptableObjects/Items/WoodScrap.asset", "SM_Item_WoodScrap", ref wired);

        return wired;
    }

    static void WireDuplicateItem(string assetPath, string prefabName, ref int wired)
    {
        var so = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BlenderKitCatalog.PrefabPath(prefabName));
        if (so == null || prefab == null || so.worldPrefab == prefab) return;
        so.worldPrefab = prefab;
        EditorUtility.SetDirty(so);
        wired++;
    }

    static bool CreatePrefabFromFbx(string name, string fbxPath, string prefabPath, bool structural)
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (source == null) return false;

        var instance = Object.Instantiate(source);
        instance.name = name;
        RemapMaterials(instance);
        ConfigureColliders(instance, structural, isItem: false);

        if (name == BlenderKitLayout.LightFixture)
        {
            try { EnsureLightFixtureComponents(instance); }
            catch (System.Exception ex) { Debug.LogWarning($"[BlenderKit] Light setup skipped on {name}: {ex.Message}"); }
        }

        var prefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        Object.DestroyImmediate(instance);
        return prefab != null;
    }

    static bool CreateItemPrefab(string name, string fbxPath)
    {
        var prefabPath = BlenderKitCatalog.PrefabPath(name);
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (source == null) return false;

        var instance = Object.Instantiate(source);
        instance.name = name;
        RemapMaterials(instance);
        ConfigureColliders(instance, structural: false, isItem: true);

        if (instance.GetComponent<WorldItemPickup>() == null)
            instance.AddComponent<WorldItemPickup>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        Object.DestroyImmediate(instance);
        return prefab != null;
    }

    static void RemapMaterials(GameObject root)
    {
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            var mats = renderer.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                var mat = mats[i];
                if (mat == null) continue;
                var mapped = MapMaterial(mat.name);
                if (mapped != null)
                    mats[i] = mapped;
            }

            renderer.sharedMaterials = mats;
        }
    }

    public static Material MapMaterialPublic(string importedName) => MapMaterial(importedName);

    public static void RemapMaterialsPublic(GameObject root) => RemapMaterials(root);

    public static void RemapMaterialsByObjectNamePublic(GameObject root) => RemapMaterialsByObjectName(root);

    static void RemapMaterialsByObjectName(GameObject root)
    {
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            string objectName = renderer.gameObject.name;
            Material mat = MapMaterialByObjectName(objectName);
            if (mat == null) continue;

            var mats = renderer.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
                mats[i] = mat;
            renderer.sharedMaterials = mats;
        }
    }

    static Material MapMaterialByObjectName(string objectName)
    {
        if (string.IsNullOrEmpty(objectName)) return null;

        if (objectName.Contains("Light", System.StringComparison.OrdinalIgnoreCase)
            || objectName.Contains("Fixture", System.StringComparison.OrdinalIgnoreCase))
            return LoadMat("Assets/Materials/Prison/PrisonLight_Emissive.mat");

        if (objectName.Contains("Door", System.StringComparison.OrdinalIgnoreCase)
            || objectName.Contains("Bars", System.StringComparison.OrdinalIgnoreCase)
            || objectName.Contains("Fence", System.StringComparison.OrdinalIgnoreCase))
            return LoadMat("Assets/Materials/Prison/PrisonBars_Metal.mat");

        if (objectName.Contains("Cafeteria", System.StringComparison.OrdinalIgnoreCase))
            return LoadMat("Assets/Materials/Prison/PrisonFloor_CafeteriaTile.mat");

        if (objectName.Contains("Roof", System.StringComparison.OrdinalIgnoreCase)
            || objectName.Contains("Ceiling", System.StringComparison.OrdinalIgnoreCase)
            || objectName.Contains("Soffit", System.StringComparison.OrdinalIgnoreCase))
            return LoadMat("Assets/Materials/Prison/PrisonCeiling_Concrete.mat");

        if (objectName.Contains("Floor", System.StringComparison.OrdinalIgnoreCase)
            || objectName.StartsWith("ASM_", System.StringComparison.OrdinalIgnoreCase))
            return LoadMat("Assets/Materials/Prison/PrisonFloor_Tile.mat");

        if (objectName.Contains("Toilet", System.StringComparison.OrdinalIgnoreCase))
            return LoadMat("Assets/Materials/Prison/PrisonToilet_Porcelain.mat");

        if (objectName.Contains("Sink", System.StringComparison.OrdinalIgnoreCase))
            return LoadMat("Assets/Materials/Prison/PrisonSink_Metal.mat");

        if (objectName.Contains("Bed", System.StringComparison.OrdinalIgnoreCase)
            || objectName.Contains("Mattress", System.StringComparison.OrdinalIgnoreCase))
            return LoadMat("Assets/Materials/Prison/PrisonBed_Mattress.mat");

        if (objectName.Contains("Pillow", System.StringComparison.OrdinalIgnoreCase))
            return LoadMat("Assets/Materials/Prison/PrisonBed_Pillow.mat");

        if (objectName.Contains("Wall", System.StringComparison.OrdinalIgnoreCase)
            || objectName.Contains("Cell_", System.StringComparison.OrdinalIgnoreCase)
            || objectName.Contains("Corr", System.StringComparison.OrdinalIgnoreCase)
            || objectName.Contains("Ext_", System.StringComparison.OrdinalIgnoreCase)
            || objectName.Contains("Pillar", System.StringComparison.OrdinalIgnoreCase))
            return LoadMat("Assets/Materials/Prison/PrisonWall_Concrete.mat");

        if (objectName.Contains("Shelf", System.StringComparison.OrdinalIgnoreCase)
            || objectName.Contains("Locker", System.StringComparison.OrdinalIgnoreCase)
            || objectName.Contains("Pipe", System.StringComparison.OrdinalIgnoreCase)
            || objectName.Contains("Duct", System.StringComparison.OrdinalIgnoreCase))
            return LoadMat("Assets/Materials/Prison/PrisonMetal_Shelf.mat");

        return null;
    }

    static Material LoadMat(string path) => AssetDatabase.LoadAssetAtPath<Material>(path);

    static Material MapMaterial(string importedName)
    {
        if (string.IsNullOrEmpty(importedName))
            return AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Prison/PrisonWall_Concrete.mat");

        foreach (var kv in BlenderMaterialToAsset)
        {
            if (importedName.Contains(kv.Key, System.StringComparison.OrdinalIgnoreCase))
                return AssetDatabase.LoadAssetAtPath<Material>(kv.Value);
        }

        foreach (var kv in TextureToMaterial)
        {
            if (importedName.Contains(kv.Key) || importedName.Contains(kv.Key.TrimStart('T', '_')))
                return AssetDatabase.LoadAssetAtPath<Material>(kv.Value);
        }

        if (importedName.Contains("Emissive", System.StringComparison.OrdinalIgnoreCase)
            || importedName.Contains("Light", System.StringComparison.OrdinalIgnoreCase))
            return AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Prison/PrisonLight_Emissive.mat");
        if (importedName.Contains("Security", System.StringComparison.OrdinalIgnoreCase)
            || importedName.Contains("Monitor", System.StringComparison.OrdinalIgnoreCase))
            return AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Prison/Accents/Accent_SecurityRed.mat");
        if (importedName.Contains("Caution", System.StringComparison.OrdinalIgnoreCase))
            return AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Prison/Accents/Accent_CautionYellow.mat");
        if (importedName.Contains("Sign", System.StringComparison.OrdinalIgnoreCase))
            return AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Prison/Accents/Accent_SignBlue.mat");
        if (importedName.Contains("Panel", System.StringComparison.OrdinalIgnoreCase)
            || importedName.Contains("White", System.StringComparison.OrdinalIgnoreCase))
            return AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Prison/Accents/Accent_PanelWhite.mat");
        if (importedName.Contains("Blanket", System.StringComparison.OrdinalIgnoreCase))
            return AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Prison/PrisonBed_Blanket.mat");
        if (importedName.Contains("Pillow", System.StringComparison.OrdinalIgnoreCase))
            return AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Prison/PrisonBed_Pillow.mat");

        return AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Prison/PrisonWall_Concrete.mat");
    }

    static void ConfigureColliders(GameObject root, bool structural, bool isItem)
    {
        foreach (var col in root.GetComponentsInChildren<Collider>(true))
            Object.DestroyImmediate(col);

        if (!isItem && !structural) return;

        var bounds = ComputeRendererBounds(root);
        if (bounds.size.sqrMagnitude < 0.0001f)
            bounds = new Bounds(root.transform.position, Vector3.one);

        var box = root.AddComponent<BoxCollider>();
        if (box == null) return;

        box.center = root.transform.InverseTransformPoint(bounds.center);
        box.size = Vector3.Max(bounds.size, Vector3.one * 0.1f);
    }

    static Bounds ComputeRendererBounds(GameObject root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(root.transform.position, Vector3.one);
        var b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);
        return b;
    }

    static void EnsureLightFixtureComponents(GameObject root)
    {
        Transform lightTf = root.transform.Find("PointLight");
        if (lightTf == null)
        {
            lightTf = new GameObject("PointLight").transform;
            lightTf.SetParent(root.transform, false);
            lightTf.localPosition = new Vector3(0f, -0.2f, 0f);
        }

        var light = lightTf.GetComponent<Light>();
        if (light == null)
            light = lightTf.gameObject.AddComponent<Light>();

        light.type = LightType.Point;
        light.intensity = 6f;
        light.range = 14f;
        light.color = new Color(1f, 0.95f, 0.85f);
        light.shadows = LightShadows.Soft;

        if (lightTf.GetComponent<UniversalAdditionalLightData>() == null)
        {
            var urp = lightTf.gameObject.AddComponent<UniversalAdditionalLightData>();
            urp.softShadowQuality = SoftShadowQuality.High;
        }
    }
}
