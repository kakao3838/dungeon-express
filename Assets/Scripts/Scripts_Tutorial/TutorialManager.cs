using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 오프닝 이후 사무실(TownScene)에서 시작해 정글 던전 첫 배달을 마칠 때까지 이어지는 튜토리얼 진행기입니다.
// 사장 대사 → 이동/상호작용 안내 → PC에서 의뢰 접수 → 문(던전 선택) → 정글 키 안내 → 배송 완료 후 전화 순서로 흘러갑니다.
// 씬을 넘어가도 이어지도록 DontDestroyOnLoad 입니다. UI는 Dungeon Express > Build Tutorial 로 TownScene에 만들어집니다.
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    // 튜토리얼 동안 플레이어 키 입력/공격/태블릿을 막기 위한 값들
    public static bool InputLocked => Instance != null && Instance.scriptedLock;
    public static bool BlocksMenu => Instance != null && Instance.townActive;

    // 한 번 끝난 튜토리얼은 사무실로 다시 돌아와도 반복하지 않음
    private static bool completed;

    private enum Phase { Idle, Intro, WaitMove, WaitPC, PCMenu, AfterPC, Dungeon, Done }

    [Header("설정")]
    [Tooltip("끄면 튜토리얼 없이 바로 플레이할 수 있음 (테스트용)")]
    public bool runTutorial = true;
    public QuestData tutorialQuest;
    public string townSceneName = "TownScene";
    [Tooltip("자동 이동할 때 사장 앞에서 멈출 거리")]
    public float stopDistanceFromBoss = 2.5f;

    [Header("연결 - 대사창")]
    public GameObject dialoguePanel;
    public TMP_Text speakerText;
    public TMP_Text lineText;
    public GameObject nextHint;
    public Button dialogueButton;

    [Header("연결 - 키 안내")]
    public GameObject keyGuidePanel;
    public TMP_Text keyGuideTitle;
    public RectTransform keyGuideRows;
    public GameObject keyGuideRowTemplate; // 자식: Keys(키캡이 들어갈 곳), Label
    public GameObject keyCapTemplate;      // 자식: Label
    public Button keyGuideConfirmButton;

    [Header("연결 - PC")]
    public GameObject pcPanel;
    public GameObject pcHomeView;
    public Button pcRequestButton;
    public GameObject pcQuestView;
    public TMP_Text pcQuestNameText;
    public TMP_Text pcDetailText;
    public Button pcAcceptButton;

    [Header("연결 - 던전 선택")]
    public GameObject dungeonPanel;
    public Button dungeonJungleButton;
    public Image dungeonJungleImage;
    public Button dungeonEnterButton;
    public Button dungeonCloseButton;

    [Header("연결 - 선택지 / 토스트")]
    public GameObject choicePanel;
    public Button choiceYesButton;
    public Button choiceNoButton;
    public GameObject toastPanel;
    public TMP_Text toastText;

    private const string Boss = "사장";
    private const string Me = "플레이어";

    private static readonly Color JungleSelectedColor = new Color(0.85f, 0.15f, 0.15f);
    private static readonly Color JungleIdleColor = new Color(0.87f, 0.87f, 0.87f);

    private Phase phase = Phase.Idle;
    private bool townActive;
    private bool scriptedLock;
    private bool returnToTown;
    private bool doorBusy;
    private bool doorIntroShown;
    private bool jungleSelected;
    private bool subscribedQuest;
    private bool holdingPause;
    private Coroutine toastRoutine;

    private PlayerController player;
    private QuestGiverTrigger bossGiver;
    private TutorialPC pc;

    // 버튼 클릭 신호
    private bool advanceClicked;
    private bool pcRequestClicked;
    private bool pcAcceptClicked;
    private bool keyGuideConfirmed;
    private int dungeonResult; // 0 대기, 1 입장, 2 닫기
    private int choiceResult;  // 0 대기, 1 예, 2 아니요
    private int shownFrame = -1;

    public bool WaitingForPC => phase == Phase.WaitPC;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState()
    {
        completed = false;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // 사무실로 돌아오면 씬에 있는 사본은 필요 없음
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;

        HideAll();
        BindButtons();
    }

    void OnDestroy()
    {
        if (Instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (subscribedQuest && QuestManager.Instance != null) QuestManager.Instance.OnQuestCompleted -= OnQuestCompleted;
        ReleasePause();
        Instance = null;
    }

    void Start()
    {
        if (Instance != this) return;
        if (!runTutorial || completed) { phase = Phase.Done; return; }
        if (SceneManager.GetActiveScene().name == townSceneName) StartCoroutine(RunTown());
    }

    void BindButtons()
    {
        if (dialogueButton != null) dialogueButton.onClick.AddListener(() => advanceClicked = true);
        if (pcRequestButton != null) pcRequestButton.onClick.AddListener(() => pcRequestClicked = true);
        if (pcAcceptButton != null) pcAcceptButton.onClick.AddListener(() => pcAcceptClicked = true);
        if (keyGuideConfirmButton != null) keyGuideConfirmButton.onClick.AddListener(() => keyGuideConfirmed = true);
        if (dungeonJungleButton != null) dungeonJungleButton.onClick.AddListener(() => SelectJungle(true));
        if (dungeonEnterButton != null) dungeonEnterButton.onClick.AddListener(() =>
        {
            if (jungleSelected) dungeonResult = 1;
            else ShowToast("입장할 던전을 먼저 선택하세요.");
        });
        if (dungeonCloseButton != null) dungeonCloseButton.onClick.AddListener(() => dungeonResult = 2);
        if (choiceYesButton != null) choiceYesButton.onClick.AddListener(() => choiceResult = 1);
        if (choiceNoButton != null) choiceNoButton.onClick.AddListener(() => choiceResult = 2);
    }

    void HideAll()
    {
        SetActive(dialoguePanel, false);
        SetActive(keyGuidePanel, false);
        SetActive(pcPanel, false);
        SetActive(dungeonPanel, false);
        SetActive(choicePanel, false);
        SetActive(toastPanel, false);
        SetActive(keyGuideRowTemplate, false);
        SetActive(keyCapTemplate, false);
    }

    static void SetActive(GameObject go, bool on)
    {
        if (go != null) go.SetActive(on);
    }

    // ---------- 사무실 튜토리얼 ----------

    IEnumerator RunTown()
    {
        townActive = true;
        phase = Phase.Intro;

        player = FindFirstObjectByType<PlayerController>();
        bossGiver = FindFirstObjectByType<QuestGiverTrigger>();
        pc = FindFirstObjectByType<TutorialPC>();
        var door = FindFirstObjectByType<DungeonDoorTrigger>();
        if (player == null || door == null)
        {
            Debug.LogWarning("[Tutorial] 플레이어나 문을 찾지 못해 튜토리얼을 건너뜁니다.");
            townActive = false;
            phase = Phase.Done;
            yield break;
        }

        // 튜토리얼 중에는 사장이 직접 퀘스트를 주지 않음 (PC에서만 접수)
        if (bossGiver != null) bossGiver.enabled = false;

        // 문에서 들어와 사장 앞까지 자동으로 걸어감
        PlacePlayerNextToDoor(door.transform);
        yield return null;
        LockInput(true);
        float stopX = (bossGiver != null ? bossGiver.transform.position.x : player.transform.position.x - 8f) + stopDistanceFromBoss;
        player.ScriptedMove = -1f;
        while (player.transform.position.x > stopX) yield return null;
        player.ScriptedMove = 0f;
        yield return new WaitForSeconds(0.3f);

        yield return Say(Me, "배송 완료했습니다.");
        yield return Say(Boss, "오, 왔는가!");
        yield return Say(Boss, "이거 보게나. PC를 설치했다네. 이제 더는 직접 발로 뛰며 의뢰를 받아오지 않아도 된다는 게지.");
        yield return Say(Boss, "다 자네가 열심히 일해준 덕분이야. 앞으로도 수고해주게! 하하하!");
        yield return Say(Boss, "자, PC를 확인해 보게나.");
        CloseDialogue();

        // 이동 안내
        LockInput(false);
        phase = Phase.WaitMove;
        ShowKeyGuide("", new[] { new KeyRow(new[] { "W", "A", "S", "D" }, "를 눌러 이동할 수 있습니다.") }, false);
        float startX = player.transform.position.x;
        while (Mathf.Abs(player.transform.position.x - startX) < 1.5f) yield return null;

        // PC 상호작용 안내
        phase = Phase.WaitPC;
        ShowKeyGuide("", new[] { new KeyRow(new[] { "F" }, "PC 앞에서 F키를 눌러 상호작용할 수 있습니다.") }, false);
        while (phase == Phase.WaitPC) yield return null; // OnPCInteract가 phase를 바꿈
        HideKeyGuide();

        yield return RunPC();

        // PC를 마치면 문으로 안내
        phase = Phase.AfterPC;
        ShowKeyGuide("", new[] { new KeyRow(new[] { "F" }, "문 앞에서 F키를 눌러 던전을 선택할 수 있습니다.") }, false);
    }

    void PlacePlayerNextToDoor(Transform door)
    {
        Vector3 p = player.transform.position;
        p.x = door.position.x - 0.8f;
        player.transform.position = p;
        var body = player.GetComponent<Rigidbody2D>();
        if (body != null) body.linearVelocity = Vector2.zero;
    }

    // TutorialPC에서 호출
    public void OnPCInteract()
    {
        if (phase == Phase.WaitPC) phase = Phase.PCMenu;
    }

    IEnumerator RunPC()
    {
        OpenPC();
        pcRequestClicked = false;
        pcAcceptClicked = false;

        yield return Say(Boss, "PC에서는 의뢰를 받을 수 있다네.");
        yield return Say(Boss, "'의뢰 접수'를 클릭하게.", false);
        while (!pcRequestClicked) yield return null;

        ShowPCQuest();
        yield return Say(Boss, "이 창에서는 어떤 의뢰를 받을지 선택할 수 있네. 의뢰에 대한 자세한 정보도 함께 확인할 수 있지.");
        yield return Say(Boss, "자, 우선 의뢰를 접수해보게나.", false);
        pcAcceptClicked = false; // 설명을 읽는 동안 눌러 둔 값은 무시
        while (!pcAcceptClicked) yield return null;

        AcceptTutorialQuest();
        ClosePC();

        yield return Say(Boss, "좋아. 이 물품은 그렇지 않은 것 같네만, 종종 특별한 주의사항이 있는 물품들이 있다네.");
        yield return Say(Boss, "그러니 반드시 물품 정보를 확인하고 그 주의점에 맞춰 배송 업무를 해주길 바라네.");
        yield return Say(Boss, "아, 깜빡했군. PC를 설치하면서 문도 함께 손을 좀 봤다네. 이제 옆의 기기를 조작하면 원하는 던전으로 바로 이동할 수 있을 걸세.");
        yield return Say(Boss, "이제 의뢰를 접수했으니, 배송하러 가보시게! 저 옆에 문이 있으니, 그쪽으로 가면 된다네.");
        CloseDialogue();
    }

    void OpenPC()
    {
        SetActive(pcPanel, true);
        SetActive(pcHomeView, true);
        SetActive(pcQuestView, false);
        HoldPause();
    }

    void ShowPCQuest()
    {
        SetActive(pcHomeView, false);
        SetActive(pcQuestView, true);
        if (tutorialQuest == null) return;

        if (pcQuestNameText != null) pcQuestNameText.text = tutorialQuest.questName;
        string itemName = "";
        if (tutorialQuest.requiredItems != null && tutorialQuest.requiredItems.Count > 0 && tutorialQuest.requiredItems[0].item != null)
            itemName = tutorialQuest.requiredItems[0].item.itemName;
        if (pcDetailText != null)
        {
            pcDetailText.text = "의뢰 품목  " + itemName
                + "\n의뢰 위치  " + tutorialQuest.targetLocation
                + "\n의뢰 보수  " + tutorialQuest.reward + " 골드"
                + "\n\n" + tutorialQuest.description;
        }
    }

    void ClosePC()
    {
        SetActive(pcPanel, false);
    }

    void AcceptTutorialQuest()
    {
        if (QuestManager.Instance == null || tutorialQuest == null) return;
        var inventory = player != null ? player.GetComponent<Inventory>() : null;
        QuestManager.Instance.StartQuest(tutorialQuest, inventory);

        if (!subscribedQuest)
        {
            subscribedQuest = true;
            QuestManager.Instance.OnQuestCompleted += OnQuestCompleted;
        }
    }

    // ---------- 문 (던전 선택) ----------

    // DungeonDoorTrigger에서 호출. 문을 눌렀을 때 던전 선택창을 띄우고 true를 돌려줌
    // (튜토리얼 중에는 의뢰 접수 후에만 열리고, 그 뒤로는 언제든 열림)
    public bool HandleDoorInteract(string sceneName)
    {
        if (townActive && phase != Phase.AfterPC)
        {
            ShowToast("먼저 PC에서 의뢰를 접수해야 합니다.");
            return true;
        }
        if (!doorBusy) StartCoroutine(RunDoor(sceneName));
        return true;
    }

    IEnumerator RunDoor(string sceneName)
    {
        doorBusy = true;
        bool inTutorial = townActive; // 튜토리얼 중이면 사장 대사와 안내를 곁들임
        HideKeyGuide();

        if (inTutorial && !doorIntroShown)
        {
            doorIntroShown = true;
            yield return Say(Boss, "사용 방법은 간단하네. 원하는 던전을 선택하고 입장 버튼을 누르면 끝!");
            CloseDialogue();
        }

        SelectJungle(false);
        dungeonResult = 0;
        SetActive(dungeonPanel, true);
        HoldPause();
        int openedAt = Time.frameCount;
        while (dungeonResult == 0)
        {
            yield return null;
            var kb = Keyboard.current;
            if (Time.frameCount != openedAt && kb != null && kb.escapeKey.wasPressedThisFrame) dungeonResult = 2;
        }
        SetActive(dungeonPanel, false);

        if (dungeonResult == 2)
        {
            // Esc로 닫을 때 같은 프레임에 일시정지 창이 같이 뜨지 않게 한 프레임 뒤에 정지를 풂
            yield return null;
            ReleasePause();
            doorBusy = false;
            if (inTutorial)
                ShowKeyGuide("", new[] { new KeyRow(new[] { "F" }, "문 앞에서 F키를 눌러 던전을 선택할 수 있습니다.") }, false);
            yield break;
        }

        if (inTutorial)
        {
            yield return Say(Boss, "그럼 다녀오시게나~");
            CloseDialogue();
        }
        ReleasePause();

        if (inTutorial)
        {
            townActive = false;
            phase = Phase.Dungeon; // 던전에 들어가면 조작 안내가 뜸
        }
        doorBusy = false;
        SceneManager.LoadScene(sceneName);
    }

    void SelectJungle(bool on)
    {
        jungleSelected = on;
        if (dungeonJungleImage != null) dungeonJungleImage.color = on ? JungleSelectedColor : JungleIdleColor;
    }

    // ---------- 정글 던전 ----------

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (phase == Phase.Dungeon && scene.name != townSceneName)
        {
            StartCoroutine(RunDungeonGuide());
        }
        else if (returnToTown && scene.name == townSceneName)
        {
            returnToTown = false;
            var p = FindFirstObjectByType<PlayerController>();
            var door = FindFirstObjectByType<DungeonDoorTrigger>();
            if (p != null && door != null)
            {
                Vector3 pos = p.transform.position;
                pos.x = door.transform.position.x - 0.8f;
                p.transform.position = pos;
                var body = p.GetComponent<Rigidbody2D>();
                if (body != null) body.linearVelocity = Vector2.zero;
            }
        }
    }

    IEnumerator RunDungeonGuide()
    {
        phase = Phase.Dungeon;
        yield return new WaitForSecondsRealtime(0.6f);

        ShowKeyGuide("조작 안내", new[]
        {
            new KeyRow(new[] { "J" }, "또는 마우스 좌클릭 : 공격"),
            new KeyRow(new[] { "2" }, "인벤토리"),
            new KeyRow(new[] { "E" }, "또는 Tab : 태블릿 메뉴"),
            new KeyRow(new[] { "Esc" }, "일시정지"),
        }, true);

        HoldPause();
        keyGuideConfirmed = false;
        while (!keyGuideConfirmed) yield return null;
        HideKeyGuide();
        ReleasePause();
    }

    void OnQuestCompleted(QuestData done)
    {
        if (done == tutorialQuest && !completed) StartCoroutine(RunCall());
    }

    IEnumerator RunCall()
    {
        // 납품 대사창 → 배송 완료 창이 모두 닫힐 때까지 기다림 (두 창 모두 GamePause를 씀)
        yield return null;
        yield return null;
        while (GamePause.IsPaused) yield return null;
        yield return new WaitForSecondsRealtime(0.3f);

        yield return Say(Me, "(사장에게 전화) 배송 완료했습니다.");
        yield return Say(Boss, "수고했네.");
        yield return Say(Boss, "아, 혹시 지금 바로 사무실로 와줄 수 있는가?");

        choiceResult = 0;
        SetActive(choicePanel, true);
        while (choiceResult == 0) yield return null;
        SetActive(choicePanel, false);

        completed = true;
        phase = Phase.Done;

        if (choiceResult == 1)
        {
            yield return Say(Boss, "기다리고 있겠네!");
            CloseDialogue();
            returnToTown = true;
            SceneManager.LoadScene(townSceneName);
        }
        else
        {
            yield return Say(Boss, "알겠네…");
            yield return Say(Boss, "그래도 최대한 빨리 와주게나…");
            CloseDialogue();
        }
    }

    // ---------- 대사창 ----------

    IEnumerator Say(string speaker, string line, bool waitForAdvance = true)
    {
        if (dialoguePanel == null) yield break;

        if (speakerText != null) speakerText.text = speaker;
        if (lineText != null) lineText.text = line;
        SetActive(nextHint, waitForAdvance);
        dialoguePanel.SetActive(true);
        HoldPause();

        advanceClicked = false;
        shownFrame = Time.frameCount;
        if (!waitForAdvance) yield break;

        while (true)
        {
            yield return null;
            // 이 줄을 띄운 프레임의 입력(앞 줄을 넘긴 F 등)은 무시
            if (Time.frameCount == shownFrame) continue;
            if (advanceClicked || AdvanceKeyPressed()) break;
        }
        advanceClicked = false;
    }

    static bool AdvanceKeyPressed()
    {
        var kb = Keyboard.current;
        return kb != null && (kb.fKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame
            || kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame);
    }

    void CloseDialogue()
    {
        SetActive(dialoguePanel, false);
        ReleasePause();
    }

    // ---------- 키 안내 ----------

    private struct KeyRow
    {
        public string[] keys;
        public string label;
        public KeyRow(string[] keys, string label) { this.keys = keys; this.label = label; }
    }

    void ShowKeyGuide(string title, KeyRow[] rows, bool withConfirm)
    {
        if (keyGuidePanel == null || keyGuideRows == null || keyGuideRowTemplate == null) return;

        for (int i = keyGuideRows.childCount - 1; i >= 0; i--)
        {
            var child = keyGuideRows.GetChild(i).gameObject;
            if (child != keyGuideRowTemplate) Destroy(child);
        }

        if (keyGuideTitle != null)
        {
            keyGuideTitle.text = title;
            keyGuideTitle.gameObject.SetActive(!string.IsNullOrEmpty(title));
        }

        foreach (var r in rows)
        {
            var row = Instantiate(keyGuideRowTemplate, keyGuideRows);
            row.SetActive(true);

            var keysParent = row.transform.Find("Keys");
            var label = row.transform.Find("Label");
            if (label != null) label.GetComponent<TMP_Text>().text = r.label;

            if (keysParent != null && keyCapTemplate != null)
            {
                foreach (var k in r.keys)
                {
                    var cap = Instantiate(keyCapTemplate, keysParent);
                    cap.SetActive(true);
                    var capLabel = cap.transform.Find("Label");
                    if (capLabel != null) capLabel.GetComponent<TMP_Text>().text = k;
                    // 글자 수가 긴 키(Esc, Tab)는 키캡을 더 넓게
                    var le = cap.GetComponent<LayoutElement>();
                    if (le != null && k.Length > 1) le.preferredWidth = 40f + 22f * k.Length;
                }
            }
        }

        SetActive(keyGuideConfirmButton != null ? keyGuideConfirmButton.gameObject : null, withConfirm);
        keyGuidePanel.SetActive(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate(keyGuideRows);
    }

    void HideKeyGuide()
    {
        SetActive(keyGuidePanel, false);
    }

    // ---------- 토스트 ----------

    void ShowToast(string message, float seconds = 2.5f)
    {
        if (toastPanel == null) return;
        if (toastText != null) toastText.text = message;
        toastPanel.SetActive(true);
        if (toastRoutine != null) StopCoroutine(toastRoutine);
        toastRoutine = StartCoroutine(HideToastAfter(seconds));
    }

    IEnumerator HideToastAfter(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        toastRoutine = null;
        SetActive(toastPanel, false);
    }

    // ---------- 공통 ----------

    void LockInput(bool on)
    {
        scriptedLock = on;
        if (player == null) return;
        player.InputLocked = on;
        player.ScriptedMove = 0f;
    }

    void HoldPause()
    {
        if (holdingPause) return;
        holdingPause = true;
        GamePause.Push();
    }

    void ReleasePause()
    {
        if (!holdingPause) return;
        holdingPause = false;
        GamePause.Pop();
    }
}
