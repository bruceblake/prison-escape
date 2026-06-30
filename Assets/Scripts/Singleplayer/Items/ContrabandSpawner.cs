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
        List<Transform> available = new List<Transform>(spawnPoints);

        for (int i = 0; i < itemsToSpawn; i++)
        {
            if (available.Count == 0) break;

            int index = Random.Range(0, available.Count);
            Transform point = available[index];
            available.RemoveAt(index);

            ItemType chosenType = possibleItems[Random.Range(0, possibleItems.Length)];

            // Cast the enum to an int to match the prefab array index
            GameObject item = Instantiate(itemPrefabs[(int)chosenType], point.position, Quaternion.identity);
            
            // Instead of BombPartPickup, we use the PickupItem script from the previous step
            item.GetComponent<PickupItem>().itemData = GetItemData(chosenType);
        }
    }

    private ItemData GetItemData(ItemType type)
    {
        foreach (ItemData data in Resources.LoadAll<ItemData>("ItemData"))
        {
            if (data.networkId == (ushort)type)
                return data;
        }
        return null;
    }
}