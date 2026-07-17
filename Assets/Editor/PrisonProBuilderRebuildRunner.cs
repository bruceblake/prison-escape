using System.Collections.Generic;
using System.Text;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
// Requires com.unity.probuilder in Packages/manifest.json
using UnityEngine.ProBuilder;
#endif

/// <summary>
/// ProBuilder-first prison rebuild pipeline. See Assets/Docs/Prison_Rebuild_Master_Plan.md.
/// </summary>
public static class PrisonProBuilderRebuildRunner
{
    private const string MenuRoot = "Prison/Rebuild/";

    [MenuItem(MenuRoot + "0 — Strip Legacy Outer Shell")]
    public static void StripLegacyOuterShell()
    {
        Transform legacyRoot = EnsureRoot(PrisonLayoutSpec.LegacyShellRootName);
        int archived = 0;

        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (t == null || IsProtectedFromStrip(t)) continue;
            if (!ShouldArchiveToLegacyShell(t)) continue;

            ArchiveTransform(t, legacyRoot);
            archived++;
        }

        var oldLayout = GameObject.Find(PrisonLayoutSpec.OldLayoutRootName);
        if (oldLayout != null)
        {
            Object.DestroyImmediate(oldLayout);
            Debug.Log("[PrisonRebuild] Removed procedural PrisonLayout root (superseded by ProBuilder pipeline).");
        }

