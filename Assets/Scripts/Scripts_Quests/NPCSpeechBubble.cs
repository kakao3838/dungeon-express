using System.Collections;
using TMPro;
using UnityEngine;

// NPC 머리 위 말풍선입니다. 같은 오브젝트의 QuestTurnInTrigger가 받는 퀘스트가 완료되면
// completeLine을 잠깐 띄웁니다. 다른 대사는 Show(문장)으로 직접 띄울 수도 있습니다.
public class NPCSpeechBubble : MonoBehaviour
{
    [Header("연결")]
    public QuestTurnInTrigger turnIn;
    public GameObject bubbleRoot;
    public TMP_Text lineText;

    [Header("대사")]
    [TextArea] public string completeLine = "완벽해요! 물건이 무사히 도착했네요. 감사합니다!";
    public float showSeconds = 3f;

    private QuestManager questManager;
    private Coroutine hideRoutine;

    void Awake()
    {
        if (turnIn == null) turnIn = GetComponent<QuestTurnInTrigger>();
        if (bubbleRoot != null) bubbleRoot.SetActive(false);
    }

    void Update()
    {
        // QuestManager는 Town에서 만들어져 넘어오므로, 잡힐 때까지 계속 시도
        if (questManager == null && QuestManager.Instance != null)
        {
            questManager = QuestManager.Instance;
            questManager.OnQuestCompleted += OnQuestCompleted;
        }
    }

    void OnDestroy()
    {
        if (questManager != null) questManager.OnQuestCompleted -= OnQuestCompleted;
    }

    void OnQuestCompleted(QuestData completed)
    {
        if (turnIn == null || completed != turnIn.questToComplete) return;
        Show(completeLine);
    }

    public void Show(string line)
    {
        if (bubbleRoot == null) return;
        if (lineText != null) lineText.text = line;

        bubbleRoot.SetActive(true);
        if (hideRoutine != null) StopCoroutine(hideRoutine);
        hideRoutine = StartCoroutine(HideAfter(showSeconds));
    }

    public void Hide()
    {
        if (hideRoutine != null) StopCoroutine(hideRoutine);
        hideRoutine = null;
        if (bubbleRoot != null) bubbleRoot.SetActive(false);
    }

    IEnumerator HideAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        hideRoutine = null;
        if (bubbleRoot != null) bubbleRoot.SetActive(false);
    }
}
