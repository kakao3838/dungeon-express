using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 의뢰 탭 목록의 퀘스트 한 줄
public class QuestEntryUI : MonoBehaviour
{
    public TMP_Text badgeText;
    public TMP_Text nameText;
    public GameObject selectedFrame;

    private QuestData quest;
    private QuestPanelController panelController;

    public QuestData Quest => quest;

    public void Setup(QuestData data, QuestPanelController controller)
    {
        quest = data;
        panelController = controller;
        badgeText.text = QuestPanelController.GetCategoryName(data.category);
        nameText.text = data.questName;
        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        selectedFrame.SetActive(selected);
    }

    public void OnClick()
    {
        panelController.Select(this);
    }
}
