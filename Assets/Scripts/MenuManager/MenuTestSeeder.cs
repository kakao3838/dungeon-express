using System.Collections.Generic;
using UnityEngine;

// MenuTest 씬 전용. Player 태그 + Inventory + PlayerWallet이 붙은 오브젝트에 달아 두면
// 시작할 때 아이템/퀘스트/골드를 채워 태블릿 메뉴를 바로 확인할 수 있습니다.
// (타운/던전 씬에는 필요 없음 - 거기서는 실제 플레이어와 QuestManager 데이터가 표시됨)
[RequireComponent(typeof(Inventory), typeof(PlayerWallet))]
public class MenuTestSeeder : MonoBehaviour
{
    public List<ItemData> items = new List<ItemData>();
    public QuestData activeQuest;
    public int gold = 1234;

    void Start()
    {
        var inventory = GetComponent<Inventory>();
        GetComponent<PlayerWallet>().AddGold(gold);
        foreach (var item in items) inventory.AddItem(item);

        if (QuestManager.Instance != null && activeQuest != null)
            QuestManager.Instance.currentQuest = activeQuest;
    }
}
