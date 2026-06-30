using UnityEngine;

public class HeldItemDisplay : MonoBehaviour
{
    public PlayerInventory inventory;
    public Transform holdPoint;

    private GameObject spawnedModel;

    void Start()
    {
        if (inventory == null)
            inventory = GetComponentInParent<PlayerInventory>();
        if (holdPoint == null)
            holdPoint = transform;
    }

    void Update()
    {
        if (inventory == null) return;

        ItemData equipped = inventory.GetEquippedItem();

        if (equipped is WeaponData)
        {
            if (spawnedModel != null) spawnedModel.SetActive(false);
            return;
        }

        if (equipped is ToolData toolData && toolData.holdModelPrefab is GameObject prefab)
        {
            if (spawnedModel == null || spawnedModel.name != prefab.name + "(Clone)")
            {
                if (spawnedModel != null) Destroy(spawnedModel);
                spawnedModel = Instantiate(prefab, holdPoint);
            }
            if (spawnedModel != null)
            {
                spawnedModel.transform.localPosition = toolData.holdPositionOffset;
                spawnedModel.transform.localRotation = Quaternion.Euler(toolData.holdRotationOffset);
                spawnedModel.transform.localScale = toolData.holdScale;
                if (!spawnedModel.activeSelf)
                    spawnedModel.SetActive(true);
            }
        }
        else
        {
            if (spawnedModel != null)
                spawnedModel.SetActive(false);
        }
    }
}
