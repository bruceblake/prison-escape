using Prison;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Reorganizes PrisonLevel1: moves corridor out of cell block, places wings with clearance,
/// fixes spawn points on cell floors, and rebuilds connectors + yard.
/// Menu: Prison / Rebuild Prison Layout
/// </summary>
public static class PrisonLayoutRebuildRunner
{
    private const string LayoutRootName = "PrisonLayout";

    [MenuItem("Prison/Rebuild Prison Layout")]
    public static void Run()
    {
        Debug.Log("[PrisonLayoutRebuild] v3 — rebuilding layout (spawn fix + wall passages + wing connectors).");
        RemoveMisplacedBuild();
        RelocateLegacyCorridor();
        OpenPerimeterPassages();
        Transform layoutRoot = EnsureLayoutRoot();
        BuildCorridorNetwork(layoutRoot);
        BuildExpandedWings(layoutRoot);
        BuildNorthYard(layoutRoot);
        SetupZones();
        FixAllCellSpawns();
        RepositionWorldSpawns();
        BakeNavMesh();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[PrisonLayoutRebuild] Layout rebuilt. Enter Play Mode to verify spawns and wings.");
    }

    private static void RemoveMisplacedBuild()
    {
        var levelBuild = GameObject.Find("LevelBuild");
        if (levelBuild != null)
            Object.DestroyImmediate(levelBuild);

        // Legacy tunnel/vent cubes that sat inside cells
        foreach (var name in new[] { "MaintenanceTunnel", "VentCrawlspace" })
        {
            var go = GameObject.Find(name);
            if (go != null) Object.DestroyImmediate(go);
        }
    }

    private static void RelocateLegacyCorridor()
    {
        var corridorFloor = GameObject.Find("CorridorFloor");
        if (corridorFloor != null)
        {
            corridorFloor.transform.position = PrisonLayoutAnchors.MainCorridorCenter;
            corridorFloor.transform.localScale = new Vector3(52f, 0.2f, 8f);
        }

        var corridorCeiling = GameObject.Find("CorridorCeiling");
        if (corridorCeiling != null)
        {
            corridorCeiling.transform.position = PrisonLayoutAnchors.MainCorridorCenter + Vector3.up * 6.6f;
            corridorCeiling.transform.localScale = new Vector3(52f, 0.2f, 8f);
        }
    }

