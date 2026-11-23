using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Reuses Resources/SaveSlotPanel, extracted from TitleScene's original slot UI.
public sealed class SaveSlotWindow : MonoBehaviour
{
    private static SaveSlotWindow instance;
    private static int closedFrame = -1;
    public static bool IsShowing => instance != null && instance.IsOpen;
    // The closing click/Esc must not also attack, interact or open PauseMenu.
    public static bool BlocksGameplayInput => IsShowing || SaveFlowCoordinator.IsBusy || closedFrame == Time.frameCount;
    public bool IsOpen => panel != null && panel.activeSelf;

    private GameObject panel;
    private RectTransform windowRect;
    private Image backdrop, windowBackground;
    private Color backdropColor, windowColor;
    private TMP_Text heading, notice, confirmationText;
    private Button confirmButton, backButton, yesButton, noButton;
    private Button skipSaveButton;
    private GameObject slotGroup;
    private GameObject confirmation;
    private readonly Button[] rows = new Button[3];
    private readonly TMP_Text[] labels = new TMP_Text[3];
    private readonly SaveSlotInfo[] slots = new SaveSlotInfo[3];
    private bool saveMode, holdingPause, busy;
    private bool newGameMode, saveConfirmationMode, dungeonEntryMode;
    private int selected;
    private Action pendingConfirmation, onClosed;
    private Action<int> newGameSelected;
    private Action<bool> entryFinished;
    private DungeonEntrySession entrySession;
    private int openedFrame;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { instance = null; closedFrame = -1; }

    public static SaveSlotWindow OpenSave(Action closed = null, bool allowEditorTestScene = false,
        string officeSceneName = "TownScene", bool allowOutsideOffice = false)
    {
        if (!allowOutsideOffice && !SaveScenePolicy.IsAllowed(SceneManager.GetActiveScene().name, officeSceneName,
            Application.isEditor, allowEditorTestScene))
            throw new InvalidOperationException("Saving is only allowed in the office.");
        SaveSlotWindow window = Open(true, closed);
        window.ConfigureCurrentProfileSave();
        return window;
    }
    public static SaveSlotWindow OpenLoad(Action closed = null, GameObject hostPanel = null) => Open(false, closed, hostPanel);

    // New games can only be created in an empty profile. Existing profiles remain available to Continue.
    public static SaveSlotWindow OpenNewGame(Action<int> selected, Action closed = null)
    {
        if (selected == null) throw new ArgumentNullException(nameof(selected));
        SaveSlotWindow window = Open(false, closed);
        window.newGameMode = true;
        window.saveMode = true;
        window.newGameSelected = selected;
        window.saveConfirmationMode = false;
        window.dungeonEntryMode = false;
        window.slotGroup.SetActive(true);
        window.skipSaveButton.gameObject.SetActive(false);
        window.heading.text = "\uC0C8 \uAC8C\uC784 \uD504\uB85C\uD544 \uC120\uD0DD";
        window.confirmButton.GetComponentInChildren<TMP_Text>().text = "\uC2DC\uC791";
        window.notice.text = "\uAE30\uC874 \uD504\uB85C\uD544\uC744 \uC120\uD0DD\uD558\uBA74 \uB36E\uC5B4\uC501\uB2C8\uB2E4.";
        window.Refresh();
        return window;
    }

    public static SaveSlotWindow OpenDungeonEntry(Action<bool> completed, bool allowEditorTestScene)
    {
        if (IsShowing) throw new InvalidOperationException("Another slot window is already open.");
        if (!SaveScenePolicy.IsAllowed(SceneManager.GetActiveScene().name, "TownScene",
            Application.isEditor, allowEditorTestScene))
            throw new InvalidOperationException("Dungeon entry saving is only allowed from the office.");
        SaveSlotWindow window = Open(true, null);
        window.entryFinished = completed;
        window.entrySession = new DungeonEntrySession();
        window.newGameMode = false;
        window.saveConfirmationMode = false;
        window.dungeonEntryMode = true;
        window.slotGroup.SetActive(false);
        window.heading.text = "세이브 하시겠습니까?";
        window.confirmButton.GetComponentInChildren<TMP_Text>().text = "예";
        window.confirmButton.interactable = true;
        window.skipSaveButton.gameObject.SetActive(true);
        window.ApplyLayout(true);
        window.backButton.GetComponentInChildren<TMP_Text>().text = "취소";
        return window;
    }

