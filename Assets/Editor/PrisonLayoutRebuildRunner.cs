using Prison;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Reorganizes PrisonLevel1 into an enclosed shell: corridor tunnels, wing rooms,
/// yard gate, cafeteria wiring, and cell spawn fixes.
/// Menu: Prison / Rebuild Prison Layout
/// </summary>
public static class PrisonLayoutRebuildRunner
{
    private const string LayoutRootName = "PrisonLayout";

    [MenuItem("Prison/Fix Yard Zone Collider")]
    public static void FixYardZoneColliderMenu()
    {
        EnsureYardZoneSetup(null);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[PrisonLayoutRebuild] YardZone collider repaired.");
    }

    [MenuItem("Prison/Rebuild Prison Layout")]
    public static void Run()
    {
        Debug.Log("[PrisonLayoutRebuild] v4 — enclosed shell, corridor tunnels, wing rooms, yard gate.");
        RemoveMisplacedBuild();
        RestoreSceneWalls();
        ResetPerimeterWallPositions();
        RelocateLegacyCorridor();
        Transform layoutRoot = EnsureLayoutRoot();
        BuildCorridorNetwork(layoutRoot);
        BuildExpandedWings(layoutRoot);
        WireExistingCafeteria();
        BuildNorthYard(layoutRoot);
        RestoreCellFloorColliders();
        DisableCellRoofColliders();
        SetupZones();
        FixAllCellSpawns();
        RepositionWorldSpawns();
        BakeNavMesh();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[PrisonLayoutRebuild] Layout rebuilt. Enter Play Mode to verify spawns and wings.");
    }

    /// <summary>
    /// Wires zones, spawns, and registry without rebuilding procedural geometry.
    /// Used by the ProBuilder rebuild pipeline after shell bake.
    /// </summary>
    public static void WireGameplayIntegration()
    {
        WireExistingCafeteria();
        SetupZones();
        FixAllCellSpawns();
        RepositionWorldSpawns();
        BakeNavMesh();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[PrisonLayoutRebuild] Gameplay zones and spawns wired (no procedural geometry rebuild).");
    }

    private static void RemoveMisplacedBuild()
    {
        var levelBuild = GameObject.Find("LevelBuild");
        if (levelBuild != null)
            Object.DestroyImmediate(levelBuild);

        foreach (var name in new[] { "MaintenanceTunnel", "VentCrawlspace" })
        {
            var go = GameObject.Find(name);
            if (go != null) Object.DestroyImmediate(go);
        }
    }

    /// <summary>
    /// Re-enables perimeter wall colliders/renderers disabled by earlier rebuild passes.
    /// </summary>
    private static void RestoreSceneWalls()
    {
        int restored = 0;
        foreach (Collider col in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
        {
            if (col == null || col.isTrigger || col.enabled) continue;
            if (!IsWallLikeCollider(col)) continue;
            col.enabled = true;
            EditorUtility.SetDirty(col);
            restored++;
        }

        foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (r == null || r.enabled) continue;
            if (!IsWallLikeRenderer(r)) continue;
            r.enabled = true;
            EditorUtility.SetDirty(r.gameObject);
            restored++;
        }

        if (restored > 0)
            Debug.Log($"[PrisonLayoutRebuild] Restored {restored} wall colliders/renderers.");
    }

    /// <summary>
    /// Undo outward perimeter shifts from legacy open-layout rebuilds.
    /// </summary>
    private static void ResetPerimeterWallPositions()
    {
        int reset = 0;
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (t.name.StartsWith("LeftPrisonWall") && t.position.x <= -90f)
            {
                Vector3 p = t.position;
                p.x = PrisonLayoutAnchors.InnerLeftWallX;
                t.position = p;
                reset++;
                EditorUtility.SetDirty(t.gameObject);
            }
            else if (t.name.StartsWith("RightPrisonWall") && t.position.x >= 40f)
            {
                Vector3 p = t.position;
                p.x = PrisonLayoutAnchors.InnerRightWallX;
                t.position = p;
                reset++;
                EditorUtility.SetDirty(t.gameObject);
            }
        }

        if (reset > 0)
            Debug.Log($"[PrisonLayoutRebuild] Reset {reset} perimeter wall segments to inner positions.");
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

        BuildEnclosedCorridor(corridors, "CellSouthConnector",
            PrisonLayoutAnchors.CellSouthConnectorCenter, new Vector3(62f, 0.15f, 6f));

