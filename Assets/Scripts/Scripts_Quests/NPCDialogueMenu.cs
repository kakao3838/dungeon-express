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
    [Tooltip("대화창 프리팹의 표시 컴포넌트. 비워두면 Dialogue Panel에서 찾습니다.")]
    [SerializeField] private DialogView dialogueView;

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
        dialogueView = DialogView.Install(dialoguePanel, speakerText, dialogueText, CloseDialogue);
        DialogView.Install(questDetailPanel, questSpeakerText, questDescriptionText, CloseQuestDetail, true);
        ResolveDialogueView();
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

        DialogView.Display(questDetailPanel, currentNPC.npcName, quest.description,
            questSpeakerText, questDescriptionText);

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
        ResolveDialogueView();
        if (dialogueView != null)
        {
            dialogueView.SetContent(currentNPC.npcName, currentNPC.talkLine);
        }
        else
        {
            // 아직 공통 프리팹으로 교체하지 않은 씬도 기존 연결로 동작합니다.
            if (speakerText != null) speakerText.text = currentNPC.npcName;
            if (dialogueText != null) dialogueText.text = currentNPC.talkLine;
        }
    }

    private void ResolveDialogueView()
    {
        if (dialogueView == null && dialoguePanel != null)
            dialogueView = dialoguePanel.GetComponent<DialogView>();
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
