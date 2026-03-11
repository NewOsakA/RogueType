using System.Collections.Generic;
using UnityEngine;

public class ItemInventory : MonoBehaviour
{
    public static ItemInventory Instance;

    private Dictionary<UsableItemType, int> items =
        new Dictionary<UsableItemType, int>();

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public int GetCount(UsableItemType type)
    {
        return items.ContainsKey(type) ? items[type] : 0;
    }

    public bool CanAdd(ItemData data)
    {
        return GetCount(data.itemType) < data.maxCarry;
    }

    public void Add(ItemData data)
    {
        if (!items.ContainsKey(data.itemType))
            items[data.itemType] = 0;

        items[data.itemType]++;
    }

    public bool Consume(UsableItemType type)
    {
        if (!items.ContainsKey(type) || items[type] <= 0)
            return false;

        items[type]--;
        return true;
    }
}
