using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Paths and prefab loading for <see cref="BlenderKitAssetSetup"/> output.
/// Source FBX: Assets/Models/BlenderKit/ per docs/PrisonEscape/04 Content & Assets/Blender Asset Kit.md
/// </summary>
public static class BlenderKitCatalog
{
    public const string FbxRoot = "Assets/Models/BlenderKit";
    public const string PrefabRoot = "Assets/Prefabs/BlenderKit";
    public const string ItemPrefabRoot = "Assets/Prefabs/BlenderKit/Items";
    public const string TextureRoot = "Assets/Models/BlenderKit/Textures";

    static readonly Dictionary<string, string> ItemAssetToSo = new()
    {
        { "SM_Item_Paperclip", "Assets/ScriptableObjects/Items/Paperclip.asset" },
        { "SM_Item_Soap", "Assets/ScriptableObjects/Items/Soap.asset" },
        { "SM_Item_PlasticBottle", "Assets/ScriptableObjects/Items/Plastic Bottle.asset" },
        { "SM_Item_Rag", "Assets/ScriptableObjects/Items/Rag.asset" },
        { "SM_Item_Pillow", "Assets/ScriptableObjects/Items/Pillow.asset" },
        { "SM_Item_BedSheet", "Assets/ScriptableObjects/Items/Bed Sheet.asset" },
        { "SM_Item_WoodScrap", "Assets/ScriptableObjects/Items/Wood Scrap.asset" },
        { "SM_Item_MetalRod", "Assets/ScriptableObjects/Metal Rod.asset" },
        { "SM_Item_FlatMetal", "Assets/ScriptableObjects/Flat Metal.asset" },
        { "SM_Item_Coin", "Assets/ScriptableObjects/Items/Coin.asset" },
        { "SM_Item_Charcoal", "Assets/ScriptableObjects/Items/Charcoal.asset" },
        { "SM_Item_DuctTape", "Assets/ScriptableObjects/Items/Duct Tape.asset" },
        { "SM_Item_Wire", "Assets/ScriptableObjects/Items/Wire.asset" },
        { "SM_Item_MetalScrap", "Assets/ScriptableObjects/Items/Metal Scrap.asset" },
        { "SM_Item_GlassBottle", "Assets/ScriptableObjects/Items/Glass Bottle.asset" },
        { "SM_Item_Alcohol", "Assets/ScriptableObjects/Items/Alcohol.asset" },
        { "SM_Item_Mirror", "Assets/ScriptableObjects/Items/Mirror.asset" },
        { "SM_Item_File", "Assets/ScriptableObjects/Items/File.asset" },
        { "SM_Item_Screwdriver", "Assets/ScriptableObjects/Items/Screwdriver.asset" },
        { "SM_Item_FakeBedDummy", "Assets/ScriptableObjects/Items/Fake Bed Dummy.asset" },
        { "SM_Item_Shovel", "Assets/ScriptableObjects/Items/Shovel.asset" },
        { "SM_Item_WireCutters", "Assets/ScriptableObjects/Items/Wire Cutters.asset" },
        { "SM_Item_Ladder", "Assets/ScriptableObjects/Items/Ladder.asset" },
        { "SM_Item_GrapplingHook", "Assets/ScriptableObjects/Items/Grappling Hook.asset" },
        { "SM_Item_Molotov", "Assets/ScriptableObjects/Items/Molotov.asset" },
    };

    public static string FbxPath(string assetName) =>
        assetName.StartsWith("SM_Item_")
            ? $"{FbxRoot}/Items/{assetName}.fbx"
            : $"{FbxRoot}/{assetName}.fbx";

    public static string PrefabPath(string assetName) =>
        assetName.StartsWith("SM_Item_")
            ? $"{ItemPrefabRoot}/{assetName}.prefab"
            : $"{PrefabRoot}/{assetName}.prefab";

    public static GameObject LoadPrefab(string assetName)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(assetName));
        if (prefab != null) return prefab;

        // Fallback: instantiate directly from FBX model root if prefab not generated yet.
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath(assetName));
        return model;
    }

    public static bool HasKit => AssetDatabase.IsValidFolder(PrefabRoot)
        || AssetDatabase.LoadAssetAtPath<GameObject>($"{FbxRoot}/SM_Floor_Tile_4m.fbx") != null;

    public static IReadOnlyDictionary<string, string> ItemPrefabToScriptableObject => ItemAssetToSo;
}
