using System.Collections.Generic;
using Prison;
using UnityEngine;

/// <summary>
/// Shared layout validation helpers for editor runners and EditMode tests.
/// </summary>
public static class PrisonLayoutValidator
{
    public struct ValidationIssue
    {
        public string Code;
        public string Message;
        public bool IsError;
    }

    public struct ValidationReport
    {
        public List<ValidationIssue> Issues;

        public int ErrorCount
        {
            get
            {
                int count = 0;
                foreach (ValidationIssue issue in Issues)
                {
                    if (issue.IsError) count++;
                }

                return count;
            }
        }

        public int WarningCount => Issues.Count - ErrorCount;
        public bool Passed => ErrorCount == 0;
    }

    public static ValidationReport ValidateScene()
    {
        var issues = new List<ValidationIssue>();

        ValidateSpecConstants(issues);
        ValidateRequiredRoots(issues);
        ValidateJailCells(issues);
        ValidateLegacyShellArchived(issues);
        ValidateSpawnFloorHeights(issues);
        ValidateRegistryZones(issues);

        return new ValidationReport { Issues = issues };
    }

    public static void ValidateSpecConstants(List<ValidationIssue> issues)
    {
        if (PrisonLayoutAnchors.WallHeight != PrisonLayoutSpec.StandardWallHeight)
        {
            issues.Add(Error("SPEC_WALL_HEIGHT",
                $"PrisonLayoutAnchors.WallHeight ({PrisonLayoutAnchors.WallHeight}) != spec ({PrisonLayoutSpec.StandardWallHeight})."));
        }

        if (PrisonLayoutAnchors.FloorY < 0.5f || PrisonLayoutAnchors.FloorY > 1.5f)
        {
            issues.Add(Warn("SPEC_FLOOR_Y",
                $"PrisonLayoutAnchors.FloorY ({PrisonLayoutAnchors.FloorY}) looks unusual."));
        }
    }

    public static void ValidateRequiredRoots(List<ValidationIssue> issues)
    {
        if (GameObject.Find(PrisonLayoutSpec.JailCellsRootName) == null)
        {
            issues.Add(Error("MISSING_JAIL_CELLS",
                $"Scene is missing '{PrisonLayoutSpec.JailCellsRootName}'."));
        }
    }

    public static void ValidateJailCells(List<ValidationIssue> issues)
    {
        var jailCells = GameObject.Find(PrisonLayoutSpec.JailCellsRootName);
        if (jailCells == null) return;

        int cellCount = jailCells.transform.childCount;
        if (cellCount != PrisonLayoutSpec.ExpectedCellCount)
        {
            issues.Add(Error("CELL_COUNT",
                $"Expected {PrisonLayoutSpec.ExpectedCellCount} cells under JailCells, found {cellCount}."));
        }

        int spawnCount = 0;
        foreach (Transform cell in jailCells.transform)
        {
            Transform spawn = cell.Find("SpawnPoint");
            if (spawn != null) spawnCount++;
        }

        if (spawnCount != PrisonLayoutSpec.ExpectedCellCount)
        {
            issues.Add(Error("SPAWN_POINT_COUNT",
                $"Expected {PrisonLayoutSpec.ExpectedCellCount} SpawnPoint transforms, found {spawnCount}."));
        }
    }

    public static void ValidateLegacyShellArchived(List<ValidationIssue> issues)
    {
        var legacy = GameObject.Find(PrisonLayoutSpec.LegacyShellRootName);
        if (legacy == null)
        {
            issues.Add(Warn("NO_LEGACY_SHELL",
                $"'{PrisonLayoutSpec.LegacyShellRootName}' not found — run Strip Legacy Outer Shell if rebuilding."));
            return;
        }

        foreach (Renderer renderer in legacy.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer.enabled)
            {
                issues.Add(Warn("LEGACY_SHELL_VISIBLE",
                    $"Legacy shell renderer still enabled: {renderer.gameObject.name}."));
            }
        }
    }

    public static void ValidateSpawnFloorHeights(List<ValidationIssue> issues)
    {
        var jailCells = GameObject.Find(PrisonLayoutSpec.JailCellsRootName);
        if (jailCells == null) return;

        foreach (Transform cell in jailCells.transform)
        {
            Transform spawn = cell.Find("SpawnPoint");
            if (spawn == null) continue;

            float y = spawn.position.y;
            if (y < PrisonLayoutSpec.SpawnFloorMinY || y > PrisonLayoutSpec.SpawnFloorMaxY)
            {
                issues.Add(Error("SPAWN_HEIGHT",
                    $"{cell.name}/SpawnPoint Y={y:0.###} outside floor band [{PrisonLayoutSpec.SpawnFloorMinY}, {PrisonLayoutSpec.SpawnFloorMaxY}]."));
            }

            if (!RaycastHitsFloorNotRoof(spawn.position, out RaycastHit hit))
            {
                issues.Add(Error("SPAWN_RAYCAST",
                    $"{cell.name}/SpawnPoint raycast did not hit floor collider (hit={hit.collider?.name ?? "none"})."));
            }
        }
    }

    public static void ValidateRegistryZones(List<ValidationIssue> issues)
    {
        var registry = Object.FindAnyObjectByType<PrisonLocationRegistry>();
        if (registry == null)
        {
            issues.Add(Error("MISSING_REGISTRY", "PrisonLocationRegistry not found in scene."));
            return;
        }

        if (registry.yard == null)
            issues.Add(Error("ZONE_YARD", "PrisonLocationRegistry.yard is not wired."));
        if (registry.cafeteria == null)
            issues.Add(Error("ZONE_CAFETERIA", "PrisonLocationRegistry.cafeteria is not wired."));
    }

    public static void ValidateBakedShell(List<ValidationIssue> issues)
    {
        var buildRoot = GameObject.Find(PrisonLayoutSpec.PrisonBuildRootName);
        if (buildRoot == null)
        {
            issues.Add(Warn("NO_PRISON_BUILD", $"'{PrisonLayoutSpec.PrisonBuildRootName}' not created yet."));
            return;
        }

        Transform baked = buildRoot.transform.Find(PrisonLayoutSpec.BakedShellRootName);
        if (baked == null)
        {
            issues.Add(Warn("NO_BAKED_SHELL", "ProBuilder draft not baked yet."));
            return;
        }

        int colliderCount = baked.GetComponentsInChildren<Collider>(true).Length;
        if (colliderCount < 4)
        {
            issues.Add(Error("BAKED_SHELL_COLLIDERS",
                $"BakedShell has only {colliderCount} colliders (expected many room surfaces)."));
        }
    }

    public static bool RaycastHitsFloorNotRoof(Vector3 spawnPosition, out RaycastHit hit)
    {
        Vector3 origin = spawnPosition + Vector3.up * 8f;
        if (!Physics.Raycast(origin, Vector3.down, out hit, 12f))
            return false;

        if (hit.point.y >= PrisonLayoutSpec.RoofRaycastRejectMinY)
            return false;

        string name = hit.collider.name.ToLowerInvariant();
        if (name.Contains("roof") || name.Contains("ceiling") || name.Contains("deck"))
            return false;

        return hit.point.y >= PrisonLayoutSpec.SpawnFloorMinY && hit.point.y <= PrisonLayoutSpec.SpawnFloorMaxY + 0.5f;
    }

    private static ValidationIssue Error(string code, string message) =>
        new ValidationIssue { Code = code, Message = message, IsError = true };

    private static ValidationIssue Warn(string code, string message) =>
        new ValidationIssue { Code = code, Message = message, IsError = false };
}
