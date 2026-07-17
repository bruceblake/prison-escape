using System.Collections.Generic;
using System.Linq;
using Prison;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Installs the assembled <see cref="PrisonFacility.fbx"/> as the level geometry and wires gameplay anchors.
/// Replaces procedural LayoutFloors/Walls/RoomProps from the layout runner.
/// </summary>
public static class PrisonFacilityInstaller
{
    public const string FbxPath = "Assets/Models/BlenderKit/PrisonFacility.fbx";
    public const string PrefabPath = "Assets/Prefabs/BlenderKit/PrisonFacility.prefab";
    public const string RootName = "PrisonFacility";

    const float CafLocalX = 26f;
    const float CafLocalZ = 98f;
    const float HubX = -26f;
    const float HubZ = -98f;
    const float FloorY = 0.82f;

    static readonly HashSet<string> ColliderSkipTokens = new()
    {
        "Light", "Screw", "Tray", "Poster", "Clock", "Sign", "Monitor",
        "Hoop", "Weight", "Pull", "Food", "Kitchen", "Warmer",
        "Drain", "Faucet", "Barrel", "Pallet", "Crate", "Board", "Tool",
        "Cover", "Dummy", "Coin", "Vent", "Duct", "Pipe",
    };

    public static Vector3 FacilityPosition => new(HubX - CafLocalX, FloorY, HubZ - CafLocalZ);

    public static string FbxCellName(int gameCellNumber)
    {
        int fbxNum = gameCellNumber <= 8 ? 8 + gameCellNumber : gameCellNumber - 8;
        return $"Cell_{fbxNum:D2}";
    }

    [MenuItem("Prison/Layout/Install Prison Facility")]
    public static void InstallMenu()
    {
        ClearLegacyGeometry();
        var facility = Install();
        WireCellAnchors(facility);
        WireInteractables(facility);
        WireZones(facility);
        PrisonLevelLayoutRunner.WireRegistryPublic();
        PrisonLevelLayoutRunner.BuildEscapeSystemsPublic();
        PrisonLevelLayoutRunner.SaveScenePublic();
        Debug.Log("[PrisonFacility] Installed PrisonFacility.fbx and wired gameplay.");
    }

    public static GameObject Install()
    {
        ClearLegacyGeometry();

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var source = prefab != null ? prefab : AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
        if (source == null)
        {
            Debug.LogError("[PrisonFacility] Missing PrisonFacility FBX/prefab.");
            return null;
        }

        var root = GetOrCreateRoot(RootName);
        ClearChildren(root);

        var facility = (GameObject)PrefabUtility.InstantiatePrefab(source, root.transform);
        facility.name = "PrisonFacility_Mesh";
        facility.transform.SetPositionAndRotation(FacilityPosition, Quaternion.identity);
        RemapMaterials(facility);
        AddStructuralColliders(facility);
        AddCellPropColliders(facility);

        var renderers = facility.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            var b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            Debug.Log($"[PrisonFacility] Placed at {FacilityPosition}; bounds center {b.center}, size {b.size}.");
        }