    public static void ShowLoadProgress()
    {
        if (instance == null || !instance.IsOpen) return;
        instance.busy = true;
        instance.notice.text = "Loading...";
        foreach (Button row in instance.rows) row.interactable = false;
        instance.confirmButton.interactable = false;
        instance.backButton.interactable = false;
    }

    public static void CompleteLoad(bool usedBackup)
    {
        if (instance != null && instance.IsOpen)
            instance.StartCoroutine(instance.ShowSuccessAndClose(usedBackup ? "백업에서 불러오기 완료" : "불러오기 완료"));
    }

    public static void ShowLoadError(Action closed)
    {
        if (IsShowing) instance.Close();
        SaveSlotWindow window = OpenLoad(closed);
        window.notice.text = "불러오지 못했습니다. 슬롯을 다시 선택하거나 닫고 타이틀로 돌아가 주세요.";
    }

    private static SaveSlotWindow Open(bool saving, Action closed, GameObject hostPanel = null)
    {
        if (instance == null)
        {
            var root = new GameObject("SaveSlotWindow", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            DontDestroyOnLoad(root);
            instance = root.AddComponent<SaveSlotWindow>();
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            root.GetComponent<Canvas>().sortingOrder = 32000;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }

        // The title already has an editable ContinuePanel shell. Bind the slot
        // controller to that hierarchy instead of creating a second visible UI.
        // Other callers keep using the shared Resources prefab.
        if (hostPanel != null && instance.panel != hostPanel)
        {
            if (instance.IsOpen) return instance;
            if (instance.panel != null) Destroy(instance.panel);
            instance.Build(hostPanel);
        }
        else if (instance.panel == null)
        {
            GameObject prefab = Resources.Load<GameObject>("SaveSlotPanel");
            if (prefab == null) throw new InvalidOperationException("Resources/SaveSlotPanel is missing.");
            instance.Build(Instantiate(prefab, instance.transform));
        }

        if (instance.IsOpen) return instance;
        EnsureEventSystem();
        instance.Show(saving, closed);
        return instance;
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        var events = new GameObject("SaveSlotEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        // Scene-local: the next scene supplies its own EventSystem.
    }

    private void Awake() { SceneManager.sceneLoaded += OnSceneLoaded; }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // The old scene's title/menu objects may already have been destroyed.
        onClosed = null;
        // A title-hosted panel is destroyed when starting a new game without
        // loading. Recreate the reusable Resources panel for later save screens.
        if (panel == null)
        {
            GameObject prefab = Resources.Load<GameObject>("SaveSlotPanel");
            if (prefab != null) Build(Instantiate(prefab, transform));
        }
        if (IsOpen && !SaveFlowCoordinator.IsLoading) Close();
    }
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        ReleasePause();
        if (instance == this) instance = null;
    }
    private void OnDisable() { if (IsOpen) Close(); else ReleasePause(); }

    private void Show(bool saving, Action closed)
    {
        saveMode = saving;
        newGameMode = false;
        saveConfirmationMode = false;
        dungeonEntryMode = false;
        newGameSelected = null;
        entrySession = null;
        entryFinished = null;
        slotGroup.SetActive(true);
        skipSaveButton.gameObject.SetActive(false);
        backButton.GetComponentInChildren<TMP_Text>().text = "닫기";
        ApplyLayout(false);
        selected = 0;
        busy = false;
        onClosed = closed;
        openedFrame = Time.frameCount;
        panel.SetActive(true);
        confirmation.SetActive(false);
        backButton.interactable = true;
        heading.text = saving ? "저장 슬롯 선택" : "불러올 슬롯 선택";
        notice.text = "";
        confirmButton.GetComponentInChildren<TMP_Text>().text = saving ? "저장" : "불러오기";
        if (!holdingPause) { holdingPause = true; GamePause.Push(); }
        var player = FindFirstObjectByType<PlayerController>();
        if (player != null) player.StopForMenu();
        Refresh();
        EventSystem.current?.SetSelectedGameObject(backButton.gameObject);
    }

