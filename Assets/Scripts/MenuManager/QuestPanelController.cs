using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 태블릿 메뉴 '의뢰' 탭. QuestManager의 진행 중 / 완료 퀘스트를 QuestData 그대로 보여줍니다.
public class QuestPanelController : MonoBehaviour
{
    private enum QuestTab { Main, Regular, Completed }

    [Header("목록")]
    public Transform listParent;
    public QuestEntryUI entryPrefab;
    public GameObject emptyListText;

    [Header("탭 (0: 메인, 1: 일반, 2: 완료)")]
    public GameObject[] tabUnderlines;

    [Header("상세 정보")]
    public GameObject detailRoot;
    public TMP_Text categoryLabel;
    public TMP_Text nameText;
    public TMP_Text itemsText;
    public TMP_Text locationText;
    public TMP_Text rewardText;
    public TMP_Text descriptionText;
    public TMP_Text objectivesText;

    private readonly List<QuestEntryUI> entries = new List<QuestEntryUI>();
    private QuestTab currentTab = QuestTab.Main;
    private QuestData selectedQuest;

    public static string GetCategoryName(QuestCategory category)
    {
        switch (category)
        {
            case QuestCategory.Main: return "메인";
            case QuestCategory.Side: return "서브";
            default: return "일반";
        }
    }

    void OnEnable()
    {
        // 열 때마다 진행 중인 퀘스트가 있는 탭으로 이동 (없으면 메인)
        QuestData active = QuestManager.Instance != null ? QuestManager.Instance.currentQuest : null;
        currentTab = active != null && active.category != QuestCategory.Main ? QuestTab.Regular : QuestTab.Main;
        selectedQuest = null;
        Refresh();
    }

    public void SelectTab(int tab)
    {
        currentTab = (QuestTab)tab;
        selectedQuest = null;
        Refresh();
    }

    List<QuestData> CollectQuests()
    {
        var result = new List<QuestData>();
        var manager = QuestManager.Instance;
        if (manager == null) return result;

        if (currentTab == QuestTab.Completed)
        {
            result.AddRange(manager.CompletedQuests);
            return result;
        }

        QuestData active = manager.currentQuest;
        if (active == null) return result;

        bool isMain = active.category == QuestCategory.Main;
        if ((currentTab == QuestTab.Main) == isMain) result.Add(active);
        return result;
    }

    void Refresh()
    {
        List<QuestData> quests = CollectQuests();

        if (selectedQuest == null || !quests.Contains(selectedQuest))
            selectedQuest = quests.Count > 0 ? quests[0] : null;

        while (entries.Count < quests.Count)
            entries.Add(Instantiate(entryPrefab, listParent));

        for (int i = 0; i < entries.Count; i++)
        {
            bool used = i < quests.Count;
            entries[i].gameObject.SetActive(used);
            if (!used) continue;

            entries[i].Setup(quests[i], this);
            entries[i].SetSelected(quests[i] == selectedQuest);
        }

        for (int i = 0; i < tabUnderlines.Length; i++)
            tabUnderlines[i].SetActive(i == (int)currentTab);

        emptyListText.SetActive(quests.Count == 0);
        ShowDetail(selectedQuest);
    }

    public void Select(QuestEntryUI entry)
    {
        selectedQuest = entry.Quest;
        foreach (var e in entries) e.SetSelected(e == entry);
        ShowDetail(selectedQuest);
    }

    void ShowDetail(QuestData quest)
    {
        detailRoot.SetActive(quest != null);
        if (quest == null) return;

        categoryLabel.text = GetCategoryName(quest.category) + " 퀘스트";
        nameText.text = quest.questName;
        itemsText.text = FormatRequiredItems(quest);
        locationText.text = string.IsNullOrEmpty(quest.targetLocation) ? "-" : quest.targetLocation;
        rewardText.text = quest.reward.ToString("N0") + " 골드";
        descriptionText.text = quest.description;

        bool done = currentTab == QuestTab.Completed;
        var sb = new StringBuilder();
        foreach (string objective in quest.objectives)
        {
            if (sb.Length > 0) sb.Append('\n');
            sb.Append(done ? "■  " : "□  ").Append(objective);
        }
        objectivesText.text = sb.ToString();
    }

    static string FormatRequiredItems(QuestData quest)
    {
        if (quest.requiredItems == null || quest.requiredItems.Count == 0) return "-";

        var sb = new StringBuilder();
        foreach (var req in quest.requiredItems)
        {
            if (req.item == null) continue;
            if (sb.Length > 0) sb.Append(", ");
            sb.Append(req.item.itemName).Append(' ').Append(req.quantity).Append("개");
        }
        return sb.Length > 0 ? sb.ToString() : "-";
    }
}