        BuildEnclosedCorridor(corridors, "MainHubCorridor",
            PrisonLayoutAnchors.MainCorridorCenter, new Vector3(52f, 0.15f, 8f));

        BuildEnclosedCorridor(corridors, "WestWingConnector",
            new Vector3(-57f, PrisonLayoutAnchors.CorridorY, -45f), new Vector3(62f, 0.15f, 7f));

        BuildEnclosedCorridor(corridors, "EastWingConnector",
            new Vector3(11f, PrisonLayoutAnchors.CorridorY, -45f), new Vector3(74f, 0.15f, 7f));

        BuildEnclosedCorridor(corridors, "NorthYardConnector",
            new Vector3(-26f, PrisonLayoutAnchors.CorridorY, 66f), new Vector3(62f, 0.15f, 8f));

        BuildEnclosedCorridor(corridors, "CellExitRamp_W",
            new Vector3(-49f, PrisonLayoutAnchors.CorridorY, -27f), new Vector3(8f, 0.15f, 14f));

        BuildEnclosedCorridor(corridors, "CellExitRamp_E",
            new Vector3(-3f, PrisonLayoutAnchors.CorridorY, -27f), new Vector3(8f, 0.15f, 14f));

        CutWallDoorway(new Vector3(PrisonLayoutAnchors.InnerLeftWallX, 2f, -45f), new Vector3(12f, 6f, 14f));
        CutWallDoorway(new Vector3(PrisonLayoutAnchors.InnerRightWallX, 2f, -45f), new Vector3(12f, 6f, 14f));
        CutWallDoorway(new Vector3(-26f, 2f, -30f), new Vector3(58f, 6f, 12f));