    private static Transform EnsureLayoutRoot()
    {
        var existing = GameObject.Find(LayoutRootName);
        if (existing == null)
            existing = new GameObject(LayoutRootName);

        for (int i = existing.transform.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(existing.transform.GetChild(i).gameObject);

        return existing.transform;
    }

    private static void BuildCorridorNetwork(Transform root)
    {
        Transform corridors = CreateChild(root, "Corridors");

        // South frontage — connects cell block doors to main hub
        BuildFloorSlab(corridors, "CellSouthConnector",
            PrisonLayoutAnchors.CellSouthConnectorCenter, new Vector3(62f, 0.15f, 6f));

        // East-west service hub (workshop / laundry / cafeteria branches)
        BuildFloorSlab(corridors, "MainHubCorridor",
            PrisonLayoutAnchors.MainCorridorCenter, new Vector3(52f, 0.15f, 8f));

        // Walkable paths through the old perimeter wall line out to the wings
        BuildFloorSlab(corridors, "WestWingConnector",
            new Vector3(-57f, PrisonLayoutAnchors.CorridorY, -45f), new Vector3(62f, 0.15f, 7f));
        BuildFloorSlab(corridors, "EastWingConnector",
            new Vector3(11f, PrisonLayoutAnchors.CorridorY, -45f), new Vector3(74f, 0.15f, 7f));

        // South to cafeteria
        BuildFloorSlab(corridors, "CafeteriaConnector",
            PrisonLayoutAnchors.CafeteriaConnectorCenter, new Vector3(10f, 0.15f, 22f));

        // North — roll call to yard
        BuildFloorSlab(corridors, "NorthYardConnector",
            new Vector3(-26f, PrisonLayoutAnchors.CorridorY, 66f), new Vector3(62f, 0.15f, 8f));

        // Ramps from cell block down to south connector (south row exits)
        BuildFloorSlab(corridors, "CellExitRamp_W",
            new Vector3(-49f, PrisonLayoutAnchors.CorridorY, -27f), new Vector3(8f, 0.15f, 14f));
        BuildFloorSlab(corridors, "CellExitRamp_E",
            new Vector3(-3f, PrisonLayoutAnchors.CorridorY, -27f), new Vector3(8f, 0.15f, 14f));

        PlaceDoorway(corridors, PrisonLayoutAnchors.WorkshopCenter + new Vector3(14f, 0f, 0f), 90f);
        PlaceDoorway(corridors, PrisonLayoutAnchors.LaundryCenter + new Vector3(-14f, 0f, 0f), -90f);
        PlaceDoorway(corridors, new Vector3(-52f, 1f, -45f), 90f);
        PlaceDoorway(corridors, new Vector3(0f, 1f, -45f), -90f);
    }

    /// <summary>
    /// Moves perimeter walls outward and cuts doorway-sized holes so wings are reachable.
    /// </summary>
    private static void OpenPerimeterPassages()
    {
        int moved = 0;
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (t.name.StartsWith("LeftPrisonWall") && t.position.x > -90f && t.position.x < 0f)
            {
                Vector3 p = t.position;
                p.x = -100f;
                t.position = p;
                moved++;
                EditorUtility.SetDirty(t.gameObject);
            }
            else if (t.name.StartsWith("RightPrisonWall") && t.position.x > -5f && t.position.x < 40f)
            {
                Vector3 p = t.position;
                p.x = 52f;
                t.position = p;
                moved++;
                EditorUtility.SetDirty(t.gameObject);
            }
        }

        CutBoxOpening(new Vector3(-52f, 2f, -45f), new Vector3(12f, 6f, 14f));
        CutBoxOpening(new Vector3(0f, 2f, -45f), new Vector3(12f, 6f, 14f));
        CutBoxOpening(new Vector3(-26f, 2f, -30f), new Vector3(58f, 6f, 12f));
        CutBoxOpening(new Vector3(-57f, 2f, -45f), new Vector3(50f, 6f, 10f));
        CutBoxOpening(new Vector3(11f, 2f, -45f), new Vector3(60f, 6f, 10f));
        DisableCellRoofColliders();
        Debug.Log($"[PrisonLayoutRebuild] Perimeter: moved {moved} wall segments, cut passage openings.");
    }

    private static void DisableCellRoofColliders()
    {
        var jail = GameObject.Find("JailCells");
        if (jail == null) return;

        int notWalkableArea = NavMesh.GetAreaFromName("Not Walkable");
        foreach (Collider col in jail.GetComponentsInChildren<Collider>(true))
        {
            if (col == null || col.isTrigger) continue;
            Bounds b = col.bounds;
            if (b.min.y >= 5f && b.max.y <= 9.5f && b.size.y < 4f)
            {
                col.enabled = false;
                EditorUtility.SetDirty(col);
            }
        }

        // Roof deck renderers still bake walkable navmesh unless excluded.
        foreach (Renderer r in jail.GetComponentsInChildren<Renderer>(true))
        {
            if (r == null) continue;
            Bounds b = r.bounds;
            if (b.min.y < 5f || b.max.y > 9.5f || b.size.y >= 4f) continue;
            var mod = r.GetComponent<NavMeshModifier>() ?? r.gameObject.AddComponent<NavMeshModifier>();
            mod.overrideArea = true;
            mod.area = notWalkableArea;
            EditorUtility.SetDirty(r.gameObject);
        }
    }

    private static void CutBoxOpening(Vector3 center, Vector3 size)
    {
        Bounds box = new Bounds(center, size);
        foreach (Collider col in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
        {
            if (col == null || col.isTrigger) continue;
            if (!col.bounds.Intersects(box)) continue;
            if (col.bounds.min.y > 6f) continue;
            col.enabled = false;
            EditorUtility.SetDirty(col);
        }

        foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (r == null) continue;
            if (!r.bounds.Intersects(box)) continue;
            if (r.bounds.min.y > 8f) continue;
            string n = r.gameObject.name;
            if (!n.Contains("Wall") && !n.Contains("Prison") && !n.Contains("Perimeter") && !n.Contains("Modular")) continue;
            r.enabled = false;
            EditorUtility.SetDirty(r.gameObject);
        }
    }

