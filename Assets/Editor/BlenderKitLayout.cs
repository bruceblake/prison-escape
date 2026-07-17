using System.Collections.Generic;
using Prison;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Instantiates BlenderKit modular prefabs for the prison layout runner.
/// Kit rules: 0.5 m snap grid, 1 m modules — see Blender Asset Kit vault note.
/// </summary>
public static class BlenderKitLayout
{
    public const string LightFixture = "SM_Light_CeilingFixture";

    static readonly int[] WallModuleSizes = { 4, 2, 1 };
    static readonly int[] FloorModuleSizes = { 4, 2 };

    public static bool IsAvailable => BlenderKitCatalog.HasKit;

    public static GameObject PlaceKit(string assetName, Transform parent, Vector3 worldPos, Quaternion worldRot,
        bool structural = false)
    {
        var prefab = BlenderKitCatalog.LoadPrefab(assetName);
        if (prefab == null) return null;

        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.transform.SetPositionAndRotation(worldPos, worldRot);
        go.name = assetName;

        if (!structural)
            StripColliders(go);

        return go;
    }

    public static void TileFloorPlate(Transform parent, string plateName, Vector3 center, float width, float depth, float floorY)
    {
        TileRectangle(parent, plateName, center, width, depth, floorY, Quaternion.identity, FloorModuleSizes, "SM_Floor_Tile_{0}m", structural: true);
    }

    public static int TileWallRun(Transform parent, string prefix, Vector3 start, Vector3 end, float floorTop, float wallHeight,
        bool alongX, bool structural = true)
    {
        var dir = (end - start);
        float length = dir.magnitude;
        if (length < 0.05f) return 0;

        var forward = dir.normalized;
        var rot = Quaternion.LookRotation(alongX ? forward : forward, Vector3.up);
        if (!alongX)
            rot = Quaternion.LookRotation(forward, Vector3.up);

        int placed = 0;
        float cursor = 0f;
        int seg = 0;
        while (cursor < length - 0.01f)
        {
            float remaining = length - cursor;
            int module = PickModuleSize(remaining, WallModuleSizes);
            if (module <= 0) break;

            float mid = cursor + module * 0.5f;
            Vector3 pos = start + forward * mid;
            pos.y = floorTop;

            string asset = $"SM_Wall_Straight_{module}m";
            PlaceKit(asset, parent, pos, rot, structural);
            placed++;
            cursor += module;
            seg++;
        }

        return placed;
    }

    public static GameObject PlaceDoorway(Transform parent, string name, Vector3 worldPos, Quaternion rot, float floorTop)
    {
        var pos = worldPos;
        pos.y = floorTop;
        var go = PlaceKit("SM_Wall_Doorway_4m", parent, pos, rot, structural: true);
        if (go != null) go.name = name;
        return go;
    }

    public static void TileRoofPlate(Transform parent, string plateName, Vector3 center, float width, float depth, float ceilingY)
    {
        TileRectangle(parent, "Roof_" + plateName, center, width + 1f, depth + 1f, ceilingY, Quaternion.identity, FloorModuleSizes,
            "SM_Roof_Slab_{0}m", structural: false);
    }

    public static GameObject PlaceLight(Transform parent, string name, Vector3 worldPos, bool indoor)
    {
        var go = PlaceKit(LightFixture, parent, worldPos, Quaternion.identity, structural: false);
        if (go == null) return null;
        go.name = name;

        var light = go.GetComponentInChildren<Light>();
        if (light != null)
        {
            light.intensity = indoor ? 6f : 3.5f;
            light.range = indoor ? 14f : 18f;
            light.shadows = indoor ? LightShadows.Soft : LightShadows.None;
        }

        return go;
    }

    public static GameObject PlaceCellShell(Transform cell, Vector3 spawnPos, Quaternion cellRotation)
    {
        StripNamedChildren(cell, "CellShell", "CellKit");

        var shell = PlaceKit("SM_Cell_Shell_4x5-5m", cell, spawnPos, cellRotation, structural: true);
        if (shell != null) shell.name = "CellKit_Shell";
        return shell;
    }