        PlaceDoorway(corridors, PrisonLayoutAnchors.WorkshopCenter + new Vector3(14f, 0f, 0f), 90f);
        PlaceDoorway(corridors, PrisonLayoutAnchors.LaundryCenter + new Vector3(-14f, 0f, 0f), -90f);
        PlaceDoorway(corridors, PrisonLayoutAnchors.YardGateCenter, 0f);
    }

    private static void BuildEnclosedCorridor(Transform parent, string name, Vector3 center, Vector3 floorSize)
    {
        Transform corridor = CreateChild(parent, name);
        BuildFloorSlab(corridor, name + "_Floor", center, floorSize);

        float h = PrisonLayoutAnchors.WallHeight;
        float halfX = floorSize.x * 0.5f;
        float halfZ = floorSize.z * 0.5f;
        float wallY = h * 0.5f;

        BuildWall(corridor, center + new Vector3(0f, wallY, halfZ), new Vector3(floorSize.x, h, 0.25f), name + "_N");
        BuildWall(corridor, center + new Vector3(0f, wallY, -halfZ), new Vector3(floorSize.x, h, 0.25f), name + "_S");
        BuildWall(corridor, center + new Vector3(halfX, wallY, 0f), new Vector3(0.25f, h, floorSize.z), name + "_E");
        BuildWall(corridor, center + new Vector3(-halfX, wallY, 0f), new Vector3(0.25f, h, floorSize.z), name + "_W");
    }

    private static void BuildExpandedWings(Transform root)
    {
        Transform wings = CreateChild(root, "ServiceWings");

        BuildEnclosedWingRoom(wings, "WorkshopWing", PrisonLayoutAnchors.WorkshopCenter,
            new Vector3(20f, PrisonLayoutAnchors.WallHeight, 16f), "WORKSHOP", 90f);

        BuildEnclosedWingRoom(wings, "LaundryWing", PrisonLayoutAnchors.LaundryCenter,
            new Vector3(20f, PrisonLayoutAnchors.WallHeight, 16f), "LAUNDRY", -90f);

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
    }

    private static void BuildEnclosedWingRoom(Transform parent, string name, Vector3 center, Vector3 size, string sign, float doorYRot)
    {
        Transform wing = CreateChild(parent, name);
        float h = size.y;
        float halfX = size.x * 0.5f;
        float halfZ = size.z * 0.5f;
        float wallY = h * 0.5f;

        BuildFloorSlab(wing, name + "_Floor", center, new Vector3(size.x, 0.12f, size.z));
        BuildWall(wing, center + new Vector3(0f, wallY, halfZ), new Vector3(size.x, h, 0.25f), name + "_N");
        BuildWall(wing, center + new Vector3(0f, wallY, -halfZ), new Vector3(size.x, h, 0.25f), name + "_S");
        BuildWall(wing, center + new Vector3(halfX, wallY, 0f), new Vector3(0.25f, h, size.z), name + "_E");
        BuildWall(wing, center + new Vector3(-halfX, wallY, 0f), new Vector3(0.25f, h, size.z), name + "_W");

        Vector3 doorPos = doorYRot > 0f
            ? center + new Vector3(halfX, 0f, 0f)
            : center + new Vector3(-halfX, 0f, 0f);
        PlaceDoorway(wing, doorPos, doorYRot);
        AddSign(wing, center + new Vector3(0f, h + 0.5f, -halfZ - 0.5f), sign);
    }

    private static void WireExistingCafeteria()
    {
        var cafeteriaRoot = GameObject.Find("Cafeteria");
        Vector3 zoneCenter = PrisonLayoutAnchors.CafeteriaCenter;
        if (cafeteriaRoot != null)
            zoneCenter = cafeteriaRoot.transform.position;

        var zoneGo = GameObject.Find("CafeteriaZone") ?? new GameObject("CafeteriaZone");
        zoneGo.transform.position = zoneCenter + Vector3.up * 0.5f;

        var box = EnsureBoxTriggerCollider(zoneGo);
        box.size = new Vector3(28f, 4f, 24f);
        box.center = Vector3.zero;

        var zone = zoneGo.GetComponent<PrisonLocationZone>() ?? zoneGo.AddComponent<PrisonLocationZone>();
        zone.zoneType = ZoneType.Cafeteria;
        zone.hudDisplayName = "CAFETERIA";

        WireRegistry(r => r.cafeteria = zoneGo.GetComponent<PrisonLocationZone>());

        RemoveDuplicateKitchenSigns(zoneCenter);
        Debug.Log("[PrisonLayoutRebuild] Wired existing cafeteria zone.");
    }

    private static void RemoveDuplicateKitchenSigns(Vector3 cafeteriaCenter)
    {
        int removed = 0;
        foreach (TextMesh tm in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))
        {
            if (tm == null || tm.text != "KITCHEN") continue;
            if (Vector3.Distance(tm.transform.position, cafeteriaCenter) > 35f) continue;
            Object.DestroyImmediate(tm.gameObject);
            removed++;
        }

        var layout = GameObject.Find(LayoutRootName);
        if (layout != null)
        {
            foreach (Transform t in layout.GetComponentsInChildren<Transform>(true))
            {
                if (t.name != "KitchenBackRoom" && !t.name.Contains("KitchenBackRoom")) continue;
                Object.DestroyImmediate(t.gameObject);
                removed++;
            }
        }

        if (removed > 0)
            Debug.Log($"[PrisonLayoutRebuild] Removed {removed} duplicate kitchen sign(s)/room(s) near cafeteria.");
    }

    private static void BuildNorthYard(Transform root)
    {
        Transform yard = CreateChild(root, "YardExpansion");

        BuildFloorSlab(yard, "YardPad", PrisonLayoutAnchors.YardCenter, new Vector3(70f, 0.12f, 40f),
            "Assets/Materials/Prison/PrisonFloor_Tile.mat");

        Vector3 c = PrisonLayoutAnchors.YardCenter;
        const float halfW = 35f;
        const float halfD = 20f;

        for (int i = -4; i <= 4; i++)
            PlacePrefab(yard, "Assets/Models/Modular/Fence_Panel_Modular.prefab", c + new Vector3(i * 8f, 0f, halfD));

        for (int i = -3; i <= 3; i++)
        {
            PlacePrefab(yard, "Assets/Models/Modular/Fence_Panel_Modular.prefab", c + new Vector3(halfW, 0f, i * 6f));
            PlacePrefab(yard, "Assets/Models/Modular/Fence_Panel_Modular.prefab", c + new Vector3(-halfW, 0f, i * 6f));
        }

        PlacePrefab(yard, "Assets/Models/GymBench.prefab", c + new Vector3(-15f, 0f, -5f));
        PlacePrefab(yard, "Assets/Models/GymBench.prefab", c + new Vector3(12f, 0f, 6f), 90f);
        PlacePrefab(yard, "Assets/Models/StorageCrate.prefab", c + new Vector3(18f, 0f, -8f));

        var tunnel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tunnel.name = "MaintenanceTunnel";
        tunnel.transform.SetParent(yard, true);
        tunnel.transform.position = PrisonLayoutAnchors.MainCorridorCenter + new Vector3(0f, -0.5f, 0f);
        tunnel.transform.localScale = new Vector3(8f, 0.6f, 18f);
        ApplyMat(tunnel, "Assets/Materials/Prison/PrisonWall_Concrete.mat");
    }

    /// <summary>
    /// Re-enables cell/corridor floor colliders that doorway cuts may have disabled by mistake.
    /// </summary>
    private static void RestoreCellFloorColliders()
    {
        int restored = 0;
        foreach (Collider col in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
        {
            if (col == null || col.isTrigger || col.enabled) continue;
            if (!IsFloorLikeCollider(col)) continue;
            col.enabled = true;
            EditorUtility.SetDirty(col);
            restored++;
        }

        if (restored > 0)
            Debug.Log($"[PrisonLayoutRebuild] Restored {restored} floor colliders.");
    }

    private static void DisableCellRoofColliders()
    {
        var jail = GameObject.Find("JailCells");
        if (jail == null) return;

        int notWalkableArea = UnityEngine.AI.NavMesh.GetAreaFromName("Not Walkable");
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

    private static void CutWallDoorway(Vector3 center, Vector3 size)
    {
        Bounds box = new Bounds(center, size);
        foreach (Collider col in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
        {
            if (col == null || col.isTrigger) continue;
            if (!col.bounds.Intersects(box)) continue;
            if (col.bounds.min.y > 6f) continue;
            if (IsFloorLikeCollider(col)) continue;
            if (!IsWallLikeCollider(col)) continue;
            col.enabled = false;
            EditorUtility.SetDirty(col);
        }

        foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (r == null) continue;
            if (!r.bounds.Intersects(box)) continue;
            if (r.bounds.min.y > 8f) continue;
            if (!IsWallLikeRenderer(r)) continue;
            r.enabled = false;
            EditorUtility.SetDirty(r.gameObject);
        }
    }

    private static bool IsFloorLikeCollider(Collider col)
    {
        string n = col.gameObject.name;
        if (n.Contains("Floor") || n.Contains("Connector") || n.Contains("Ramp") || n.Contains("Slab"))
            return true;

        Bounds b = col.bounds;
        return b.size.y <= 1.5f && b.max.y <= 4f;
    }

    private static bool IsWallLikeCollider(Collider col)
    {
        string n = col.gameObject.name;
        if (n.Contains("Wall") || n.Contains("Perimeter") || n.Contains("Doorway") || n.Contains("Prison"))
            return true;

        Bounds b = col.bounds;
        return b.size.y >= 2f && b.min.y < 6f;
    }

    private static bool IsWallLikeRenderer(Renderer r)
    {
        string n = r.gameObject.name;
        if (n.Contains("Wall") || n.Contains("Perimeter") || n.Contains("Doorway") ||
            n.Contains("Prison") || n.Contains("Modular"))
            return true;

        Bounds b = r.bounds;
        return b.size.y >= 2f && b.min.y < 8f;
    }

    private static void SetupZones()
    {
        SetupYardZone();
        SetupRollCallZone();
    }

    private static void SetupYardZone()
    {
        EnsureYardZoneSetup(null);
    }

    /// <summary>
    /// Ensures YardZone exists with a trigger BoxCollider (handles missing/broken component slots).
    /// Called from loot setup as well.
    /// </summary>
    public static void EnsureYardZoneSetup(Vector3? worldCenter)
    {
        var zoneGo = GameObject.Find("YardZone");
        if (zoneGo == null)
        {
            zoneGo = new GameObject("YardZone");
            Undo.RegisterCreatedObjectUndo(zoneGo, "Create YardZone");
        }

        Vector3 center = worldCenter ?? PrisonLayoutAnchors.YardCenter;
        zoneGo.transform.position = center + Vector3.up * 0.5f;

        var box = EnsureBoxTriggerCollider(zoneGo);
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
            stand.transform.position = center + new Vector3(-20f + i * 10f, 0f, 0f);
        }

        WireRegistry(r => r.yard = zone);
        EditorUtility.SetDirty(zoneGo);
    }

    private static BoxCollider EnsureBoxTriggerCollider(GameObject go)
    {
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);

        if (!go.TryGetComponent(out BoxCollider box))
            box = Undo.AddComponent<BoxCollider>(go);

        box.isTrigger = true;
        return box;
    }

    private static void SetupRollCallZone()
    {
        var zoneGo = GameObject.Find("RollCallArea") ?? new GameObject("RollCallArea");
        zoneGo.transform.position = PrisonLayoutAnchors.RollCallCenter;

        var box = EnsureBoxTriggerCollider(zoneGo);
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
        sign.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
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
