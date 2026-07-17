using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// One-shot repair for "walking/seeing through walls" (validated 7/16/2026 on PrisonLevel1 +
/// CountyJail):
///  1. Adds MeshColliders to every scene renderer that has none (props, pipes, signs, doors),
///     so physics and the PhysicsColliders NavMesh bake both see them.
///  2. Removes duplicate GlobalNavMesh surfaces AND clears the legacy built-in scene bake —
///     both contained a stale pre-wall mesh that unioned in and let agents cross current walls.
///  3. Rebakes via <see cref="PrisonPolishPass.RebakeNavMesh"/> with cell-door colliders
///     temporarily disabled so doorways stay walkable. (Humanoid bake radius must be ≤0.3:
///     1.2 m doorways seal at the old 0.5. The radius lives in ProjectSettings and was lowered.)
///  4. Doors get a carving NavMeshObstacle plus a doorway <see cref="Prison.CellDoorNavMeshLink"/> —
///     the only cell↔corridor connection, active exactly while the schedule opens the door.
///  5. Clamps camera near-clip planes to 0.05 (prefabs + scene cameras) so standing against a
///     wall can't poke the view through it.
/// </summary>
public static class PrisonCollisionAndCameraFixer
{
    private static readonly string[] FacilityScenes =
    {
        "Assets/Scenes/PrisonLevel1.unity",
        "Assets/Scenes/CountyJail.unity",
    };

    private const float NearClip = 0.05f;

    [MenuItem("Prison/Fix Collision & Camera Clipping (Current Scene)")]
    public static void RunCurrentScene()
    {
        FixPrefabCameras();
        FixOpenScene();
        EditorSceneManager.SaveOpenScenes();
    }

