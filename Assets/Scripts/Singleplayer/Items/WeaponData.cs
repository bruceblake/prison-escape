using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Inventory/Weapon")]
public class WeaponData : ItemData
{
    [Header("Weapon Link")]
    [Tooltip("Index into WeaponController.allGuns - which gun GameObject to show when equipped")]
    public int gunIndex;

    private void OnEnable()
    {
        category = ItemCategory.Weapon;
    }
}