    public static GameObject PlaceCellDoor(Transform cell, Vector3 spawnPos, Quaternion cellRotation)
    {
        var existing = cell.Find("CellDoor") ?? cell.Find("CellKit_Door");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        // Door module origin is on the shell front; offset to corridor side (+cell.right).
        var doorPos = spawnPos + cell.right * 2f;
        var door = PlaceKit("SM_Cell_DoorBarred", cell, doorPos, cellRotation, structural: false);
        if (door == null) return null;

        door.name = "CellKit_Door";

        // Canonical alignment path (same as the facility installer): snap to the
        // corridor-facing shell wall, slide 2.4 m along the wall, bake the closed pose.
        var shell = cell.Find("CellKit_Shell");
        var bed = cell.Find("CellKit_Bed");
        if (shell != null)
            PrisonFacilityInstaller.AlignDoorToCellWall(door.transform, shell, bed);

        var controller = door.GetComponent<CellDoorController>() ?? door.AddComponent<CellDoorController>();
        controller.openOffset = PrisonFacilityInstaller.ComputeDoorOpenOffsetLocal(door.transform, bed);
        controller.slideSpeed = 3f;
        controller.InitializeClosedPosition();
        PrisonFacilityInstaller.EnsureDoorCollider(door.transform);
        return door;
    }

    public static void FurnishCellInterior(Transform cell, Vector3 spawnPos, Quaternion rot)
    {
        var right = rot * Vector3.right;
        var back = rot * Vector3.back;

        PlaceKitProp(cell, "SM_Cell_Bed", spawnPos + back * 1.8f + right * 0.8f, rot, "CellKit_Bed", bed: true);
        PlaceKitProp(cell, "SM_Cell_Pillow", spawnPos + back * 1.8f + right * 1.2f, rot, "CellKit_Pillow", pillow: true);
        PlaceKitProp(cell, "SM_Cell_Toilet", spawnPos + back * 2.2f - right * 1.2f, rot, "CellKit_Toilet");
        PlaceKitProp(cell, "SM_Cell_Sink", spawnPos + back * 0.5f - right * 1.5f, rot, "CellKit_Sink");
        PlaceKitProp(cell, "SM_Cell_Shelf", spawnPos + back * 2.3f + right * 0f, rot, "CellKit_Shelf");
        PlaceKitProp(cell, "SM_Prop_CellDesk", spawnPos + back * 1.0f + right * 1.3f, rot, "CellKit_Desk");
        PlaceKitProp(cell, "SM_Prop_CellStool", spawnPos + back * 0.8f + right * 1.3f, rot, "CellKit_Stool");
        PlaceVentCover(cell, spawnPos + back * 2.5f, rot);
    }

    static void PlaceKitProp(Transform parent, string asset, Vector3 pos, Quaternion rot, string childName,
        bool bed = false, bool pillow = false)
    {
        var old = parent.Find(childName);
        if (old != null) Object.DestroyImmediate(old.gameObject);

        var go = PlaceKit(asset, parent, pos, rot, structural: false);
        if (go == null) return;
        go.name = childName;

        if (bed && go.GetComponent<CellBed>() == null)
            go.AddComponent<CellBed>();

        if (pillow && go.GetComponent<PillowStash>() == null)
            go.AddComponent<PillowStash>();
    }

    static void PlaceVentCover(Transform cell, Vector3 backWallPos, Quaternion rot)
    {
        var old = cell.Find("CellKit_Vent");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        var vent = PlaceKit("SM_Vent_Cover", cell, backWallPos + Vector3.up * 1.5f, rot, structural: false);
        if (vent == null) return;
        vent.name = "CellKit_Vent";

        var cover = vent.GetComponent<VentCover>() ?? vent.AddComponent<VentCover>();
        var screws = new List<InteractableScrew>();
        foreach (Transform child in vent.transform)
        {
            if (!child.name.ToLowerInvariant().Contains("screw")) continue;
            var screw = child.GetComponent<InteractableScrew>() ?? child.gameObject.AddComponent<InteractableScrew>();
            screw.parentVent = cover;
            screws.Add(screw);
        }

        cover.screws = screws.ToArray();
    }