    [MenuItem("Prison/Fix Collision & Camera Clipping (All Facility Scenes)")]
    public static void RunAllFacilityScenes()
    {
        FixPrefabCameras();
        foreach (string path in FacilityScenes)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
            {
                Debug.LogWarning($"[PrisonCollisionFix] Scene missing, skipped: {path}");
                continue;
            }
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            FixOpenScene();
            EditorSceneManager.SaveOpenScenes();
        }
    }

    private static void FixOpenScene()
    {
        int added = AddMissingColliders();
        int removedSurfaces = RemoveDuplicateNavMeshSurfaces();
        int doorGear = EnsureDoorObstaclesAndLinks();
        int rebaked = RebakeWithDoorwaysOpen();
        int cams = FixSceneCameras();
        Debug.Log($"[PrisonCollisionFix] {EditorSceneManager.GetActiveScene().name}: " +
                  $"{added} colliders, {removedSurfaces} dup surfaces removed, {doorGear} doors gated, " +
                  $"{rebaked} surfaces rebaked, {cams} cameras clamped.");
    }

    // ------------------------------------------------------------------
    // Colliders
    // ------------------------------------------------------------------

    private static int AddMissingColliders()
    {
        int added = 0;
        foreach (var mr in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            var go = mr.gameObject;
            if (IsCharacter(go.transform)) continue;
            if (HasColliderInSelfOrParents(go.transform)) continue;

            var mf = go.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;

            var mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
            mc.convex = false; // static level geometry; convex only matters under dynamic rigidbodies
            added++;
        }
        return added;
    }

    private static bool IsCharacter(Transform t)
    {
        for (var cur = t; cur != null; cur = cur.parent)
        {
            if (cur.GetComponent<NavMeshAgent>() != null) return true;
            if (cur.GetComponent<CharacterController>() != null) return true;
            if (cur.GetComponent<PrisonerController>() != null) return true;
        }
        return false;
    }

    private static bool HasColliderInSelfOrParents(Transform t)
    {
        for (var cur = t; cur != null; cur = cur.parent)
            if (cur.GetComponent<Collider>() != null)
                return true;
        return false;
    }

    // ------------------------------------------------------------------
    // NavMesh sources
    // ------------------------------------------------------------------

    private static int RemoveDuplicateNavMeshSurfaces()
    {
        var keeperByAgent = new Dictionary<int, Unity.AI.Navigation.NavMeshSurface>();
        int removed = 0;
        foreach (var s in Object.FindObjectsByType<Unity.AI.Navigation.NavMeshSurface>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!keeperByAgent.ContainsKey(s.agentTypeID))
            {
                keeperByAgent[s.agentTypeID] = s;
                continue;
            }
            Debug.Log($"[PrisonCollisionFix] Removing duplicate NavMeshSurface '{s.gameObject.name}'.");
            Object.DestroyImmediate(s.gameObject);
            removed++;
        }
        return removed;
    }

    // ------------------------------------------------------------------
    // Doors: carve obstacle + schedule-gated doorway link
    // ------------------------------------------------------------------

    private static int EnsureDoorObstaclesAndLinks()
    {
        int touched = 0;
        var doors = Object.FindObjectsByType<Prison.CellDoorController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var existingLinks = Object.FindObjectsByType<Prison.CellDoorNavMeshLink>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var door in doors)
        {
            var mf = door.GetComponentInChildren<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;
            bool changed = false;

            if (door.GetComponent<NavMeshObstacle>() == null)
            {
                var ob = door.gameObject.AddComponent<NavMeshObstacle>();
                ob.shape = NavMeshObstacleShape.Box;
                ob.center = mf.sharedMesh.bounds.center;
                ob.size = mf.sharedMesh.bounds.size;
                ob.carving = true;
                changed = true;
            }

            bool hasLink = false;
            foreach (var l in existingLinks)
                if (l != null && l.door == door) { hasLink = true; break; }
            if (!hasLink)
            {
                // Doorway normal = world axis of the panel's thinnest dimension.
                Vector3 worldSize = Vector3.Scale(mf.sharedMesh.bounds.size, door.transform.lossyScale);
                Vector3 localThin = Vector3.forward;
                if (worldSize.x <= worldSize.y && worldSize.x <= worldSize.z) localThin = Vector3.right;
                else if (worldSize.y <= worldSize.x && worldSize.y <= worldSize.z) localThin = Vector3.up;
                Vector3 normal = door.transform.TransformDirection(localThin).normalized;

                var go = new GameObject("DoorNavLink_" + door.name);
                go.transform.SetParent(door.transform.parent, false); // static — never slides with the panel
                go.transform.position = new Vector3(door.transform.position.x, 0.85f, door.transform.position.z);
                go.transform.rotation = Quaternion.LookRotation(normal, Vector3.up);

                var link = go.AddComponent<Unity.AI.Navigation.NavMeshLink>();
                link.startPoint = new Vector3(0f, 0f, -0.85f);
                link.endPoint = new Vector3(0f, 0f, 0.85f);
                link.width = 1.0f;
                link.bidirectional = true;
                link.autoUpdate = false;

                go.AddComponent<Prison.CellDoorNavMeshLink>().door = door;
                changed = true;
            }

            if (changed) touched++;
        }
        return touched;
    }

    private static int RebakeWithDoorwaysOpen()
    {
        var toggled = new List<Collider>();
        foreach (var door in Object.FindObjectsByType<Prison.CellDoorController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            foreach (var col in door.GetComponentsInChildren<Collider>(true))
                if (col.enabled) { col.enabled = false; toggled.Add(col); }

        UnityEditor.AI.NavMeshBuilder.ClearAllNavMeshes(); // stale legacy scene bake
        int rebaked = PrisonPolishPass.RebakeNavMesh();

        foreach (var col in toggled) col.enabled = true;
        return rebaked;
    }

    // ------------------------------------------------------------------
    // Cameras
    // ------------------------------------------------------------------

    private static int FixSceneCameras()
    {
        int fixedCount = 0;
        foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (cam.nearClipPlane <= NearClip + 0.001f) continue;
            cam.nearClipPlane = NearClip;
            fixedCount++;
        }
        return fixedCount;
    }

    private static void FixPrefabCameras()
    {
        foreach (string path in new[] { "Assets/Prefabs/LocalPlayer.prefab", "Assets/Prefabs/Player.prefab" })
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            bool changed = false;
            foreach (var cam in prefab.GetComponentsInChildren<Camera>(true))
            {
                if (cam.nearClipPlane <= NearClip + 0.001f) continue;
                cam.nearClipPlane = NearClip;
                changed = true;
            }
            if (changed)
            {
                EditorUtility.SetDirty(prefab);
                Debug.Log($"[PrisonCollisionFix] {path}: camera near clip → {NearClip}.");
            }
        }
        AssetDatabase.SaveAssets();
    }
}
