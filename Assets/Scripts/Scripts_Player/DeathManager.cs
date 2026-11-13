using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// 플레이어가 죽었을 때의 흐름을 맡습니다. (기획서: 사망 → Game Over 창 → 저장된 곳에서 불러오기 / 타이틀로)
//   사망 모션이 끝나면 → 가방 아이템 일부를 랜덤으로 잃고(DeathRules)
//   → "Game Over" 창에 사라진 물품과 진행 중이던 의뢰 목록(포기 가능)을 보여주고
//   → [불러오기]는 저장 슬롯 창을 열어 고른 슬롯으로 이어서 하고, [타이틀로]는 타이틀 화면으로 돌아갑니다.
// DeathDirector 오브젝트(씬 간 유지)에 둡니다. Dungeon Express > Build Death Screen 으로 만들어집니다.
public class DeathManager : MonoBehaviour
{
    public static DeathManager Instance { get; private set; }

    private enum Choice { None, Load, Title }

    [Header("설정")]
    [Tooltip("창이 뜬 뒤 이 시간(초) 동안은 버튼이 눌리지 않음 (죽자마자 실수로 누르지 않게)")]
    public float minimumShowSeconds = 1f;
    public float fadeSeconds = 0.6f;

    [Header("개발용 (임시)")]
    [Tooltip("켜면 F9 키로 바로 죽을 수 있음 (사망 흐름 테스트용). 테스트가 끝나면 끄거나 지우세요")]
    public bool debugDieKey = true;

    [Header("화면")]
    public CanvasGroup screen;
    public Button loadButton;
    public Button titleButton;

    [Header("사라진 물품 칸 (순서대로 채움)")]
    public Image[] lostSlotIcons;
    public TMP_Text[] lostSlotQualities;

    [Header("의뢰 목록")]
    public Button dungeonButton;
    public TMP_Text dungeonLabel;
    public RectTransform questListParent;
    public GameObject questRowTemplate; // 꺼둔 견본. 자식: Badge(TMP), Name(TMP), Give(Button) 안에 Label(TMP)
    public GameObject emptyQuestText;

    [Header("의뢰 상세")]
    public GameObject detailRoot;
    public TMP_Text detailName;
    public TMP_Text detailClient;
    public TMP_Text detailItems;
    public TMP_Text detailLocation;
    public TMP_Text detailReward;
    public TMP_Text detailDescription;

    [Header("의뢰 포기 확인")]
    public GameObject confirmRoot;
    public TMP_Text confirmQuestName;
    public Button confirmYes;
    public Button confirmNo;

    private static readonly Color RowNormal = new Color(1f, 1f, 1f, 0.55f);
    private static readonly Color RowSelected = Color.white;