        RestoreCellFloors();
        DisableCellRoofColliders();
        MarkDirty();
        Debug.Log($"[PrisonRebuild] Strip complete — archived {archived} objects under {PrisonLayoutSpec.LegacyShellRootName}.");
    }

    [MenuItem(MenuRoot + "1 — Create ProBuilder Workspace")]
    public static void CreateProBuilderWorkspace()
    {
        Transform buildRoot = EnsureRoot(PrisonLayoutSpec.PrisonBuildRootName);
        Transform draftRoot = EnsureChild(buildRoot, PrisonLayoutSpec.ProBuilderDraftRootName);

        ClearChildren(draftRoot);
        CreateRoomGuide(draftRoot, "Guide_CellBlock", PrisonLayoutAnchors.CellBlockCenter,
            new Vector3(58f, PrisonLayoutSpec.StandardWallHeight, 76f), new Color(0.2f, 0.5f, 0.9f, 0.15f));
        CreateRoomGuide(draftRoot, "Guide_MainCorridor", PrisonLayoutAnchors.MainCorridorCenter,
            new Vector3(52f, PrisonLayoutSpec.StandardWallHeight, 8f), new Color(0.3f, 0.8f, 0.4f, 0.15f));
        CreateRoomGuide(draftRoot, "Guide_Workshop", PrisonLayoutAnchors.WorkshopCenter,
            new Vector3(20f, PrisonLayoutSpec.StandardWallHeight, 16f), new Color(0.9f, 0.6f, 0.2f, 0.15f));
        CreateRoomGuide(draftRoot, "Guide_Laundry", PrisonLayoutAnchors.LaundryCenter,
            new Vector3(20f, PrisonLayoutSpec.StandardWallHeight, 16f), new Color(0.9f, 0.6f, 0.2f, 0.15f));
        CreateRoomGuide(draftRoot, "Guide_Cafeteria", PrisonLayoutAnchors.CafeteriaCenter,
            new Vector3(28f, PrisonLayoutSpec.StandardWallHeight, 24f), new Color(0.7f, 0.4f, 0.9f, 0.15f));
        CreateRoomGuide(draftRoot, "Guide_Yard", PrisonLayoutAnchors.YardCenter,
            new Vector3(70f, 2f, 40f), new Color(0.4f, 0.9f, 0.9f, 0.12f));

        EnsureChild(buildRoot, PrisonLayoutSpec.BakedShellRootName);
        MarkDirty();
        Debug.Log("[PrisonRebuild] ProBuilder workspace ready under PrisonBuild/ProBuilderDraft. Author walls/floors here with ProBuilder, then run step 2.");
    }

    [MenuItem(MenuRoot + "2 — Bake ProBuilder Draft To Shell")]
    public static void BakeProBuilderDraftToShell()
    {
        Transform buildRoot = GameObject.Find(PrisonLayoutSpec.PrisonBuildRootName)?.transform;
        if (buildRoot == null)
        {
            Debug.LogError("[PrisonRebuild] Run step 1 first — no PrisonBuild root.");
            return;
        }

        Transform draftRoot = buildRoot.Find(PrisonLayoutSpec.ProBuilderDraftRootName);
        if (draftRoot == null)
        {
            Debug.LogError("[PrisonRebuild] No ProBuilderDraft root found.");
            return;
        }

        Transform bakedRoot = EnsureChild(buildRoot, PrisonLayoutSpec.BakedShellRootName);
        ClearChildren(bakedRoot);

        int baked = BakeProBuilderMeshes(draftRoot, bakedRoot);
        if (baked == 0)
        {
            Debug.LogWarning("[PrisonRebuild] No ProBuilderMesh found under ProBuilderDraft — create geometry with ProBuilder first.");
        }
        else
        {
            Debug.Log($"[PrisonRebuild] Baked {baked} ProBuilder object(s) to BakedShell.");
        }

        MarkDirty();
    }

    [MenuItem(MenuRoot + "3 — Wire Zones And Spawns")]
    public static void WireZonesAndSpawns()
    {
        PrisonLayoutRebuildRunner.WireGameplayIntegration();
        MarkDirty();
        Debug.Log("[PrisonRebuild] Zones/spawns wired (gameplay integration pass).");
    }

    [MenuItem(MenuRoot + "4 — Validate Layout (Report)")]
    public static void ValidateLayoutReport()
    {
        PrisonLayoutValidator.ValidationReport report = PrisonLayoutValidator.ValidateScene();
        var issues = new List<PrisonLayoutValidator.ValidationIssue>(report.Issues);
        PrisonLayoutValidator.ValidateBakedShell(issues);
        report = new PrisonLayoutValidator.ValidationReport { Issues = issues };

        var sb = new StringBuilder();
        sb.AppendLine($"[PrisonRebuild] Layout validation: {(report.Passed ? "PASS" : "FAIL")} " +
                      $"({report.ErrorCount} errors, {report.WarningCount} warnings)");
        foreach (PrisonLayoutValidator.ValidationIssue issue in report.Issues)
            sb.AppendLine($"  {(issue.IsError ? "ERROR" : "WARN")} [{issue.Code}] {issue.Message}");

        if (report.Passed)
            Debug.Log(sb.ToString());
        else
            Debug.LogError(sb.ToString());
    }

    [MenuItem(MenuRoot + "5 — Full Rebuild Pipeline")]
    public static void RunFullPipeline()
    {
        StripLegacyOuterShell();
        CreateProBuilderWorkspace();
        BakeProBuilderDraftToShell();
        WireZonesAndSpawns();
        ValidateLayoutReport();
    }

    private static int BakeProBuilderMeshes(Transform draftRoot, Transform bakedRoot)
    {
        int count = 0;
        foreach (ProBuilderMesh pb in draftRoot.GetComponentsInChildren<ProBuilderMesh>(true))
        {
            if (pb == null || pb.gameObject.name.StartsWith("Guide_")) continue;

            var bakedGo = new GameObject(pb.gameObject.name + "_Baked");
            bakedGo.transform.SetParent(bakedRoot, false);
            bakedGo.transform.SetPositionAndRotation(pb.transform.position, pb.transform.rotation);
            bakedGo.transform.localScale = pb.transform.lossyScale;

            // ProBuilder 6+: ToMesh(MeshTopology) rebuilds the MeshFilter; copy that mesh out.
            pb.ToMesh();
            pb.Refresh(RefreshMask.All);
            Mesh source = pb.GetComponent<MeshFilter>()?.sharedMesh;
            if (source == null)
            {
                Object.DestroyImmediate(bakedGo);
                continue;
            }

            Mesh mesh = Object.Instantiate(source);
            mesh.name = pb.gameObject.name + "_Baked";

            var filter = bakedGo.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            var renderer = bakedGo.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = pb.GetComponent<MeshRenderer>()?.sharedMaterials ?? new Material[0];

            var collider = bakedGo.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
            collider.convex = false;

            count++;
        }

        return count;
    }

    private static bool ShouldArchiveToLegacyShell(Transform t)
    {
        if (PrisonLayoutSpec.IsLegacyPerimeterName(t.name))
            return true;

        if (t.name == "LevelBuild" || t.name.StartsWith("LevelBuild_"))
            return true;

        // Outermost legacy shell segments shifted far outside inner prison bounds
        if (t.name.StartsWith("LeftPrisonWall") && t.position.x <= -90f) return true;
        if (t.name.StartsWith("RightPrisonWall") && t.position.x >= 40f) return true;

        return false;
    }

    private static bool IsProtectedFromStrip(Transform t)
    {
        Transform walk = t;
        while (walk != null)
        {
            string n = walk.name;
            if (n == PrisonLayoutSpec.LegacyShellRootName
                || n == PrisonLayoutSpec.JailCellsRootName
                || n == PrisonLayoutSpec.PrisonBuildRootName
                || n == "Cafeteria"
                || n == "Managers"
                || n == "GameManager")
                return true;
            walk = walk.parent;
        }

        return t.CompareTag("Player");
    }

    private static void ArchiveTransform(Transform t, Transform legacyRoot)
    {
        t.SetParent(legacyRoot, true);
        foreach (Renderer r in t.GetComponentsInChildren<Renderer>(true))
            r.enabled = false;
        foreach (Collider c in t.GetComponentsInChildren<Collider>(true))
            c.enabled = false;
        EditorUtility.SetDirty(t.gameObject);
    }

    private static void RestoreCellFloors()
    {
        int restored = 0;
        var jailCells = GameObject.Find(PrisonLayoutSpec.JailCellsRootName);
        if (jailCells == null) return;

        foreach (Collider col in jailCells.GetComponentsInChildren<Collider>(true))
        {
            if (col == null || col.isTrigger) continue;
            if (!IsFloorLike(col)) continue;
            if (col.enabled) continue;
            col.enabled = true;
            restored++;
        }

        if (restored > 0)
            Debug.Log($"[PrisonRebuild] Restored {restored} cell floor colliders.");
    }

    private static void DisableCellRoofColliders()
    {
        int disabled = 0;
        var jailCells = GameObject.Find(PrisonLayoutSpec.JailCellsRootName);
        if (jailCells == null) return;

        foreach (Collider col in jailCells.GetComponentsInChildren<Collider>(true))
        {
            if (col == null || col.isTrigger) continue;
            if (!IsRoofLike(col)) continue;
            col.enabled = false;
            disabled++;
        }

        if (disabled > 0)
            Debug.Log($"[PrisonRebuild] Disabled {disabled} cell roof colliders (prevents spawn snap to roof).");
    }

    private static bool IsFloorLike(Collider col)
    {
        string n = col.name.ToLowerInvariant();
        return n.Contains("floor") || (col.bounds.size.y <= 0.5f && col.bounds.max.y <= 2f);
    }

    private static bool IsRoofLike(Collider col)
    {
        string n = col.name.ToLowerInvariant();
        return n.Contains("roof") || n.Contains("ceiling") || n.Contains("deck")
               || col.bounds.min.y >= 5f;
    }

    private static void CreateRoomGuide(Transform parent, string name, Vector3 center, Vector3 size, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, true);
        go.transform.position = center + Vector3.up * (size.y * 0.5f);
        go.transform.localScale = size;

        var collider = go.GetComponent<Collider>();
        if (collider != null) Object.DestroyImmediate(collider);

        var renderer = go.GetComponent<Renderer>();
        if (renderer != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = color;
            mat.SetFloat("_Surface", 1f);
            renderer.sharedMaterial = mat;
        }
    }

    private static Transform EnsureRoot(string name)
    {
        var existing = GameObject.Find(name);
        if (existing != null) return existing.transform;
        return new GameObject(name).transform;
    }

    private static Transform EnsureChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null) return child;
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(parent.GetChild(i).gameObject);
    }

    private static string GetHierarchyPath(Transform t)
    {
        var names = new List<string>();
        while (t != null)
        {
            names.Add(t.name);
            t = t.parent;
        }

        names.Reverse();
        return string.Join("/", names);
    }

    private static void MarkDirty()
    {
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }
}