    private static void BuildExpandedWings(Transform root)
    {
        Transform wings = CreateChild(root, "ServiceWings");
        BuildWingRoom(wings, "WorkshopWing", PrisonLayoutAnchors.WorkshopCenter, new Vector3(20f, 3.5f, 16f), "WORKSHOP");
        BuildWingRoom(wings, "LaundryWing", PrisonLayoutAnchors.LaundryCenter, new Vector3(20f, 3.5f, 16f), "LAUNDRY");

        Vector3 kitchen = PrisonLayoutAnchors.KitchenCenter(new Vector3(-16f, PrisonLayoutAnchors.CorridorY, -83f));
        BuildWingRoom(wings, "KitchenBackRoom", kitchen, new Vector3(16f, 3.5f, 12f), "KITCHEN");

        PlacePrefab(wings, "Assets/Models/StorageCrate.prefab", PrisonLayoutAnchors.WorkshopCenter + new Vector3(-4f, 0f, 3f));
        PlacePrefab(wings, "Assets/Models/StorageCrate.prefab", PrisonLayoutAnchors.WorkshopCenter + new Vector3(3f, 0f, -3f));
        PlacePrefab(wings, "Assets/Models/ConcretePillar.prefab", PrisonLayoutAnchors.WorkshopCenter + new Vector3(6f, 0f, 5f), 0.85f);

        for (int i = 0; i < 3; i++)
        {
            var drum = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            drum.name = $"Washer_{i}";
            drum.transform.SetParent(wings, true);
            drum.transform.position = PrisonLayoutAnchors.LaundryCenter + new Vector3(-4f + i * 4f, 0.85f, 2f);
            drum.transform.localScale = new Vector3(1.3f, 0.85f, 1.3f);
            ApplyMat(drum, "Assets/Materials/Prison/PrisonMetal_Shelf.mat");
        }

        PlacePrefab(wings, "Assets/Models/CafeteriaTable.prefab", PrisonLayoutAnchors.LaundryCenter + new Vector3(0f, 0f, -3f), 0.65f);
        PlacePrefab(wings, "Assets/Models/StorageCrate.prefab", kitchen + new Vector3(-3f, 0f, 2f));
        PlacePrefab(wings, "Assets/Models/StorageCrate.prefab", kitchen + new Vector3(4f, 0f, -2f));
    }

    private static void BuildNorthYard(Transform root)
    {
        Transform yard = CreateChild(root, "YardExpansion");

        BuildFloorSlab(yard, "YardPad", PrisonLayoutAnchors.YardCenter, new Vector3(70f, 0.12f, 40f),
            "Assets/Materials/Prison/PrisonFloor_Tile.mat");

        for (int i = -4; i <= 4; i++)
            PlacePrefab(yard, "Assets/Models/Modular/Fence_Panel_Modular.prefab", PrisonLayoutAnchors.YardCenter + new Vector3(i * 8f, 0f, -18f));

        PlacePrefab(yard, "Assets/Models/GymBench.prefab", PrisonLayoutAnchors.YardCenter + new Vector3(-15f, 0f, -5f));
        PlacePrefab(yard, "Assets/Models/GymBench.prefab", PrisonLayoutAnchors.YardCenter + new Vector3(12f, 0f, 6f), 90f);
        PlacePrefab(yard, "Assets/Models/StorageCrate.prefab", PrisonLayoutAnchors.YardCenter + new Vector3(18f, 0f, -8f));

        // Hidden maintenance tunnel — under service corridor, not inside cells
        var tunnel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tunnel.name = "MaintenanceTunnel";
        tunnel.transform.SetParent(yard, true);
        tunnel.transform.position = PrisonLayoutAnchors.MainCorridorCenter + new Vector3(0f, -0.5f, 0f);
        tunnel.transform.localScale = new Vector3(8f, 0.6f, 18f);
        ApplyMat(tunnel, "Assets/Materials/Prison/PrisonWall_Concrete.mat");
    }

    private static void SetupZones()
    {
        SetupYardZone();
        SetupRollCallZone();
    }

