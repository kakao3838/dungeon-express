using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemCatalog", menuName = "Dungeon Express/Item Catalog")]
public class ItemCatalog : ScriptableObject
{
    public const string ResourcePath = "ItemCatalog";
    public List<ItemData> allItems = new List<ItemData>();

    public static ItemCatalog Load() => Resources.Load<ItemCatalog>(ResourcePath);

    public void ValidateIds()
    {
        if (allItems == null) throw new InvalidOperationException("ItemCatalog list is missing.");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (ItemData item in allItems)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.itemId))
                throw new InvalidOperationException("ItemCatalog contains an empty entry or item ID.");
            if (!ids.Add(item.itemId))
                throw new InvalidOperationException($"Duplicate item ID: {item.itemId}");
        }
    }

    public ItemData FindById(string itemId)
    {
        if (string.IsNullOrEmpty(itemId) || allItems == null) return null;
        foreach (ItemData item in allItems)
            if (item != null && item.itemId == itemId) return item;
        return null;
    }
}
