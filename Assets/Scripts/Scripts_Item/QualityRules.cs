using UnityEngine;

// 물품 품질(%) 감소 규칙입니다. (기획서: 물품 품질 시스템)
//  - 플레이어의 하트가 1개 깎일 때마다 가방 안 모든 물품의 품질이 10%씩 감소
//  - 피해 원인에 맞는 특성의 물품은 추가로 10%씩 더 감소
//      낙하 → 충격 주의 / 불 → 고온 주의 / 물 → 수분 주의
//  - 가방 강화로 감소량을 줄일 수 있음 (Inventory.qualityLossMultiplier)
// 숫자를 바꾸고 싶으면 이 파일의 상수만 고치면 됩니다.
public static class QualityRules
{
    public const int LossPerHeart = 10;
    public const int ExtraLossPerHeart = 10;

    // 하트를 heartsLost개 잃었을 때 이 물품이 깎일 품질(%)
    public static int LossFor(ItemData item, int heartsLost, DamageCause cause)
    {
        if (item == null || heartsLost <= 0) return 0;

        int perHeart = LossPerHeart;
        if (IsSensitiveTo(item.itemType, cause)) perHeart += Mathf.RoundToInt(ExtraLossPerHeart * Mathf.Max(0f, item.sensitivity));
        return perHeart * heartsLost;
    }

    public static bool IsSensitiveTo(ItemType type, DamageCause cause)
    {
        switch (cause)
        {
            case DamageCause.Fall: return type == ItemType.Fragile;
            case DamageCause.Fire: return type == ItemType.HeatSensitive;
            case DamageCause.Water: return type == ItemType.WaterSensitive;
            default: return false;
        }
    }
}