    private void Update()
    {
        if (!IsOpen || busy || Time.frameCount == openedFrame) return;
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            if (confirmation.activeSelf) CancelConfirmation();
            else Close();
        }
    }

    public void Close()
    {
        if (!IsOpen) return;
        StopAllCoroutines();
        panel.SetActive(false);
        confirmation.SetActive(false);
        pendingConfirmation = null;
        busy = false;
        closedFrame = Time.frameCount;
        ReleasePause();
        Action<bool> entryCallback = entryFinished;
        entryFinished = null;
        entrySession?.Cancel();
        bool allowed = entrySession != null && entrySession.CanEnter;
        entrySession = null;
        Action callback = onClosed;
        onClosed = null;
        callback?.Invoke();
        entryCallback?.Invoke(allowed);
    }

    private void ReleasePause()
    {
        if (!holdingPause) return;
        holdingPause = false;
        GamePause.Pop();
    }

    private void Refresh()
    {
        for (int i = 0; i < 3; i++)
        {
            slots[i] = SaveSlotInfo.Read(SaveGameService.Files, i + 1);
            SaveSlotInfo entry = slots[i];
            string prefix = (selected == i + 1 ? "> " : "") + "슬롯 " + (i + 1);
            if (entry.State == SaveSlotState.Empty) labels[i].text = prefix + "\n빈 슬롯";
            else if (entry.State == SaveSlotState.Unreadable) labels[i].text = prefix + "\n저장 파일을 읽을 수 없습니다";
            else labels[i].text = prefix + "  ·  " + SaveSlotDisplay.Chapter(entry.Data) +
                "\n" + SaveSlotDisplay.SavedAt(entry.Data) + "\n플레이 시간 " + SaveSlotDisplay.PlayTime(entry.Data) +
                (entry.State == SaveSlotState.Backup ? " (백업)" : "");
            rows[i].interactable = newGameMode || saveMode || entry.CanLoad;
        }
        UpdateConfirmButton();
    }

    private void Select(int slot)
    {
        if (busy || confirmation.activeSelf) return;
        selected = slot;
        notice.text = "";
        Refresh();
    }

    private void UpdateConfirmButton()
    {
        if (saveConfirmationMode || dungeonEntryMode)
        {
            confirmButton.interactable = !busy && SaveGameService.HasActiveProfile;
            return;
        }
        confirmButton.interactable = !busy && selected > 0 &&
            (newGameMode || saveMode || slots[selected - 1].CanLoad);
    }

    private void ConfirmSelection()
    {
        if (busy || confirmation.activeSelf) return;
        if (newGameMode)
        {
            if (selected < 1) return;
            int profileSlot = selected;
            if (SaveGameService.Files.Exists(profileSlot))
            {
                Ask("\uD504\uB85C\uD544 " + profileSlot + "\uC758 \uC800\uC7A5 \uB370\uC774\uD130\uB97C \uB36E\uC5B4\uC4F0\uACE0 \uC0C8 \uAC8C\uC784\uC744 \uC2DC\uC791\uD560\uAE4C\uC694?",
                    () => StartNewGameInProfile(profileSlot, true));
                return;
            }
            StartNewGameInProfile(profileSlot, false);
            return;
        }
        if (saveConfirmationMode)
        {
            SaveCurrentProfileAndClose();
            return;
        }
        if (dungeonEntryMode)
        {
            SaveCurrentProfileForDungeonEntry();
            return;
        }
        if (entrySession != null && entrySession.State == DungeonEntryState.Choosing)
        {
            entrySession.ChooseSave();
            slotGroup.SetActive(true);
            skipSaveButton.gameObject.SetActive(false);
            ApplyLayout(false);
            heading.text = "저장 슬롯 선택";
            confirmButton.GetComponentInChildren<TMP_Text>().text = "저장";
            Refresh();
            EventSystem.current?.SetSelectedGameObject(rows[0].gameObject);
            return;
        }
        if (selected == 0) return;
        int slot = selected;
        if (saveMode)
        {
            // Recheck disk, not just the information shown when the window opened.
            if (SaveGameService.Files.Exists(slot))
                Ask("슬롯 " + slot + "의 저장을 덮어쓸까요?", () => Save(slot, true));
            else Save(slot, false);
        }
        else Load(slot);
    }

    private void StartNewGameInProfile(int slot, bool overwriteExisting)
    {
        try
        {
            if (overwriteExisting) SaveGameService.Delete(slot);
            Action<int> callback = newGameSelected;
            newGameSelected = null;
            Close();
            callback?.Invoke(slot);
        }
        catch (Exception e) { ShowFailure(e, "\uD504\uB85C\uD544\uC744 \uC900\uBE44\uD558\uC9C0 \uBABB\uD588\uC2B5\uB2C8\uB2E4."); }
    }

    private void Save(int slot, bool overwrite)
    {
        try
        {
            var inventory = FindFirstObjectByType<Inventory>();
            var wallet = FindFirstObjectByType<PlayerWallet>();
            if (inventory == null || wallet == null || QuestManager.Instance == null)
                throw new InvalidOperationException("Player save components are not ready.");
            SaveGameService.Save(slot, inventory, wallet, QuestManager.Instance, overwrite);
            entrySession?.Saved(); // Only successful disk writes can authorize entry.
            selected = slot;
            Refresh();
            StartCoroutine(ShowSuccessAndClose("저장 완료"));
        }
        catch (Exception e) { ShowFailure(e, "저장하지 못했습니다. 게임 데이터와 저장 경로를 확인한 뒤 다시 시도해 주세요."); }
    }

    private void ConfigureCurrentProfileSave()
    {
        newGameMode = false;
        saveConfirmationMode = true;
        dungeonEntryMode = false;
        slotGroup.SetActive(false);
        skipSaveButton.gameObject.SetActive(false);
        heading.text = "\uD604\uC7AC \uD504\uB85C\uD544\uC5D0 \uC800\uC7A5\uD558\uC2DC\uACA0\uC2B5\uB2C8\uAE4C?";
        confirmButton.GetComponentInChildren<TMP_Text>().text = "\uC800\uC7A5";
        backButton.GetComponentInChildren<TMP_Text>().text = "\uCDE8\uC18C";
        confirmButton.interactable = SaveGameService.HasActiveProfile;
        notice.text = SaveGameService.HasActiveProfile ? "" : "\uC120\uD0DD\uB41C \uD504\uB85C\uD544\uC774 \uC5C6\uC2B5\uB2C8\uB2E4.";
        ApplyLayout(true);
    }

    private void SaveCurrentProfileAndClose()
    {
        try
        {
            SaveCurrentProfile();
            StartCoroutine(ShowSuccessAndClose("\uC800\uC7A5 \uC644\uB8CC"));
        }
        catch (Exception e) { ShowFailure(e, "\uC800\uC7A5\uD558\uC9C0 \uBABB\uD588\uC2B5\uB2C8\uB2E4. \uC800\uC7A5 \uB370\uC774\uD130\uB97C \uD655\uC778\uD574 \uC8FC\uC138\uC694."); }
    }

    private void SaveCurrentProfileForDungeonEntry()
    {
        try
        {
            SaveCurrentProfile();
            entrySession?.ChooseSave();
            entrySession?.Saved();
            Close();
        }
        catch (Exception e) { ShowFailure(e, "\uC800\uC7A5\uD558\uC9C0 \uBABB\uD588\uC2B5\uB2C8\uB2E4. \uB358\uC804\uC5D0 \uC785\uC7A5\uD560 \uC218 \uC5C6\uC2B5\uB2C8\uB2E4."); }
    }

    private static void SaveCurrentProfile()
    {
        var inventory = FindFirstObjectByType<Inventory>();
        var wallet = FindFirstObjectByType<PlayerWallet>();
        if (inventory == null || wallet == null || QuestManager.Instance == null)
            throw new InvalidOperationException("Player save components are not ready.");
        SaveGameService.SaveCurrentProfile(inventory, wallet, QuestManager.Instance);
    }

    private void Load(int slot)
    {
        try
        {
            bool backup;
            SaveData data = SaveGameService.Read(slot, out backup);
            var inventory = FindFirstObjectByType<Inventory>();
            var wallet = FindFirstObjectByType<PlayerWallet>();
            if (SceneManager.GetActiveScene().name != "TitleScene" && !SaveLoadRequest.HasPending &&
                inventory != null && wallet != null && QuestManager.Instance != null && TutorialManager.Instance != null)
            {
                SaveGameService.RestoreReadData(slot, data, inventory, wallet, QuestManager.Instance, TutorialManager.Instance);
                StartCoroutine(ShowSuccessAndClose(backup ? "백업에서 불러오기 완료" : "불러오기 완료"));
            }
            else
            {
                bool connected = SaveLoadRequest.HasHandler;
                SaveLoadRequest.Select(slot, data, backup);
                // Keep the selected slot window visible while the title scene is
                // replaced by TownScene. The persistent controller owns this canvas.
                if (SaveFlowCoordinator.IsLoading) MovePanelToPersistentCanvas();
                if (!connected && IsOpen) notice.text = "저장 데이터를 읽었습니다. 사무실 이동 연결은 아직 준비되지 않았습니다.";
            }
        }
        catch (Exception e) { ShowFailure(e, "불러오지 못했습니다. 저장 파일을 확인한 뒤 다시 시도해 주세요."); }
    }

    private void Ask(string message, Action action)
    {
        confirmationText.text = message;
        pendingConfirmation = action;
        confirmation.SetActive(true);
        foreach (Button row in rows) row.interactable = false;
        confirmButton.interactable = false;
        backButton.interactable = false;
        EventSystem.current?.SetSelectedGameObject(noButton.gameObject);
    }

    private void CancelConfirmation()
    {
        pendingConfirmation = null;
        confirmation.SetActive(false);
        backButton.interactable = true;
        Refresh();
        EventSystem.current?.SetSelectedGameObject(backButton.gameObject);
    }

    private void AcceptConfirmation()
    {
        Action action = pendingConfirmation;
        CancelConfirmation();
        action?.Invoke();
    }

    private void ShowFailure(Exception e, string message)
    {
        Debug.LogException(e);
        busy = false;
        backButton.interactable = true;
        confirmation.SetActive(false);
        pendingConfirmation = null;
        Refresh();
        notice.text = message;
        UpdateConfirmButton();
    }

    private IEnumerator ShowSuccessAndClose(string message)
    {
        busy = true;
        backButton.interactable = false;
        foreach (Button row in rows) row.interactable = false;
        UpdateConfirmButton();
        notice.text = message;
        yield return new WaitForSecondsRealtime(0.7f);
        Close();
    }

    private void Build(GameObject shell)
    {
        panel = shell;
        var panelRect = (RectTransform)panel.transform;
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;
        Transform window = panel.transform.Find("Window");
        windowRect = (RectTransform)window;
        backdrop = panel.GetComponent<Image>();
        windowBackground = window.GetComponent<Image>();
        backdropColor = backdrop.color;
        windowColor = windowBackground.color;
        confirmButton = window.Find("BottomButtons/Btn_ResetKeyboard").GetComponent<Button>();
        backButton = window.Find("BottomButtons/Btn_Back").GetComponent<Button>();
        confirmButton.onClick.AddListener(ConfirmSelection);
        backButton.onClick.AddListener(Close);
        backButton.GetComponentInChildren<TMP_Text>().text = "닫기";
        heading = panel.transform.Find("title").GetComponent<TMP_Text>();
        Place(heading.rectTransform, new Vector2(0, 260), new Vector2(1000, 60));
        heading.alignment = TextAlignmentOptions.Center;
        Transform group = window.Find("SaveSlotGroup");
        slotGroup = group.gameObject;
        skipSaveButton = CloneButton(backButton, window.Find("BottomButtons"), "SkipSave", "아니오", () =>
        {
            if (entrySession == null || entrySession.State != DungeonEntryState.Choosing || busy) return;
            entrySession.SkipSave();
            Close();
        });
        skipSaveButton.transform.SetSiblingIndex(1);
        skipSaveButton.gameObject.SetActive(false);
        Place((RectTransform)group, Vector2.zero, new Vector2(740, 330));
        Place((RectTransform)window.Find("BottomButtons"), new Vector2(0, -300), new Vector2(460, 50));
        foreach (Button button in new[] { confirmButton, backButton })
        {
            var layout = button.gameObject.GetComponent<LayoutElement>() ?? button.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 200;
            layout.preferredHeight = 50;
        }
        for (int i = 0; i < 3; i++)
        {
            int slot = i + 1;
            rows[i] = group.Find("Slot" + slot).GetComponent<Button>();
            rows[i].onClick.AddListener(() => Select(slot));
            labels[i] = rows[i].GetComponentInChildren<TMP_Text>();
            labels[i].fontSize = 24;
            labels[i].enableAutoSizing = true;
            labels[i].fontSizeMin = 18;
            labels[i].fontSizeMax = 24;
            labels[i].alignment = TextAlignmentOptions.MidlineLeft;
            labels[i].margin = new Vector4(18, 3, 18, 3);
            labels[i].raycastTarget = false;

        }
        notice = CloneText(heading, window, "Notice", new Vector2(0, -220), new Vector2(1050, 80), 24);
        BuildConfirmation(window);
        panel.SetActive(false);
    }

    private void MovePanelToPersistentCanvas()
    {
        if (panel == null || panel.transform.parent == transform) return;
        panel.transform.SetParent(transform, false);
        var rect = panel.transform as RectTransform;
        if (rect == null) return;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private void BuildConfirmation(Transform parent)
    {
        confirmation = new GameObject("Confirmation", typeof(RectTransform), typeof(Image));
        confirmation.transform.SetParent(parent, false);
        var rect = (RectTransform)confirmation.transform;
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        confirmation.GetComponent<Image>().color = new Color(0, 0, 0, 0.65f);
        var card = new GameObject("Card", typeof(RectTransform), typeof(Image));
        card.transform.SetParent(confirmation.transform, false);
        Place((RectTransform)card.transform, Vector2.zero, new Vector2(900, 320));
        card.GetComponent<Image>().color = new Color(0.95f, 0.95f, 0.95f, 1);
        confirmationText = CloneText(heading, card.transform, "Message", new Vector2(0, 55), new Vector2(820, 160), 30);
        yesButton = CloneButton(confirmButton, card.transform, "Yes", "확인", AcceptConfirmation);
        noButton = CloneButton(backButton, card.transform, "No", "취소", CancelConfirmation);
        Place((RectTransform)yesButton.transform, new Vector2(-140, -100), new Vector2(220, 50));
        Place((RectTransform)noButton.transform, new Vector2(140, -100), new Vector2(220, 50));
        confirmation.SetActive(false);
    }

    private static Button CloneButton(Button template, Transform parent, string name, string text, Action action)
    {
        Button button = Instantiate(template, parent);
        button.name = name;
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(() => action());
        button.GetComponentInChildren<TMP_Text>().text = text;
        button.interactable = true;
        return button;
    }

    private static TMP_Text CloneText(TMP_Text template, Transform parent, string name, Vector2 position, Vector2 size, int fontSize)
    {
        TMP_Text text = Instantiate(template, parent);
        text.name = name;
        Place(text.rectTransform, position, size);
        text.fontSize = fontSize;
        text.enableAutoSizing = false;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.black;
        text.raycastTarget = false;
        text.text = "";
        return text;
    }

    private static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private void ApplyLayout(bool compact)
    {
        if (compact) Place(windowRect, Vector2.zero, new Vector2(760, 280));
        else
        {
            windowRect.anchorMin = Vector2.zero;
            windowRect.anchorMax = Vector2.one;
            windowRect.offsetMin = windowRect.offsetMax = Vector2.zero;
        }
        backdrop.color = compact ? new Color(0f, 0f, 0f, 0.55f) : backdropColor;
        windowBackground.color = compact ? new Color(0.95f, 0.95f, 0.95f, 1f) : windowColor;
        heading.fontSize = compact ? 34 : 40;
        Place(heading.rectTransform, new Vector2(0, compact ? 55 : 260),
            new Vector2(compact ? 700 : 1000, 60));
        Place((RectTransform)confirmButton.transform.parent,
            new Vector2(0, compact ? -65 : -300), new Vector2(compact ? 680 : 460, 50));
        foreach (Button button in new[] { confirmButton, backButton, skipSaveButton })
        {
            var layout = button.GetComponent<LayoutElement>();
            if (layout != null) layout.preferredWidth = compact ? 180 : 200;
        }
    }
}
