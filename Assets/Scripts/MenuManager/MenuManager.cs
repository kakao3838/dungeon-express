using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum MenuTab
{
    Equipment,
    Inventory,
    Quest,
    Map,
    Codex
}

// 태블릿 메뉴(장비/가방/의뢰/지도/도감) 관리자.
// - Resources/TabletMenu 프리팹을 첫 씬 로드 시 자동 생성하고 DontDestroyOnLoad로 유지하므로
//   타운/던전 등 어느 씬에도 따로 배치할 필요가 없습니다.
// - 씬에 직접 배치된 MenuManager(예: MenuTest)가 있으면 자동 생성하지 않습니다.
public class MenuManager : MonoBehaviour
{
    private const string PrefabResourcePath = "TabletMenu";

    public static MenuManager Instance { get; private set; }

    // 플레이어 입력 스크립트가 메뉴가 열려 있을 때 입력을 무시하도록 확인하는 용도
    public static bool IsMenuOpen => Instance != null && Instance.IsOpen;

    [Header("Menu Root")]
    [SerializeField] private GameObject menuRoot;

    [Header("Panels")]
    [SerializeField] private GameObject equipmentPanel;
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private GameObject questPanel;
    [SerializeField] private GameObject mapPanel;
    [SerializeField] private GameObject codexPanel;

    [Header("Sidebar 선택 표시 (MenuTab 순서: 장비, 가방, 의뢰, 지도, 도감)")]
    [SerializeField] private GameObject[] tabHighlights;

    [Header("메뉴를 열 수 없는 씬")]
    [SerializeField] private string[] blockedScenes = { "BootScene", "TitleScene" };

    public bool IsOpen { get; private set; }
    public MenuTab CurrentTab { get; private set; } = MenuTab.Inventory;

    private bool holdingPause; // GamePause를 내가 잡고 있는지 (대화창/결과창 등과 같은 정지 처리를 공유)

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        if (FindFirstObjectByType<MenuManager>(FindObjectsInactive.Include) != null) return;

        var prefab = Resources.Load<GameObject>(PrefabResourcePath);
        if (prefab == null)
        {
            Debug.LogWarning("[MenuManager] Resources/" + PrefabResourcePath + " 프리팹을 찾을 수 없습니다.");
            return;
        }
        Instantiate(prefab);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (transform.parent == null) DontDestroyOnLoad(gameObject);

        IsOpen = false;
        menuRoot.SetActive(false);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance != this) return;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        ReleasePause();
        Instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 씬이 바뀌면 이전 씬의 플레이어/가방을 보고 있을 수 있으므로 항상 닫음
        if (IsOpen) Close();
    }

    private bool IsBlockedScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        foreach (string blocked in blockedScenes)
        {
            if (sceneName == blocked) return true;
        }
        return false;
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (IsBlockedScene())
        {
            if (IsOpen) Close();
            return;
        }

        // E / Tab : 마지막으로 보던 탭으로 열기 / 닫기 (Esc는 PauseMenu가 쓰므로 여기서는 처리하지 않음)
        if (keyboard.eKey.wasPressedThisFrame || keyboard.tabKey.wasPressedThisFrame)
        {
            if (IsOpen) Close();
            else Open(CurrentTab);
        }

        // 1~5, M : 해당 탭으로 바로 열기 (이미 그 탭이면 닫기)
        if (keyboard.digit1Key.wasPressedThisFrame) ToggleTab(MenuTab.Equipment);
        if (keyboard.digit2Key.wasPressedThisFrame) ToggleTab(MenuTab.Inventory);
        if (keyboard.digit3Key.wasPressedThisFrame) ToggleTab(MenuTab.Quest);
        if (keyboard.digit4Key.wasPressedThisFrame) ToggleTab(MenuTab.Map);
        if (keyboard.digit5Key.wasPressedThisFrame) ToggleTab(MenuTab.Codex);
        if (keyboard.mKey.wasPressedThisFrame) ToggleTab(MenuTab.Map);
    }

    // NPC 대화창은 GamePause를 쓰지 않아서 따로 확인
    private static bool IsNpcDialogueOpen()
    {
        return NPCDialogueMenu.Instance != null && NPCDialogueMenu.Instance.IsOpen;
    }

    private void ToggleTab(MenuTab tab)
    {
        if (IsOpen && CurrentTab == tab) Close();
        else Open(tab);
    }

    // 단축키, 사이드바 버튼에서 호출
    public void Open(MenuTab tab)
    {
        if (IsBlockedScene()) return;
        // 대화창, 결과창, 일시정지 창 등 다른 창이 게임을 멈추고 있으면 열지 않음
        if (!IsOpen && (GamePause.IsPaused || IsNpcDialogueOpen())) return;

        EnsureEventSystem();

        if (!holdingPause)
        {
            holdingPause = true;
            GamePause.Push(); // 메뉴를 보는 동안 게임 일시정지 + 플레이어 조작 비활성화
        }

        IsOpen = true;
        CurrentTab = tab;
        menuRoot.SetActive(true);
        ShowOnly(tab);
    }

    public void Close()
    {
        if (!IsOpen) return;

        IsOpen = false;
        menuRoot.SetActive(false);
        ReleasePause();
    }

    private void ReleasePause()
    {
        if (!holdingPause) return;
        holdingPause = false;
        GamePause.Pop();
    }

    private void ShowOnly(MenuTab tab)
    {
        equipmentPanel.SetActive(tab == MenuTab.Equipment);
        inventoryPanel.SetActive(tab == MenuTab.Inventory);
        questPanel.SetActive(tab == MenuTab.Quest);
        mapPanel.SetActive(tab == MenuTab.Map);
        codexPanel.SetActive(tab == MenuTab.Codex);

        if (tabHighlights == null) return;
        for (int i = 0; i < tabHighlights.Length; i++)
        {
            if (tabHighlights[i] != null) tabHighlights[i].SetActive(i == (int)tab);
        }
    }

    // 씬에 EventSystem이 없으면(예: 정글 던전) 버튼 클릭이 안 되므로 하나 만들어 둠.
    // DontDestroyOnLoad를 걸지 않아서 씬이 바뀌면 사라지고, 다음 씬의 EventSystem과 겹치지 않음
    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;

        var go = new GameObject("EventSystem (TabletMenu)");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
    }

    // 사이드바 버튼 OnClick 연결용
    public void OpenEquipment() => Open(MenuTab.Equipment);
    public void OpenInventory() => Open(MenuTab.Inventory);
    public void OpenQuest() => Open(MenuTab.Quest);
    public void OpenMap() => Open(MenuTab.Map);
    public void OpenCodex() => Open(MenuTab.Codex);
}
