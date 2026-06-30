using UnityEngine;

[CreateAssetMenu(fileName = "New Tool", menuName = "Inventory/Usable Tool")]
public class ToolData : ItemData
{
    [Header("Tool Stats")]
    public int durability = 100;
    public float interactionSpeedModifier = 1.5f;

    [Header("Held Visual")]
    [Tooltip("Prefab (GameObject) to show in player hands when this tool is equipped")]
    public UnityEngine.Object holdModelPrefab;
    [Tooltip("Position offset from hold point (in local space)")]
    public Vector3 holdPositionOffset;
    [Tooltip("Rotation offset in euler angles (degrees)")]
    public Vector3 holdRotationOffset;
    [Tooltip("Scale of the held model")]
    public Vector3 holdScale = Vector3.one;

    private void OnEnable()
    {
        category = ItemCategory.Tool;
    }
}