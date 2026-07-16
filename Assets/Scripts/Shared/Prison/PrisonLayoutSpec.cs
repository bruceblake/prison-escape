using UnityEngine;

/// <summary>
/// Single source of truth for prison layout dimensions and counts.
/// Used by editor rebuild runners and EditMode validation tests.
/// </summary>
public static class PrisonLayoutSpec
{
    public const int ExpectedCellCount = 8;
    public const float FloorY = 0.72f;
    public const float SpawnFloorMinY = 0.5f;
    public const float SpawnFloorMaxY = 1.2f;
    public const float RoofRaycastRejectMinY = 5f;
    public const float MinCorridorWidth = 2.5f;
    public const float StandardWallHeight = 3.5f;
    public const float StandardWallThickness = 0.25f;
    public const float StandardDoorWidth = 2f;
    public const float StandardDoorHeight = 2.5f;

    public const string LegacyShellRootName = "_LegacyShell";
    public const string PrisonBuildRootName = "PrisonBuild";
    public const string ProBuilderDraftRootName = "ProBuilderDraft";
    public const string BakedShellRootName = "BakedShell";
    public const string JailCellsRootName = "JailCells";
    public const string OldLayoutRootName = "PrisonLayout";

    public static readonly string[] LegacyPerimeterNamePrefixes =
    {
        "LeftPrisonWall",
        "RightPrisonWall",
        "NorthPrisonWall",
        "SouthPrisonWall",
        "OuterWall",
        "PerimeterFence",
        "PerimeterWall"
    };

    public static readonly string[] RequiredZoneObjectNames =
    {
        "YardZone",
        "CafeteriaZone"
    };

    public static Bounds CellBlockBounds => new Bounds(
        PrisonLayoutAnchors.CellBlockCenter,
        new Vector3(58f, 8f, 76f));

    public static bool IsLegacyPerimeterName(string objectName)
    {
        if (string.IsNullOrEmpty(objectName)) return false;
        foreach (string prefix in LegacyPerimeterNamePrefixes)
        {
            if (objectName.StartsWith(prefix))
                return true;
        }

        return false;
    }
}
