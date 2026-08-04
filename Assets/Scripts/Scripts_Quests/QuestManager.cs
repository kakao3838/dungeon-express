using System;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Header("현재 진행 중인 퀘스트 (테스트용으로 직접 연결 가능)")]
    public QuestData currentQuest;

    public event Action<QuestData> OnQuestStarted;
    public event Action<QuestData> OnQuestCompleted;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Town → Dungeon 씬 전환에도 퀘스트 상태 유지
    }

    public void StartQuest(QuestData quest)
    {
        currentQuest = quest;
        Debug.Log("퀘스트 시작: " + quest.questName);
        OnQuestStarted?.Invoke(quest);
    }

    // 인벤토리가 현재 퀘스트에 필요한 물품을 다 갖고 있는지 확인
    public bool HasRequiredItems(Inventory inventory)
    {
        if (currentQuest == null || inventory == null) return false;

        foreach (var req in currentQuest.requiredItems)
        {
            int count = 0;
            foreach (var item in inventory.Items)
            {
                if (item == req.item) count++;
            }
            if (count < req.quantity) return false;
        }
        return true;
    }

    // 퀘스트 완료 처리: 물품 제거, 보상 지급(로그), 다음 퀘스트로 전환
    public void CompleteCurrentQuest(Inventory inventory)
    {
        if (currentQuest == null) return;

        foreach (var req in currentQuest.requiredItems)
        {
            int removed = 0;
            // 리스트를 뒤에서부터 지우면서 필요한 개수만큼 제거
            for (int i = inventory.Items.Count - 1; i >= 0 && removed < req.quantity; i--)
            {
                if (inventory.Items[i] == req.item)
                {
                    inventory.RemoveItem(req.item);
                    removed++;
                }
            }
        }

        Debug.Log(currentQuest.questName + " 완료! 보상: " + currentQuest.reward + " 골드");
        OnQuestCompleted?.Invoke(currentQuest);

        // 다음 퀘스트로 전환 (분기가 있으면 첫 번째만 자동 시작, 나머지는 나중에 수동 처리)
        if (currentQuest.unlocksQuests != null && currentQuest.unlocksQuests.Count > 0)
        {
            StartQuest(currentQuest.unlocksQuests[0]);
        }
        else
        {
            currentQuest = null;
        }
    }
}
