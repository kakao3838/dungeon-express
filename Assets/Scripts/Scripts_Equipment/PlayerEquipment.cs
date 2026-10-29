using System;
using UnityEngine;

public class PlayerEquipment : MonoBehaviour
{
    [Header("시작 장비 (게임 시작 시 비어 있는 부위에만 장착)")]
    public EquipmentData[] startingEquipment;

    private EquipmentData[] equipped = new EquipmentData[4]; // EquipSlotType 개수만큼

    public event Action OnEquipmentChanged;

    void Start()
    {
        foreach (var item in startingEquipment)
        {
            if (item != null && GetEquipped(item.slotType) == null)
                GiveEquipment(item);
        }
    }

    public EquipmentData GetEquipped(EquipSlotType slot) => equipped[(int)slot];

    // 보물상자 등에서 장비를 획득했을 때 호출 — 해당 부위 장비를 교체
    public void GiveEquipment(EquipmentData item)
    {
        if (item == null) return;
        equipped[(int)item.slotType] = item;
        OnEquipmentChanged?.Invoke();
    }
}