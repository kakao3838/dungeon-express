using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuestBoardUI : MonoBehaviour
{
    [Header("연결 - 패널")]
    public GameObject boardPanel;         // PC 화면 전체 패널
    public GameObject tabAcceptPanel;     // 의뢰접수 탭 내용 (실제 동작)
    public GameObject tabEquipmentPanel;  // 장비강화 탭 (준비 중 표시만)
    public GameObject tabBagPanel;        // 가방강화 탭 (준비 중 표시만)

    [Header("연결 - 탭 버튼")]
    public Button tabAcceptButton;
    public Button tabEquipmentButton;
    public Button tabBagButton;

    [Header("연결 - 의뢰 목록")]
    public Transform questListContainer;   // 퀘스트 이름 버튼들이 세로로 나열될 부모
    public Button questListItemPrefab;     // 퀘스트 하나짜리 버튼 프리팹 (자식에 Text 포함)

    [Header("연결 - 의뢰 상세")]
    public Text detailNameText;
    public Text detailItemsText;
    public Text detailLocationText;
    public Text detailRewardText;
    public Text detailDescriptionText;
    public Button acceptButton;

    [Header("이 PC에서 제공하는 퀘스트 목록 (일반/서브 퀘스트를 여기에 등록)")]
    public List<QuestData> availableQuests = new List<QuestData>();

    private QuestData selectedQuest;

    void Start()
    {
        if (boardPanel != null) boardPanel.SetActive(false);

        if (tabAcceptButton != null) tabAcceptButton.onClick.AddListener(() => ShowTab(0));
        if (tabEquipmentButton != null) tabEquipmentButton.onClick.AddListener(() => ShowTab(1));
        if (tabBagButton != null) tabBagButton.onClick.AddListener(() => ShowTab(2));
        if (acceptButton != null) acceptButton.onClick.AddListener(AcceptSelectedQuest);

        ClearDetail();
    }

    public void OpenBoard()
    {
        if (boardPanel != null) boardPanel.SetActive(true);
        ShowTab(0);
        RefreshQuestList();
    }

    public void CloseBoard()
    {
        if (boardPanel != null) boardPanel.SetActive(false);
    }

    void ShowTab(int index)
    {
        if (tabAcceptPanel != null) tabAcceptPanel.SetActive(index == 0);
        if (tabEquipmentPanel != null) tabEquipmentPanel.SetActive(index == 1);
        if (tabBagPanel != null) tabBagPanel.SetActive(index == 2);
    }

    void RefreshQuestList()
    {
        if (questListContainer == null || questListItemPrefab == null) return;

        foreach (Transform child in questListContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (var quest in availableQuests)
        {
            if (quest == null) continue;

            if (QuestManager.Instance != null &&
                (QuestManager.Instance.IsQuestActive(quest) || QuestManager.Instance.IsQuestCompleted(quest)))
            {
                continue; // 이미 진행 중이거나 완료한 건 목록에서 제외
            }

            Button item = Instantiate(questListItemPrefab, questListContainer);
            Text label = item.GetComponentInChildren<Text>();
            if (label != null) label.text = quest.questName;

            QuestData captured = quest; // 클로저 캡처 문제 방지
            item.onClick.AddListener(() => SelectQuest(captured));
        }
    }

    void SelectQuest(QuestData quest)
    {
        selectedQuest = quest;

        if (detailNameText != null) detailNameText.text = quest.questName;
        if (detailLocationText != null) detailLocationText.text = "의뢰 장소: " + quest.targetLocation;
        if (detailRewardText != null) detailRewardText.text = "의뢰 보수: " + quest.reward + " 골드";
        if (detailDescriptionText != null) detailDescriptionText.text = quest.description;

        if (detailItemsText != null)
        {
            string itemsStr = "의뢰 물품: ";
            foreach (var req in quest.requiredItems)
            {
                itemsStr += (req.item != null ? req.item.itemName : "?") + " x" + req.quantity + "  ";
            }
            detailItemsText.text = itemsStr;
        }
    }

    void AcceptSelectedQuest()
    {
        if (selectedQuest == null || QuestManager.Instance == null) return;

        QuestManager.Instance.StartQuest(selectedQuest);
        ClearDetail();
        RefreshQuestList();
    }

    void ClearDetail()
    {
        selectedQuest = null;
        if (detailNameText != null) detailNameText.text = "";
        if (detailItemsText != null) detailItemsText.text = "";
        if (detailLocationText != null) detailLocationText.text = "";
        if (detailRewardText != null) detailRewardText.text = "";
        if (detailDescriptionText != null) detailDescriptionText.text = "";
    }
}
