using System.Collections.Generic;
using System.Linq;
using Prison;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Block-out prison layout: connected diagram floors, 6 m walls/roofs, scratch-built props, full lighting.
/// Geometry build v2 — floor sync, door jambs, roof overhang, exterior soffits.
/// </summary>
public static class PrisonLevelLayoutRunner
{
    const float WallThickness = 0.2f;
    const float RoofThickness = 0.2f;
    const float FloorScaleY = 0.2f;
    const float EdgeTolerance = 0.35f;
    const float RoofOverhang = 0.5f;
    const float SoffitDrop = 0.35f;
    const float CellInteriorWidth = 4f;
    const float CellInteriorDepth = 5.5f;
    const float LegacyShellMinScale = 8f;

    /// <summary>Floor cube center Y — synced to jail cell spawn height during each build.</summary>
    static float _floorY = 0.6f;
    static float FloorSurfaceY => _floorY + FloorScaleY * 0.5f;

    // Hub — cafeteria center (diagram: cells west/east, yard north, showers south).
    const float HubX = -26f;
    const float HubZ = -98f;

    const string WallMatPath = "Assets/Materials/Prison/PrisonWall_Concrete.mat";
    const string CeilingMatPath = "Assets/Materials/Prison/PrisonCeiling_Concrete.mat";
    const string LightMatPath = "Assets/Materials/Prison/PrisonLight_Emissive.mat";
    const string MetalMatPath = "Assets/Materials/Prison/PrisonMetal_Shelf.mat";
    const string PanelMatPath = "Assets/Materials/Prison/Accents/Accent_PanelWhite.mat";
    const string SecurityMatPath = "Assets/Materials/Prison/Accents/Accent_SecurityRed.mat";
    const string TileMatPath = "Assets/Materials/Prison/PrisonFloor_Tile.mat";
    const string ToiletMatPath = "Assets/Materials/Prison/PrisonToilet_Porcelain.mat";
    const string SinkMatPath = "Assets/Materials/Prison/PrisonSink_Metal.mat";
    const string BlanketMatPath = "Assets/Materials/Prison/PrisonBed_Blanket.mat";

    static readonly Dictionary<string, string> RightCellRename = new()
    {
        { "JailCell_01", "JailCell_09" },
        { "JailCell_02", "JailCell_10" },
        { "JailCell_03", "JailCell_11" },
        { "JailCell_04", "JailCell_12" },
        { "JailCell_05", "JailCell_13" },
        { "JailCell_06", "JailCell_14" },
        { "JailCell_07", "JailCell_15" },
        { "JailCell_08", "JailCell_16" },
    };

    struct CellMetrics
    {
        public float FloorSurfaceY;
        public float WallHeight;
        public float LightFixtureY;

        public static CellMetrics SampleFromScene()
        {
            var cell = GameObject.Find("JailCell_01");
            if (cell == null) return Default();

            var spawn = cell.transform.Find("SpawnPoint");
            var wall = cell.transform.Find("LeftWall");
            if (spawn == null || wall == null) return Default();

            float floorY = spawn.position.y;
            float wallH = wall.lossyScale.y;
            return new CellMetrics
            {
                FloorSurfaceY = floorY,
                WallHeight = wallH,
                LightFixtureY = floorY + wallH - 0.28f,
            };
        }

        static CellMetrics Default() => new()
        {
            FloorSurfaceY = 0.82f,
            WallHeight = 6f,
            LightFixtureY = 6.54f,
        };

        public float FloorTopFor(Transform floor) => floor.position.y + floor.lossyScale.y * 0.5f;
        public float CeilingYFor(Transform floor) => FloorTopFor(floor) + WallHeight;
        public float LightYFor(Transform floor) => CeilingYFor(floor) - 0.28f;
    }

    [MenuItem("Prison/Layout/0 — Apply Connected Diagram Layout")]
    public static void ApplyConnectedLayoutMenu() => ApplyConnectedDiagramLayout();

    [MenuItem("Prison/Layout/1 — Rename East Cells (09-16)")]
    public static void RenameEastCellsMenu() => RenameEastCells();

    [MenuItem("Prison/Layout/2 — Build Walls + Roofs")]
    public static void BuildStructureMenu()
    {
        SyncFloorHeightToCells();
        CleanupAndRebuildJailCellWalls();
        BuildWallsAroundFloors();
        BuildRoofs();
        BuildRoofSoffits();
    }

    [MenuItem("Prison/Layout/3 — Build All Lighting")]
    public static void BuildLightingMenu() => BuildAllLighting();

    [MenuItem("Prison/Layout/4 — Furnish Rooms (Scratch Build)")]
    public static void FurnishRoomsMenu() => FurnishRooms();

    [MenuItem("Prison/Layout/5 — Wire Registry")]
    public static void WireRegistryMenu() => WireRegistry();

    [MenuItem("Prison/Layout/6 — Build Escape Systems")]
    public static void BuildEscapeSystemsMenu() => BuildEscapeSystems();

    [MenuItem("Prison/Layout/Run Full Build")]
    public static void RunFullBuild()
    {
        RenameEastCells();
        SyncFloorHeightToCells();
        ApplyConnectedDiagramLayout();
        BuildWallsAroundFloors();
        BuildRoofs();
        BuildRoofSoffits();
        BuildAllLighting();
        FurnishRooms();
        WireRegistry();
        BuildEscapeSystems();
        SaveScene();
        Debug.Log("[PrisonLayout] Full build complete.");
    }

    struct FloorPlate
    {
        public string Name;
        public float Cx, Cz, Sx, Sz;

        public float MinX => Cx - Sx * 0.5f;
        public float MaxX => Cx + Sx * 0.5f;
        public float MinZ => Cz - Sz * 0.5f;
        public float MaxZ => Cz + Sz * 0.5f;

        public static FloorPlate Rect(string name, float cx, float cz, float sx, float sz) =>
            new() { Name = name, Cx = cx, Cz = cz, Sx = sx, Sz = sz };

        public bool OverlapsZ(FloorPlate o, float tol) =>
            !(MaxZ + tol < o.MinZ || MinZ - tol > o.MaxZ);

        public bool OverlapsX(FloorPlate o, float tol) =>
            !(MaxX + tol < o.MinX || MinX - tol > o.MaxX);

        public bool TouchesEast(FloorPlate o, float tol) =>
            Mathf.Abs(MaxX - o.MinX) <= tol && OverlapsZ(o, tol);

        public bool TouchesWest(FloorPlate o, float tol) =>
            Mathf.Abs(MinX - o.MaxX) <= tol && OverlapsZ(o, tol);

        public bool TouchesNorth(FloorPlate o, float tol) =>
            Mathf.Abs(MaxZ - o.MinZ) <= tol && OverlapsX(o, tol);

        public bool TouchesSouth(FloorPlate o, float tol) =>
            Mathf.Abs(MinZ - o.MaxZ) <= tol && OverlapsX(o, tol);
    }

    static List<FloorPlate> _activePlates = new();
    static List<Bounds> _jailCellBounds = new();

