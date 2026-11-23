using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem; // [취소] Esc로 팝업 닫기
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

    [Header("의뢰 취소")] // [취소]
    public GameObject cancelButton;  // 상세 화면의 [의뢰 취소] 버튼
    public GameObject confirmPopup;  // "정말 취소할까요?" 팝업
    public TMP_Text confirmText;     // 팝업 안의 안내 문구

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
        // 진행 중인 메인 퀘스트가 없고 일반 퀘스트만 있으면 일반 탭으로 시작
        var manager = QuestManager.Instance;
        bool hasMain = false, hasRegular = false;
        if (manager != null)
        {
            foreach (var q in manager.activeQuests)
            {
                if (q.category == QuestCategory.Main) hasMain = true;
                else hasRegular = true;
            }
        }
        currentTab = !hasMain && hasRegular ? QuestTab.Regular : QuestTab.Main;
        selectedQuest = null;
        CloseConfirm(); // [취소] 탭을 다시 열 때 팝업이 남아 있지 않게
        Refresh();
    }

    // [취소] 팝업이 열려 있을 때 Esc는 태블릿을 닫지 않고 팝업만 닫음
    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null && confirmPopup != null && confirmPopup.activeSelf
            && keyboard.escapeKey.wasPressedThisFrame)
        {
            CloseConfirm();
            MenuManager.EscapeConsumed = true;
        }
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

        foreach (var quest in manager.activeQuests)
        {
            bool isMain = quest.category == QuestCategory.Main;
            if ((currentTab == QuestTab.Main) == isMain) result.Add(quest);
        }
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
        CloseConfirm(); // [취소] 다른 의뢰를 고르거나 탭을 바꾸면 팝업 닫기
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

        // [취소] 진행 중인 의뢰(완료 탭 제외)에는 모두 [의뢰 취소] 버튼을 보여줌
        if (cancelButton != null) cancelButton.SetActive(!done);
    }

    // [취소] [의뢰 취소] 버튼 → 확인 팝업 열기
    public void OnClickCancel()
    {
        if (selectedQuest == null || confirmPopup == null) return;

        confirmText.text = selectedQuest.questName
            + "\n받은 물품은 회수되고, 의뢰를 준 곳에서 다시 받을 수 있어요.";
        confirmPopup.SetActive(true);
    }

    // [취소] 팝업의 [취소하기] 버튼 → 실제로 취소
    public void OnConfirmCancel()
    {
        var manager = QuestManager.Instance;
        if (manager != null && selectedQuest != null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            Inventory inventory = player != null ? player.GetComponent<Inventory>() : null;
            manager.CancelQuest(selectedQuest, inventory);
        }

        selectedQuest = null;
        Refresh(); // 목록에서 사라지고 다음 의뢰(없으면 빈 화면)가 선택됨
    }

    // [취소] 팝업의 [돌아가기] 버튼
    public void CloseConfirm()
    {
        if (confirmPopup != null) confirmPopup.SetActive(false);
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