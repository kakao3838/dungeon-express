using UnityEngine;

[CreateAssetMenu(fileName = "NewEquipment", menuName = "Dungeon Express/Equipment Data")]
public class EquipmentData : ScriptableObject
{
    [Header("기본 정보")]
    public string equipName;
    public EquipSlotType slotType;
    public Sprite icon;

    [Header("능력치")]
    public int attackPower; // 와이어프레임의 "공격력 10" — 무기 외엔 0으로 두거나 다른 의미로 사용
    public string magicEffect; // 와이어프레임의 "화염" — 지금은 텍스트로만 표시, 실제 효과는 나중에

    [Header("강화 단계 (코인)")]
    public UpgradeGrade grade = UpgradeGrade.Lowest;

    [Header("표시용")]
    [TextArea] public string description;

    public enum Rarity { Lowest, Common, Rare, Epic, Legendary }
    public Rarity rarity;

    public static string GetGradeLabel(UpgradeGrade g)
    {
        switch (g)
        {
            case UpgradeGrade.Lowest: return "최하급";
            case UpgradeGrade.Low: return "하급";
            case UpgradeGrade.Mid: return "중급";
            case UpgradeGrade.High: return "상급";
            case UpgradeGrade.Highest: return "최상급";
            default: return "";
        }
    }
}