    static FloorPlate[] BuildDiagramPlates()
    {
        // Connected diagram — shared edges, 1 unit ≈ 1 m. +Z = north.
        const float cr = 4f;
        const float cw = 22f;
        const float ww = 44f, wd = 56f;
        const float roomW = 28f;
        const float yardW = 94f, yardD = 36f;

        float hubX = HubX;
        float hubZ = HubZ;
        float cafHalfW = cw * 0.5f;
        float wingHalfW = ww * 0.5f;
        float wingHalfD = wd * 0.5f;
        float roomHalfW = roomW * 0.5f;

        float westCx = hubX - cafHalfW - wingHalfW;
        float eastCx = hubX + cafHalfW + wingHalfW;
        float secCx = westCx - wingHalfW - roomHalfW;
        float wkCx = eastCx + wingHalfW + roomHalfW;

        float wingNorth = hubZ + wingHalfD;
        float wingSouth = hubZ - wingHalfD;
        float facilityWest = secCx - roomHalfW;
        float facilityEast = wkCx + roomHalfW;

        float northCorZ = wingNorth + cr * 0.5f;
        float southCorZ = wingSouth - cr * 0.5f;
        float yardCz = northCorZ + cr * 0.5f + yardD * 0.5f;
        float showerCz = southCorZ - cr * 0.5f - yardD * 0.5f;

        float loopNorthZ = yardCz + yardD * 0.5f + cr * 0.5f;
        float loopSouthZ = showerCz - yardD * 0.5f - cr * 0.5f;
        float loopWestX = facilityWest - cr - cr * 0.5f;
        float loopEastX = facilityEast + cr + cr * 0.5f;
        float loopSpanX = loopEastX - loopWestX;
        float loopCx = (loopWestX + loopEastX) * 0.5f;
        float loopMidZ = (loopNorthZ + loopSouthZ) * 0.5f;
        float loopSpineH = loopNorthZ - loopSouthZ + cr;

        float innerSpanX = facilityEast - facilityWest;
        float innerCx = (facilityWest + facilityEast) * 0.5f;

        return new[]
        {
            FloorPlate.Rect("MainSecurityFloor", secCx, hubZ, roomW, wd),
            FloorPlate.Rect("CellWingFloor_West", westCx, hubZ, ww, wd),
            FloorPlate.Rect("CafeteriaFloor", hubX, hubZ, cw, wd),
            FloorPlate.Rect("CellWingFloor_East", eastCx, hubZ, ww, wd),
            FloorPlate.Rect("WorkshopFloor", wkCx, hubZ, roomW, wd),
            FloorPlate.Rect("Corridor_North", innerCx, northCorZ, innerSpanX, cr),
            FloorPlate.Rect("Corridor_South", innerCx, southCorZ, innerSpanX, cr),
            FloorPlate.Rect("CourtyardFloor", hubX, yardCz, yardW, yardD),
            FloorPlate.Rect("ShowerFloor", hubX, showerCz, yardW, yardD),
            FloorPlate.Rect("Corridor_LoopNorth", loopCx, loopNorthZ, loopSpanX, cr),
            FloorPlate.Rect("Corridor_LoopSouth", loopCx, loopSouthZ, loopSpanX, cr),
            FloorPlate.Rect("Corridor_WestSpine", loopWestX, loopMidZ, cr, loopSpineH),
            FloorPlate.Rect("Corridor_EastSpine", loopEastX, loopMidZ, cr, loopSpineH),
            FloorPlate.Rect("Corridor_SecurityLoop", facilityWest - cr * 0.5f, hubZ, cr, cr * 2f),
            FloorPlate.Rect("Corridor_NW", loopWestX, northCorZ, cr, cr),
            FloorPlate.Rect("Corridor_NE", loopEastX, northCorZ, cr, cr),
            FloorPlate.Rect("Corridor_SW", loopWestX, southCorZ, cr, cr),
            FloorPlate.Rect("Corridor_SE", loopEastX, southCorZ, cr, cr),
        };
    }

    static void SaveScene()
    {
        var scene = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    static void SyncFloorHeightToCells()
    {
        var metrics = CellMetrics.SampleFromScene();
        _floorY = metrics.FloorSurfaceY - FloorScaleY * 0.5f;
    }

    static void ApplyConnectedDiagramLayout()
    {
        SyncFloorHeightToCells();
        _activePlates = BuildDiagramPlates().ToList();

        RemoveLegacyRootFloors();
        ClearChildren(GetOrCreateRoot("LayoutWalls"));
        ClearChildren(GetOrCreateRoot("LayoutRoofs"));
        ClearChildren(GetOrCreateRoot("LayoutSoffits"));
        ClearChildren(GetOrCreateRoot("LayoutLighting"));
        ClearChildren(GetOrCreateRoot("RoomProps"));

        var floorsRoot = GetOrCreateRoot("LayoutFloors");
        ClearChildren(floorsRoot);

        foreach (var plate in _activePlates)
            CreateFloorFromPlate(floorsRoot.transform, plate);

        RepositionCellBlocks(_activePlates);
        CleanupAndRebuildJailCellWalls();
        LogPlateConnectivity(_activePlates);

        Debug.Log($"[PrisonLayout] Applied connected diagram layout with {_activePlates.Count} tiled floor plates.");
    }

    static void LogPlateConnectivity(List<FloorPlate> plates)
    {
        int gaps = 0;
        foreach (var a in plates)
        {
            foreach (var edge in new[] { "N", "S", "E", "W" })
            {
                bool open = edge switch
                {
                    "N" => plates.Any(b => b.Name != a.Name && a.TouchesNorth(b, EdgeTolerance)),
                    "S" => plates.Any(b => b.Name != a.Name && a.TouchesSouth(b, EdgeTolerance)),
                    "E" => plates.Any(b => b.Name != a.Name && a.TouchesEast(b, EdgeTolerance)),
                    _ => plates.Any(b => b.Name != a.Name && a.TouchesWest(b, EdgeTolerance)),
                };
                if (!open) gaps++;
            }
        }

        if (gaps > 0)
            Debug.LogWarning($"[PrisonLayout] {gaps} exterior plate edges (expected outer perimeter).");
    }

    static void RemoveLegacyRootFloors()
    {
        foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects().ToArray())
        {
            if (!go.name.Contains("Floor")) continue;
            if (go.name is "Ground") continue;
            if (go.transform.parent != null) continue;
            Object.DestroyImmediate(go);
        }
    }

