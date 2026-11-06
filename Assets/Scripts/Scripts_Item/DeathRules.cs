using UnityEngine;

// 플레이어가 죽었을 때 가방 아이템을 잃는 규칙입니다. (기획서: 죽음 처리)
//  - 가방 칸 수의 4분의 1(최소 1)이 한 번에 잃을 수 있는 최대 개수
//  - 그 안에서 1개 ~ 최대 개수 사이를 랜덤으로 정함 (들고 있는 아이템이 그보다 적으면 있는 만큼만)
//    예: 4칸 가방 → 1개, 16칸 가방 → 1~4개
// 숫자를 바꾸고 싶으면 SlotsPerMaxLoss만 고치면 됩니다.
public static class DeathRules
{
    public const int SlotsPerMaxLoss = 4;

    public static int MaxLoss(int bagSlots)
    {
        return Mathf.Max(1, bagSlots / SlotsPerMaxLoss);
    }

    // 이번 죽음에서 잃을 아이템 개수 (아이템이 하나도 없으면 0)
    public static int RollLossCount(int bagSlots, int itemCount)
    {
        if (itemCount <= 0) return 0;
        int roll = Random.Range(1, MaxLoss(bagSlots) + 1);
        return Mathf.Min(roll, itemCount);
    }
}
