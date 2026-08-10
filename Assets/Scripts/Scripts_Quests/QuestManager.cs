using System;
using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Header("현재 진행 중인 퀘스트 목록 (동시에 여러 개 가능)")]
    public List<QuestData> activeQuests = new List<QuestData>();

    [Header("완료된 퀘스트 기록 (중복 접수 방지용)")]
    public List<QuestData> completedQuests = new List<QuestData>();

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
        DontDestroyOnLoad(gameObject);
    }

    public bool IsQuestActive(QuestData quest)
    {
        return activeQuests.Contains(quest);
    }

    public bool IsQuestCompleted(QuestData quest)
    {
        return completedQuests.Contains(quest);
    }

    public void StartQuest(QuestData quest)
    {
        if (quest == null) return;

        if (activeQuests.Contains(quest))
        {
            Debug.Log("이미 진행 중인 퀘스트입니다: " + quest.questName);
            return;
        }

        if (completedQuests.Contains(quest))
        {
            Debug.Log("이미 완료한 퀘스트입니다: " + quest.questName);
            return;
        }

        activeQuests.Add(quest);
        Debug.Log("퀘스트 시작: " + quest.questName);
        OnQuestStarted?.Invoke(quest);
    }

    // 특정 퀘스트 하나에 대해서만 물품이 충분한지 확인
    public bool HasRequiredItems(QuestData quest, Inventory inventory)
    {
        if (quest == null || inventory == null) return false;

        foreach (var req in quest.requiredItems)
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

    // 정확히 어느 퀘스트를 완료할지 지정해서 처리 (자동으로 다른 퀘스트가 같이 끝나지 않음)
    public void CompleteQuest(QuestData quest, Inventory inventory)
    {
        if (quest == null || !activeQuests.Contains(quest)) return;

        foreach (var req in quest.requiredItems)
        {
            int removed = 0;
            for (int i = inventory.Items.Count - 1; i >= 0 && removed < req.quantity; i--)
            {
                if (inventory.Items[i] == req.item)
                {
                    inventory.RemoveItem(req.item);
                    removed++;
                }
            }
        }

        activeQuests.Remove(quest);
        completedQuests.Add(quest);

        Debug.Log(quest.questName + " 완료! 보상: " + quest.reward + " 골드");
        OnQuestCompleted?.Invoke(quest);

        // 연계 퀘스트가 있으면 자동으로 시작 (여러 개로 분기 가능)
        if (quest.unlocksQuests != null)
        {
            foreach (var next in quest.unlocksQuests)
            {
                StartQuest(next);
            }
        }
    }

    // 테스트 편의용: 진행/완료 기록 전체 초기화
    public void ResetAll()
    {
        activeQuests.Clear();
        completedQuests.Clear();
        Debug.Log("퀘스트 진행/완료 기록을 모두 초기화했습니다.");
    }
}