    private readonly List<GameObject> questRows = new List<GameObject>();
    private Choice choice = Choice.None;
    private bool running;
    private Inventory inventory;
    private QuestData selectedQuest;
    private QuestData pendingGiveUp;
    private string selectedDungeon;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(transform.root.gameObject); // 사무실로 돌아오면 씬에 있는 사본은 필요 없음
            return;
        }
        Instance = this;
        DontDestroyOnLoad(transform.root.gameObject);

        if (questRowTemplate != null) questRowTemplate.SetActive(false);
        if (loadButton != null) loadButton.onClick.AddListener(() => choice = Choice.Load);
        if (titleButton != null) titleButton.onClick.AddListener(() => choice = Choice.Title);
        if (dungeonButton != null) dungeonButton.onClick.AddListener(NextDungeon);
        if (confirmYes != null) confirmYes.onClick.AddListener(AcceptGiveUp);
        if (confirmNo != null) confirmNo.onClick.AddListener(CancelGiveUp);
        HideImmediate();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (!debugDieKey || Instance != this) return;

        var keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.f9Key.wasPressedThisFrame) return;

        var health = FindFirstObjectByType<PlayerHealth>();
        if (health != null && !health.IsDead) health.TakeDamage(health.maxHearts + 1);
    }

    // PlayerHealth가 사망 모션 뒤에 부름. 불러오기로 플레이어가 사라져도 끊기지 않게 DeathManager가 직접 코루틴을 돌림
    public void BeginDeath(PlayerHealth health)
    {
        if (running) return;
        StartCoroutine(RunDeath(health));
    }

    IEnumerator RunDeath(PlayerHealth health)
    {
        running = true;
        GamePause.Push(); // 사망 화면과 씬 이동 동안 몬스터 등이 움직이지 않게
        EnsureEventSystem();

        // 1) 아이템 손실
        inventory = health.GetComponent<Inventory>();
        List<LostItem> lost = inventory != null ? inventory.RemoveRandomOnDeath() : new List<LostItem>();

        // 2) Game Over 창: 사라진 물품 + 진행 중이던 의뢰
        ShowLostItems(lost);
        selectedQuest = null;
        selectedDungeon = null;
        confirmRoot.SetActive(false);
        RefreshQuests();
        SetButtonsInteractable(false);
        yield return Fade(0f, 1f);

        float shownAt = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - shownAt < minimumShowSeconds) yield return null;

        // 3) 선택 기다리기: 불러오기를 취소하면 이 창으로 돌아옴
        bool finished = false;
        while (!finished)
        {
            SetButtonsInteractable(true);
            choice = Choice.None;
            while (choice == Choice.None) yield return null;

            if (choice == Choice.Title)
            {
                SaveFlowCoordinator.ReturnToTitle();
                yield return null;
                finished = true;
            }
            else
            {
                SetButtonsInteractable(false);
                bool loaded = false;
                yield return LoadFromSlot(result => loaded = result);
                finished = loaded;
            }
        }

        // 4) 정리: 새 씬에는 새 플레이어가 있으므로 이 창만 치우면 됨
        HideImmediate();
        ClearQuestRows();
        inventory = null;
        running = false;
        GamePause.Pop();
    }

    // 저장 슬롯 창을 열어 고른 슬롯으로 불러옴. 불러오기가 끝났으면 true, 창만 닫았으면 false
    IEnumerator LoadFromSlot(System.Action<bool> done)
    {
        try { SaveSlotWindow.OpenLoad(); }
        catch (System.Exception e)
        {
            Debug.LogException(e);
            done(false);
            yield break;
        }

        bool started = false;
        while (true)
        {
            if (SaveFlowCoordinator.IsLoading) started = true;

            if (started && !SaveFlowCoordinator.IsLoading && !SaveSlotWindow.IsShowing) { done(true); yield break; }
            if (!started && !SaveSlotWindow.IsShowing) { done(false); yield break; }
            yield return null;
        }
    }

    static bool HasAnySave()
    {
        for (int slot = 1; slot <= 3; slot++)
        {
            if (SaveSlotInfo.Read(SaveGameService.Files, slot).CanLoad) return true;
        }
        return false;
    }

    void SetButtonsInteractable(bool on)
    {
        if (loadButton != null) loadButton.interactable = on && HasAnySave();
        if (titleButton != null) titleButton.interactable = on;
        if (dungeonButton != null) dungeonButton.interactable = on;
        foreach (var row in questRows)
        {
            if (row == null) continue;
            foreach (var button in row.GetComponentsInChildren<Button>(true)) button.interactable = on;
        }
    }

    // ---------- 사라진 물품 ----------

    void ShowLostItems(List<LostItem> lost)
    {
        for (int i = 0; i < lostSlotIcons.Length; i++)
        {
            bool has = i < lost.Count && lost[i].item != null;
            var icon = lostSlotIcons[i];
            icon.enabled = has && lost[i].item.icon != null;
            icon.sprite = has ? lost[i].item.icon : null;

            if (i < lostSlotQualities.Length && lostSlotQualities[i] != null)
                lostSlotQualities[i].text = has ? lost[i].quality + "%" : "";
        }
    }

    // ---------- 의뢰 목록 ----------

    // "정글 던전 (야생 정글)" → "정글 던전"
    static string DungeonKey(QuestData quest)
    {
        string place = quest.targetLocation;
        if (string.IsNullOrEmpty(place)) return "기타";
        int paren = place.IndexOf('(');
        string key = (paren > 0 ? place.Substring(0, paren) : place).Trim();
        return key.Length > 0 ? key : "기타";
    }

    List<string> CollectDungeons()
    {
        var result = new List<string>();
        var manager = QuestManager.Instance;
        if (manager == null) return result;
        foreach (var quest in manager.activeQuests)
        {
            string key = DungeonKey(quest);
            if (!result.Contains(key)) result.Add(key);
        }
        return result;
    }

    void NextDungeon()
    {
        var dungeons = CollectDungeons();
        if (dungeons.Count <= 1) return;
        int index = dungeons.IndexOf(selectedDungeon);
        selectedDungeon = dungeons[(index + 1) % dungeons.Count];
        selectedQuest = null;
        RefreshQuests();
    }

    void RefreshQuests()
    {
        ClearQuestRows();

        var dungeons = CollectDungeons();
        if (selectedDungeon == null || !dungeons.Contains(selectedDungeon))
            selectedDungeon = dungeons.Count > 0 ? dungeons[0] : null;

        dungeonLabel.text = (selectedDungeon ?? "진행 중인 의뢰 없음") + (dungeons.Count > 1 ? "  ▼" : "");

        var shown = new List<QuestData>();
        if (QuestManager.Instance != null)
        {
            foreach (var quest in QuestManager.Instance.activeQuests)
            {
                if (DungeonKey(quest) == selectedDungeon) shown.Add(quest);
            }
        }

        if (selectedQuest == null || !shown.Contains(selectedQuest))
            selectedQuest = shown.Count > 0 ? shown[0] : null;

        foreach (var quest in shown)
        {
            var row = Instantiate(questRowTemplate, questListParent);
            row.SetActive(true);
            questRows.Add(row);

            var captured = quest;
            row.transform.Find("Badge").GetComponent<TMP_Text>().text = QuestPanelController.GetCategoryName(quest.category);
            row.transform.Find("Name").GetComponent<TMP_Text>().text = quest.questName;
            row.GetComponent<Image>().color = quest == selectedQuest ? RowSelected : RowNormal;
            row.GetComponent<Button>().onClick.AddListener(() => SelectQuest(captured));

            var give = row.transform.Find("Give");
            bool canGiveUp = QuestManager.Instance != null && QuestManager.Instance.CanAbandon(quest);
            give.gameObject.SetActive(canGiveUp);
            give.GetComponent<Button>().onClick.AddListener(() => AskGiveUp(captured));
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(questListParent);

        emptyQuestText.SetActive(shown.Count == 0);
        ShowDetail(selectedQuest);
    }

    void ClearQuestRows()
    {
        foreach (var row in questRows) if (row != null) Destroy(row);
        questRows.Clear();
    }

    void SelectQuest(QuestData quest)
    {
        selectedQuest = quest;
        RefreshQuests();
    }

    void ShowDetail(QuestData quest)
    {
        detailRoot.SetActive(quest != null);
        if (quest == null) return;

        detailName.text = quest.questName;
        detailClient.text = string.IsNullOrEmpty(quest.client) ? "-" : quest.client;
        detailItems.text = FormatRequiredItems(quest);
        detailLocation.text = string.IsNullOrEmpty(quest.targetLocation) ? "-" : quest.targetLocation;
        detailReward.text = quest.reward.ToString("N0") + " 골드";
        detailDescription.text = string.IsNullOrEmpty(quest.description) ? "-" : quest.description;
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

    // ---------- 의뢰 포기 ----------

    void AskGiveUp(QuestData quest)
    {
        pendingGiveUp = quest;
        confirmQuestName.text = quest.questName;
        confirmRoot.SetActive(true);
    }

    void AcceptGiveUp()
    {
        if (pendingGiveUp != null && QuestManager.Instance != null)
            QuestManager.Instance.AbandonQuest(pendingGiveUp, inventory);

        pendingGiveUp = null;
        confirmRoot.SetActive(false);
        SelectQuest(null);
    }

    void CancelGiveUp()
    {
        pendingGiveUp = null;
        confirmRoot.SetActive(false);
    }

    // ---------- 화면 ----------

    static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        var events = new GameObject("DeathEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
    }

    void HideImmediate()
    {
        if (confirmRoot != null) confirmRoot.SetActive(false);
        if (screen == null) return;
        screen.alpha = 0f;
        screen.blocksRaycasts = false;
        screen.gameObject.SetActive(false);
    }

    // 시간이 멈춘 상태에서도 돌도록 실제 시간(unscaled)으로 서서히 밝기를 바꿈
    IEnumerator Fade(float from, float to)
    {
        if (screen == null) yield break;

        screen.gameObject.SetActive(true);
        screen.blocksRaycasts = true;

        float t = 0f;
        while (t < fadeSeconds)
        {
            t += Time.unscaledDeltaTime;
            screen.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / fadeSeconds));
            yield return null;
        }
        screen.alpha = to;

        if (to <= 0f) HideImmediate();
    }
}