    static void CreateFloorFromPlate(Transform parent, FloorPlate plate)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = plate.Name;
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(plate.Cx, _floorY, plate.Cz);
        go.transform.localScale = new Vector3(plate.Sx, FloorScaleY, plate.Sz);
        var mat = LoadMat(TileMatPath);
        if (mat != null)
            go.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(go.GetComponent<Collider>());
    }

    static void RepositionCellBlocks(List<FloorPlate> plates)
    {
        var westWing = plates.FirstOrDefault(p => p.Name == "CellWingFloor_West");
        var eastWing = plates.FirstOrDefault(p => p.Name == "CellWingFloor_East");
        if (westWing.Name == null || eastWing.Name == null) return;

        MoveCellBlock("JailCells", westWing.Cx, westWing.Cz);
        MoveCellBlock("JailCells_East", eastWing.Cx, eastWing.Cz);
    }

    static void MoveCellBlock(string blockName, float targetCx, float targetCz)
    {
        var block = GameObject.Find(blockName);
        if (block == null) return;

        var cells = block.transform.Cast<Transform>()
            .Where(t => t.name.StartsWith("JailCell_"))
            .ToList();
        if (cells.Count == 0) return;

        float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
        foreach (var c in cells)
        {
            var p = c.position;
            minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
            minZ = Mathf.Min(minZ, p.z); maxZ = Mathf.Max(maxZ, p.z);
        }

        float curCx = (minX + maxX) * 0.5f;
        float curCz = (minZ + maxZ) * 0.5f;
        var delta = new Vector3(targetCx - curCx, 0f, targetCz - curCz);

        foreach (var c in cells)
            c.position += delta;
    }

    /// <summary>
    /// Legacy cell prefabs carry duplicated, wing-sized shell pieces (20 m walls/roofs/floors)
    /// that slice through playable space. Strip those and rebuild 4×5.5 m shells from each spawn.
    /// </summary>
    static void CleanupAndRebuildJailCellWalls()
    {
        var metrics = CellMetrics.SampleFromScene();
        var wallMat = LoadMat(WallMatPath);
        int stripped = 0, shells = 0, partitions = 0;

        foreach (var blockName in new[] { "JailCells", "JailCells_East" })
        {
            var block = GameObject.Find(blockName);
            if (block == null) continue;

            var cells = block.transform.Cast<Transform>()
                .Where(t => t.name.StartsWith("JailCell_"))
                .ToList();

            foreach (var cell in cells)
            {
                stripped += StripLegacyCellShellPieces(cell);
                if (BuildProperCellShell(cell, metrics, wallMat))
                    shells++;
            }

            partitions += BuildCellBlockPartitions(cells, metrics, wallMat);
        }

        Debug.Log($"[PrisonLayout] Jail cells: stripped {stripped} legacy shell pieces, rebuilt {shells} cell shells, {partitions} shared partitions.");
    }

    static int StripLegacyCellShellPieces(Transform cell)
    {
        int count = 0;
        var remove = new List<GameObject>();

        foreach (Transform child in cell)
        {
            var name = child.name;
            if (name.Contains("("))
            {
                remove.Add(child.gameObject);
                continue;
            }

            if (name is "LeftWall" or "RightWall" or "BackWall" or "BackWallTop" or "Roof" or "Floor"
                or "CellShell_Back" or "CellShell_Left" or "CellShell_Right")
            {
                remove.Add(child.gameObject);
                continue;
            }

            var scale = child.localScale;
            if ((name.Contains("Wall") || name == "Roof") &&
                Mathf.Max(scale.x, scale.y, scale.z) >= LegacyShellMinScale)
                remove.Add(child.gameObject);
        }

        foreach (var go in remove)
        {
            Object.DestroyImmediate(go);
            count++;
        }

        return count;
    }

    static bool BuildProperCellShell(Transform cell, CellMetrics metrics, Material wallMat)
    {
        var spawn = cell.Find("SpawnPoint");
        if (spawn == null) return false;

        float w = CellInteriorWidth;
        float d = CellInteriorDepth;
        float h = metrics.WallHeight;
        float floorTop = FloorSurfaceY;
        float cy = floorTop + h * 0.5f;
        Vector3 spawnPos = spawn.position;
        Vector3 right = cell.right;
        Vector3 forward = cell.forward;

        // Door faces +right; back wall opposite corridor.
        CreateWallBlock(cell, "CellShell_Back",
            spawnPos - right * (w * 0.5f) + Vector3.up * (cy - floorTop),
            new Vector3(WallThickness, h, d), wallMat);

        return true;
    }

    static int BuildCellBlockPartitions(List<Transform> cells, CellMetrics metrics, Material wallMat)
    {
        var anchors = cells
            .Select(c => new { Cell = c, Spawn = c.Find("SpawnPoint") })
            .Where(x => x.Spawn != null)
            .Select(x => new { x.Cell, Pos = x.Spawn.position })
            .ToList();

        if (anchors.Count < 2) return 0;

        float h = metrics.WallHeight;
        float floorTop = FloorSurfaceY;
        float cy = floorTop + h * 0.5f;
        int count = 0;

        for (int i = 0; i < anchors.Count; i++)
        {
            for (int j = i + 1; j < anchors.Count; j++)
            {
                var a = anchors[i];
                var b = anchors[j];
                float dx = Mathf.Abs(a.Pos.x - b.Pos.x);
                float dz = Mathf.Abs(a.Pos.z - b.Pos.z);

                // Row neighbors (same column): partition wall between spawns along Z.
                if (dx < CellInteriorWidth && dz > CellInteriorDepth * 0.5f && dz < CellInteriorDepth * 4f)
                {
                    var mid = (a.Pos + b.Pos) * 0.5f + Vector3.up * (cy - floorTop);
                    CreateWallBlock(a.Cell.parent, $"CellPartition_{a.Cell.name}_{b.Cell.name}",
                        mid, new Vector3(CellInteriorWidth, h, WallThickness), wallMat);
                    count++;
                    continue;
                }

                // Column neighbors: partition along X.
                if (dz < CellInteriorDepth && dx > CellInteriorWidth * 0.5f && dx < CellInteriorWidth * 4f)
                {
                    var mid = (a.Pos + b.Pos) * 0.5f + Vector3.up * (cy - floorTop);
                    CreateWallBlock(a.Cell.parent, $"CellPartition_{a.Cell.name}_{b.Cell.name}",
                        mid, new Vector3(WallThickness, h, CellInteriorDepth), wallMat);
                    count++;
                }
            }
        }

        return count;
    }

    static void RenameEastCells()
    {
        var eastBlock = GameObject.Find("JailCells (1)") ?? GameObject.Find("JailCells_East");
        if (eastBlock == null) return;

        eastBlock.name = "JailCells_East";
        var cells = eastBlock.transform.Cast<Transform>().Where(t => t.name.StartsWith("JailCell_")).ToList();
        if (cells.All(t => int.TryParse(t.name.Replace("JailCell_", ""), out var n) && n >= 9))
            return;

        foreach (var cell in cells) cell.name = "_TMP_" + cell.name;
        foreach (var cell in cells)
        {
            var oldName = cell.name.Replace("_TMP_", "");
            if (!RightCellRename.TryGetValue(oldName, out var newName)) continue;
            cell.name = newName;
            UpdateCellNumberLabel(cell, newName);
        }
    }

    static void UpdateCellNumberLabel(Transform cell, string cellName)
    {
        var digits = cellName.Replace("JailCell_", "");
        foreach (var tmp in cell.GetComponentsInChildren<TextMeshPro>(true))
        {
            if (tmp.gameObject.name.StartsWith("CellNumber"))
                tmp.text = digits;
        }
    }

    enum EdgeSide { North, South, East, West }

    const float DoorwayWidth = 3.5f;
    const float DoorHeight = 3f;

    static void BuildWallsAroundFloors()
    {
        if (_activePlates.Count == 0)
            _activePlates = BuildDiagramPlates().ToList();

        var metrics = CellMetrics.SampleFromScene();
        var wallMat = LoadMat(WallMatPath);
        var wallsRoot = GetOrCreateRoot("LayoutWalls");
        ClearChildren(wallsRoot);
        _jailCellBounds = CollectJailCellBounds();

        int wallCount = 0;
        foreach (var plate in _activePlates)
        {
            var group = new GameObject("Walls_" + plate.Name);
            group.transform.SetParent(wallsRoot.transform, false);
            foreach (EdgeSide side in new[] { EdgeSide.North, EdgeSide.South, EdgeSide.East, EdgeSide.West })
                wallCount += BuildPlateEdgeWalls(plate, side, group.transform, wallMat, metrics, _activePlates);
        }

        Debug.Log($"[PrisonLayout] Built {wallCount} wall segments with doorways ({metrics.WallHeight:F1} m tall, {DoorwayWidth} m doors, {_jailCellBounds.Count} cell keep-outs).");
    }

    static List<Bounds> CollectJailCellBounds()
    {
        var list = new List<Bounds>();
        var metrics = CellMetrics.SampleFromScene();
        foreach (var blockName in new[] { "JailCells", "JailCells_East" })
        {
            var block = GameObject.Find(blockName);
            if (block == null) continue;

            foreach (Transform cell in block.transform)
            {
                if (!cell.name.StartsWith("JailCell_")) continue;
                var spawn = cell.Find("SpawnPoint");
                if (spawn != null)
                {
                    list.Add(new Bounds(spawn.position, new Vector3(CellInteriorWidth, metrics.WallHeight, CellInteriorDepth)));
                    continue;
                }

                var col = cell.GetComponent<BoxCollider>();
                if (col != null)
                {
                    list.Add(col.bounds);
                    continue;
                }

                var renderers = cell.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) continue;
                var bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    bounds.Encapsulate(renderers[i].bounds);
                list.Add(bounds);
            }
        }

        return list;
    }

    static bool IsCellWingPlate(FloorPlate plate) => plate.Name.StartsWith("CellWingFloor_");

    static bool IntersectsJailCellInterior(Vector3 center, Vector3 scale)
    {
        if (_jailCellBounds == null || _jailCellBounds.Count == 0)
            return false;

        var segment = new Bounds(center, scale);
        foreach (var cell in _jailCellBounds)
        {
            if (segment.Intersects(cell))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Builds one plate edge: exterior stretches get solid walls; edges shared with a
    /// neighboring plate get a wall with a centered doorway (+ lintel), matching the
    /// diagram's door openings. Shared walls are built once, by the North/East side owner.
    /// </summary>
    static int BuildPlateEdgeWalls(FloorPlate plate, EdgeSide side, Transform parent, Material wallMat,
        CellMetrics metrics, List<FloorPlate> all)
    {
        float t = WallThickness;
        float wallH = metrics.WallHeight;
        float floorTop = FloorSurfaceY;
        float centerY = floorTop + wallH * 0.5f;

        bool alongX = side is EdgeSide.North or EdgeSide.South;
        float a0 = alongX ? plate.MinX : plate.MinZ;
        float a1 = alongX ? plate.MaxX : plate.MaxZ;
        float edgeCoord = side switch
        {
            EdgeSide.North => plate.MaxZ + t * 0.5f,
            EdgeSide.South => plate.MinZ - t * 0.5f,
            EdgeSide.East => plate.MaxX + t * 0.5f,
            _ => plate.MinX - t * 0.5f,
        };

        // Overlap intervals with every neighbor touching this edge.
        var overlaps = new List<(float lo, float hi)>();
        foreach (var o in all)
        {
            if (o.Name == plate.Name) continue;
            bool touches = side switch
            {
                EdgeSide.North => plate.TouchesNorth(o, EdgeTolerance),
                EdgeSide.South => plate.TouchesSouth(o, EdgeTolerance),
                EdgeSide.East => plate.TouchesEast(o, EdgeTolerance),
                _ => plate.TouchesWest(o, EdgeTolerance),
            };
            if (!touches) continue;

            float lo = Mathf.Max(a0, alongX ? o.MinX : o.MinZ);
            float hi = Mathf.Min(a1, alongX ? o.MaxX : o.MaxZ);
            if (hi - lo > 1f)
                overlaps.Add((lo, hi));
        }

        // North/East plate owns shared walls; South/West only fills exterior leftovers.
        bool owner = side is EdgeSide.North or EdgeSide.East;

        List<(float a, float b)> segments;
        var doors = new List<(float center, float width)>();

        if (overlaps.Count == 0)
        {
            segments = new List<(float, float)> { (a0 - t, a1 + t) };
        }
        else if (owner)
        {
            foreach (var (lo, hi) in overlaps)
            {
                float width = Mathf.Min(DoorwayWidth, (hi - lo) * 0.8f);
                doors.Add(((lo + hi) * 0.5f, width));
            }
            segments = SubtractIntervals(a0 - t, a1 + t,
                doors.Select(d => (d.center - d.width * 0.5f, d.center + d.width * 0.5f)).ToList());
        }
        else
        {
            segments = SubtractIntervals(a0 - t, a1 + t, overlaps.Select(ov => (ov.lo, ov.hi)).ToList());
        }

        string sideTag = side.ToString().Substring(0, 1);
        int count = 0;

        foreach (var (sa, sb) in segments)
        {
            if (sb - sa < 0.15f) continue;
            float mid = (sa + sb) * 0.5f;
            float len = sb - sa;
            Vector3 pos = alongX ? new Vector3(mid, centerY, edgeCoord) : new Vector3(edgeCoord, centerY, mid);
            Vector3 scale = alongX ? new Vector3(len, wallH, t) : new Vector3(t, wallH, len);
            if (IsCellWingPlate(plate) && IntersectsJailCellInterior(pos, scale))
                continue;
            CreateWallBlock(parent, $"Wall_{sideTag}_{count}", pos, scale, wallMat);
            count++;
        }

        // Lintels close the wall above each doorway.
        foreach (var (center, width) in doors)
        {
            float lintelH = wallH - DoorHeight;
            if (lintelH < 0.1f) continue;
            float lintelY = floorTop + DoorHeight + lintelH * 0.5f;
            Vector3 pos = alongX ? new Vector3(center, lintelY, edgeCoord) : new Vector3(edgeCoord, lintelY, center);
            Vector3 scale = alongX ? new Vector3(width, lintelH, t) : new Vector3(t, lintelH, width);
            if (!(IsCellWingPlate(plate) && IntersectsJailCellInterior(pos, scale)))
            {
                CreateWallBlock(parent, $"Lintel_{sideTag}_{count}", pos, scale, wallMat);
                count++;
            }

            // Door jambs — vertical posts flanking the opening up to DoorHeight.
            float halfW = width * 0.5f;
            float jambY = floorTop + DoorHeight * 0.5f;
            float jambH = DoorHeight;
            if (alongX)
            {
                var jambLPos = new Vector3(center - halfW, jambY, edgeCoord);
                var jambRPos = new Vector3(center + halfW, jambY, edgeCoord);
                var jambScale = new Vector3(t, jambH, t);
                if (!(IsCellWingPlate(plate) && IntersectsJailCellInterior(jambLPos, jambScale)))
                    CreateWallBlock(parent, $"JambL_{sideTag}_{count}", jambLPos, jambScale, wallMat);
                if (!(IsCellWingPlate(plate) && IntersectsJailCellInterior(jambRPos, jambScale)))
                    CreateWallBlock(parent, $"JambR_{sideTag}_{count}", jambRPos, jambScale, wallMat);
            }
            else
            {
                var jambLPos = new Vector3(edgeCoord, jambY, center - halfW);
                var jambRPos = new Vector3(edgeCoord, jambY, center + halfW);
                var jambScale = new Vector3(t, jambH, t);
                if (!(IsCellWingPlate(plate) && IntersectsJailCellInterior(jambLPos, jambScale)))
                    CreateWallBlock(parent, $"JambL_{sideTag}_{count}", jambLPos, jambScale, wallMat);
                if (!(IsCellWingPlate(plate) && IntersectsJailCellInterior(jambRPos, jambScale)))
                    CreateWallBlock(parent, $"JambR_{sideTag}_{count}", jambRPos, jambScale, wallMat);
            }
            count += 2;
        }

        return count;
    }

    /// <summary>Removes hole intervals from [a0, a1], returning the remaining wall stretches.</summary>
    static List<(float a, float b)> SubtractIntervals(float a0, float a1, List<(float lo, float hi)> holes)
    {
        var result = new List<(float, float)>();
        var sorted = holes
            .Select(h => (lo: Mathf.Max(a0, h.lo), hi: Mathf.Min(a1, h.hi)))
            .Where(h => h.hi > h.lo)
            .OrderBy(h => h.lo)
            .ToList();

        float cursor = a0;
        foreach (var (lo, hi) in sorted)
        {
            if (lo > cursor)
                result.Add((cursor, lo));
            cursor = Mathf.Max(cursor, hi);
        }
        if (cursor < a1)
            result.Add((cursor, a1));
        return result;
    }

    static void BuildRoofs()
    {
        if (_activePlates.Count == 0)
            _activePlates = BuildDiagramPlates().ToList();

        var metrics = CellMetrics.SampleFromScene();
        var ceilingMat = LoadMat(CeilingMatPath) ?? LoadMat(WallMatPath);
        var roofsRoot = GetOrCreateRoot("LayoutRoofs");
        ClearChildren(roofsRoot);

        int built = 0;
        foreach (var plate in _activePlates)
        {
            if (plate.Name.StartsWith("Courtyard")) continue;
            var group = new GameObject("Roof_" + plate.Name);
            group.transform.SetParent(roofsRoot.transform, false);
            float ceilingY = FloorSurfaceY + metrics.WallHeight;
            CreateBlock(group.transform, "Ceiling", new Vector3(plate.Cx, ceilingY + RoofThickness * 0.5f, plate.Cz),
                new Vector3(plate.Sx + RoofOverhang * 2f, RoofThickness, plate.Sz + RoofOverhang * 2f), ceilingMat);
            built++;
        }

        Debug.Log($"[PrisonLayout] Built {built} connected roofs (courtyard open, {RoofOverhang:F1} m overhang).");
    }

    /// <summary>
    /// Drops a thin lip below roof edges on exterior plate sides to hide wall/roof gaps (sky leaks).
    /// </summary>
    static void BuildRoofSoffits()
    {
        if (_activePlates.Count == 0)
            _activePlates = BuildDiagramPlates().ToList();

        var metrics = CellMetrics.SampleFromScene();
        var ceilingMat = LoadMat(CeilingMatPath) ?? LoadMat(WallMatPath);
        var soffitsRoot = GetOrCreateRoot("LayoutSoffits");
        ClearChildren(soffitsRoot);

        float ceilingY = FloorSurfaceY + metrics.WallHeight;
        float lipY = ceilingY - SoffitDrop * 0.5f;
        int built = 0;

        foreach (var plate in _activePlates)
        {
            if (plate.Name.StartsWith("Courtyard")) continue;

            var group = new GameObject("Soffit_" + plate.Name);
            group.transform.SetParent(soffitsRoot.transform, false);

            foreach (EdgeSide side in new[] { EdgeSide.North, EdgeSide.South, EdgeSide.East, EdgeSide.West })
            {
                bool exterior = IsExteriorEdge(plate, side, _activePlates);
                if (!exterior) continue;

                bool alongX = side is EdgeSide.North or EdgeSide.South;
                float len = alongX ? plate.Sx + RoofOverhang * 2f : plate.Sz + RoofOverhang * 2f;
                float edgeCoord = side switch
                {
                    EdgeSide.North => plate.MaxZ + RoofOverhang,
                    EdgeSide.South => plate.MinZ - RoofOverhang,
                    EdgeSide.East => plate.MaxX + RoofOverhang,
                    _ => plate.MinX - RoofOverhang,
                };

                Vector3 pos = alongX
                    ? new Vector3(plate.Cx, lipY, edgeCoord)
                    : new Vector3(edgeCoord, lipY, plate.Cz);
                Vector3 scale = alongX
                    ? new Vector3(len, SoffitDrop, RoofThickness)
                    : new Vector3(RoofThickness, SoffitDrop, len);

                CreateBlock(group.transform, $"Soffit_{side}", pos, scale, ceilingMat);
                built++;
            }
        }

        Debug.Log($"[PrisonLayout] Built {built} roof soffit lips on exterior edges.");
    }

    static bool IsExteriorEdge(FloorPlate plate, EdgeSide side, List<FloorPlate> all)
    {
        foreach (var o in all)
        {
            if (o.Name == plate.Name) continue;
            bool touches = side switch
            {
                EdgeSide.North => plate.TouchesNorth(o, EdgeTolerance),
                EdgeSide.South => plate.TouchesSouth(o, EdgeTolerance),
                EdgeSide.East => plate.TouchesEast(o, EdgeTolerance),
                _ => plate.TouchesWest(o, EdgeTolerance),
            };
            if (touches) return false;
        }
        return true;
    }

    static void BuildAllLighting()
    {
        var metrics = CellMetrics.SampleFromScene();
        var lightMat = LoadMat(LightMatPath);
        var root = GetOrCreateRoot("LayoutLighting");
        ClearChildren(root);

        int count = 0;
        foreach (var plate in _activePlates.Count > 0 ? _activePlates : BuildDiagramPlates().ToList())
        {
            bool outdoor = plate.Name.StartsWith("Courtyard");
            count += PlaceLightGridForPlate(plate, root.transform, metrics, lightMat, outdoor);
        }

        count += LightAllCells(root.transform, metrics, lightMat);

        Debug.Log($"[PrisonLayout] Placed {count} lights across all rooms, corridors, and cells.");
    }

    static int LightAllCells(Transform parent, CellMetrics metrics, Material lightMat)
    {
        int count = 0;
        foreach (var blockName in new[] { "JailCells", "JailCells_East" })
        {
            var block = GameObject.Find(blockName);
            if (block == null) continue;

            foreach (Transform cell in block.transform)
            {
                if (!cell.name.StartsWith("JailCell_")) continue;
                var spawn = cell.Find("SpawnPoint");
                var pos = spawn != null ? spawn.position : cell.position;
                var lightPos = new Vector3(pos.x, metrics.LightFixtureY, pos.z);
                CreateCeilingLight(parent, $"CellLight_{cell.name}", lightPos, lightMat, 5f, 14f, true);
                count++;
            }
        }

        return count;
    }

    static int PlaceLightGridForPlate(FloorPlate plate, Transform parent, CellMetrics metrics, Material lightMat, bool outdoor)
    {
        float lightY = FloorSurfaceY + metrics.WallHeight - 0.28f;
        float margin = outdoor ? 2.5f : 2f;
        bool narrow = plate.Sx < plate.Sz * 0.35f || plate.Sz < plate.Sx * 0.35f;
        float area = plate.Sx * plate.Sz;
        float spacing = narrow ? 6f
            : outdoor ? 10f
            : area > 1200f ? 10f
            : area > 500f ? 8f
            : 5.5f;
        float intensity = outdoor ? 3.5f : 6f;
        float range = outdoor ? 18f : 14f;

        float usableX = Mathf.Max(2f, plate.Sx - margin * 2f);
        float usableZ = Mathf.Max(2f, plate.Sz - margin * 2f);

        int cols = narrow && plate.Sx < plate.Sz ? 1 : Mathf.Max(1, Mathf.FloorToInt(usableX / spacing) + 1);
        int rows = narrow && plate.Sz < plate.Sx ? 1 : Mathf.Max(1, Mathf.FloorToInt(usableZ / spacing) + 1);
        if (narrow)
        {
            if (plate.Sx >= plate.Sz) { cols = Mathf.Max(1, Mathf.FloorToInt(usableX / spacing) + 1); rows = 1; }
            else { rows = Mathf.Max(1, Mathf.FloorToInt(usableZ / spacing) + 1); cols = 1; }
        }

        float startX = plate.Cx - (cols - 1) * spacing * 0.5f;
        float startZ = plate.Cz - (rows - 1) * spacing * 0.5f;

        int count = 0;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                var worldPos = new Vector3(startX + c * spacing, lightY, startZ + r * spacing);
                CreateCeilingLight(parent, $"Light_{plate.Name}_{r}_{c}", worldPos, lightMat, intensity, range, !outdoor);
                count++;
            }
        }

        return count;
    }

    static void CreateCeilingLight(Transform parent, string name, Vector3 worldPos, Material lightMat,
        float intensity, float range, bool shadows)
    {
        var fixture = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fixture.name = name + "_Fixture";
        fixture.transform.SetParent(parent, true);
        fixture.transform.position = worldPos;
        fixture.transform.localScale = new Vector3(1.8f, 0.08f, 0.55f);
        if (lightMat != null)
            fixture.GetComponent<Renderer>().sharedMaterial = lightMat;
        Object.DestroyImmediate(fixture.GetComponent<Collider>());

        var lightGo = new GameObject(name);
        lightGo.transform.SetParent(parent, true);
        lightGo.transform.position = worldPos - new Vector3(0f, 0.2f, 0f);

        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Point;
        light.intensity = intensity;
        light.range = range;
        light.color = new Color(1f, 0.95f, 0.85f);
        light.shadows = shadows ? LightShadows.Soft : LightShadows.None;

        var urp = lightGo.AddComponent<UniversalAdditionalLightData>();
        urp.softShadowQuality = SoftShadowQuality.High;
    }

    static IEnumerable<Transform> FindUniqueLayoutFloors()
    {
        var seen = new HashSet<string>();
        foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
            {
                if (!IsLayoutFloor(t)) continue;
                var key = RoundKey(t.position);
                if (!seen.Add(key)) continue;
                yield return t;
            }
        }
    }

    static bool IsCourtyardFloor(Transform floor) => floor.name.StartsWith("CourtyardFloor");

    static string RoundKey(Vector3 p) => $"{Mathf.RoundToInt(p.x)}_{Mathf.RoundToInt(p.z)}";
    static string SanitizeName(string n) => n.Replace(' ', '_').Replace('(', '_').Replace(')', '_');

    static bool IsLayoutFloor(Transform t)
    {
        if (!t.name.Contains("Floor") || t.name.StartsWith("FloorStain")) return false;
        if (t.parent != null && t.parent.name.StartsWith("JailCell_")) return false;
        if (t.GetComponent<MeshRenderer>() == null && t.GetComponent<MeshFilter>() == null) return false;
        return t.lossyScale.x > 1f && t.lossyScale.z > 1f;
    }

    static void BuildPerimeterWalls(Transform floor, Transform parent, Material wallMat, CellMetrics metrics)
    {
        var pos = floor.position;
        var scale = floor.lossyScale;
        float halfX = scale.x * 0.5f;
        float halfZ = scale.z * 0.5f;
        float floorTop = metrics.FloorTopFor(floor);
        float wallH = metrics.WallHeight;
        float centerY = floorTop + wallH * 0.5f;
        float t = WallThickness;

        CreateBlock(parent, "Wall_N", new Vector3(pos.x, centerY, pos.z + halfZ + t * 0.5f),
            new Vector3(scale.x + t * 2f, wallH, t), wallMat);
        CreateBlock(parent, "Wall_S", new Vector3(pos.x, centerY, pos.z - halfZ - t * 0.5f),
            new Vector3(scale.x + t * 2f, wallH, t), wallMat);
        CreateBlock(parent, "Wall_E", new Vector3(pos.x + halfX + t * 0.5f, centerY, pos.z),
            new Vector3(t, wallH, scale.z + t * 2f), wallMat);
        CreateBlock(parent, "Wall_W", new Vector3(pos.x - halfX - t * 0.5f, centerY, pos.z),
            new Vector3(t, wallH, scale.z + t * 2f), wallMat);
    }

    static void BuildRoofSlab(Transform floor, Transform parent, Material ceilingMat, CellMetrics metrics)
    {
        var pos = floor.position;
        var scale = floor.lossyScale;
        float ceilingY = metrics.CeilingYFor(floor);
        CreateBlock(parent, "Ceiling", new Vector3(pos.x, ceilingY + RoofThickness * 0.5f, pos.z),
            new Vector3(scale.x, RoofThickness, scale.z), ceilingMat);
    }

    static void FurnishRooms()
    {
        if (_activePlates.Count == 0)
            _activePlates = BuildDiagramPlates().ToList();

        var propsRoot = GetOrCreateRoot("RoomProps");
        ClearChildren(propsRoot);
        var metrics = CellMetrics.SampleFromScene();

        FurnishCafeteria(FindPlateFloor("CafeteriaFloor"), propsRoot.transform, metrics);
        FurnishShower(FindPlateFloor("ShowerFloor"), propsRoot.transform, metrics);
        FurnishCourtyard(FindPlateFloor("CourtyardFloor"), propsRoot.transform, metrics);
        FurnishWorkshop(FindPlateFloor("WorkshopFloor"), propsRoot.transform, metrics);
        FurnishMainSecurity(FindPlateFloor("MainSecurityFloor"), propsRoot.transform, metrics);

        Debug.Log("[PrisonLayout] Scratch-built room props placed.");
    }

    static Transform FindPlateFloor(string plateName)
    {
        var floorsRoot = GameObject.Find("LayoutFloors");
        if (floorsRoot == null) return null;
        return floorsRoot.transform.Cast<Transform>().FirstOrDefault(t => t.name == plateName);
    }

    static void FurnishCafeteria(Transform floor, Transform root, CellMetrics metrics)
    {
        if (floor == null) return;
        var room = CreateRoomRoot(root, "CafeteriaProps", floor);
        var panel = LoadMat(PanelMatPath);
        var metal = LoadMat(MetalMatPath);
        var tile = LoadMat(TileMatPath);
        var c = floor.position;
        float fy = metrics.FloorTopFor(floor);
        float hx = floor.lossyScale.x * 0.5f;
        float hz = floor.lossyScale.z * 0.5f;

        CreateBlock(room, "ServingCounter", new Vector3(c.x, fy + 0.55f, c.z + hz - 1.2f),
            new Vector3(hx * 1.5f, 1.1f, 0.9f), panel);
        for (int i = 0; i < 3; i++)
        {
            float x = c.x - hx * 0.4f + i * (hx * 0.4f);
            CreateBlock(room, $"FoodWarmer_{i}", new Vector3(x, fy + 0.45f, c.z + hz - 2f),
                new Vector3(1.2f, 0.9f, 0.7f), metal);
        }

        int cols = 3, rows = 2;
        float sx = Mathf.Min(7f, hx * 1.4f / cols);
        float sz = Mathf.Min(8f, hz * 0.9f / rows);
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                float x = c.x - (cols - 1) * sx * 0.5f + col * sx;
                float z = c.z - hz * 0.1f + row * sz;
                BuildCafeteriaTableSet(room, new Vector3(x, fy, z), tile, metal, $"Table_{row}_{col}");
            }
        }

        EnsureZone(room.gameObject, ZoneType.Cafeteria, "CAFETERIA", floor, metrics);
    }

    static void BuildCafeteriaTableSet(Transform parent, Vector3 floorPos, Material top, Material leg, string name)
    {
        var table = new GameObject(name);
        table.transform.SetParent(parent, true);
        table.transform.position = floorPos;

        CreateBlock(table.transform, "Top", floorPos + new Vector3(0f, 0.42f, 0f), new Vector3(1.8f, 0.06f, 0.9f), top);
        foreach (var off in new[] { new Vector3(-0.75f, 0.2f, -0.35f), new Vector3(0.75f, 0.2f, -0.35f),
            new Vector3(-0.75f, 0.2f, 0.35f), new Vector3(0.75f, 0.2f, 0.35f) })
            CreateBlock(table.transform, "Leg", floorPos + off, new Vector3(0.08f, 0.4f, 0.08f), leg);

        CreateBlock(table.transform, "BenchL", floorPos + new Vector3(0f, 0.22f, -0.75f), new Vector3(1.6f, 0.08f, 0.35f), leg);
        CreateBlock(table.transform, "BenchR", floorPos + new Vector3(0f, 0.22f, 0.75f), new Vector3(1.6f, 0.08f, 0.35f), leg);
    }

    static void FurnishShower(Transform floor, Transform root, CellMetrics metrics)
    {
        if (floor == null) return;
        var room = CreateRoomRoot(root, "ShowerProps", floor);
        var wall = LoadMat(WallMatPath);
        var c = floor.position;
        float fy = metrics.FloorTopFor(floor);
        float hx = floor.lossyScale.x * 0.5f;
        float hz = floor.lossyScale.z * 0.5f;

        int cols = 3, rows = 2;
        float sx = hx * 1.2f / cols;
        float sz = hz * 0.8f / rows;
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                float x = c.x - hx * 0.45f + col * sx + sx * 0.5f;
                float z = c.z - hz * 0.2f + row * sz;
                BuildShowerStall(room, new Vector3(x, fy, z), $"Stall_{row}_{col}");
            }
        }

        for (int i = 0; i < 2; i++)
        {
            float x = c.x - 4f + i * 8f;
            BuildSink(room, new Vector3(x, fy, c.z - hz + 1.5f), $"Sink_{i}");
        }

        for (int i = 0; i < 2; i++)
        {
            float z = c.z - 6f + i * 12f;
            BuildBench(room, new Vector3(c.x - hx + 1.2f, fy, z), $"ChangingBench_{i}");
        }

        CreateWallBlock(room, "WetDryDivider", new Vector3(c.x, fy + metrics.WallHeight * 0.5f, c.z),
            new Vector3(hx * 1.6f, metrics.WallHeight, 0.12f), wall);
    }

    static void BuildShowerStall(Transform parent, Vector3 floorPos, string name)
    {
        var stall = new GameObject(name);
        stall.transform.SetParent(parent, true);
        stall.transform.position = floorPos;
        var panel = LoadMat(PanelMatPath);
        var metal = LoadMat(MetalMatPath);

        CreateBlock(stall.transform, "BackWall", floorPos + new Vector3(0f, 1.1f, -0.6f), new Vector3(1.4f, 2.2f, 0.08f), panel);
        CreateBlock(stall.transform, "SideL", floorPos + new Vector3(-0.65f, 1.1f, 0f), new Vector3(0.08f, 2.2f, 1.2f), panel);
        CreateBlock(stall.transform, "Head", floorPos + new Vector3(0f, 2.0f, -0.5f), new Vector3(0.2f, 0.2f, 0.2f), metal);
        CreateBlock(stall.transform, "Drain", floorPos + new Vector3(0f, 0.03f, 0.2f), new Vector3(0.35f, 0.05f, 0.35f), metal);
    }

    static void BuildToilet(Transform parent, Vector3 floorPos, string name)
    {
        var toilet = new GameObject(name);
        toilet.transform.SetParent(parent, true);
        toilet.transform.position = floorPos;
        var porcelain = LoadMat(ToiletMatPath) ?? LoadMat(PanelMatPath);

        CreateBlock(toilet.transform, "Bowl", floorPos + new Vector3(0f, 0.35f, 0f), new Vector3(0.6f, 0.7f, 0.5f), porcelain);
        CreateBlock(toilet.transform, "Tank", floorPos + new Vector3(0f, 0.6f, -0.3f), new Vector3(0.5f, 0.9f, 0.25f), porcelain);
        CreateBlock(toilet.transform, "Seat", floorPos + new Vector3(0f, 0.52f, 0.05f), new Vector3(0.45f, 0.03f, 0.35f), porcelain);
    }

    static void BuildSink(Transform parent, Vector3 floorPos, string name)
    {
        var sink = new GameObject(name);
        sink.transform.SetParent(parent, true);
        sink.transform.position = floorPos;
        var metal = LoadMat(SinkMatPath) ?? LoadMat(MetalMatPath);

        CreateBlock(sink.transform, "Basin", floorPos + new Vector3(0f, 0.85f, 0f), new Vector3(0.5f, 0.15f, 0.4f), metal);
        CreateBlock(sink.transform, "Mount", floorPos + new Vector3(0f, 0.5f, -0.15f), new Vector3(0.08f, 0.6f, 0.08f), metal);
        CreateBlock(sink.transform, "Faucet", floorPos + new Vector3(0f, 0.95f, -0.12f), new Vector3(0.08f, 0.15f, 0.08f), metal);
    }

    static void BuildBench(Transform parent, Vector3 floorPos, string name)
    {
        var metal = LoadMat(MetalMatPath);
        CreateBlock(parent, name + "_Seat", floorPos + new Vector3(0f, 0.25f, 0f), new Vector3(1.4f, 0.08f, 0.4f), metal);
        CreateBlock(parent, name + "_LegL", floorPos + new Vector3(-0.6f, 0.12f, 0f), new Vector3(0.08f, 0.24f, 0.35f), metal);
        CreateBlock(parent, name + "_LegR", floorPos + new Vector3(0.6f, 0.12f, 0f), new Vector3(0.08f, 0.24f, 0.35f), metal);
    }

    static void FurnishCourtyard(Transform floor, Transform root, CellMetrics metrics)
    {
        if (floor == null) return;
        var room = CreateRoomRoot(root, "CourtyardProps", floor);
        var metal = LoadMat(MetalMatPath);
        var tile = LoadMat(TileMatPath);
        var c = floor.position;
        float fy = metrics.FloorTopFor(floor);
        float hx = floor.lossyScale.x * 0.5f;
        float hz = floor.lossyScale.z * 0.5f;

        CreateBlock(room, "ExercisePad", new Vector3(c.x, fy + 0.03f, c.z), new Vector3(5f, 0.06f, 5f), tile);
        CreateBlock(room, "PullUp_L", new Vector3(c.x - 2.5f, fy + 1.2f, c.z - 4f), new Vector3(0.12f, 2.4f, 0.12f), metal);
        CreateBlock(room, "PullUp_R", new Vector3(c.x + 2.5f, fy + 1.2f, c.z - 4f), new Vector3(0.12f, 2.4f, 0.12f), metal);
        CreateBlock(room, "PullUp_Top", new Vector3(c.x, fy + 2.35f, c.z - 4f), new Vector3(5.2f, 0.12f, 0.12f), metal);

        for (int i = 0; i < 3; i++)
        {
            float x = c.x - hx * 0.5f + i * (hx * 0.5f);
            BuildBench(room, new Vector3(x, fy, c.z + hz - 2f), $"YardBench_{i}");
        }

        EnsureZone(room.gameObject, ZoneType.Yard, "COURTYARD", floor, metrics);
    }

    static void FurnishWorkshop(Transform floor, Transform root, CellMetrics metrics)
    {
        if (floor == null) return;
        var room = CreateRoomRoot(root, "WorkshopProps", floor);
        var panel = LoadMat(PanelMatPath);
        var metal = LoadMat(MetalMatPath);
        var c = floor.position;
        float fy = metrics.FloorTopFor(floor);
        float hx = floor.lossyScale.x * 0.5f;

        for (int i = 0; i < 3; i++)
        {
            float x = c.x - hx * 0.45f + i * (hx * 0.45f);
            BuildWorkbench(room, new Vector3(x, fy, c.z - 4f), panel, metal, $"Workbench_{i}");
            BuildToolShelf(room, new Vector3(x, fy, c.z + 3f), metal, $"Shelf_{i}");
        }
    }

    static void BuildWorkbench(Transform parent, Vector3 floorPos, Material top, Material leg, string name)
    {
        CreateBlock(parent, name + "_Top", floorPos + new Vector3(0f, 0.9f, 0f), new Vector3(2.2f, 0.1f, 0.9f), top);
        foreach (var off in new[] { new Vector3(-0.9f, 0.45f, -0.35f), new Vector3(0.9f, 0.45f, -0.35f),
            new Vector3(-0.9f, 0.45f, 0.35f), new Vector3(0.9f, 0.45f, 0.35f) })
            CreateBlock(parent, name + "_Leg", floorPos + off, new Vector3(0.1f, 0.9f, 0.1f), leg);
    }

    static void BuildToolShelf(Transform parent, Vector3 floorPos, Material metal, string name)
    {
        CreateBlock(parent, name + "_Frame", floorPos + new Vector3(0f, 1.0f, 0f), new Vector3(1.6f, 2f, 0.4f), metal);
        for (int i = 0; i < 3; i++)
            CreateBlock(parent, name + $"_Shelf{i}", floorPos + new Vector3(0f, 0.5f + i * 0.55f, 0.05f),
                new Vector3(1.4f, 0.05f, 0.35f), metal);
    }

    static void FurnishMainSecurity(Transform floor, Transform root, CellMetrics metrics)
    {
        if (floor == null) return;
        var room = CreateRoomRoot(root, "MainSecurityProps", floor);
        var panel = LoadMat(PanelMatPath);
        var security = LoadMat(SecurityMatPath);
        var metal = LoadMat(MetalMatPath);
        var c = floor.position;
        float fy = metrics.FloorTopFor(floor);
        float hz = floor.lossyScale.z * 0.5f;

        CreateBlock(room, "DeskBase", new Vector3(c.x, fy + 0.45f, c.z - hz * 0.2f), new Vector3(2.4f, 0.9f, 0.9f), panel);
        CreateBlock(room, "DeskTop", new Vector3(c.x, fy + 0.92f, c.z - hz * 0.2f), new Vector3(2.6f, 0.06f, 1.0f), panel);
        CreateBlock(room, "MonitorBank", new Vector3(c.x, fy + 1.8f, c.z - hz * 0.38f), new Vector3(4f, 1.2f, 0.12f), security ?? panel);
        BuildBench(room, new Vector3(c.x, fy, c.z - hz * 0.05f), "GuardSeat");

        CreateBlock(room, "GatePostL", new Vector3(c.x - 1.2f, fy + 1.0f, c.z + hz - 0.8f), new Vector3(0.15f, 2f, 0.15f), metal);
        CreateBlock(room, "GatePostR", new Vector3(c.x + 1.2f, fy + 1.0f, c.z + hz - 0.8f), new Vector3(0.15f, 2f, 0.15f), metal);
        CreateBlock(room, "GateBar", new Vector3(c.x, fy + 1.5f, c.z + hz - 0.8f), new Vector3(2.6f, 0.1f, 0.1f), metal);
    }

    static Transform CreateRoomRoot(Transform parent, string name, Transform floor)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = floor.position;
        return go.transform;
    }

    /// <summary>Structural wall segment — keeps physics collider so players cannot walk through.</summary>
    static GameObject CreateWallBlock(Transform parent, string name, Vector3 worldPos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, true);
        go.transform.position = worldPos;
        go.transform.localScale = scale;
        if (mat != null)
            go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    /// <summary>Visual-only geometry (props, roofs, lights) — no collider.</summary>
    static GameObject CreateBlock(Transform parent, string name, Vector3 worldPos, Vector3 scale, Material mat)
    {
        var go = CreateWallBlock(parent, name, worldPos, scale, mat);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    static void EnsureZone(GameObject host, ZoneType type, string hudName, Transform floor, CellMetrics metrics)
    {
        var zone = host.GetComponent<PrisonLocationZone>() ?? host.AddComponent<PrisonLocationZone>();
        zone.zoneType = type;
        zone.hudDisplayName = hudName;

        var col = host.GetComponent<BoxCollider>();
        if (col == null)
            col = host.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.center = Vector3.zero;
        col.size = new Vector3(floor.lossyScale.x * 0.9f, metrics.WallHeight, floor.lossyScale.z * 0.9f);
    }

    static void WireRegistry()
    {
        var registry = Object.FindAnyObjectByType<PrisonLocationRegistry>();
        if (registry == null) return;

        var cellTransforms = new List<Transform>();
        foreach (var blockName in new[] { "JailCells", "JailCells_East" })
        {
            var block = GameObject.Find(blockName);
            if (block == null) continue;
            cellTransforms.AddRange(block.transform.Cast<Transform>()
                .Where(t => t.name.StartsWith("JailCell_")).OrderBy(t => t.name));
        }

        var cells = new List<CellData>();
        for (int i = 0; i < cellTransforms.Count; i++)
        {
            var cell = cellTransforms[i];
            var spawn = cell.Find("SpawnPoint");
            var rollCall = cell.Find("RollCallPoint");
            var bed = cell.Find("Bed");

            cells.Add(new CellData
            {
                spawnPoint = spawn,
                rollCallStandPoint = rollCall != null ? rollCall : spawn,
                bedPresenceCenter = bed != null ? bed : spawn,
                shakedownSweepCenter = spawn,
                interiorCheckRadius = 10.32f
            });

            var zone = cell.GetComponent<PrisonLocationZone>() ?? cell.gameObject.AddComponent<PrisonLocationZone>();
            zone.zoneType = ZoneType.Cell;
            zone.cellIndex = i;
            zone.hudDisplayName = $"CELL {i + 1:D2}";
        }

        registry.cells = cells.ToArray();

        var cafeteriaZone = GameObject.Find("CafeteriaProps")?.GetComponent<PrisonLocationZone>();
        if (cafeteriaZone != null) registry.cafeteria = cafeteriaZone;

        var yardZone = GameObject.Find("CourtyardProps")?.GetComponent<PrisonLocationZone>();
        if (yardZone != null) registry.yard = yardZone;

        EditorUtility.SetDirty(registry);
    }

    // ------------------------------------------------------------------
    // ESCAPE SYSTEMS (spec: docs/PrisonEscape/02 Features/Escape Completion System.md)
    // ------------------------------------------------------------------

    const string BarsMatPath = "Assets/Materials/Prison/PrisonBars_Metal.mat";
    const float BoundaryMargin = 12f;

    static void BuildEscapeSystems()
    {
        if (_activePlates.Count == 0)
            _activePlates = BuildDiagramPlates().ToList();

        var metrics = CellMetrics.SampleFromScene();
        var root = GetOrCreateRoot("EscapeSystems");
        ClearChildren(root);

        var spawns = BuildSolitaryBlock(root.transform, metrics);
        BuildEscapeBoundaryRing(root.transform);
        BuildRestrictedZones(root.transform);
        WireEscapeManager(spawns);

        Debug.Log($"[PrisonLayout] Escape systems built: solitary block ({spawns.Length} cells), boundary ring, restricted zones.");
    }

    /// <summary>4 solitary cells at the south end of Main Security (5 m pitch, cube-built).</summary>
    static Transform[] BuildSolitaryBlock(Transform parent, CellMetrics metrics)
    {
        var security = _activePlates.FirstOrDefault(p => p.Name == "MainSecurityFloor");
        if (security.Name == null)
        {
            Debug.LogWarning("[PrisonLayout] MainSecurityFloor plate missing — solitary block skipped.");
            return new Transform[0];
        }

        var block = new GameObject("SolitaryBlock");
        block.transform.SetParent(parent, false);

        var wallMat = LoadMat(WallMatPath);
        var barsMat = LoadMat(BarsMatPath) ?? LoadMat(MetalMatPath);

        float floorTop = FloorSurfaceY;
        float wallH = metrics.WallHeight;
        float wallCy = floorTop + wallH * 0.5f;

        const int cellCount = 4;
        const float cellWidth = 5f;
        const float cellDepth = 6f;
        const float doorWidth = 1.4f;

        float blockWest = security.Cx - cellCount * cellWidth * 0.5f;
        float southZ = security.MinZ + 2f;             // 2 m off the exterior south wall
        float northZ = southZ + cellDepth;              // barred front line

        // Partitions between/flanking cells (5 walls).
        for (int i = 0; i <= cellCount; i++)
        {
            float x = blockWest + i * cellWidth;
            CreateWallBlock(block.transform, $"SolitaryPartition_{i}",
                new Vector3(x, wallCy, (southZ + northZ) * 0.5f),
                new Vector3(WallThickness, wallH, cellDepth), wallMat);
        }

        // Back wall closing the 2 m gap line.
        CreateWallBlock(block.transform, "SolitaryBackWall",
            new Vector3(security.Cx, wallCy, southZ),
            new Vector3(cellCount * cellWidth + WallThickness, wallH, WallThickness), wallMat);

        var spawns = new Transform[cellCount];
        for (int i = 0; i < cellCount; i++)
        {
            float cellWestX = blockWest + i * cellWidth;
            float cellCenterX = cellWestX + cellWidth * 0.5f;

            // Barred front with a centered door gap.
            float segW = (cellWidth - doorWidth) * 0.5f;
            CreateWallBlock(block.transform, $"SolitaryFront_{i}_L",
                new Vector3(cellWestX + segW * 0.5f, wallCy, northZ),
                new Vector3(segW, wallH, WallThickness), barsMat);
            CreateWallBlock(block.transform, $"SolitaryFront_{i}_R",
                new Vector3(cellWestX + cellWidth - segW * 0.5f, wallCy, northZ),
                new Vector3(segW, wallH, WallThickness), barsMat);

            // Slab bed.
            CreateBlock(block.transform, $"SolitaryBed_{i}",
                new Vector3(cellCenterX, floorTop + 0.25f, southZ + 1.1f),
                new Vector3(2f, 0.5f, 0.9f), LoadMat(MetalMatPath));

            var spawn = new GameObject($"SolitarySpawn_{i}");
            spawn.transform.SetParent(block.transform, false);
            spawn.transform.position = new Vector3(cellCenterX, floorTop + 0.1f, (southZ + northZ) * 0.5f);
            spawns[i] = spawn.transform;
        }

        return spawns;
    }

    /// <summary>Ring of 4 trigger boxes ~12 m outside the perimeter walls — crossing any = escaped.</summary>
    static void BuildEscapeBoundaryRing(Transform parent)
    {
        GetFacilityOuterBounds(out float west, out float east, out float south, out float north);

        float bw = west - BoundaryMargin;
        float be = east + BoundaryMargin;
        float bs = south - BoundaryMargin;
        float bn = north + BoundaryMargin;
        float cx = (bw + be) * 0.5f;
        float cz = (bs + bn) * 0.5f;
        float spanX = be - bw + 8f;
        float spanZ = bn - bs + 8f;

        var ring = new GameObject("EscapeBoundary");
        ring.transform.SetParent(parent, false);

        CreateBoundarySegment(ring.transform, "Boundary_N", new Vector3(cx, 10f, bn), new Vector3(spanX, 20f, 4f));
        CreateBoundarySegment(ring.transform, "Boundary_S", new Vector3(cx, 10f, bs), new Vector3(spanX, 20f, 4f));
        CreateBoundarySegment(ring.transform, "Boundary_W", new Vector3(bw, 10f, cz), new Vector3(4f, 20f, spanZ));
        CreateBoundarySegment(ring.transform, "Boundary_E", new Vector3(be, 10f, cz), new Vector3(4f, 20f, spanZ));
    }

    static void CreateBoundarySegment(Transform parent, string name, Vector3 center, Vector3 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = center;
        var box = go.AddComponent<BoxCollider>();
        box.size = size;
        box.isTrigger = true;
        go.AddComponent<EscapeBoundary>();
    }

    /// <summary>
    /// Always-restricted band between the perimeter walls and the escape boundary,
    /// plus phase-restricted volumes over the cafeteria and workshop at night.
    /// </summary>
    static void BuildRestrictedZones(Transform parent)
    {
        GetFacilityOuterBounds(out float west, out float east, out float south, out float north);

        var zonesRoot = new GameObject("RestrictedZones");
        zonesRoot.transform.SetParent(parent, false);

        float band = BoundaryMargin;
        float cx = (west + east) * 0.5f;
        float cz = (south + north) * 0.5f;
        float outerSpanX = east - west + band * 2f;
        float outerSpanZ = north - south + band * 2f;

        CreateRestrictedBox(zonesRoot.transform, "Restricted_PerimeterN",
            new Vector3(cx, 6f, north + band * 0.5f), new Vector3(outerSpanX, 12f, band), true, null);
        CreateRestrictedBox(zonesRoot.transform, "Restricted_PerimeterS",
            new Vector3(cx, 6f, south - band * 0.5f), new Vector3(outerSpanX, 12f, band), true, null);
        CreateRestrictedBox(zonesRoot.transform, "Restricted_PerimeterW",
            new Vector3(west - band * 0.5f, 6f, cz), new Vector3(band, 12f, outerSpanZ), true, null);
        CreateRestrictedBox(zonesRoot.transform, "Restricted_PerimeterE",
            new Vector3(east + band * 0.5f, 6f, cz), new Vector3(band, 12f, outerSpanZ), true, null);

        var nightPhases = new[] { PrisonEventType.LightsOut, PrisonEventType.NightRollCall };
        foreach (var name in new[] { "CafeteriaFloor", "WorkshopFloor" })
        {
            var plate = _activePlates.FirstOrDefault(p => p.Name == name);
            if (plate.Name == null) continue;
            CreateRestrictedBox(zonesRoot.transform, "Restricted_" + name.Replace("Floor", "") + "_Night",
                new Vector3(plate.Cx, 3.7f, plate.Cz), new Vector3(plate.Sx, 6f, plate.Sz), false, nightPhases);
        }
    }

    static void CreateRestrictedBox(Transform parent, string name, Vector3 center, Vector3 size,
        bool always, PrisonEventType[] during)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = center;
        var box = go.AddComponent<BoxCollider>();
        box.size = size;
        box.isTrigger = true;
        var zone = go.AddComponent<RestrictedZone>();
        zone.zoneName = name;
        zone.alwaysRestricted = always;
        zone.restrictedDuring = during ?? new PrisonEventType[0];
    }

    static void GetFacilityOuterBounds(out float west, out float east, out float south, out float north)
    {
        west = float.MaxValue; east = float.MinValue; south = float.MaxValue; north = float.MinValue;
        foreach (var p in _activePlates)
        {
            west = Mathf.Min(west, p.MinX);
            east = Mathf.Max(east, p.MaxX);
            south = Mathf.Min(south, p.MinZ);
            north = Mathf.Max(north, p.MaxZ);
        }
    }

    static void WireEscapeManager(Transform[] solitarySpawns)
    {
        var go = GameObject.Find("EscapeManager") ?? new GameObject("EscapeManager");
        var manager = go.GetComponent<EscapeManager>() ?? go.AddComponent<EscapeManager>();
        if (go.GetComponent<PlayerStats>() == null) go.AddComponent<PlayerStats>();
        if (go.GetComponent<PrisonSuspicion>() == null) go.AddComponent<PrisonSuspicion>();
        manager.solitarySpawnPoints = solitarySpawns;
        EditorUtility.SetDirty(manager);
    }

    static Material LoadMat(string path) => AssetDatabase.LoadAssetAtPath<Material>(path);

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
