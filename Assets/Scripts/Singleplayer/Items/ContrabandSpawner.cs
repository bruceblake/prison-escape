using UnityEngine;
using System.Collections.Generic;

public class ContrabandSpawner : MonoBehaviour
{
    public List<Transform> spawnPoints;          
    public GameObject[] itemPrefabs;           
    public int itemsToSpawn = 6;                 

    public ItemType[] possibleItems;         

    private void Start()
    {
        SpawnItems();
    }

    private void SpawnItems()
    {
        if (spawnPoints == null || spawnPoints.Count == 0 || possibleItems == null || possibleItems.Length == 0 || itemPrefabs == null || itemPrefabs.Length == 0)
        {
            Debug.LogWarning($"[ContrabandSpawner] {name} is missing spawnPoints, possibleItems, or itemPrefabs; skipping spawn.");
            return;
        }

        List<Transform> available = new List<Transform>(spawnPoints);
        ItemData[] allItemData = Resources.LoadAll<ItemData>("ItemData");

        for (int i = 0; i < itemsToSpawn; i++)
        {
            if (available.Count == 0) break;

            int index = Random.Range(0, available.Count);
            Transform point = available[index];
            available.RemoveAt(index);

            if (point == null) continue;

            ItemType chosenType = possibleItems[Random.Range(0, possibleItems.Length)];

            // Cast the enum to an int to match the prefab array index
            int prefabIndex = (int)chosenType;
            if (prefabIndex < 0 || prefabIndex >= itemPrefabs.Length || itemPrefabs[prefabIndex] == null)
            {
                Debug.LogWarning($"[ContrabandSpawner] No prefab mapped for ItemType '{chosenType}' (index {prefabIndex}); skipping.");
                continue;
            }

            GameObject item = Instantiate(itemPrefabs[prefabIndex], point.position, Quaternion.identity);

            // Instead of BombPartPickup, we use the PickupItem script from the previous step
            PickupItem pickup = item.GetComponent<PickupItem>();
            if (pickup != null)
                pickup.itemData = GetItemData(chosenType, allItemData);
            else
                Debug.LogWarning($"[ContrabandSpawner] Prefab for ItemType '{chosenType}' has no PickupItem component.");
        }
    }

    private ItemData GetItemData(ItemType type, ItemData[] candidates)
    {
        if (candidates == null) return null;
        foreach (ItemData data in candidates)
        {
            if (data != null && data.networkId == (ushort)type)
                return data;
        }
        return null;
    }
}