    private static void SetupYardZone()
    {
        var zoneGo = GameObject.Find("YardZone") ?? new GameObject("YardZone");
        zoneGo.transform.position = PrisonLayoutAnchors.YardCenter + Vector3.up * 0.5f;

        var box = zoneGo.GetComponent<BoxCollider>() ?? zoneGo.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(70f, 4f, 40f);
        box.center = Vector3.zero;

        var zone = zoneGo.GetComponent<PrisonLocationZone>() ?? zoneGo.AddComponent<PrisonLocationZone>();
        zone.zoneType = ZoneType.Yard;
        zone.hudDisplayName = "YARD";

        ClearChildren(zoneGo.transform);
        for (int i = 0; i < 5; i++)
        {
            var stand = new GameObject($"Stand_{i}");
            stand.transform.SetParent(zoneGo.transform, false);
            stand.transform.position = PrisonLayoutAnchors.YardCenter + new Vector3(-20f + i * 10f, 0f, 0f);
        }

        WireRegistry(r => r.yard = zoneGo.GetComponent<PrisonLocationZone>());
    }

    private static void SetupRollCallZone()
    {
        var zoneGo = GameObject.Find("RollCallArea") ?? new GameObject("RollCallArea");
        zoneGo.transform.position = PrisonLayoutAnchors.RollCallCenter;

        var box = zoneGo.GetComponent<BoxCollider>() ?? zoneGo.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(62f, 4f, 10f);
        box.center = Vector3.zero;

        var zone = zoneGo.GetComponent<PrisonLocationZone>() ?? zoneGo.AddComponent<PrisonLocationZone>();
        zone.zoneType = ZoneType.RollCallArea;
        zone.hudDisplayName = "ROLL CALL";

        ClearChildren(zoneGo.transform);
        var jail = GameObject.Find("JailCells");
        if (jail != null)
        {
            int i = 0;
            foreach (Transform t in jail.GetComponentsInChildren<Transform>(true))
            {
                if (t.name != "RollCallPoint") continue;
                var stand = new GameObject($"Stand_{i++}");
                stand.transform.SetParent(zoneGo.transform, true);
                stand.transform.position = new Vector3(t.position.x, PrisonLayoutAnchors.FloorY, PrisonLayoutAnchors.RollCallCenter.z);
            }
        }

        WireRegistry(r => r.rollCallArea = zoneGo.GetComponent<PrisonLocationZone>());
    }

    private static void WireRegistry(System.Action<PrisonLocationRegistry> apply)
    {
        var registry = Object.FindAnyObjectByType<PrisonLocationRegistry>();
        if (registry == null) return;
        apply(registry);
        EditorUtility.SetDirty(registry);
    }

    public static void FixAllCellSpawns()
    {
        var jailCells = GameObject.Find("JailCells");
        if (jailCells == null) return;

        var registry = Object.FindAnyObjectByType<PrisonLocationRegistry>();
        int fixedCount = 0;
        int wiredCount = 0;

        foreach (Transform cell in jailCells.transform)
        {
            Transform floor = FindDeepChild(cell, "Floor");
            Transform spawnPoint = FindDeepChild(cell, "SpawnPoint");
            Transform rollCallPoint = FindDeepChild(cell, "RollCallPoint");

            if (spawnPoint == null && floor != null)
            {
                var spGo = new GameObject("SpawnPoint");
                spGo.transform.SetParent(cell, false);
                spawnPoint = spGo.transform;
            }

            if (floor != null && spawnPoint != null)
            {
                Vector3 floorPos = floor.position;
                Vector3 interior = cell.position - floorPos;
                interior.y = 0f;
                if (interior.sqrMagnitude < 0.25f)
                    interior = cell.forward.sqrMagnitude > 0.1f ? cell.forward : Vector3.forward;
                interior.Normalize();

                spawnPoint.position = floorPos + interior * 1.8f + Vector3.up * SpawnPlacementUtility.CharacterFloorOffset;
                spawnPoint.rotation = Quaternion.LookRotation(-interior, Vector3.up);
                fixedCount++;
            }

            if (rollCallPoint != null && floor != null)
            {
                rollCallPoint.position = new Vector3(
                    spawnPoint != null ? spawnPoint.position.x : floor.position.x,
                    PrisonLayoutAnchors.FloorY + SpawnPlacementUtility.CharacterFloorOffset,
                    PrisonLayoutAnchors.RollCallCenter.z);
            }

            if (registry != null && spawnPoint != null)
            {
                int idx = ParseJailCellIndex(cell.name);
                if (idx >= 0 && idx < registry.cells.Length)
                {
                    registry.cells[idx].spawnPoint = spawnPoint;
                    registry.cells[idx].rollCallStandPoint = rollCallPoint != null ? rollCallPoint : spawnPoint;
                    wiredCount++;
                }
            }
        }

        if (registry != null)
            EditorUtility.SetDirty(registry);

        Debug.Log($"[PrisonLayoutRebuild] Fixed {fixedCount} spawn points on cell floors; wired {wiredCount} registry cells.");
    }