        return facility;
    }

    public static void ClearLegacyGeometry()
    {
        foreach (var name in new[]
        {
            "LayoutFloors", "LayoutWalls", "LayoutRoofs", "LayoutSoffits", "LayoutLighting", "RoomProps",
            "EscapeSystems", "CafeteriaProps", "CourtyardProps", "WorkshopProps", "ShowerProps",
            "SecurityProps", "SolitaryProps", "CellBlockProps",
        })
        {
            var go = GameObject.Find(name);
            if (go != null) Object.DestroyImmediate(go);
        }

        var facilityRoot = GameObject.Find(RootName);
        if (facilityRoot != null) Object.DestroyImmediate(facilityRoot);

        foreach (var blockName in new[] { "JailCells", "JailCells_East" })
        {
            var block = GameObject.Find(blockName);
            if (block == null) continue;
            for (int i = block.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(block.transform.GetChild(i).gameObject);
        }

        foreach (var go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (go.transform.parent != null) continue;
            var n = go.name;
            if (n.StartsWith("Point Light")) { Object.DestroyImmediate(go); continue; }
            if (n is "CorridorLightFixtures" or "CeilingPipes" or "CorridorRailing") { Object.DestroyImmediate(go); continue; }
            if (n.StartsWith("RedAccentLight")) { Object.DestroyImmediate(go); continue; }
            if (n.StartsWith("SM_") && !n.StartsWith("SM_Item")) { Object.DestroyImmediate(go); continue; }
            if (!n.Contains("Floor") || n is "Ground") continue;
            if (n.StartsWith("ASM_")) continue;
            Object.DestroyImmediate(go);
        }
    }

    public static void WireCellAnchors(GameObject facility)
    {
        var west = GetOrCreateRoot("JailCells");
        var east = GetOrCreateRoot("JailCells_East");
        ClearChildren(west);
        ClearChildren(east);

        for (int gameNum = 1; gameNum <= 16; gameNum++)
        {
            var fbxName = FbxCellName(gameNum);
            var fbxCell = FindDeepChild(facility.transform, fbxName);
            if (fbxCell == null)
            {
                Debug.LogWarning($"[PrisonFacility] Missing FBX node for game cell {gameNum:D2} ({fbxName}).");
                continue;
            }

            var bed = FindDeepChild(facility.transform, $"{fbxName}_Bed");
            var door = FindDeepChild(facility.transform, $"{fbxName}_Door");
            ComputeCellAnchorPoints(fbxCell, bed, door, out var spawnPos, out var rollCallPos, out var nightApproachPos, out var facing);

            var block = gameNum <= 8 ? west.transform : east.transform;
            var anchor = new GameObject($"JailCell_{gameNum:D2}");
            anchor.transform.SetParent(block, false);
            anchor.transform.position = spawnPos;
            anchor.transform.rotation = facing;

            var spawnTf = CreateChildAt(anchor.transform, "SpawnPoint", spawnPos);
            spawnTf.rotation = facing;
            CreateChildAt(anchor.transform, "RollCallPoint", rollCallPos).rotation = facing;
            CreateChildAt(anchor.transform, "ShakedownCenter", spawnPos);
            CreateChildAt(anchor.transform, "NightCheckApproach", nightApproachPos);

            if (bed != null)
            {
                var bedRef = new GameObject("Bed");
                bedRef.transform.SetParent(anchor.transform, false);
                bedRef.transform.position = bed.position;
                bedRef.transform.rotation = bed.rotation;
            }

            var zone = anchor.GetComponent<PrisonLocationZone>();
            if (zone == null)
            {
                zone = anchor.gameObject.AddComponent<PrisonLocationZone>();
                zone.zoneType = ZoneType.Cell;
                zone.cellIndex = gameNum - 1;
                zone.hudDisplayName = $"CELL {gameNum:D2}";
            }

            AddCellZoneTrigger(anchor.transform, fbxCell, zone);
        }

        Debug.Log("[PrisonFacility] Wired 16 cell anchors from facility bed/door geometry.");
    }

    static void ComputeCellAnchorPoints(
        Transform fbxCell, Transform bed, Transform door,
        out Vector3 spawnPos, out Vector3 rollCallPos, out Vector3 nightApproachPos, out Quaternion facing)
    {
        float floorY = FloorY + 0.1f;
        Vector3 interior = bed != null ? bed.position : fbxCell.position;
        Vector3 doorPos = door != null ? door.position : fbxCell.position;

        if (bed != null)
        {
            var renderers = bed.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                var b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
                interior = new Vector3(b.center.x, floorY, b.center.z);
            }
            else
            {
                interior = new Vector3(bed.position.x, floorY, bed.position.z);
            }
        }
        else
        {
            interior.y = floorY;
        }

        Vector3 intoCell = interior - doorPos;
        intoCell.y = 0f;
        if (intoCell.sqrMagnitude < 0.01f)
            intoCell = fbxCell.forward;
        intoCell.Normalize();

        spawnPos = interior;
        rollCallPos = interior + intoCell * 0.6f;
        rollCallPos.y = floorY;
        nightApproachPos = doorPos - intoCell * 1.2f;
        nightApproachPos.y = floorY;
        facing = Quaternion.LookRotation(intoCell, Vector3.up);
    }

    public static Transform FindFacilityBed(int gameCellNumber)
    {
        var mesh = GameObject.Find($"{RootName}/PrisonFacility_Mesh");
        if (mesh == null) return null;
        return FindDeepChild(mesh.transform, $"{FbxCellName(gameCellNumber)}_Bed");
    }

    public static void WireInteractables(GameObject facility)
    {
        int doors = 0, beds = 0, stashes = 0, vents = 0;

        for (int gameNum = 1; gameNum <= 16; gameNum++)
        {
            string fbx = FbxCellName(gameNum);
            var shell = FindDeepChild(facility.transform, fbx);
            var bed = FindDeepChild(facility.transform, $"{fbx}_Bed");
            WireDoor(facility.transform, $"{fbx}_Door", shell, bed, ref doors);
            WireBed(facility.transform, $"{fbx}_Bed", gameNum - 1, ref beds);
            WirePillowStash(facility.transform, $"{fbx}_Pillow", ref stashes);
            WireVent(facility.transform, $"{fbx}_Cover", ref vents);
        }

        WireSolitarySpawns(facility.transform);
        Debug.Log($"[PrisonFacility] Interactables: {doors} doors, {beds} beds, {stashes} stashes, {vents} vents.");
    }

    public static void WireZones(GameObject facility)
    {
        WireAllFloorZones(facility.transform);
    }

    static void WireAllFloorZones(Transform facility)
    {
        var zones = new (string plate, ZoneType type, string hud)[]
        {
            ("ASM_CafeteriaFloor", ZoneType.Cafeteria, "CAFETERIA"),
            ("ASM_CourtyardFloor", ZoneType.Yard, "COURTYARD"),
            ("ASM_WorkshopFloor", ZoneType.Workshop, "WORKSHOP"),
            ("ASM_ShowerFloor", ZoneType.Showers, "SHOWERS"),
            ("ASM_MainSecurityFloor", ZoneType.Security, "SECURITY"),
            ("ASM_CellWingFloor_West", ZoneType.RollCallArea, "CELL BLOCK WEST"),
            ("ASM_CellWingFloor_East", ZoneType.RollCallArea, "CELL BLOCK EAST"),
            ("ASM_Corridor_North", ZoneType.Corridor, "NORTH CORRIDOR"),
            ("ASM_Corridor_South", ZoneType.Corridor, "SOUTH CORRIDOR"),
            ("ASM_Corridor_WestSpine", ZoneType.Corridor, "WEST SPINE"),
            ("ASM_Corridor_EastSpine", ZoneType.Corridor, "EAST SPINE"),
            ("ASM_Corridor_LoopNorth", ZoneType.Corridor, "LOOP NORTH"),
            ("ASM_Corridor_LoopSouth", ZoneType.Corridor, "LOOP SOUTH"),
            ("ASM_Corridor_SecurityLink", ZoneType.Corridor, "SECURITY LINK"),
        };

        foreach (var (plate, type, hud) in zones)
            WireZoneOnPlate(facility, plate, type, hud);

        AddStandPointsOnPlate(facility, "ASM_CafeteriaFloor", 6);
        AddStandPointsOnPlate(facility, "ASM_CourtyardFloor", 4);
    }

    public static Bounds GetFacilityBounds()
    {
        var facility = GameObject.Find(RootName);
        if (facility == null) return default;

        var renderers = facility.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(FacilityPosition, new Vector3(180f, 8f, 145f));

        var b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
        return b;
    }

    public static Transform[] GetSolitarySpawnPoints(Transform facility)
    {
        var spawns = new List<Transform>();
        for (int i = 1; i <= 4; i++)
        {
            var node = FindDeepChild(facility, $"Solitary_{i}");
            if (node == null) continue;
            var spawn = node.Find("SpawnPoint");
            if (spawn == null)
            {
                var go = new GameObject("SpawnPoint");
                go.transform.SetParent(node, false);
                go.transform.localPosition = Vector3.up * 0.1f;
                spawn = go.transform;
            }

            spawns.Add(spawn);
        }

        return spawns.ToArray();
    }

    static void WireSolitarySpawns(Transform facility)
    {
        for (int i = 1; i <= 4; i++)
        {
            var node = FindDeepChild(facility, $"Solitary_{i}");
            if (node == null) continue;
            if (node.Find("SpawnPoint") == null)
                CreateChildAt(node, "SpawnPoint", node.position + Vector3.up * 0.1f);
        }
    }

    static PrisonLocationZone FindZoneOnPlate(string plateName)
    {
        var plate = GameObject.Find(plateName);
        if (plate == null) return null;
        return plate.GetComponentInChildren<PrisonLocationZone>();
    }

    public static PrisonLocationZone FindZoneOnPlatePublic(string plateName) => FindZoneOnPlate(plateName);

    static void WireZoneOnPlate(Transform facility, string plateName, ZoneType type, string hud)
    {
        var plate = FindDeepChild(facility, plateName);
        if (plate == null) return;

        var trigger = plate.Find("LocationZoneTrigger");
        if (trigger == null)
        {
            var go = new GameObject("LocationZoneTrigger");
            go.transform.SetParent(plate, false);
            trigger = go.transform;
        }

        var host = trigger.gameObject;
        var zone = host.GetComponent<PrisonLocationZone>() ?? host.AddComponent<PrisonLocationZone>();
        zone.zoneType = type;
        zone.hudDisplayName = hud;

        var col = host.GetComponent<BoxCollider>();
        if (col == null)
            col = host.AddComponent<BoxCollider>();
        col.isTrigger = true;

        var renderers = plate.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            var b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            trigger.position = b.center;
            col.center = Vector3.zero;
            col.size = new Vector3(b.size.x, Mathf.Max(b.size.y, 6f), b.size.z);
        }
        else
        {
            trigger.localPosition = Vector3.zero;
            col.center = Vector3.zero;
            col.size = new Vector3(20f, 6f, 40f);
        }
    }

    static void AddCellZoneTrigger(Transform anchor, Transform fbxCell, PrisonLocationZone zone)
    {
        var trigger = anchor.Find("LocationZoneTrigger");
        if (trigger == null)
        {
            var go = new GameObject("LocationZoneTrigger");
            go.transform.SetParent(anchor, false);
            trigger = go.transform;
        }

        var host = trigger.gameObject;
        var zoneOnTrigger = host.GetComponent<PrisonLocationZone>() ?? host.AddComponent<PrisonLocationZone>();
        zoneOnTrigger.zoneType = zone.zoneType;
        zoneOnTrigger.cellIndex = zone.cellIndex;
        zoneOnTrigger.hudDisplayName = zone.hudDisplayName;
        if (anchor.GetComponent<PrisonLocationZone>() != null)
            Object.DestroyImmediate(anchor.GetComponent<PrisonLocationZone>());

        var col = host.GetComponent<BoxCollider>();
        if (col == null)
            col = host.AddComponent<BoxCollider>();
        col.isTrigger = true;

        var renderers = fbxCell.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            var b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            trigger.position = b.center;
            col.center = Vector3.zero;
            col.size = new Vector3(b.size.x * 1.1f, Mathf.Max(b.size.y, 3f), b.size.z * 1.1f);
        }
        else
        {
            trigger.localPosition = Vector3.zero;
            col.center = Vector3.zero;
            col.size = new Vector3(4f, 3f, 5f);
        }
    }

    static void AddStandPointsOnPlate(Transform facility, string plateName, int count)
    {
        var plate = FindDeepChild(facility, plateName);
        if (plate == null) return;

        var trigger = plate.Find("LocationZoneTrigger");
        if (trigger == null) return;

        for (int i = trigger.childCount - 1; i >= 0; i--)
        {
            if (trigger.GetChild(i).name.StartsWith("StandPoint_"))
                Object.DestroyImmediate(trigger.GetChild(i).gameObject);
        }

        var renderers = plate.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        var b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);

        var points = new List<Transform>();
        for (int i = 0; i < count; i++)
        {
            float t = (i + 0.5f) / count;
            var go = new GameObject($"StandPoint_{i + 1:D2}");
            go.transform.SetParent(trigger, false);
            go.transform.position = new Vector3(
                Mathf.Lerp(b.min.x, b.max.x, t),
                b.min.y + 0.1f,
                b.center.z);
            points.Add(go.transform);
        }

        var zone = trigger.GetComponent<PrisonLocationZone>();
        if (zone != null)
            zone.standPoints = points.ToArray();
    }

    static void WireDoor(Transform facility, string doorName, Transform cellShell, Transform bed, ref int count)
    {
        var door = FindDeepChild(facility, doorName);
        if (door == null) return;

        // Keep BlenderKit authored pose — shell-center re-align drifts doors one bay
        // sideways and can flip yaw relative to the mesh.
        RestoreAuthoredDoorPose(door);
        SetupDoorController(door, bed);
        count++;
    }

    /// <summary>
    /// Wires <see cref="CellDoorController"/> on an already-posed door (authored FBX pose).
    /// </summary>
    public static void SetupDoorController(Transform door, Transform bed)
    {
        if (door == null) return;
        var controller = door.GetComponent<CellDoorController>() ?? door.gameObject.AddComponent<CellDoorController>();
        controller.slideSpeed = 3f;
        controller.openOffset = ComputeDoorOpenOffsetLocal(door, bed);
        // Capture closed from the authored/restored pose, then keep the scene door there.
        // Leaving doors slid open caused Play Mode Start() to treat open as closed.
        controller.InitializeClosedPosition();
        door.localPosition = controller.closedLocalPosition;
        EnsureDoorCollider(door);
        EditorUtility.SetDirty(door.gameObject);
        EditorUtility.SetDirty(controller);
    }

    /// <summary>
    /// Restores local TRS from <see cref="PrefabPath"/> so doors sit in the BlenderKit
    /// doorway again after a bad align pass.
    /// </summary>
    public static bool RestoreAuthoredDoorPose(Transform door)
    {
        if (door == null) return false;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null) return false;

        Transform auth = null;
        foreach (var t in prefab.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == door.name)
            {
                auth = t;
                break;
            }
        }

        if (auth == null) return false;

        door.localPosition = auth.localPosition;
        door.localRotation = auth.localRotation;
        door.localScale = auth.localScale;
        EditorUtility.SetDirty(door.gameObject);
        return true;
    }

    /// <summary>
    /// Legacy align helper kept for EditMode tests. Runtime/facility wiring uses
    /// <see cref="RestoreAuthoredDoorPose"/> instead — shell-center placement drifts.
    /// </summary>
    public static void AlignDoorToCellWall(Transform door, Transform cellShell, Transform bed)
    {
        if (door == null || cellShell == null) return;

        // Prefer authored pose when this is a facility door.
        if (door.name.StartsWith("Cell_", System.StringComparison.Ordinal) && door.name.EndsWith("_Door"))
        {
            if (RestoreAuthoredDoorPose(door))
                return;
        }

        var bounds = GetObjectBounds(cellShell, excludeDoorAndBed: true);
        if (bounds.size.sqrMagnitude < 0.01f) return;

        Vector3 bedPos = bed != null ? bed.position : bounds.center;
        bedPos = new Vector3(
            Mathf.Clamp(bedPos.x, bounds.min.x, bounds.max.x),
            bedPos.y,
            Mathf.Clamp(bedPos.z, bounds.min.z, bounds.max.z));
        if (!TryGetCorridorDoorFace(bounds, bedPos, out Vector3 faceCenter, out Vector3 outwardNormal))
            return;

        // Project the door onto the corridor face, keeping authored lateral position.
        Vector3 pos = door.position;
        float depth = Vector3.Dot(pos - faceCenter, outwardNormal);
        pos -= depth * outwardNormal;
        pos.y = FloorY;

        door.rotation = Quaternion.LookRotation(-outwardNormal, Vector3.up);
        door.position = pos;

        var doorBounds = GetObjectBounds(door);
        if (doorBounds.size.sqrMagnitude > 0.01f)
        {
            float bottomOffset = doorBounds.min.y - door.position.y;
            // Only correct depth onto the face + floor; do NOT recentre laterally onto shell.
            Vector3 center = doorBounds.center;
            float depthErr = Vector3.Dot(center - faceCenter, outwardNormal);
            door.position -= outwardNormal * depthErr;
            door.position = new Vector3(door.position.x, FloorY - bottomOffset, door.position.z);
        }
    }

    static bool TryGetCorridorDoorFace(Bounds cellBounds, Vector3 bedPos, out Vector3 faceCenter, out Vector3 outwardNormal)
    {
        float floorY = cellBounds.min.y;
        var faces = new (Vector3 center, Vector3 normal)[]
        {
            (new Vector3(cellBounds.min.x, floorY, cellBounds.center.z), Vector3.left),
            (new Vector3(cellBounds.max.x, floorY, cellBounds.center.z), Vector3.right),
            (new Vector3(cellBounds.center.x, floorY, cellBounds.min.z), Vector3.back),
            (new Vector3(cellBounds.center.x, floorY, cellBounds.max.z), Vector3.forward),
        };

        int best = 0;
        float bestInward = float.NegativeInfinity;
        for (int i = 0; i < faces.Length; i++)
        {
            float inward = Vector3.Dot(bedPos - faces[i].center, -faces[i].normal);
            if (inward > bestInward)
            {
                bestInward = inward;
                best = i;
            }
        }

        faceCenter = faces[best].center;
        outwardNormal = faces[best].normal;
        return true;
    }

    static Bounds GetObjectBounds(Transform root, bool excludeDoorAndBed = false)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(true);

        Bounds bounds = default;
        bool has = false;
        foreach (var r in renderers)
        {
            if (r == null) continue;
            if (excludeDoorAndBed && IsDoorOrBedRenderer(r.transform))
                continue;

            if (!has)
            {
                bounds = r.bounds;
                has = true;
            }
            else
            {
                bounds.Encapsulate(r.bounds);
            }
        }

        return bounds;
    }

    static bool IsDoorOrBedRenderer(Transform t)
    {
        while (t != null)
        {
            if (t.GetComponent<CellDoorController>() != null)
                return true;
            string n = t.name;
            if (n.IndexOf("Door", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (n.IndexOf("Bed", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            t = t.parent;
        }
        return false;
    }

    public static void EnsureDoorCollider(Transform door)
    {
        var existing = door.GetComponent<Collider>();
        if (existing != null)
            Object.DestroyImmediate(existing);

        var bounds = GetObjectBounds(door);
        if (bounds.size.sqrMagnitude < 0.01f) return;

        var box = door.gameObject.AddComponent<BoxCollider>();
        box.center = door.InverseTransformPoint(bounds.center);
        Vector3 localSize = door.InverseTransformVector(bounds.size);
        box.size = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
    }

    static bool IsCellPropName(string objectName)
    {
        if (!objectName.StartsWith("Cell_")) return false;
        foreach (var suffix in new[] { "_Bed", "_Toilet", "_Sink", "_Shelf", "_Desk", "_Stool", "_Pillow" })
        {
            if (objectName.EndsWith(suffix, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    static void AddCellPropColliders(GameObject facility)
    {
        int added = 0;
        foreach (var mf in facility.GetComponentsInChildren<MeshFilter>(true))
        {
            if (!IsCellPropName(mf.gameObject.name)) continue;
            if (mf.sharedMesh == null) continue;
            if (mf.GetComponent<Collider>() != null) continue;

            var mc = mf.gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
            mc.convex = true;
            added++;
        }

        Debug.Log($"[PrisonFacility] Added {added} cell prop colliders.");
    }

    // Authored BlenderKit doors are ~1.2 m wide; slide just past that onto the pier.
    public const float DoorSlideDistance = 1.35f;

    public static Vector3 ComputeDoorOpenOffsetLocal(Transform door, Transform bed)
    {
        // door.forward is set by AlignDoorToCellWall to the wall inward normal.
        // Do not re-derive from the bed — off-center beds skew the slide axis.
        Vector3 intoCell = door.forward;
        intoCell.y = 0f;
        if (intoCell.sqrMagnitude < 0.01f)
            intoCell = Vector3.forward;
        intoCell.Normalize();

        Vector3 alongWall = Vector3.Cross(Vector3.up, intoCell);
        if (alongWall.sqrMagnitude < 0.01f)
            alongWall = door.right;
        alongWall.Normalize();

        Vector3 localDir = door.InverseTransformDirection(alongWall);
        localDir.y = 0f;
        if (localDir.sqrMagnitude < 0.01f)
            localDir = Vector3.right;

        float ax = Mathf.Abs(localDir.x);
        float az = Mathf.Abs(localDir.z);
        Vector3 axis = ax >= az
            ? new Vector3(Mathf.Sign(localDir.x), 0f, 0f)
            : new Vector3(0f, 0f, Mathf.Sign(localDir.z));

        // Slide at least the fitted door width so the opening fully clears.
        float doorWidth = 0f;
        var bounds = GetObjectBounds(door);
        if (bounds.size.sqrMagnitude > 0.01f)
            doorWidth = Mathf.Abs(Vector3.Dot(bounds.size, alongWall));
        float slide = Mathf.Max(DoorSlideDistance, doorWidth + 0.1f);
        // Cap so an open door parks on the pier, not in the neighbor opening (~4 m pitch).
        slide = Mathf.Min(slide, 1.6f);

        return axis * slide;
    }

    static void WireBed(Transform facility, string bedName, int cellIndex, ref int count)
    {
        var bed = FindDeepChild(facility, bedName);
        if (bed == null) return;

        var cellBed = bed.GetComponent<CellBed>() ?? bed.gameObject.AddComponent<CellBed>();
        cellBed.cellIndex = cellIndex;
        count++;
    }

    static void WirePillowStash(Transform facility, string pillowName, ref int count)
    {
        var pillow = FindDeepChild(facility, pillowName);
        if (pillow == null) return;

        if (pillow.GetComponent<PillowStash>() == null)
            pillow.gameObject.AddComponent<PillowStash>();
        count++;
    }

    static void WireVent(Transform facility, string coverName, ref int count)
    {
        var cover = FindDeepChild(facility, coverName);
        if (cover == null) return;

        var vent = cover.GetComponent<VentCover>() ?? cover.gameObject.AddComponent<VentCover>();
        vent.ventCoverTransform = cover;

        var screws = new List<InteractableScrew>();
        foreach (Transform child in cover)
        {
            if (!child.name.Contains("Screw", System.StringComparison.OrdinalIgnoreCase)) continue;
            var screw = child.GetComponent<InteractableScrew>() ?? child.gameObject.AddComponent<InteractableScrew>();
            screw.parentVent = vent;
            screws.Add(screw);
        }

        vent.screws = screws.ToArray();

        var passage = cover.Find("Passage") ?? cover;
        var col = passage.GetComponent<Collider>();
        if (col == null)
        {
            col = passage.gameObject.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.enabled = false;
        }

        vent.passageCollider = col;
        count++;
    }

    static void AddStructuralColliders(GameObject facility)
    {
        int added = 0;
        foreach (var mf in facility.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.sharedMesh == null) continue;
            if (ShouldSkipCollider(mf.gameObject.name)) continue;
            if (mf.GetComponent<MeshCollider>() != null) continue;

            var mc = mf.gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
            mc.convex = false;
            added++;
        }

        Debug.Log($"[PrisonFacility] Added {added} structural mesh colliders.");
    }

    static bool ShouldSkipCollider(string objectName)
    {
        if (IsCellPropName(objectName)) return true;
        if (objectName.EndsWith("_Door", System.StringComparison.OrdinalIgnoreCase)) return true;
        if (objectName.Contains("Screw", System.StringComparison.OrdinalIgnoreCase)) return true;
        if (objectName.Contains("Cover", System.StringComparison.OrdinalIgnoreCase)) return true;

        foreach (var token in ColliderSkipTokens)
        {
            if (objectName.Contains(token, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    static void RemapMaterials(GameObject root)
    {
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            var mats = renderer.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                var mapped = BlenderKitAssetSetup.MapMaterialPublic(mats[i]?.name ?? "");
                if (mapped != null) mats[i] = mapped;
            }

            renderer.sharedMaterials = mats;
        }

        BlenderKitAssetSetup.RemapMaterialsByObjectNamePublic(root);
    }

    static Transform FindDeepChild(Transform root, string name)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == name) return t;
        }

        return null;
    }

    static Transform CreateChildAt(Transform parent, string name, Vector3 worldPos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = worldPos;
        return go.transform;
    }

    static GameObject GetOrCreateRoot(string name)
    {
        var existing = GameObject.Find(name);
        return existing != null ? existing : new GameObject(name);
    }

    static void ClearChildren(GameObject root)
    {
        for (int i = root.transform.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
    }
}
