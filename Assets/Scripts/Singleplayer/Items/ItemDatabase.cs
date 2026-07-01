using System.Collections.Generic;
using UnityEngine;

public class ItemDatabase : MonoBehaviour
{
    // A classic Singleton so any script can access the database instantly
    public static ItemDatabase Singleton { get; private set; }

    [Header("Item Registry")]
    [Tooltip("Drag EVERY ItemData ScriptableObject in your game into this list.")]
    public List<ItemData> allItemsInGame = new List<ItemData>();

    // The fast-lookup dictionary
    private Dictionary<ushort, ItemData> itemRegistry = new Dictionary<ushort, ItemData>();

    private void Awake()
    {
        // Singleton setup
        if (Singleton != null && Singleton != this)
        {
            Destroy(gameObject);
            return;
        }
        Singleton = this;
        DontDestroyOnLoad(gameObject);

        BuildDatabase();
    }

    private void BuildDatabase()
    {
        itemRegistry.Clear();

        foreach (ItemData item in allItemsInGame)
        {
            if (item != null)
            {
                if (itemRegistry.ContainsKey(item.networkId))
                {
                    Debug.LogError($"[ItemDatabase] CRITICAL ERROR: Duplicate Network ID {item.networkId} found on {item.itemName}!");
                }
                else
                {
                    itemRegistry.Add(item.networkId, item);
                }
            }
        }
        Debug.Log($"[ItemDatabase] Successfully loaded {itemRegistry.Count} items into the network registry.");
    }

    // Pass in a network ID, get the ScriptableObject back
    public ItemData GetItemById(ushort id)
    {
        if (itemRegistry.TryGetValue(id, out ItemData item))
        {
            return item;
        }
        
        Debug.LogWarning($"[ItemDatabase] No item found for Network ID {id}!");
        return null;
    }
}