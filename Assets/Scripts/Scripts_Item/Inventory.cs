using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public const int FullQuality = 100;

    [Header("가방 설정")]
    public int maxSlots = 2; // 기획서 기준 1챕터 초기 가방 = 2칸

    [Header("품질 감소 완화 (가방 강화용)")]
    [Tooltip("품질이 깎일 때 곱해지는 배율. 1이면 그대로, 0.5면 절반만 깎임. 가방 슬롯 강화로 줄일 수 있게 열어 둠")]
    [Range(0f, 1f)] public float qualityLossMultiplier = 1f;

    private readonly List<ItemData> items = new List<ItemData>();
    // items와 같은 순서로 아이템 하나하나의 품질(%)을 들고 있음 (같은 종류의 물품도 품질이 서로 다를 수 있음)
    private readonly List<int> qualities = new List<int>();

    // UI가 이 이벤트를 구독해서 슬롯 표시를 갱신하면 됨 (아이템 추가/제거와 품질 변화 모두에서 호출됨)
    public event Action OnInventoryChanged;

    public IReadOnlyList<ItemData> Items => items;
    public bool IsFull => items.Count >= maxSlots;

    public int GetQuality(int index)
    {
        return index >= 0 && index < qualities.Count ? qualities[index] : FullQuality;
    }

    public bool AddItem(ItemData item, int quality = FullQuality)
    {
        if (item == null) return false;

        if (IsFull)
        {
            Debug.Log("가방이 가득 찼습니다.");
            return false;
        }

        items.Add(item);
        qualities.Add(Mathf.Clamp(quality, 0, FullQuality));
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool RemoveItem(ItemData item)
    {
        int index = items.IndexOf(item);
        if (index < 0) return false;

        int ignored;
        return RemoveAt(index, out ignored);
    }

    // 지정한 칸의 아이템을 빼고, 그 아이템의 품질을 돌려줌 (배송 완료 때 실제 품질로 정산하기 위함)
    public bool RemoveAt(int index, out int quality)
    {
        quality = FullQuality;
        if (index < 0 || index >= items.Count) return false;

        quality = qualities[index];
        items.RemoveAt(index);
        qualities.RemoveAt(index);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public void ClearAll()
    {
        items.Clear();
        qualities.Clear();
        OnInventoryChanged?.Invoke();
    }

    // 가방 안 모든 물품의 품질을 깎음. lossFor는 아이템마다 깎을 %를 정해 줌 (가방 완화 배율이 곱해짐)
    public void ReduceQuality(Func<ItemData, int> lossFor)
    {
        bool changed = false;
        for (int i = 0; i < items.Count; i++)
        {
            int loss = Mathf.RoundToInt(lossFor(items[i]) * qualityLossMultiplier);
            if (loss <= 0) continue;

            int before = qualities[i];
            qualities[i] = Mathf.Max(0, before - loss);
            if (qualities[i] != before) changed = true;
        }
        if (changed) OnInventoryChanged?.Invoke();
    }
}
