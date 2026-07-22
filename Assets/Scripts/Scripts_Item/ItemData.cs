using UnityEngine;

public enum ItemType
{
    Normal,      // 기본 - 일반 배송 물품
    RocketDelivery, // 로켓배송 - 제한 시간 내 배송 필요 (신선식품류)
    HeatSensitive,  // 고온x - 고온 환경 및 화염 공격에 취약 (냉동류)
    WaterSensitive, // 물기x - 물에 노출 시 품질 저하 (종이류)
    Fragile      // 잘깨짐 - 낙하 및 강한 충격 시 품질 저하 (파손주의)
}

[CreateAssetMenu(fileName = "NewItem", menuName = "Dungeon Express/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("기본 정보")]
    public string itemName;
    public ItemType itemType;

    [Header("품질 관련 (계산 방식은 기획 확정 후 결정 예정)")]
    [Tooltip("미정")]
    public float sensitivity = 1f;

    [Header("비주얼 (나중에 실제 아이콘 생기면 교체)")]
    public Sprite icon;

    [Header("설명 (선택)")]
    [TextArea]
    public string description;
}