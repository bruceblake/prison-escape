using UnityEngine;

/// <summary>
/// World-space anchors for the enclosed PrisonLevel1 layout.
/// Cells occupy roughly X [-58, 6], Z [-28, 48], floor Y ≈ 0.72.
/// </summary>
public static class PrisonLayoutAnchors
{
    public const float FloorY = 0.72f;
    public const float CorridorY = 0.6f;
    public const float WallHeight = 3.5f;

    public static Vector3 CellBlockCenter => new Vector3(-26f, 0.5f, 10f);
    public static Vector3 MainCorridorCenter => new Vector3(-26f, CorridorY, -45f);
    public static Vector3 CellSouthConnectorCenter => new Vector3(-26f, CorridorY, -30f);
    public static Vector3 CafeteriaConnectorCenter => new Vector3(-26f, CorridorY, -58f);
    public static Vector3 WorkshopCenter => new Vector3(-88f, CorridorY, -42f);
    public static Vector3 LaundryCenter => new Vector3(48f, CorridorY, -42f);
    public static Vector3 CafeteriaCenter => new Vector3(-16f, CorridorY, -62f);
    public static Vector3 RollCallCenter => new Vector3(-26f, 0.5f, 55f);
    public static Vector3 YardCenter => new Vector3(-26f, 0f, 78f);
    public static Vector3 YardGateCenter => new Vector3(-26f, 1f, 66f);

    public const float InnerLeftWallX = -52f;
    public const float InnerRightWallX = 0f;
}
