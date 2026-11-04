using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 메인 퀘스트 1-1 이야기 진행기입니다. 대사(DialogueSequence)와 실제 플레이어 행동(F키, PC 클릭)을 이어 붙입니다.
//   튜토리얼 후 사장 F → "PC를 확인해 보게나" → PC에서 '의뢰 접수' 클릭 → 사장이 대화하며 메인 퀘스트를 접수해 줌
//   → 1-2-2에서 레온 F → 물품 전달(배송 완료 창) → 영약 이야기, 메인 1-2 퀘스트 3개 시작 → 영약 우물 F → 영약 3개 획득
// 진행 상태는 QuestManager의 플래그에 기록하므로 씬을 넘어도 유지되고, 나중에 저장 기능이 그대로 읽고 쓸 수 있습니다.
// StoryDirector 오브젝트(씬 간 유지)에 DialogueRunner와 함께 둡니다. Dungeon Express > Build Story 로 만들어집니다.
public class StoryManager : MonoBehaviour
{
    public static StoryManager Instance { get; private set; }

    // 진행 기록 (QuestManager 플래그 이름)
    public const string FlagBossIntro = "main11_boss_intro"; // 사장에게 "PC를 확인해 보게나"를 들음
    public const string FlagAccepted = "main11_accepted";     // 사장이 메인 퀘스트 1-1을 접수해 줌
    public const string FlagLeonMet = "main11_leon_met";      // 레온과 대화를 마침 (1-1 완료, 1-2 시작)
    public const string FlagElixirTaken = "main11_elixir_taken"; // 영약 우물에서 영약 3개를 얻음

    // 상호작용 ID (StoryInteractable.interactionId)
    public const string IdLeon = "leon";
    public const string IdElixirWell = "elixir_well";

    [Header("대사")]
    public DialogueSequence bossIntro;
    public DialogueSequence bossRemind;
    public DialogueSequence pcAccept;   // PC를 확인하며 사장이 접수해 주기까지
    public DialogueSequence pcAfter;    // 접수 후 마지막 한마디
    public DialogueSequence leonMeet;   // 레온 첫 만남 ~ 물품을 건넴
    public DialogueSequence leonTalk;   // 물품을 건넨 뒤 ~ 영약 부탁
    public DialogueSequence leonRemind; // 이미 대화한 뒤 다시 말을 걸었을 때

    [Header("퀘스트와 아이템")]
    public QuestData main11;
    public QuestData[] main12;
    public ItemData elixir;

    [Header("설정")]
    [Tooltip("메인 1-1 배송 완료 창에 보이는 보수 (이야기상 실제로 받는 돈)")]
    public int deliveryPayout = 2000;
    [Tooltip("우물에서 한 번에 얻는 영약 개수")]
    public int elixirCount = 3;

    [Header("개발용 (임시)")]
    [Tooltip("켜면 메인 퀘스트 1-1을 접수한 뒤 던전에 들어갈 때 레온 바로 앞으로 이동함. 테스트가 끝나면 끄거나, 이 항목과 WarpToLeon 함수를 지우세요")]
    public bool debugWarpToLeon = true;

    private bool running;

    private QuestManager Quests => QuestManager.Instance;

    // 튜토리얼을 끝냈거나, 튜토리얼을 끈 상태(테스트)면 메인 퀘스트 스토리를 시작할 수 있음
    private bool StoryReady
    {
        get
        {
            if (Quests == null) return false;
            if (Quests.HasFlag(QuestManager.FlagTutorialDone)) return true;
            return TutorialManager.Instance != null && !TutorialManager.Instance.runTutorial;
        }
    }

