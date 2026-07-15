using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Prison;

/// <summary>
/// One-shot repair pass for the prison scene: realigns every cell door to its cell
/// shell (single canonical alignment path), creates missing per-cell stand-point
/// transforms so the registry never collapses them onto the spawn point, snaps guard
/// patrol waypoints onto the NavMesh, re-wires the location registry, and saves.
/// Batchmode-safe: opens PrisonLevel1 if it isn't the active scene.
/// </summary>
public static class PrisonDoorAndWaypointFixer
{
    const string ScenePath = "Assets/Scenes/PrisonLevel1.unity";
    const float WaypointNavMeshSampleRadius = 4f;
    const float StandPointNavMeshSampleRadius = 2.5f;

    [MenuItem("Prison/Fix Cell Doors & Waypoints")]
    public static void FixAll()
    {
        EnsureSceneOpen();

        int doors = FixCellDoors();
        int points = EnsureCellStandPoints();
        int waypoints = SnapPatrolWaypointsToNavMesh();

        PrisonLevelLayoutRunner.WireRegistryPublic();

        var scene = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log($"[DoorWaypointFixer] Done — {doors} doors realigned, {points} stand points created, {waypoints} patrol waypoints snapped. Scene saved.");
    }

    static void EnsureSceneOpen()
    {
        var active = SceneManager.GetActiveScene();
        if (active.path != ScenePath)
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    // ------------------------------------------------------------------
    // Doors: one alignment path for both hierarchies (modular CellKit_Door
    // under JailCell_* and facility Cell_NN_Door under PrisonFacility).
    // ------------------------------------------------------------------
    static int FixCellDoors()
    {
        int fixedCount = 0;
        foreach (var door in Object.FindObjectsByType<CellDoorController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!TryFindShellAndBed(door.transform, out Transform shell, out Transform bed))
            {
                Debug.LogWarning($"[DoorWaypointFixer] No cell shell found for door '{GetPath(door.transform)}' — skipped.", door);
                continue;
            }

            PrisonFacilityInstaller.AlignDoorToCellWall(door.transform, shell, bed);
            door.openOffset = PrisonFacilityInstaller.ComputeDoorOpenOffsetLocal(door.transform, bed);
            door.slideSpeed = Mathf.Max(door.slideSpeed, 3f);
            door.InitializeClosedPosition();
            PrisonFacilityInstaller.EnsureDoorCollider(door.transform);
            EditorUtility.SetDirty(door.gameObject);
            fixedCount++;
        }

        return fixedCount;
    }

    static bool TryFindShellAndBed(Transform door, out Transform shell, out Transform bed)
    {
        shell = null;
        bed = null;

        // Modular kit layout: JailCell_XX / CellKit_Door with sibling shell + bed.
        var cell = door.parent;
        if (cell != null)
        {
            shell = cell.Find("CellKit_Shell");
            bed = cell.Find("CellKit_Bed");
            if (bed == null) bed = cell.Find("Bed");
            if (shell != null)
                return true;
        }

        // Facility FBX layout: Cell_NN_Door with Cell_NN shell and Cell_NN_Bed elsewhere in the hierarchy.
        var match = Regex.Match(door.name, @"^Cell_(\d+)_Door$");
        if (match.Success)
        {
            var root = door.root;
            string cellName = $"Cell_{match.Groups[1].Value}";
            shell = FindDeepChild(root, cellName);
            bed = FindDeepChild(root, cellName + "_Bed");
            return shell != null;
        }

        return false;
    }

    // ------------------------------------------------------------------
    // Stand points: create the child transforms WireRegistry looks for, so
    // roll-call / night-check / shakedown points stop collapsing onto spawn.
    // Cell local axes: door + corridor sit on +right, interior depth along back.
    // ------------------------------------------------------------------
    static int EnsureCellStandPoints()
    {
        int created = 0;
        foreach (var blockName in new[] { "JailCells", "JailCells_East" })
        {
            var block = GameObject.Find(blockName);
            if (block == null) continue;

            foreach (Transform cell in block.transform)
            {
                if (!cell.name.StartsWith("JailCell_")) continue;
                var spawn = cell.Find("SpawnPoint");
                if (spawn == null) continue;

                Vector3 right = cell.right;
                created += EnsureChildPoint(cell, "RollCallPoint", spawn.position + right * 1.0f, snapToNavMesh: true);
                created += EnsureChildPoint(cell, "NightCheckApproach", spawn.position + right * 3.0f, snapToNavMesh: true);
                created += EnsureChildPoint(cell, "ShakedownCenter", spawn.position, snapToNavMesh: false);
            }
        }

        return created;
    }

    static int EnsureChildPoint(Transform cell, string name, Vector3 worldPos, bool snapToNavMesh)
    {
        if (cell.Find(name) != null) return 0;

        if (snapToNavMesh && NavMesh.SamplePosition(worldPos, out NavMeshHit hit, StandPointNavMeshSampleRadius, NavMesh.AllAreas))
            worldPos = hit.position;

        var go = new GameObject(name);
        go.transform.SetParent(cell, false);
        go.transform.position = worldPos;
        go.transform.rotation = cell.rotation;
        EditorUtility.SetDirty(cell.gameObject);
        return 1;
    }

    // ------------------------------------------------------------------
    // Guard patrol waypoints: hand-authored transforms that layout rebuilds can
    // strand inside walls — snap each onto the NavMesh and report the unfixable.
    // ------------------------------------------------------------------
    static int SnapPatrolWaypointsToNavMesh()
    {
        var gm = Object.FindAnyObjectByType<GameManager>();
        var seen = new HashSet<Transform>();
        int snapped = 0;

        if (gm != null && gm.guardSpawnTable != null)
        {
            foreach (var entry in gm.guardSpawnTable)
            {
                if (entry?.patrolWaypoints == null) continue;
                foreach (var wp in entry.patrolWaypoints)
                    snapped += SnapWaypoint(wp, seen);
            }
        }

        // Legacy loose waypoints (objects named Waypoint*) used by the fallback spawn path.
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (t != null && t.name.StartsWith("Waypoint"))
                snapped += SnapWaypoint(t, seen);
        }

        return snapped;
    }

    static int SnapWaypoint(Transform wp, HashSet<Transform> seen)
    {
        if (wp == null || !seen.Add(wp)) return 0;

        if (NavMesh.SamplePosition(wp.position, out NavMeshHit hit, WaypointNavMeshSampleRadius, NavMesh.AllAreas))
        {
            if (Vector3.Distance(wp.position, hit.position) > 0.01f)
            {
                wp.position = hit.position;
                EditorUtility.SetDirty(wp.gameObject);
                return 1;
            }
            return 0;
        }

        Debug.LogWarning($"[DoorWaypointFixer] Patrol waypoint '{GetPath(wp)}' is more than {WaypointNavMeshSampleRadius} m off the NavMesh — move it manually.", wp);
        return 0;
    }

    static Transform FindDeepChild(Transform root, string name)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    static string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
