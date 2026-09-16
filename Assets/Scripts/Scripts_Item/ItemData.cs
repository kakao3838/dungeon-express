using UnityEngine;

public enum ItemType
{
    Normal,         // 기본 - 일반 배송 품목
    RocketDelivery, // 로켓배송 - 짧은 시간 내 배송 필요 (신선식품류)
    HeatSensitive,  // 고온x - 고온 환경 시 화재 위험에 취약 (양초류)
    WaterSensitive, // 수분x - 물에 젖으면 품질 저하 (편지류)
    Fragile         // 파손주의 - 충격 시 내용물 파손 위험 (유리병류)
}

[CreateAssetMenu(fileName = "NewItem", menuName = "Dungeon Express/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("기본 정보")]
    public string itemName;
    public ItemType itemType;

    [Header("품질 관련 (배달 방식에 따른 감점 산정 시 사용)")]
    [Tooltip("민감도")]
    public float sensitivity = 1f;

    [Header("표시용 (인벤토리 UI)")]
    public Sprite icon;
    [TextArea] public string description;
    [Range(0, 100)] public int quality;
    public bool isLocked;

    public enum Rarity { Common, Rare, Epic, Legendary }
    public Rarity rarity;
}
