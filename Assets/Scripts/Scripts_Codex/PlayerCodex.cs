using System;
using System.Collections.Generic;
using UnityEngine;

// 플레이어가 한 번이라도 가방에 넣어 본 배달 아이템을 기록한다. (Player에 붙임)
[RequireComponent(typeof(Inventory))]
public class PlayerCodex : MonoBehaviour
{
    private Inventory inventory;
    private readonly List<ItemData> discovered = new List<ItemData>(); // 얻은 순서대로

    public IReadOnlyList<ItemData> Discovered => discovered;
    public event Action OnCodexChanged;

    void Awake() => inventory = GetComponent<Inventory>();

    void OnEnable()
    {
        inventory.OnInventoryChanged += Scan;
        Scan();
    }

    void OnDisable() => inventory.OnInventoryChanged -= Scan;

    void Scan()
    {
        bool changed = false;
        foreach (var item in inventory.Items)
        {
            if (item != null && !discovered.Contains(item))
            {
                discovered.Add(item);
                changed = true;
            }
        }
        if (changed) OnCodexChanged?.Invoke();
    }
}