    // PC 앞에서 F를 누르면 스토리가 PC 화면을 여는 차례인지
    public bool WaitingForPC => !running && StoryReady && Has(FlagBossIntro) && !Has(FlagAccepted);

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(transform.root.gameObject); // 사무실로 돌아오면 씬에 있는 사본은 필요 없음
            return;
        }
        Instance = this;
        DontDestroyOnLoad(transform.root.gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Instance = null;
    }

    // ---------- 개발용 (임시): 던전에 들어가면 레온 앞으로 이동 ----------

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (debugWarpToLeon && Has(FlagAccepted) && !Has(FlagLeonMet)) StartCoroutine(WarpToLeon());
    }

    IEnumerator WarpToLeon()
    {
        // 플레이어 생성/배치와 씬 오브젝트가 준비될 때까지 잠시 기다림 (최대 약 3초)
        for (int i = 0; i < 180; i++)
        {
            yield return null;

            StoryInteractable leon = null;
            foreach (var s in FindObjectsByType<StoryInteractable>(FindObjectsSortMode.None))
            {
                if (s.interactionId == IdLeon) { leon = s; break; }
            }
            var player = FindFirstObjectByType<PlayerController>();
            if (leon == null || player == null) continue;

            // 레온 왼쪽 코앞 (F 안내가 뜨는 범위 안)
            Vector3 target = leon.transform.position + new Vector3(-1.5f, 0.3f, 0f);
            player.transform.position = target;
            var body = player.GetComponent<Rigidbody2D>();
            if (body != null) body.linearVelocity = Vector2.zero;
            Debug.Log("[Story] (개발용) 레온 앞으로 이동했습니다: " + target);
            yield break;
        }
    }

    bool Has(string flag)
    {
        return Quests != null && Quests.HasFlag(flag);
    }

    // ---------- 사장 ----------

    // QuestGiverTrigger(사장)에서 F를 눌렀을 때 호출. 스토리가 대화를 맡으면 true
    public bool HandleBossInteract()
    {
        if (running || !StoryReady || Has(FlagAccepted)) return false;
        StartCoroutine(RunBoss());
        return true;
    }

    IEnumerator RunBoss()
    {
        running = true;
        var runner = DialogueRunner.Instance;
        if (!Has(FlagBossIntro))
        {
            yield return runner.Play(bossIntro);
            Quests.SetFlag(FlagBossIntro);
        }
        else
        {
            yield return runner.Play(bossRemind);
        }
        running = false;
    }

    // ---------- PC ----------

    // OfficePC에서 F를 눌렀을 때 호출 (WaitingForPC일 때만)
    public void OnPCInteract()
    {
        if (WaitingForPC) StartCoroutine(RunPCAccept());
    }

    IEnumerator RunPCAccept()
    {
        running = true;
        var runner = DialogueRunner.Instance;
        var board = FindFirstObjectByType<PCBoardUI>();

        // PC 화면을 열고 플레이어가 '의뢰 접수'를 누를 때까지 기다림 (접수 버튼은 쓸 수 없음, 닫을 수도 없음)
        bool tabOpened = false;
        System.Action onTab = () => tabOpened = true;
        if (board != null)
        {
            board.OnRequestTabOpened += onTab;
            board.Open(new List<QuestData> { main11 }, false, false);
            while (!tabOpened) yield return null;
        }

        yield return runner.Play(pcAccept, false);

        // "일단 의뢰는 접수해놓겠네" → 사장이 접수해 주고 물품(유리병)이 가방에 들어옴
        Quests.StartQuest(main11, FindFirstObjectByType<Inventory>());

        if (board != null)
        {
            board.OnRequestTabOpened -= onTab;
            board.Close();
        }

        yield return runner.Play(pcAfter);
        Quests.SetFlag(FlagAccepted);
        running = false;
    }

    // ---------- 1-2-2 (레온, 영약 우물) ----------

    // 지금 이 상호작용을 할 수 있는지 (StoryInteractable이 F 안내를 보여줄지 정할 때 씀)
    public bool CanInteract(string id)
    {
        if (running || Quests == null) return false;

        switch (id)
        {
            case IdLeon:
                // 메인 1-1을 진행 중이면 첫 대화, 한 번 대화한 뒤에는 언제든 다시 말을 걸 수 있음 (같은 대사가 계속 나옴)
                return Quests.IsQuestActive(main11) || Has(FlagLeonMet);
            case IdElixirWell:
                return Has(FlagLeonMet) && !Has(FlagElixirTaken);
            default:
                return false;
        }
    }

    public void HandleInteract(string id)
    {
        if (!CanInteract(id)) return;

        if (id == IdLeon) StartCoroutine(RunLeon());
        else if (id == IdElixirWell) StartCoroutine(RunElixirWell());
    }

    IEnumerator RunLeon()
    {
        running = true;
        var runner = DialogueRunner.Instance;
        var inventory = FindFirstObjectByType<Inventory>();

        if (!Has(FlagLeonMet))
        {
            // "(고개를 끄덕이며 의뢰 물품을 건넨다)"까지
            yield return runner.Play(leonMeet, false);

            // 물품을 건네면 메인 1-1이 완료됨 (물품이 가방에서 빠짐)
            Quests.CompleteQuest(main11, inventory);

            // 레온과의 대화를 모두 끝낸 뒤에 배송 완료 창(보수 2000원)을 보여줌: 대화 → 배송 완료 창 → 우물
            yield return runner.Play(leonTalk, false);
            yield return ShowDeliveryResult(runner);
            runner.Close();

            // 메인 1-2 퀘스트 3개 시작 (영약은 우물에서 직접 얻으므로 자동 지급 없음)
            foreach (var quest in main12) Quests.StartQuest(quest, inventory);
            Quests.SetFlag(FlagLeonMet);
        }
        else
        {
            yield return runner.Play(leonRemind);
        }
        running = false;
    }

    // 배송 완료 창을 띄우고 닫힐 때까지 기다림. 보수는 이야기상 받는 돈(deliveryPayout)으로 보여줌
    IEnumerator ShowDeliveryResult(DialogueRunner runner)
    {
        var window = FindFirstObjectByType<DeliveryCompleteWindow>();
        if (window == null) yield break;

        var shown = Instantiate(main11); // 원본 퀘스트 에셋의 보수(5000만)는 건드리지 않음
        shown.reward = deliveryPayout;

        runner.SetVisible(false); // 창이 닫힌 뒤에는 호출한 쪽이 대사창을 닫음 (정지는 그때까지 유지)
        window.Show(shown);
        yield return null;
        while (window.IsOpen) yield return null;

        Destroy(shown);
    }

    IEnumerator RunElixirWell()
    {
        running = true;
        var runner = DialogueRunner.Instance;
        var inventory = FindFirstObjectByType<Inventory>();

        if (inventory == null || inventory.maxSlots - inventory.Items.Count < elixirCount)
        {
            yield return runner.Say("", "가방에 빈 칸이 부족하다. (영약 " + elixirCount + "개를 담으려면 빈 칸이 " + elixirCount + "개 필요하다)");
            runner.Close();
            running = false;
            yield break;
        }

        for (int i = 0; i < elixirCount; i++) inventory.AddItem(elixir);
        Quests.SetFlag(FlagElixirTaken);

        yield return runner.Say("", "영약 " + elixirCount + "개를 얻었다.");
        runner.Close();
        running = false;
    }
}