    public static void TileCourtyardFence(Transform parent, Vector3 center, float halfW, float halfD, float floorY)
    {
        float zNorth = center.z + halfD;
        float zSouth = center.z - halfD;
        float xWest = center.x - halfW;
        float xEast = center.x + halfW;
        var y = floorY;

        TileFenceRun(parent, new Vector3(xWest, y, zSouth), new Vector3(xEast, y, zSouth), "Fence_S");
        TileFenceRun(parent, new Vector3(xWest, y, zNorth), new Vector3(xEast, y, zNorth), "Fence_N");
        TileFenceRun(parent, new Vector3(xWest, y, zSouth), new Vector3(xWest, y, zNorth), "Fence_W", alongX: false);
        TileFenceRun(parent, new Vector3(xEast, y, zSouth), new Vector3(xEast, y, zNorth), "Fence_E", alongX: false);

        PlaceKit("SM_Fence_CornerPost", parent, new Vector3(xWest, y, zSouth), Quaternion.identity, false);
        PlaceKit("SM_Fence_CornerPost", parent, new Vector3(xEast, y, zSouth), Quaternion.identity, false);
        PlaceKit("SM_Fence_CornerPost", parent, new Vector3(xWest, y, zNorth), Quaternion.identity, false);
        PlaceKit("SM_Fence_CornerPost", parent, new Vector3(xEast, y, zNorth), Quaternion.identity, false);

        // Pre-cut panel for future wire-cutter route (north side center).
        PlaceKit("SM_Fence_Panel_4m_Cut", parent, new Vector3(center.x, y, zNorth), Quaternion.Euler(0f, 180f, 0f), false);
    }

    static void TileFenceRun(Transform parent, Vector3 start, Vector3 end, string prefix, bool alongX = true)
    {
        var dir = end - start;
        float len = dir.magnitude;
        if (len < 0.1f) return;
        var forward = dir.normalized;
        var rot = Quaternion.LookRotation(forward, Vector3.up);
        float cursor = 0f;
        int i = 0;
        while (cursor < len - 0.01f)
        {
            float remaining = len - cursor;
            float module = remaining >= 4f ? 4f : remaining;
            Vector3 pos = start + forward * (cursor + module * 0.5f);
            var piece = PlaceKit(module >= 3.5f ? "SM_Fence_Panel_4m" : "SM_Fence_Post", parent, pos, rot, false);
            if (piece != null) piece.name = $"{prefix}_{i}";
            cursor += module;
            i++;
        }
    }

    static void TileRectangle(Transform parent, string groupName, Vector3 center, float width, float depth, float y,
        Quaternion rot, int[] moduleSizes, string assetPattern, bool structural)
    {
        var group = new GameObject(groupName);
        group.transform.SetParent(parent, false);
        group.transform.position = new Vector3(center.x, y, center.z);

        float startX = -width * 0.5f;
        float startZ = -depth * 0.5f;
        float cx = 0f;
        while (cx < width - 0.01f)
        {
            float rowW = PickModuleSize(width - cx, moduleSizes);
            float cz = 0f;
            while (cz < depth - 0.01f)
            {
                float rowD = PickModuleSize(depth - cz, moduleSizes);
                var local = new Vector3(startX + cx + rowW * 0.5f, 0f, startZ + cz + rowD * 0.5f);
                var world = group.transform.TransformPoint(local);
                string asset = string.Format(assetPattern, Mathf.RoundToInt(rowW) == rowW ? (int)rowW : 2);
                PlaceKit(asset, group.transform, world, rot, structural);
                cz += rowD;
            }

            cx += rowW;
        }
    }

    static int PickModuleSize(float remaining, int[] modules)
    {
        foreach (var m in modules)
        {
            if (remaining >= m - 0.01f)
                return m;
        }

        return remaining > 0.4f ? modules[^1] : 0;
    }

    static void StripColliders(GameObject go)
    {
        foreach (var col in go.GetComponentsInChildren<Collider>(true))
            Object.DestroyImmediate(col);
    }

    static void StripNamedChildren(Transform parent, params string[] prefixes)
    {
        var remove = new List<GameObject>();
        foreach (Transform child in parent)
        {
            foreach (var p in prefixes)
            {
                if (!child.name.StartsWith(p)) continue;
                remove.Add(child.gameObject);
                break;
            }
        }

        foreach (var go in remove)
            Object.DestroyImmediate(go);
    }
}
