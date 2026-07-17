using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Prison;

/// <summary>
/// Visual + physical polish for the prison scene, batchmode-safe:
/// solid colliders on every prop, procedural textures on the shared materials,
/// extra corridor/room props, ambient lighting, full-facility guard patrol
/// routes, and a NavMesh rebake. Run after the layout is built.
/// Includes post-process + fixture PointLight helpers (Lighting menu).
/// </summary>
public static class PrisonPolishPass
{
    const string TextureFolder = "Assets/Textures/Prison";

    [MenuItem("Prison/Polish Pass (Props, Colliders, Textures, Patrols)")]
    public static void RunAll()
    {
        PrisonLevelLayoutRunner.FurnishRoomsMenu();
        FurnishExtraProps();
        int colliders = EnsurePropColliders();
        GeneratePrisonTextures();
        PrisonLevelLayoutRunner.BuildLightingMenu();
        ApplyAmbientLighting();
        ConfigurePrisonPostProcess();
        EnsureFixturePointLights();
        int surfaces = RebakeNavMesh();
        int routes = RebuildGuardPatrolRoutes();

        Debug.Log($"[PrisonPolish] Done — {colliders} colliders added, {surfaces} NavMesh surfaces rebaked, {routes} guard entries re-routed.");
    }

    // ------------------------------------------------------------------
    // Colliders: props are placed with structural:false (colliders stripped),
    // so the player clips straight through them. Give every rendered prop a
    // convex MeshCollider unless something already covers it.
    // ------------------------------------------------------------------
    public static int EnsurePropColliders()
    {
        int added = 0;
        foreach (var rootName in new[] { "RoomProps", "PolishProps", "JailCells", "JailCells_East", PrisonFacilityInstaller.RootName })
        {
            var root = GameObject.Find(rootName);
            if (root == null) continue;

            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                var go = renderer.gameObject;
                if (ShouldSkipCollider(go)) continue;

                var mf = go.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                if (HasColliderInSelfOrParents(go.transform, root.transform)) continue;

                var mc = go.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                mc.convex = true;
                EditorUtility.SetDirty(go);
                added++;
            }
        }

