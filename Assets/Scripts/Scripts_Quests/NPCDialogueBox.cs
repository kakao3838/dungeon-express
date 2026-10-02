using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// 화면 아래에 뜨는 대사창입니다. (사장과 대화할 때 쓰는 대화창과 같은 모양)
// 같은 오브젝트의 QuestTurnInTrigger가 받는 퀘스트가 완료되는 순간(F키 납품) 대사를 띄웁니다.
// 닫기 버튼, 또는 F키를 다시 누르면 닫히고, resultWindow가 연결돼 있으면 이어서 배송 완료 창을 띄웁니다.
// 열려 있는 동안에는 게임 시간이 멈추고 플레이어 조작이 막힙니다.
public class NPCDialogueBox : MonoBehaviour
{
    [Header("연결")]
    public QuestTurnInTrigger turnIn;
    public GameObject panel;
    public TMP_Text speakerText;
    public TMP_Text dialogueText;
    [Tooltip("대사창을 닫은 뒤 이어서 띄울 배송 완료 창 (없으면 그냥 끝)")]
    public DeliveryCompleteWindow resultWindow;

    [Header("대사")]
    public string npcName = "카일";
    [TextArea] public string completeLine = "완벽해요! 물건이 무사히 도착했네요. 감사합니다!";

    public bool IsOpen => panel != null && panel.activeSelf;

    private QuestManager questManager;
    private int openedFrame = -1;
    private bool holdingPause;
    private QuestData pendingResult; // 대사가 끝나면 결과창에 보여줄 완료된 퀘스트

    void Awake()
    {
        if (turnIn == null) turnIn = GetComponent<QuestTurnInTrigger>();
        if (panel != null) panel.SetActive(false);
    }

    void Update()
    {
        // QuestManager는 Town에서 만들어져 넘어오므로, 잡힐 때까지 계속 시도
        if (questManager == null && QuestManager.Instance != null)
        {
            questManager = QuestManager.Instance;
            questManager.OnQuestCompleted += OnQuestCompleted;
        }

        // 열린 뒤 F를 다시 누르면 닫기 (열린 그 프레임의 F는 무시)
        if (IsOpen && Time.frameCount != openedFrame)
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.fKey.wasPressedThisFrame) Close();
        }
    }

    void OnDestroy()
    {
        if (questManager != null) questManager.OnQuestCompleted -= OnQuestCompleted;
        ReleasePause(); // 대화창이 열린 채로 씬이 바뀌어도 시간이 멈춰 있지 않게
    }

    void OnQuestCompleted(QuestData completed)
    {
        if (turnIn == null || completed != turnIn.questToComplete) return;
        pendingResult = completed;
        Show(npcName, completeLine);
    }

    public void Show(string speaker, string line)
    {
        if (panel == null) return;
        if (speakerText != null) speakerText.text = speaker;
        if (dialogueText != null) dialogueText.text = line;
        panel.SetActive(true);
        openedFrame = Time.frameCount;

        if (!holdingPause)
        {
            holdingPause = true;
            GamePause.Push();
        }
    }

    // 닫기 버튼의 OnClick에 연결됨
    public void Close()
    {
        if (panel != null) panel.SetActive(false);

        // 결과창을 먼저 열어서(정지 유지) 시간이 잠깐 풀리는 틈이 없게 한 뒤 대사창의 정지를 풀음
        var result = pendingResult;
        pendingResult = null;
        if (result != null && resultWindow != null) resultWindow.Show(result);

        ReleasePause();
    }

    void ReleasePause()
    {
        if (!holdingPause) return;
        holdingPause = false;
        GamePause.Pop();
    }
}
