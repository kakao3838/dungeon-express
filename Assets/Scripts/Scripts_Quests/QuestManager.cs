using System;
using System.Collections.Generic;
using UnityEngine;

// 퀘스트 진행 상태를 들고 있는 씬 간 영속 싱글턴입니다.
// 진행 중인 퀘스트는 여러 개를 동시에 가질 수 있고(메인 + 서브), 완료한 퀘스트와 이야기 플래그도 여기서 관리합니다.
// 나중에 저장 기능이 이 값들을 questId(문자열) 기준으로 읽고 쓰므로, 상태는 QuestData 에셋이 아니라 이 안에만 둡니다.
public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    // 해금/이야기 플래그 이름 (SetFlag / HasFlag)
    public const string FlagEquipmentUpgrade = "unlock_equipment_upgrade";
    public const string FlagBagUpgrade = "unlock_bag_upgrade";
    public const string FlagTutorialDone = "tutorial_done";

    [Header("진행 중인 퀘스트 (테스트용으로 직접 연결 가능, 동시에 여러 개 가능)")]
    public List<QuestData> activeQuests = new List<QuestData>();

    // 완료한 퀘스트 기록 (태블릿 메뉴 '의뢰' 탭의 완료 목록, 중복 접수 방지에 사용)
    private readonly List<QuestData> completedQuests = new List<QuestData>();
    public IReadOnlyList<QuestData> CompletedQuests => completedQuests;

    // 이야기 진행/기능 해금 기록 (예: "cave_collapsed", FlagEquipmentUpgrade)
    private readonly HashSet<string> flags = new HashSet<string>();
    public IReadOnlyCollection<string> Flags => flags;

    // 배송 완료 때 낸 물품들의 평균 품질 (퀘스트 ID별, 의뢰 물품 순서대로). 배송 완료 창이 정산에 씀
    private readonly Dictionary<string, int[]> deliveredQualities = new Dictionary<string, int[]>();

    public event Action<QuestData> OnQuestStarted;
    public event Action<QuestData> OnQuestCompleted;
    public event Action<string> OnFlagChanged;
    public event Action OnStateRestored;

    // Do not use StartQuest/CompleteQuest here: they grant items and trigger story events.
    public void RestoreSaveState(List<QuestData> active, List<QuestData> finished,
        List<string> restoredFlags, Dictionary<string, int[]> qualityRecords)
    {
        activeQuests.Clear();
        activeQuests.AddRange(active);
        completedQuests.Clear();
        completedQuests.AddRange(finished);
        flags.Clear();
        foreach (string flag in restoredFlags) flags.Add(flag);
        deliveredQualities.Clear();
        foreach (var pair in qualityRecords)
            deliveredQualities.Add(pair.Key, (int[])pair.Value.Clone());
    }

    public void NotifySaveRestored() => OnStateRestored?.Invoke();

    // HUD처럼 하나만 보여줘야 하는 곳에서 쓰는 추적 대상: 가장 최근에 시작한 진행 중 퀘스트
    public QuestData TrackedQuest => activeQuests.Count > 0 ? activeQuests[activeQuests.Count - 1] : null;

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

    public bool IsQuestActive(QuestData quest)
    {
        return quest != null && activeQuests.Contains(quest);
    }

    public bool IsQuestCompleted(QuestData quest)
    {
        return quest != null && completedQuests.Contains(quest);
    }

    // 퀘스트를 시작합니다. 이미 진행 중이거나 완료한 퀘스트면 false
    public bool StartQuest(QuestData quest, Inventory inventory = null)
    {
        if (quest == null) return false;

        if (IsQuestActive(quest) || IsQuestCompleted(quest))
        {
            Debug.Log("이미 진행 중이거나 완료한 퀘스트입니다: " + quest.questName);
            return false;
        }

        activeQuests.Add(quest);
        Debug.Log("퀘스트 시작: " + quest.questName);

        // 배달할 물품을 미리 가방에 넣어줌 (플레이어가 직접 구할 방법이 없는 배달용 아이템)
        if (inventory != null && quest.giveItemsOnStart)
        {
            foreach (var req in quest.requiredItems)
            {
                for (int i = 0; i < req.quantity; i++)
                {
                    inventory.AddItem(req.item);
                }
            }
        }

        OnQuestStarted?.Invoke(quest);
        return true;
    }

    // 인벤토리가 이 퀘스트에 필요한 물품을 다 갖고 있는지 확인
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

    // 지정한 퀘스트를 완료 처리: 물품 제거, 완료 기록, 이어지는 퀘스트 시작
    // (퀘스트를 직접 지정하므로 다른 진행 중 퀘스트가 같이 끝나는 일이 없음)
    public void CompleteQuest(QuestData quest, Inventory inventory)
    {
        if (!IsQuestActive(quest)) return;

        var delivered = new int[quest.requiredItems.Count];
        for (int r = 0; r < delivered.Length; r++) delivered[r] = Inventory.FullQuality;

        if (inventory != null)
        {
            for (int r = 0; r < quest.requiredItems.Count; r++)
            {
                var req = quest.requiredItems[r];
                int removed = 0;
                int qualitySum = 0;
                // 리스트를 뒤에서부터 지우면서 필요한 개수만큼 제거하고, 낸 물품들의 품질을 모음
                for (int i = inventory.Items.Count - 1; i >= 0 && removed < req.quantity; i--)
                {
                    if (inventory.Items[i] != req.item) continue;

                    int quality;
                    inventory.RemoveAt(i, out quality);
                    qualitySum += quality;
                    removed++;
                }
                if (removed > 0) delivered[r] = Mathf.RoundToInt(qualitySum / (float)removed);
            }
        }
        deliveredQualities[QuestKey(quest)] = delivered;

        // 보상 지급은 DeliveryCompleteWindow가 PlayerWallet에 하므로 여기서는 완료 기록만 남김
        activeQuests.Remove(quest);
        completedQuests.Add(quest);

        Debug.Log(quest.questName + " 완료! 보상: " + quest.reward + " 골드");
        OnQuestCompleted?.Invoke(quest);

        // 이어지는 퀘스트가 있으면 모두 시작 (한 퀘스트가 여러 퀘스트를 동시에 열 수 있음)
        if (quest.unlocksQuests != null)
        {
            foreach (var next in quest.unlocksQuests) StartQuest(next, inventory);
        }
    }

    // 배송 완료한 퀘스트의 r번째 의뢰 물품 평균 품질 (기록이 없으면 fallback)
    public int GetDeliveredQuality(QuestData quest, int requirementIndex, int fallback)
    {
        int[] delivered;
        if (quest == null || !deliveredQualities.TryGetValue(QuestKey(quest), out delivered)) return fallback;
        return requirementIndex >= 0 && requirementIndex < delivered.Length ? delivered[requirementIndex] : fallback;
    }

    // 배송 완료 창이 퀘스트 복사본(보수만 바꾼 것)을 받아도 같은 기록을 찾을 수 있게 ID로 구분
    static string QuestKey(QuestData quest)
    {
        return string.IsNullOrEmpty(quest.questId) ? quest.questName : quest.questId;
    }

    // ---------- 플래그 ----------

    public bool HasFlag(string flag)
    {
        return !string.IsNullOrEmpty(flag) && flags.Contains(flag);
    }

    public void SetFlag(string flag, bool on = true)
    {
        if (string.IsNullOrEmpty(flag)) return;
        bool changed = on ? flags.Add(flag) : flags.Remove(flag);
        if (changed) OnFlagChanged?.Invoke(flag);
    }
}