        return added;
    }

    static bool ShouldSkipCollider(GameObject go)
    {
        string n = go.name;
        if (n.Contains("Light") || n.Contains("Lamp") || n.Contains("Roof") || n.Contains("Soffit") || n.Contains("Ceiling"))
            return true;
        // World item pickups and interactables manage their own colliders/triggers.
        if (go.GetComponentInParent<PickupItem>() != null) return true;
        if (go.GetComponentInParent<CellDoorController>() != null) return true;
        return false;
    }

    static bool HasColliderInSelfOrParents(Transform t, Transform stopAt)
    {
        for (var cur = t; cur != null && cur != stopAt.parent; cur = cur.parent)
        {
            foreach (var col in cur.GetComponents<Collider>())
                if (col != null && !col.isTrigger)
                    return true;
            if (cur == stopAt) break;
        }
        return false;
    }

    // ------------------------------------------------------------------
    // Procedural textures: the shared materials are flat colors; bake simple
    // grayscale detail maps (they multiply with each material's tint).
    // ------------------------------------------------------------------
    public static void GeneratePrisonTextures()
    {
        Directory.CreateDirectory(TextureFolder);

        // Cinderblock wall: mortar courses + grain, so walls read like real block, not flat paint.
        var wall = BakeTexture("T_CinderBlockWall", (x, y) =>
        {
            const int rows = 5, cols = 3;
            float ry = y * rows;
            int row = (int)ry;
            float fx = (x * cols + (row % 2) * 0.5f) % 1f;
            float fy = ry % 1f;
            bool mortar = fy < 0.05f || fy > 0.95f || fx < 0.03f || fx > 0.97f;
            float grain = 0.74f + 0.14f * Mathf.PerlinNoise(x * 26f, y * 26f) + 0.06f * Mathf.PerlinNoise(x * 90f, y * 90f);
            float v = mortar ? 0.46f : grain;
            return new Color(v, v, v);
        });

        var concrete = BakeTexture("T_ConcreteGrain", (x, y) =>
        {
            float v = 0.78f + 0.13f * Mathf.PerlinNoise(x * 14f, y * 14f) + 0.06f * Mathf.PerlinNoise(x * 55f, y * 55f);
            return new Color(v, v, v);
        });

        var tile = BakeTexture("T_FloorTile", (x, y) =>
        {
            const int tiles = 8;
            float fx = x * tiles % 1f;
            float fy = y * tiles % 1f;
            bool grout = fx < 0.035f || fy < 0.035f || fx > 0.965f || fy > 0.965f;
            float variance = 0.9f + 0.08f * Mathf.PerlinNoise(Mathf.Floor(x * tiles) * 5.17f, Mathf.Floor(y * tiles) * 3.31f);
            float v = grout ? 0.55f : variance;
            return new Color(v, v, v);
        });

        var metal = BakeTexture("T_BrushedMetal", (x, y) =>
        {
            float v = 0.85f + 0.10f * Mathf.PerlinNoise(x * 3f, y * 90f);
            return new Color(v, v, v);
        });

        int applied = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/Materials" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null || !mat.HasProperty("_BaseMap")) continue;

            string n = mat.name;
            Texture2D tex = null;
            Vector2 tiling = Vector2.one * 4f;
            bool overrideExisting = false;

            if (n.Contains("Wall")) { tex = wall; tiling = new Vector2(3f, 2f); overrideExisting = true; }
            else if (n.Contains("Ceiling") || n.Contains("Concrete")) { tex = concrete; tiling = new Vector2(4f, 4f); overrideExisting = true; }
            else if (n.Contains("Tile") || n.Contains("Floor")) { tex = tile; tiling = new Vector2(8f, 8f); }
            else if (n.Contains("Metal") || n.Contains("Bars") || n.Contains("Sink") || n.Contains("Shelf")) { tex = metal; tiling = new Vector2(2f, 2f); }

            if (tex == null) continue;
            // Walls/ceilings: replace the flat placeholder map. Floors/metal: only fill gaps.
            if (mat.GetTexture("_BaseMap") != null && !overrideExisting) continue;

            mat.SetTexture("_BaseMap", tex);
            mat.SetTextureScale("_BaseMap", tiling);
            EditorUtility.SetDirty(mat);
            applied++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[PrisonPolish] Textures baked and applied to {applied} materials.");
    }

    static Texture2D BakeTexture(string name, System.Func<float, float, Color> sample)
    {
        const int size = 256;
        string path = $"{TextureFolder}/{name}.png";

        var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing != null) return existing;

        var tex = new Texture2D(size, size, TextureFormat.RGB24, false);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, sample(x / (float)size, y / (float)size));
        tex.Apply();

        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    // ------------------------------------------------------------------
    // Extra props: pillars + clutter along the corridors, more seating in the
    // yard and cafeteria, storage details. Kept under their own root so the
    // base FurnishRooms rebuild never wipes or duplicates them.
    // ------------------------------------------------------------------
    public static void FurnishExtraProps()
    {
        if (!BlenderKitLayout.IsAvailable)
        {
            Debug.LogWarning("[PrisonPolish] BlenderKit unavailable — skipping extra props.");
            return;
        }

        var root = GameObject.Find("PolishProps");
        if (root != null) Object.DestroyImmediate(root);
        root = new GameObject("PolishProps");

        float fy = FloorTopOf("Corridor_North");

        // Pillars + clutter down both inner corridors.
        foreach (var plateName in new[] { "Corridor_North", "Corridor_South" })
        {
            var plate = FindPlate(plateName);
            if (plate == null) continue;
            float half = plate.lossyScale.x * 0.5f - 2f;
            for (int i = -2; i <= 2; i++)
            {
                var pos = plate.position + new Vector3(i * half / 2f, 0f, 0f);
                pos.y = fy;
                BlenderKitLayout.PlaceKit("SM_Pillar_Concrete", root.transform, pos, Quaternion.identity, true);
            }
        }

        // Corner clutter on the outer loop.
        foreach (var (corner, asset) in new[]
        {
            ("Corridor_NW", "SM_Prop_Barrel"), ("Corridor_NE", "SM_Prop_Pallet"),
            ("Corridor_SW", "SM_Work_StorageCrate"), ("Corridor_SE", "SM_Prop_Barrel"),
        })
        {
            var plate = FindPlate(corner);
            if (plate == null) continue;
            var pos = plate.position; pos.y = fy;
            BlenderKitLayout.PlaceKit(asset, root.transform, pos + new Vector3(1.2f, 0f, 1.2f), Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), true);
        }

        // More cafeteria tables (third row) and yard seating.
        var caf = FindPlate("CafeteriaFloor");
        if (caf != null)
        {
            float cfy = FloorTopOf("CafeteriaFloor");
            float hz = caf.lossyScale.z * 0.5f;
            for (int col = 0; col < 3; col++)
            {
                float x = caf.position.x - 7f + col * 7f;
                BlenderKitLayout.PlaceKit("SM_Caf_TableBench", root.transform,
                    new Vector3(x, cfy, caf.position.z - hz * 0.45f), Quaternion.identity, true);
            }
            BlenderKitLayout.PlaceKit("SM_Prop_FireExtinguisher", root.transform,
                new Vector3(caf.position.x - caf.lossyScale.x * 0.5f + 0.6f, cfy, caf.position.z), Quaternion.identity, true);
        }

        var yard = FindPlate("CourtyardFloor");
        if (yard != null)
        {
            float yfy = FloorTopOf("CourtyardFloor");
            float hx = yard.lossyScale.x * 0.5f;
            for (int i = 0; i < 4; i++)
            {
                float x = yard.position.x - hx * 0.6f + i * hx * 0.4f;
                BlenderKitLayout.PlaceKit("SM_Shower_Bench", root.transform,
                    new Vector3(x, yfy, yard.position.z + yard.lossyScale.z * 0.5f - 3f), Quaternion.identity, true);
            }
            BlenderKitLayout.PlaceKit("SM_Yard_WeightBench", root.transform,
                new Vector3(yard.position.x + 8f, yfy, yard.position.z + 4f), Quaternion.Euler(0f, 45f, 0f), true);
        }

        // Wing detail: filing cabinet + clock per cell wing entrance.
        foreach (var wing in new[] { "CellWingFloor_West", "CellWingFloor_East" })
        {
            var plate = FindPlate(wing);
            if (plate == null) continue;
            float wfy = FloorTopOf(wing);
            BlenderKitLayout.PlaceKit("SM_Prop_FilingCabinet", root.transform,
                plate.position + new Vector3(0f, wfy - plate.position.y, plate.lossyScale.z * 0.5f - 1.5f), Quaternion.identity, true);
        }

        Debug.Log("[PrisonPolish] Extra props placed under PolishProps.");
    }

    static Transform FindPlate(string name)
    {
        // Legacy procedural layout plates under LayoutFloors.
        var floors = GameObject.Find("LayoutFloors");
        if (floors != null)
        {
            var legacy = floors.transform.Cast<Transform>().FirstOrDefault(t => t.name == name);
            if (legacy != null) return legacy;
        }

        // BlenderKit facility plates are named ASM_<plate> and live under PrisonFacility.
        string asmName = name.StartsWith("ASM_") ? name : "ASM_" + name;
        var asm = GameObject.Find(asmName);
        if (asm != null) return asm.transform;

        // Deep search in case the plate is nested and Find misses inactive roots.
        var facility = GameObject.Find("PrisonFacility");
        if (facility != null)
        {
            foreach (var t in facility.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name || t.name == asmName)
                    return t;
            }
        }

        return null;
    }

    static float FloorTopOf(string plateName)
    {
        var plate = FindPlate(plateName);
        if (plate == null) return 0.82f;

        // Mesh floors (BlenderKit): use renderer top. Scaled cubes (legacy): half-height.
        var renderer = plate.GetComponent<Renderer>() ?? plate.GetComponentInChildren<Renderer>();
        if (renderer != null)
            return renderer.bounds.max.y;

        return plate.position.y + plate.lossyScale.y * 0.5f;
    }

    // ------------------------------------------------------------------
    // Lighting ambience (fixtures come from Build All Lighting).
    // ------------------------------------------------------------------
    public static void ApplyAmbientLighting()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.58f, 0.63f, 0.72f);
        RenderSettings.ambientEquatorColor = new Color(0.42f, 0.43f, 0.47f);
        RenderSettings.ambientGroundColor = new Color(0.24f, 0.23f, 0.22f);
        RenderSettings.ambientIntensity = 1.05f;
    }

    /// <summary>
    /// BlenderKit Light_* meshes are visual-only. Attach realtime PointLights to a
    /// thinned subset so interiors read lit without ~200 overlapping lights.
    /// </summary>
    public static int EnsureFixturePointLights(int stride = 3, float intensity = 4.2f, float range = 11f)
    {
        stride = Mathf.Max(1, stride);
        int added = 0;
        int seen = 0;
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (t == null) continue;
            string n = t.name;
            if (!n.StartsWith("Light_", System.StringComparison.Ordinal) &&
                !n.StartsWith("CellLight_", System.StringComparison.Ordinal))
                continue;
            if (t.GetComponentInChildren<Light>(true) != null) continue;

            seen++;
            if ((seen - 1) % stride != 0) continue;

            var lightGo = new GameObject("RealtimePointLight");
            lightGo.transform.SetParent(t, false);
            lightGo.transform.localPosition = Vector3.zero;
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.intensity = intensity;
            light.range = range;
            light.color = new Color(1f, 0.93f, 0.82f);
            light.shadows = LightShadows.None;
            light.renderMode = LightRenderMode.Auto;
            EditorUtility.SetDirty(t.gameObject);
            added++;
        }

        // Prefer a single key directional with soft shadows.
        var dirs = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(l => l != null && l.type == LightType.Directional)
            .OrderBy(l => l.name)
            .ToArray();
        for (int i = 0; i < dirs.Length; i++)
        {
            if (i == 0)
            {
                dirs[i].enabled = true;
                dirs[i].intensity = Mathf.Max(dirs[i].intensity, 1.15f);
                dirs[i].shadows = LightShadows.Soft;
                continue;
            }
            // Disable duplicates that wash out contrast.
            if (dirs[i].name.Contains("(1)") || dirs.Length > 1)
                dirs[i].enabled = false;
        }

        Debug.Log($"[PrisonPolish] Attached {added} realtime point lights to fixture meshes (stride={stride}).");
        return added;
    }

    /// <summary>Fill the empty PrisonPostProcess volume profile with mild URP looks.</summary>
    public static void ConfigurePrisonPostProcess()
    {
        var guids = AssetDatabase.FindAssets("PrisonPostProcess t:VolumeProfile");
        string path = guids.Length > 0
            ? AssetDatabase.GUIDToAssetPath(guids[0])
            : "Assets/Settings/PrisonPostProcess.asset";

        var profile = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>(path);
        if (profile == null)
        {
            Directory.CreateDirectory("Assets/Settings");
            profile = ScriptableObject.CreateInstance<UnityEngine.Rendering.VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
        }

        // Profile previously had null component slots (broken overrides) — clear them.
        for (int i = profile.components.Count - 1; i >= 0; i--)
        {
            if (profile.components[i] == null)
                profile.components.RemoveAt(i);
        }

        EnsureVolumeComponent<UnityEngine.Rendering.Universal.Bloom>(profile, bloom =>
        {
            bloom.active = true;
            bloom.threshold.Override(0.92f);
            bloom.intensity.Override(0.22f);
            bloom.scatter.Override(0.65f);
        });
        EnsureVolumeComponent<UnityEngine.Rendering.Universal.ColorAdjustments>(profile, ca =>
        {
            ca.active = true;
            ca.postExposure.Override(0.18f);
            ca.contrast.Override(12f);
            ca.saturation.Override(8f);
            ca.colorFilter.Override(new Color(1f, 0.97f, 0.92f));
        });
        EnsureVolumeComponent<UnityEngine.Rendering.Universal.Tonemapping>(profile, tm =>
        {
            tm.active = true;
            tm.mode.Override(UnityEngine.Rendering.Universal.TonemappingMode.ACES);
        });
        EnsureVolumeComponent<UnityEngine.Rendering.Universal.Vignette>(profile, vg =>
        {
            vg.active = true;
            vg.intensity.Override(0.18f);
            vg.smoothness.Override(0.45f);
        });
        EnsureVolumeComponent<UnityEngine.Rendering.Universal.WhiteBalance>(profile, wb =>
        {
            wb.active = true;
            wb.temperature.Override(-4f);
            wb.tint.Override(2f);
        });

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();

        var volume = Object.FindFirstObjectByType<UnityEngine.Rendering.Volume>();
        if (volume != null)
        {
            volume.isGlobal = true;
            volume.weight = 1f;
            volume.sharedProfile = profile;
            EditorUtility.SetDirty(volume);
        }

        foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var data = cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            if (data == null) continue;
            data.renderPostProcessing = true;
            EditorUtility.SetDirty(cam);
        }

        Debug.Log($"[PrisonPolish] Configured post-process profile at {path}.");
    }

    static void EnsureVolumeComponent<T>(UnityEngine.Rendering.VolumeProfile profile, System.Action<T> configure)
        where T : UnityEngine.Rendering.VolumeComponent
    {
        if (!profile.TryGet(out T comp))
        {
            comp = profile.Add<T>(true);
        }
        configure?.Invoke(comp);
    }

    [MenuItem("Prison/Lighting/Configure Post-Process + Fixture Lights")]
    public static void ConfigureLightingLooksMenu()
    {
        ApplyAmbientLighting();
        ConfigurePrisonPostProcess();
        EnsureFixturePointLights();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
    }

    // ------------------------------------------------------------------
    // Guard patrol routes covering the whole facility, generated from the
    // layout plates (positions come from the scene, not hard-coded numbers).
    // ------------------------------------------------------------------
    public static int RebuildGuardPatrolRoutes()
    {
        var old = GameObject.Find("GuardPatrolRoutes");
        if (old != null) Object.DestroyImmediate(old);
        var root = new GameObject("GuardPatrolRoutes");

        var perimeter = BuildRoute(root.transform, "Route_Perimeter", PerimeterRingPositions());
        var inner = BuildRoute(root.transform, "Route_InnerCorridors", InnerLoopPositions());
        var wings = BuildRoute(root.transform, "Route_CellWings", WingPositions());

        var gm = Object.FindAnyObjectByType<GameManager>();
        if (gm == null || gm.guardSpawnTable == null || gm.guardSpawnTable.Length == 0)
        {
            Debug.LogWarning("[PrisonPolish] No GameManager guard spawn table — routes built but not assigned.");
            return 0;
        }

        int standardIndex = 0;
        int assigned = 0;
        bool hasSweeper = false;
        foreach (var entry in gm.guardSpawnTable)
        {
            if (entry == null) continue;

            // Keep every guard always on duty so the morning line-up sweep isn't gated
            // out by a shift window (a StandardPatrol guard switches to sweeper during
            // MorningRollCall; see GuardShiftController.ApplyStandardPatrolShift).
            entry.onDutyDuring = null;

            if (entry.role == GuardSpawnRole.NightCellVerifier)
            {
                entry.patrolWaypoints = wings;
            }
            else if (entry.role == GuardSpawnRole.StandardPatrol)
            {
                entry.patrolWaypoints = (standardIndex++ % 2 == 0) ? perimeter : inner;
                hasSweeper = true;
            }
            else if (entry.role == GuardSpawnRole.MorningShakedown)
            {
                hasSweeper = true;
            }
            assigned++;
        }

        // Guarantee at least one guard can run the morning sweep.
        if (!hasSweeper)
        {
            foreach (var entry in gm.guardSpawnTable)
            {
                if (entry == null) continue;
                entry.role = GuardSpawnRole.StandardPatrol;
                entry.patrolWaypoints = perimeter;
                Debug.Log("[PrisonPolish] No sweeper-capable guard found — promoted one to StandardPatrol for roll call.");
                break;
            }
        }

        EditorUtility.SetDirty(gm);
        return assigned;
    }

    static List<Vector3> PerimeterRingPositions()
    {
        var pts = new List<Vector3>();
        var north = FindPlate("Corridor_LoopNorth");
        var south = FindPlate("Corridor_LoopSouth");
        var west = FindPlate("Corridor_WestSpine");
        var east = FindPlate("Corridor_EastSpine");
        if (north == null || south == null || west == null || east == null) return pts;

        float y = FloorTopOf("Corridor_LoopNorth");
        float nz = north.position.z, sz = south.position.z;
        float wx = west.position.x, ex = east.position.x;
        float midX = (wx + ex) * 0.5f;
        float midZ = (nz + sz) * 0.5f;

        pts.Add(new Vector3(wx, y, nz));
        pts.Add(new Vector3(midX, y, nz));
        pts.Add(new Vector3(ex, y, nz));
        pts.Add(new Vector3(ex, y, midZ));
        pts.Add(new Vector3(ex, y, sz));
        pts.Add(new Vector3(midX, y, sz));
        pts.Add(new Vector3(wx, y, sz));
        pts.Add(new Vector3(wx, y, midZ));
        return pts;
    }

    static List<Vector3> InnerLoopPositions()
    {
        var pts = new List<Vector3>();
        var north = FindPlate("Corridor_North");
        var south = FindPlate("Corridor_South");
        if (north == null || south == null) return pts;

        float y = FloorTopOf("Corridor_North");
        float halfN = PlateHalfExtent(north, axisX: true) - 2f;
        float halfS = PlateHalfExtent(south, axisX: true) - 2f;
        if (halfN < 2f) halfN = 10f;
        if (halfS < 2f) halfS = 10f;

        pts.Add(north.position + new Vector3(-halfN, 0f, 0f)); pts[^1] = WithY(pts[^1], y);
        pts.Add(WithY(north.position, y));
        pts.Add(north.position + new Vector3(halfN, 0f, 0f)); pts[^1] = WithY(pts[^1], y);
        pts.Add(south.position + new Vector3(halfS, 0f, 0f)); pts[^1] = WithY(pts[^1], y);
        pts.Add(WithY(south.position, y));
        pts.Add(south.position + new Vector3(-halfS, 0f, 0f)); pts[^1] = WithY(pts[^1], y);
        return pts;
    }

    static List<Vector3> WingPositions()
    {
        var pts = new List<Vector3>();
        foreach (var name in new[] { "CellWingFloor_West", "CafeteriaFloor", "CellWingFloor_East", "CafeteriaFloor" })
        {
            var plate = FindPlate(name);
            if (plate == null) continue;
            float y = FloorTopOf(name);
            float halfD = Mathf.Max(2f, PlateHalfExtent(plate, axisX: false) * 0.5f);
            pts.Add(new Vector3(plate.position.x, y, plate.position.z + halfD));
            pts.Add(new Vector3(plate.position.x, y, plate.position.z - halfD));
        }
        return pts;
    }

    static float PlateHalfExtent(Transform plate, bool axisX)
    {
        var renderer = plate.GetComponent<Renderer>() ?? plate.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            Vector3 size = renderer.bounds.size;
            return (axisX ? size.x : size.z) * 0.5f;
        }
        return (axisX ? plate.lossyScale.x : plate.lossyScale.z) * 0.5f;
    }

    static Vector3 WithY(Vector3 v, float y) => new Vector3(v.x, y, v.z);

    static Transform[] BuildRoute(Transform parent, string routeName, List<Vector3> positions)
    {
        var route = new GameObject(routeName);
        route.transform.SetParent(parent, false);

        var result = new List<Transform>();
        for (int i = 0; i < positions.Count; i++)
        {
            var wp = new GameObject($"Waypoint_{routeName}_{i}");
            wp.transform.SetParent(route.transform, false);
            Vector3 pos = positions[i];
            if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 6f, NavMesh.AllAreas))
                pos = hit.position;
            wp.transform.position = pos;
            result.Add(wp.transform);
        }

        return result.ToArray();
    }

    // ------------------------------------------------------------------
    // NavMesh rebake — carve from ALL render geometry (walls included) at a
    // fine voxel size so 0.2 m walls reliably block agents (fixes NPCs walking
    // through walls and the shakedown guard failing to reach the cells).
    // ------------------------------------------------------------------
    // NavMesh rebake — prefer PhysicsColliders so solid walls block agents; fall back to
    // RenderMeshes if the bake produces no surface (missing colliders on some meshes).
    public static int RebakeNavMesh()
    {
        int count = 0;
        foreach (var surface in Object.FindObjectsByType<NavMeshSurface>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            surface.collectObjects = CollectObjects.All;
            surface.layerMask = ~0;
            surface.overrideVoxelSize = true;
            surface.voxelSize = 0.05f;
            surface.overrideTileSize = true;
            surface.tileSize = 32;
            surface.minRegionArea = 2f;

            surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();

            // Empty bake → walls likely lack colliders; use render meshes at fine voxel.
            if (surface.navMeshData == null)
            {
                surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.RenderMeshes;
                surface.BuildNavMesh();
            }

            EditorUtility.SetDirty(surface);
            count++;
        }
        return count;
    }
}
