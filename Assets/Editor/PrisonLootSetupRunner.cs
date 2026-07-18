using System.Collections.Generic;
using System.IO;
using Prison;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// One-click setup for Phase A+B: world pickup prefab, loot tables, item network IDs,
/// world prefab assignment, and ItemSpawnNode placement in PrisonLevel1.
/// Menu: Prison / Setup Items & World Loot
/// </summary>
public static class PrisonLootSetupRunner
{
    private const string WorldPickupPrefabPath = "Assets/Prefabs/Items/WorldPickup_Generic.prefab";
    private const string LootTablesFolder = "Assets/ScriptableObjects/LootTables";
    private const string WorldSpawnsRootName = "WorldSpawns";

    private static readonly Dictionary<string, ushort> NetworkIds = new Dictionary<string, ushort>
    {
        { "Metal Rod", 1 },
        { "Flat Metal", 2 },
        { "Screwdriver", 3 },
        { "Paperclip", 10 },
        { "Plastic Bottle", 11 },
        { "Rag", 12 },
        { "Wood Scrap", 13 },
        { "Soap", 14 },
        { "Pillow", 15 },
        { "Bed Sheet", 16 },
        { "Metal Scrap", 20 },
        { "Duct Tape", 21 },
        { "Wire", 22 },
        { "Charcoal", 23 },
        { "Coin", 24 },
        { "File", 30 },
        { "Mirror", 31 },
        { "Glass Bottle", 32 },
        { "Alcohol", 33 },
        { "Fake Bed Dummy", 40 },
        { "Wire Cutters", 41 },
        { "Shovel", 42 },
        { "Ladder", 43 },
        { "Grappling Hook", 44 },
        { "Molotov", 50 },
    };

    [MenuItem("Prison/Setup Items & World Loot")]
    public static void Run()
    {
        EnsureFolders();
        RemoveDuplicateItemAssets();
        GameObject worldPickupPrefab = CreateOrUpdateWorldPickupPrefab();
        AssignItemData(worldPickupPrefab);
        CreateLootTables();
        FixCellSpawnPoints();
        PlaceWorldSpawnNodesFromLayout();
        EnsureItemDatabase();
        WireCraftingRecipes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[PrisonLootSetup] Phase A+B complete. Save the scene and enter Play Mode to test spawns.");
    }

    /// <summary>Batchmode entry — opens PrisonLevel1, runs full loot setup, saves scene.</summary>
    public static void RunBatchMode()
    {
        const string scenePath = "Assets/Scenes/PrisonLevel1.unity";
        if (EditorSceneManager.GetActiveScene().path != scenePath)
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        Run();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("[PrisonLootSetup] Batch mode complete — scene saved.");
    }

    private static void FixCellSpawnPoints()
    {
        PrisonLayoutRebuildRunner.FixAllCellSpawns();
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Items"))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            AssetDatabase.CreateFolder("Assets/Prefabs", "Items");
        }

