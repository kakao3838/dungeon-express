using UnityEngine;
using UnityEngine.UI;

public class BossDialogueUI : MonoBehaviour
{
    [Header("연결 - 패널")]
    public GameObject dialoguePanel; // 전체 대화창
    public GameObject choicePanel;   // "1.메인퀘스트 2.대화 3.나가기" 선택지 화면
    public GameObject talkPanel;     // 잡담 대사 표시 화면

    [Header("연결 - 선택지 버튼")]
    public Button mainQuestButton;
    public Text mainQuestButtonLabel;
    public Button talkButton;
    public Button exitButton;

    [Header("연결 - 잡담 화면")]
    public Text talkText;
    public Button talkBackButton; // 잡담 보고 다시 선택지로 돌아가는 버튼

    [Header("이 사장님이 줄 메인 퀘스트")]
    public QuestData mainQuest;

    [Header("평범한 잡담 대사 (여러 개면 랜덤으로 하나 표시)")]
    public string[] talkLines = new string[] { "ㅎㅇ 나 사장" };

    private Inventory playerInventory;

    void Start()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);

        if (mainQuestButton != null) mainQuestButton.onClick.AddListener(OnClickMainQuest);
        if (talkButton != null) talkButton.onClick.AddListener(OnClickTalk);
        if (exitButton != null) exitButton.onClick.AddListener(CloseDialogue);
        if (talkBackButton != null) talkBackButton.onClick.AddListener(ShowChoices);
    }

    public void OpenDialogue()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerInventory = player.GetComponent<Inventory>();

        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        ShowChoices();
    }

    public void CloseDialogue()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
    }

    void ShowChoices()
    {
        if (choicePanel != null) choicePanel.SetActive(true);
        if (talkPanel != null) talkPanel.SetActive(false);

        UpdateMainQuestButtonState();
    }

    void UpdateMainQuestButtonState()
    {
        if (mainQuestButton == null || mainQuest == null) return;

        bool alreadyHandled = QuestManager.Instance != null &&
            (QuestManager.Instance.IsQuestActive(mainQuest) || QuestManager.Instance.IsQuestCompleted(mainQuest));

        mainQuestButton.interactable = !alreadyHandled;

        if (mainQuestButtonLabel != null)
        {
            mainQuestButtonLabel.text = alreadyHandled ? "1. 메인 퀘스트 (진행 중/완료)" : "1. 메인 퀘스트";
        }
    }

    void OnClickMainQuest()
    {
        if (mainQuest == null || QuestManager.Instance == null) return;
        if (QuestManager.Instance.IsQuestActive(mainQuest) || QuestManager.Instance.IsQuestCompleted(mainQuest)) return;

        QuestManager.Instance.StartQuest(mainQuest);

        // Town에 실제 물품 지급 연출이 아직 없어서, 수락과 동시에 즉시 지급합니다.
        if (playerInventory != null)
        {
            foreach (var req in mainQuest.requiredItems)
            {
                for (int i = 0; i < req.quantity; i++)
                {
                    playerInventory.AddItem(req.item);
                }
            }
        }

        CloseDialogue();
    }

    void OnClickTalk()
    {
        if (choicePanel != null) choicePanel.SetActive(false);
        if (talkPanel != null) talkPanel.SetActive(true);

        if (talkText != null && talkLines.Length > 0)
        {
            talkText.text = talkLines[Random.Range(0, talkLines.Length)];
        }
    }
}
