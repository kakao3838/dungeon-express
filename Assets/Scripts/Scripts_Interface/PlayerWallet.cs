using System;
using UnityEngine;

// 플레이어가 가진 돈(골드)입니다. Player 오브젝트에 붙이세요.
// HUD가 OnGoldChanged를 구독해서 표시를 갱신합니다.
public class PlayerWallet : MonoBehaviour
{
    [SerializeField] private int gold = 0;

    public int Gold => gold;

    public event Action<int> OnGoldChanged;

    public void AddGold(int amount)
    {
        gold = Mathf.Max(0, gold + amount);
        OnGoldChanged?.Invoke(gold);
    }

    public bool TrySpend(int amount)
    {
        if (amount > gold) return false;
        gold -= amount;
        OnGoldChanged?.Invoke(gold);
        return true;
    }
}