        if (!AssetDatabase.IsValidFolder(LootTablesFolder))
            AssetDatabase.CreateFolder("Assets/ScriptableObjects", "LootTables");
    }

    private static void RemoveDuplicateItemAssets()
    {
        DeleteAssetIfExists("Assets/ScriptableObjects/Items/MetalScrap.asset");
        DeleteAssetIfExists("Assets/ScriptableObjects/Items/WoodScrap.asset");
    }

    private static void DeleteAssetIfExists(string path)
    {
        if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
            AssetDatabase.DeleteAsset(path);
    }

    private static GameObject CreateOrUpdateWorldPickupPrefab()
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(WorldPickupPrefabPath);
        if (existing != null)
            return existing;

        var root = GameObject.CreatePrimitive(PrimitiveType.Cube);
        root.name = "WorldPickup_Generic";
        root.transform.localScale = new Vector3(0.22f, 0.22f, 0.22f);

        var renderer = root.GetComponent<Renderer>();
        if (renderer != null)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Prison/PrisonMetal_Shelf.mat");
            if (mat != null)
                renderer.sharedMaterial = mat;
        }

        var collider = root.GetComponent<BoxCollider>();
        if (collider != null)
            collider.size = Vector3.one;

        root.AddComponent<WorldItemPickup>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, WorldPickupPrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static void AssignItemData(GameObject worldPickupPrefab)
    {
        string[] guids = AssetDatabase.FindAssets("t:ItemData", new[] { "Assets/ScriptableObjects" });
        int updated = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (item == null) continue;

            if (NetworkIds.TryGetValue(item.itemName, out ushort id))
            {
                item.networkId = id;
                updated++;
            }

            // Craft results and starting bedding are not world-spawned.
            if (item.category == ItemCategory.Tool || item.category == ItemCategory.Weapon)
                continue;
            if (item.itemName is "Pillow" or "Bed Sheet")
                continue;

            if (item.worldPrefab == null)
                item.worldPrefab = worldPickupPrefab;

            EditorUtility.SetDirty(item);
        }

        Debug.Log($"[PrisonLootSetup] Updated {updated} item network IDs; assigned worldPrefab where missing.");
    }

    private static ItemData FindItemByName(string name)
    {
        string[] guids = AssetDatabase.FindAssets("t:ItemData", new[] { "Assets/ScriptableObjects" });
        foreach (string guid in guids)
        {
            var item = AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(guid));
            if (item != null && item.itemName == name)
                return item;
        }
        return null;
    }

    private static LootTable CreateLootTableAsset(string assetName, params (string itemName, float weightMult)[] entries)
    {
        string path = $"{LootTablesFolder}/{assetName}.asset";
        var table = AssetDatabase.LoadAssetAtPath<LootTable>(path);
        if (table == null)
        {
            table = ScriptableObject.CreateInstance<LootTable>();
            AssetDatabase.CreateAsset(table, path);
        }

        table.possibleItems = new List<ItemData>();
        foreach (var entry in entries)
        {
            ItemData item = FindItemByName(entry.itemName);
            if (item == null)
            {
                Debug.LogWarning($"[PrisonLootSetup] Loot table '{assetName}' missing item '{entry.itemName}'.");
                continue;
            }

            item.weightMultiplier = entry.weightMult;
            table.possibleItems.Add(item);
            EditorUtility.SetDirty(item);
        }

        EditorUtility.SetDirty(table);
        return table;
    }

    private static void CreateLootTables()
    {
        CreateLootTableAsset("LootTable_Prison_Common",
            ("Paperclip", 1f), ("Plastic Bottle", 1f), ("Rag", 1f), ("Wood Scrap", 0.8f), ("Soap", 0.6f));

        CreateLootTableAsset("LootTable_Yard",
            ("Plastic Bottle", 1.2f), ("Wood Scrap", 1f), ("Rag", 0.8f), ("Metal Scrap", 0.7f), ("Coin", 0.4f));

        CreateLootTableAsset("LootTable_Kitchen",
            ("Plastic Bottle", 1f), ("Rag", 0.8f), ("Charcoal", 1f), ("Glass Bottle", 0.5f), ("Alcohol", 0.3f));

        CreateLootTableAsset("LootTable_Workshop",
            ("Metal Scrap", 1.2f), ("Wire", 1f), ("Duct Tape", 0.8f), ("Metal Rod", 0.6f), ("Flat Metal", 0.6f), ("File", 0.25f));

        CreateLootTableAsset("LootTable_Laundry",
            ("Soap", 1.2f), ("Rag", 1f));

        CreateLootTableAsset("LootTable_RareHidden",
            ("File", 1f), ("Mirror", 0.8f), ("Alcohol", 0.5f), ("Coin", 0.6f));

        Debug.Log("[PrisonLootSetup] Created/updated 6 loot tables.");
    }

    private static Transform EnsureSpawnRoot(string childName)
    {
        GameObject root = GameObject.Find(WorldSpawnsRootName);
        if (root == null)
            root = new GameObject(WorldSpawnsRootName);

        Transform child = root.transform.Find(childName);
        if (child == null)
        {
            var go = new GameObject(childName);
            go.transform.SetParent(root.transform, false);
            child = go.transform;
        }

        return child;
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(parent.GetChild(i).gameObject);
    }

    private static void PlaceSpawnNode(Transform parent, string name, Vector3 worldPos, LootTable table, float chance)
    {
        Vector3 snapped = SpawnPlacementUtility.SnapPickupPosition(worldPos);
        var go = new GameObject(name);
        go.transform.SetParent(parent, true);
        go.transform.position = snapped;

        var node = go.AddComponent<ItemSpawnNode>();
        node.lootTable = table;
        node.spawnChance = chance;
        node.spawnRolls = 2;
    }

    private static Vector3 AnchorPosition(string objectName, Vector3 fallback)
    {
        var go = GameObject.Find(objectName);
        return go != null ? go.transform.position : fallback;
    }

    private static Vector3 FindChildWorldPosition(string rootName, string childName, Vector3 fallback)
    {
        var root = GameObject.Find(rootName);
        if (root == null) return fallback;
        var child = root.transform.Find(childName);
        if (child == null)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == childName) return t.position;
            }
            return fallback;
        }
        return child.position;
    }

    public static void PlaceWorldSpawnNodesFromLayout()
    {
        var common = AssetDatabase.LoadAssetAtPath<LootTable>($"{LootTablesFolder}/LootTable_Prison_Common.asset");
        var yardTable = AssetDatabase.LoadAssetAtPath<LootTable>($"{LootTablesFolder}/LootTable_Yard.asset");
        var kitchenTable = AssetDatabase.LoadAssetAtPath<LootTable>($"{LootTablesFolder}/LootTable_Kitchen.asset");
        var workshopTable = AssetDatabase.LoadAssetAtPath<LootTable>($"{LootTablesFolder}/LootTable_Workshop.asset");
        var laundryTable = AssetDatabase.LoadAssetAtPath<LootTable>($"{LootTablesFolder}/LootTable_Laundry.asset");
        var rare = AssetDatabase.LoadAssetAtPath<LootTable>($"{LootTablesFolder}/LootTable_RareHidden.asset");

        if (common == null || yardTable == null)
        {
            Debug.LogError("[PrisonLootSetup] Loot tables missing — run CreateLootTables first.");
            return;
        }

        Transform corridorRoot = EnsureSpawnRoot("Spawn_Corridor");
        Transform cafeteriaRoot = EnsureSpawnRoot("Spawn_Cafeteria");
        Transform cellsRoot = EnsureSpawnRoot("Spawn_Cells");
        Transform yardRoot = EnsureSpawnRoot("Spawn_Yard");
        Transform kitchenRoot = EnsureSpawnRoot("Spawn_Kitchen");
        Transform workshopRoot = EnsureSpawnRoot("Spawn_Workshop");
        Transform laundryRoot = EnsureSpawnRoot("Spawn_Laundry");
        Transform hiddenRoot = EnsureSpawnRoot("Spawn_Hidden");

        ClearChildren(corridorRoot);
        ClearChildren(cafeteriaRoot);
        ClearChildren(cellsRoot);
        ClearChildren(yardRoot);
        ClearChildren(kitchenRoot);
        ClearChildren(workshopRoot);
        ClearChildren(laundryRoot);
        ClearChildren(hiddenRoot);

        Vector3 hub = PrisonLayoutAnchors.MainCorridorCenter;
        Vector3 connector = PrisonLayoutAnchors.CellSouthConnectorCenter;
        Vector3 cells = PrisonLayoutAnchors.CellBlockCenter;
        Vector3 yardC = PrisonLayoutAnchors.YardCenter;
        Vector3 workshop = PrisonLayoutAnchors.WorkshopCenter;
        Vector3 laundry = PrisonLayoutAnchors.LaundryCenter;
        Vector3 cafeteria = PrisonLayoutAnchors.CafeteriaCenter;
        Vector3 servingCounter = FindChildWorldPosition("Cafeteria", "ServingCounter", new Vector3(-16f, 1.3f, -71f));

        PlaceSpawnNode(corridorRoot, "C01", hub + new Vector3(-12f, 0f, 0f), common, 0.85f);
        PlaceSpawnNode(corridorRoot, "C02", hub + new Vector3(12f, 0f, 0f), common, 0.85f);
        PlaceSpawnNode(corridorRoot, "C03", hub + new Vector3(0f, 0f, -3f), common, 0.80f);
        PlaceSpawnNode(corridorRoot, "C04", connector + new Vector3(-15f, 0f, 0f), common, 0.85f);
        PlaceSpawnNode(corridorRoot, "C05", connector + new Vector3(0f, 0f, 0f), common, 0.90f);
        PlaceSpawnNode(corridorRoot, "C06", connector + new Vector3(15f, 0f, 0f), common, 0.85f);
        PlaceSpawnNode(corridorRoot, "C07", PrisonLayoutAnchors.CafeteriaConnectorCenter, common, 0.80f);
        PlaceSpawnNode(corridorRoot, "C08", hub + new Vector3(-25f, 0f, 0f), common, 0.75f);

        PlaceSpawnNode(cellsRoot, "L01", cells + new Vector3(-20f, 0f, -8f), common, 0.90f);
        PlaceSpawnNode(cellsRoot, "L02", cells + new Vector3(20f, 0f, -8f), common, 0.90f);
        PlaceSpawnNode(cellsRoot, "L03", cells + new Vector3(0f, 0f, -12f), common, 0.85f);
        PlaceSpawnNode(cellsRoot, "L04", cells + new Vector3(-20f, 0f, 8f), rare ?? common, 0.50f);
        PlaceSpawnNode(cellsRoot, "L05", cells + new Vector3(20f, 0f, 8f), common, 0.85f);
        PlaceSpawnNode(cellsRoot, "L06", cells + new Vector3(0f, 0f, 12f), common, 0.80f);
        PlaceSpawnNode(cellsRoot, "L07", connector + new Vector3(-8f, 0f, 2f), common, 0.85f);
        PlaceSpawnNode(cellsRoot, "L08", connector + new Vector3(8f, 0f, 2f), common, 0.90f);

        PlaceSpawnNode(cafeteriaRoot, "F01", servingCounter + new Vector3(4f, 0f, 2f), kitchenTable ?? common, 0.65f);
        PlaceSpawnNode(cafeteriaRoot, "F02", servingCounter + new Vector3(-6f, 0f, -4f), kitchenTable ?? common, 0.65f);
        PlaceSpawnNode(cafeteriaRoot, "F03", servingCounter + new Vector3(8f, 0f, -8f), common, 0.70f);
        PlaceSpawnNode(cafeteriaRoot, "F04", servingCounter + new Vector3(-10f, 0f, 6f), common, 0.65f);
        PlaceSpawnNode(cafeteriaRoot, "F05", servingCounter + new Vector3(0f, 0f, -12f), kitchenTable ?? common, 0.60f);
        PlaceSpawnNode(cafeteriaRoot, "F06", servingCounter + new Vector3(-14f, 0f, -2f), rare ?? common, 0.40f);

        PlaceSpawnNode(yardRoot, "Y01", yardC + new Vector3(-18f, 0f, -8f), yardTable, 0.70f);
        PlaceSpawnNode(yardRoot, "Y02", yardC + new Vector3(14f, 0f, -6f), yardTable, 0.65f);
        PlaceSpawnNode(yardRoot, "Y03", yardC + new Vector3(-25f, 0f, 6f), yardTable, 0.65f);
        PlaceSpawnNode(yardRoot, "Y04", yardC + new Vector3(22f, 0f, 8f), yardTable, 0.70f);
        PlaceSpawnNode(yardRoot, "Y05", yardC + new Vector3(0f, 0f, 10f), common, 0.60f);
        PlaceSpawnNode(yardRoot, "Y06", PrisonLayoutAnchors.RollCallCenter + new Vector3(-15f, 0f, 0f), yardTable, 0.55f);
        PlaceSpawnNode(yardRoot, "Y07", yardC + new Vector3(8f, 0f, -12f), yardTable, 0.55f);
        PlaceSpawnNode(yardRoot, "Y08", yardC + new Vector3(-8f, 0f, 12f), rare ?? common, 0.40f);

        if (workshopTable != null)
        {
            PlaceSpawnNode(workshopRoot, "W01", workshop + new Vector3(-4f, 0f, 3f), workshopTable, 0.70f);
            PlaceSpawnNode(workshopRoot, "W02", workshop + new Vector3(3f, 0f, -3f), workshopTable, 0.65f);
            PlaceSpawnNode(workshopRoot, "W03", workshop + new Vector3(6f, 0f, 1f), workshopTable, 0.60f);
        }

        if (laundryTable != null)
        {
            PlaceSpawnNode(laundryRoot, "LA01", laundry + new Vector3(-3f, 0f, 2f), laundryTable, 0.70f);
            PlaceSpawnNode(laundryRoot, "LA02", laundry + new Vector3(3f, 0f, -2f), laundryTable, 0.65f);
            PlaceSpawnNode(laundryRoot, "LA03", laundry + new Vector3(0f, 0f, 3f), laundryTable, 0.60f);
        }

        PlaceSpawnNode(kitchenRoot, "K01", cafeteria + new Vector3(-6f, 0f, -3f), kitchenTable ?? common, 0.65f);
        PlaceSpawnNode(kitchenRoot, "K02", cafeteria + new Vector3(5f, 0f, 2f), kitchenTable ?? common, 0.60f);
        PlaceSpawnNode(kitchenRoot, "K03", cafeteria + new Vector3(-2f, 0f, 5f), kitchenTable ?? common, 0.55f);

        if (rare != null)
        {
            PlaceSpawnNode(hiddenRoot, "H01", hub + new Vector3(0f, 0f, -6f), rare, 0.55f);
            PlaceSpawnNode(hiddenRoot, "H02", workshop + new Vector3(7f, 0f, 5f), rare, 0.50f);
            PlaceSpawnNode(hiddenRoot, "H03", laundry + new Vector3(-6f, 0f, 5f), rare, 0.50f);
            PlaceSpawnNode(hiddenRoot, "H04", cafeteria + new Vector3(8f, 0f, -4f), rare, 0.45f);
            PlaceSpawnNode(hiddenRoot, "H05", yardC + new Vector3(-28f, 0f, -14f), rare, 0.40f);
            PlaceSpawnNode(hiddenRoot, "H06", PrisonLayoutAnchors.CafeteriaConnectorCenter + new Vector3(4f, 0f, -8f), rare, 0.40f);
        }

        PrisonLayoutRebuildRunner.EnsureYardZoneSetup(yardC);
        Debug.Log("[PrisonLootSetup] Placed world loot spawn nodes from layout anchors.");
    }

    private static void PlaceWorldSpawnNodes()
    {
        PlaceWorldSpawnNodesFromLayout();
    }

    private static void EnsureItemDatabase()
    {
        var managers = GameObject.Find("Managers");
        if (managers == null)
            managers = new GameObject("Managers");

        var db = managers.GetComponent<ItemDatabase>();
        if (db == null)
            db = managers.AddComponent<ItemDatabase>();

        db.allItemsInGame = new List<ItemData>();
        string[] guids = AssetDatabase.FindAssets("t:ItemData", new[] { "Assets/ScriptableObjects" });
        foreach (string guid in guids)
        {
            var item = AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(guid));
            if (item == null) continue;
            if (item is WeaponData) continue;
            if (item.category == ItemCategory.Weapon) continue;
            if (!db.allItemsInGame.Contains(item))
                db.allItemsInGame.Add(item);
        }

        EditorUtility.SetDirty(db);
        Debug.Log($"[PrisonLootSetup] ItemDatabase registered {db.allItemsInGame.Count} items on Managers.");
    }

    private static void WireCraftingRecipes()
    {
        string[] recipeGuids = AssetDatabase.FindAssets("t:CraftingRecipe", new[] { "Assets/ScriptableObjects/Recipes" });
        var recipes = new List<CraftingRecipe>();
        foreach (string guid in recipeGuids)
        {
            var recipe = AssetDatabase.LoadAssetAtPath<CraftingRecipe>(AssetDatabase.GUIDToAssetPath(guid));
            if (recipe != null)
                recipes.Add(recipe);
        }

        foreach (var ui in Object.FindObjectsByType<InventoryUI>(FindObjectsInactive.Include))
        {
            ui.recipes = recipes.ToArray();
            EditorUtility.SetDirty(ui);
        }

        foreach (var nb in Object.FindObjectsByType<StolenNotebookUI>(FindObjectsInactive.Include))
        {
            nb.recipes = recipes.ToArray();
            EditorUtility.SetDirty(nb);
        }

        Debug.Log($"[PrisonLootSetup] Wired {recipes.Count} crafting recipes to inventory/notebook UIs.");
    }
}
