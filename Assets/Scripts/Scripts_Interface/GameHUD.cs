using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 화면 왼쪽 위 기본 HUD (환경 정보 아이콘, 체력, 돈, 진행 중인 퀘스트).
// 프리팹(GameHUD)으로 만들어 두었으니 어느 씬에든 인스턴스만 놓으면 동작합니다.
// 체력 하트는 같은 프리팹 안의 HeartsUI가 담당합니다.
public class GameHUD : MonoBehaviour
{
    [Header("환경 정보")]
    public Image environmentIcon; // 사진은 나중에 여기 sprite만 넣으면 됨

    [Header("돈")]
    public TMP_Text goldText;

    [Header("퀘스트")]
    public GameObject questRoot;
    public TMP_Text questNameText;
    public TMP_Text questSummaryText;
    public GameObject checkBox; // 목표가 없으면 같이 숨김

    [Header("배경 (요약이 길어져 여러 줄이 되면 세로로 같이 늘어남)")]
    public RectTransform background;

    private PlayerWallet wallet;
    private QuestManager questManager;
    private float baseBackgroundHeight;
    private float baseSummaryHeight;

    void Awake()
    {
        if (background != null) baseBackgroundHeight = background.sizeDelta.y;
        if (questSummaryText != null) baseSummaryHeight = questSummaryText.rectTransform.sizeDelta.y;

        // 퀘스트를 확인하기 전(QuestManager가 아직 없을 때)에는 견본 문구가 보이지 않게 숨겨둠
        if (questRoot != null) questRoot.SetActive(false);
    }

    void Update()
    {
        // Player, QuestManager는 씬 전환 뒤에 나중에 생길 수 있으니 찾을 때까지 계속 시도
        if (wallet == null)
        {
            wallet = FindFirstObjectByType<PlayerWallet>();
            if (wallet != null)
            {
                wallet.OnGoldChanged += RefreshGold;
                RefreshGold(wallet.Gold);
            }
        }

        if (questManager == null && QuestManager.Instance != null)
        {
            questManager = QuestManager.Instance;
            questManager.OnQuestStarted += RefreshQuest;
            questManager.OnQuestCompleted += OnQuestCompleted;
            RefreshQuest(questManager.currentQuest);
        }
    }

    void OnDestroy()
    {
        if (wallet != null) wallet.OnGoldChanged -= RefreshGold;
        if (questManager != null)
        {
            questManager.OnQuestStarted -= RefreshQuest;
            questManager.OnQuestCompleted -= OnQuestCompleted;
        }
    }

    void RefreshGold(int gold)
    {
        if (goldText != null) goldText.text = gold.ToString();
    }

    void OnQuestCompleted(QuestData completed)
    {
        // 완료 이벤트가 오는 시점에는 QuestManager.currentQuest가 아직 방금 완료한 퀘스트 그대로라서
        // 그 값을 읽으면 완료된 퀘스트가 계속 남아 보임. 일단 비우고,
        // 다음 퀘스트가 자동으로 시작되면 이어서 오는 OnQuestStarted가 다시 채워줌
        RefreshQuest(null);
    }

    void RefreshQuest(QuestData quest)
    {
        bool has = quest != null;
        if (questRoot != null) questRoot.SetActive(has);
        if (!has) return;

        if (questNameText != null) questNameText.text = quest.questName;
        // 플레이어 화면에는 의뢰 설명(description)이 아니라 목표 설명(objectives)을 보여줌
        string objectiveText = quest.objectives != null ? string.Join("\n", quest.objectives) : "";
        if (checkBox != null) checkBox.SetActive(!string.IsNullOrEmpty(objectiveText));

        if (questSummaryText != null)
        {
            questSummaryText.text = objectiveText;
            FitSummary();
        }
    }

    // 요약이 몇 줄이든 글자가 잘리지 않게 높이를 맞추고, 남색 배경도 그만큼 늘림
    void FitSummary()
    {
        var rt = questSummaryText.rectTransform;
        float needed = questSummaryText.GetPreferredValues(questSummaryText.text, rt.rect.width, 0f).y;
        float height = Mathf.Max(baseSummaryHeight, needed);
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, height);

        if (background != null)
        {
            background.sizeDelta = new Vector2(background.sizeDelta.x,
                baseBackgroundHeight + (height - baseSummaryHeight));
        }
    }
}