    private static void RepositionWorldSpawns()
    {
        PrisonLootSetupRunner.PlaceWorldSpawnNodesFromLayout();
    }

    private static void BakeNavMesh()
    {
        var surface = Object.FindAnyObjectByType<NavMeshSurface>();
        if (surface == null)
        {
            Debug.LogWarning("[PrisonLayoutRebuild] No NavMeshSurface — skipping bake.");
            return;
        }

        surface.BuildNavMesh();
        EditorUtility.SetDirty(surface);
    }

    private static Transform CreateChild(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static void BuildFloorSlab(Transform parent, string name, Vector3 center, Vector3 size, string matPath = null)
    {
        var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        slab.name = name;
        slab.transform.SetParent(parent, true);
        slab.transform.position = center + Vector3.down * 0.05f;
        slab.transform.localScale = size;
        ApplyMat(slab, matPath ?? "Assets/Materials/Prison/PrisonFloor_Tile.mat");
    }

    private static void BuildWingRoom(Transform parent, string name, Vector3 center, Vector3 size, string sign)
    {
        Transform wing = CreateChild(parent, name);
        BuildFloorSlab(wing, name + "_Floor", center, new Vector3(size.x, 0.12f, size.z));
        BuildWall(wing, center + new Vector3(0f, size.y * 0.5f, size.z * 0.5f), new Vector3(size.x, size.y, 0.25f), name + "_N");
        // South face left open toward the hub corridor
        BuildWall(wing, center + new Vector3(size.x * 0.5f, size.y * 0.5f, 0f), new Vector3(0.25f, size.y, size.z), name + "_E");
        BuildWall(wing, center + new Vector3(-size.x * 0.5f, size.y * 0.5f, 0f), new Vector3(0.25f, size.y, size.z), name + "_W");
        AddSign(wing, center + new Vector3(0f, size.y + 0.5f, -size.z * 0.5f - 0.5f), sign);
    }

    private static void BuildWall(Transform parent, Vector3 pos, Vector3 scale, string wallName)
    {
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = wallName;
        wall.transform.SetParent(parent, true);
        wall.transform.position = pos;
        wall.transform.localScale = scale;
        ApplyMat(wall, "Assets/Materials/Prison/PrisonWall_Concrete.mat");
    }

    private static void PlaceDoorway(Transform parent, Vector3 pos, float yRot)
    {
        PlacePrefab(parent, "Assets/Models/Modular/Wall_Doorway_Modular.prefab", pos, yRot);
    }

    private static void PlacePrefab(Transform parent, string path, Vector3 pos, float yRot = 0f, float scale = 1f)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) return;
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.SetParent(parent, true);
        instance.transform.position = pos;
        instance.transform.rotation = Quaternion.Euler(0f, yRot, 0f);
        instance.transform.localScale = Vector3.one * scale;
    }

    private static void AddSign(Transform parent, Vector3 pos, string text)
    {
        var sign = new GameObject("Sign");
        sign.transform.SetParent(parent, true);
        sign.transform.position = pos;
        var tm = sign.AddComponent<TextMesh>();
        tm.text = text;
        tm.characterSize = 0.22f;
        tm.fontSize = 64;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = Color.white;
    }

    private static void ApplyMat(GameObject go, string matPath)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        var r = go.GetComponent<Renderer>();
        if (mat != null && r != null)
            r.sharedMaterial = mat;
    }

    private static Transform FindDeepChild(Transform root, string name)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    private static int ParseJailCellIndex(string cellName)
    {
        const string prefix = "JailCell_";
        if (!cellName.StartsWith(prefix)) return -1;
        return int.TryParse(cellName.Substring(prefix.Length), out int num) ? num - 1 : -1;
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(parent.GetChild(i).gameObject);
    }
}
