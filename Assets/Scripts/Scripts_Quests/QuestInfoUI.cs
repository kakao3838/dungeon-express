using UnityEngine;
using UnityEngine.UI;

// 좌상단 HUD에 현재 진행 중인 퀘스트 이름/목표를 표시합니다.
// QuestManager는 Town 씬에서 생성되는 씬 간 영속 싱글턴이라, 이 씬에 아직 없을 수 있으므로
// Update에서 나타날 때까지 기다렸다가 이벤트를 구독합니다.
public class QuestInfoUI : MonoBehaviour
{
    [Header("연결")]
    public Text questNameText;
    public Text questSummaryText;

    private QuestManager subscribedManager;

    void Update()
    {
        if (subscribedManager == null && QuestManager.Instance != null)
        {
            subscribedManager = QuestManager.Instance;
            subscribedManager.OnQuestStarted += HandleQuestChanged;
            subscribedManager.OnQuestCompleted += HandleQuestChanged;
            Refresh();
        }
    }

    void OnDisable()
    {
        if (subscribedManager != null)
        {
            subscribedManager.OnQuestStarted -= HandleQuestChanged;
            subscribedManager.OnQuestCompleted -= HandleQuestChanged;
            subscribedManager = null;
        }
    }

    void HandleQuestChanged(QuestData quest)
    {
        Refresh();
    }

    void Refresh()
    {
        QuestData current = null;
        if (subscribedManager != null && subscribedManager.activeQuests.Count > 0)
        {
            current = subscribedManager.activeQuests[subscribedManager.activeQuests.Count - 1];
        }

        if (current == null)
        {
            if (questNameText != null) questNameText.text = "";
            if (questSummaryText != null) questSummaryText.text = "";
            return;
        }

        if (questNameText != null) questNameText.text = current.questName;
        if (questSummaryText != null) questSummaryText.text = string.Join("\n", current.objectives);
    }
}
