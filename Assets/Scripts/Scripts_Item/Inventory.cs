using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [Header("가방 설정")]
    public int maxSlots = 2; // 기획서 기준 1챕터 초기 가방 = 2칸

    private readonly List<ItemData> items = new List<ItemData>();

    // UI가 이 이벤트를 구독해서 슬롯 표시를 갱신하면 됨
    public event Action OnInventoryChanged;

    public IReadOnlyList<ItemData> Items => items;
    public bool IsFull => items.Count >= maxSlots;

    public bool AddItem(ItemData item)
    {
        if (item == null) return false;

        if (IsFull)
        {
            Debug.Log("가방이 가득 찼습니다.");
            return false;
        }

        items.Add(item);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool RemoveItem(ItemData item)
    {
        bool removed = items.Remove(item);
        if (removed)
        {
            OnInventoryChanged?.Invoke();
        }
        return removed;
    }

    public void ClearAll()
    {
        items.Clear();
        OnInventoryChanged?.Invoke();
    }
}
