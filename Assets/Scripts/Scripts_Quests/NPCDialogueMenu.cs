using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NPCDialogueMenu : MonoBehaviour
{
    public static NPCDialogueMenu Instance { get; private set; }

    [Header("연결 - 선택지 메뉴")]
    public GameObject menuPanel;
    public TMP_Text npcNameText;

    [Header("연결 - 대화")]
    public GameObject dialoguePanel;
    public TMP_Text speakerText;
    public TMP_Text dialogueText;

    [Header("연결 - 의뢰 물품 카드")]
    public GameObject questDetailPanel;
    public TMP_Text questSpeakerText;
    public TMP_Text questDescriptionText;
    public Image itemCardIcon;
    public TMP_Text itemNameText;
    public TMP_Text itemTraitText;

    private QuestGiverTrigger currentNPC;

    public bool IsOpen => (menuPanel != null && menuPanel.activeSelf)
        || (dialoguePanel != null && dialoguePanel.activeSelf)
        || (questDetailPanel != null && questDetailPanel.activeSelf);

    void Awake()
    {
        Instance = this;
        Close();
    }

    public void Open(QuestGiverTrigger npc)
    {
        currentNPC = npc;
        if (npcNameText != null) npcNameText.text = npc.npcName;
        if (menuPanel != null) menuPanel.SetActive(true);
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
    }

    public void Close()
    {
        currentNPC = null;
        if (menuPanel != null) menuPanel.SetActive(false);
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (questDetailPanel != null) questDetailPanel.SetActive(false);
    }

    // "1. 메인 퀘스트" 선택
    public void OnClickMainQuest()
    {
        if (currentNPC == null) return;

        QuestData quest = currentNPC.questToGive;
        currentNPC.GiveMainQuest();

        if (quest != null)
        {
            ShowQuestDetail(quest);
        }
        else
        {
            Close();
        }
    }

    // 퀘스트 수락 시 의뢰 물품 카드 표시
    void ShowQuestDetail(QuestData quest)
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (questDetailPanel != null) questDetailPanel.SetActive(true);

        if (questSpeakerText != null) questSpeakerText.text = currentNPC.npcName;
        if (questDescriptionText != null) questDescriptionText.text = quest.description;

        ItemData firstItem = null;
        if (quest.requiredItems != null && quest.requiredItems.Count > 0)
        {
            firstItem = quest.requiredItems[0].item;
        }

        if (itemNameText != null) itemNameText.text = firstItem != null ? firstItem.itemName : "";
        if (itemTraitText != null) itemTraitText.text = firstItem != null ? ItemData.GetTraitLabel(firstItem.itemType) : "";
        if (itemCardIcon != null)
        {
            Sprite icon = firstItem != null ? firstItem.icon : null;
            itemCardIcon.sprite = icon;
            itemCardIcon.enabled = icon != null;
        }
    }

    // 의뢰 물품 카드에서 확인/닫기
    public void CloseQuestDetail()
    {
        Close();
    }

    // "2. 대화" 선택
    public void OnClickTalk()
    {
        if (currentNPC == null) return;
        if (menuPanel != null) menuPanel.SetActive(false);
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        if (speakerText != null) speakerText.text = currentNPC.npcName;
        if (dialogueText != null) dialogueText.text = currentNPC.talkLine;
    }

    // "3. 나가기" 선택
    public void OnClickExit()
    {
        Close();
    }

    // 대화창에서 뒤로가기
    public void CloseDialogue()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (menuPanel != null) menuPanel.SetActive(true);
    }
}
