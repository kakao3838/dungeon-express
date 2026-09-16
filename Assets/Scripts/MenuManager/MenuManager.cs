using UnityEngine;
using UnityEngine.InputSystem;

public enum MenuTab
{
    None,       // ← 추가: 아무 탭도 선택 안 된 "선택 대기" 상태
    Equipment,
    Inventory,
    Quest,
    Map,
    Codex
}

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance { get; private set; }

    [Header("Menu Root")]
    [SerializeField] private GameObject menuRoot;

    [Header("Panels")]
    [SerializeField] private GameObject equipmentPanel;
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private GameObject questPanel;
    [SerializeField] private GameObject mapPanel;
    [SerializeField] private GameObject codexPanel;

    public bool IsOpen { get; private set; }

    private MenuTab currentTab = MenuTab.None;   // 지금 실제로 떠 있는 탭
    private MenuTab lastRealTab = MenuTab.Equipment; // Tab 키로 열 때 돌아갈 탭

    private void Awake()
    {
        Instance = this;
        IsOpen = false;
        menuRoot.SetActive(false);
    }

    private void Update()
    {
        // E : 태블릿을 "선택 대기 상태"로 열기 / 닫기
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (IsOpen)
                Close();
            else
                OpenSelection();
        }

        // Tab : 마지막으로 보던 탭 그대로 열기 / 닫기
        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            if (IsOpen)
                Close();
            else
                Open(lastRealTab);
        }

        // 1 : 장비
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
            ToggleTab(MenuTab.Equipment);

        // 2 : 가방
        if (Keyboard.current.digit2Key.wasPressedThisFrame)
            ToggleTab(MenuTab.Inventory);

        // 3 : 의뢰
        if (Keyboard.current.digit3Key.wasPressedThisFrame)
            ToggleTab(MenuTab.Quest);

        // 4 : 지도
        if (Keyboard.current.digit4Key.wasPressedThisFrame)
            ToggleTab(MenuTab.Map);

        // 5 : 도감
        if (Keyboard.current.digit5Key.wasPressedThisFrame)
            ToggleTab(MenuTab.Codex);

        // M : 지도
        if (Keyboard.current.mKey.wasPressedThisFrame)
            ToggleTab(MenuTab.Map);
    }

    private void ToggleTab(MenuTab tab)
    {
        // 이미 그 탭이 열려있는 상태에서 같은 키를 또 누르면 닫기
        if (IsOpen && currentTab == tab)
            Close();
        else
            Open(tab);
    }

    // 실제 콘텐츠가 있는 탭을 여는 함수 (숫자키, Tab, UI 버튼용)
    public void Open(MenuTab tab)
    {
        IsOpen = true;
        currentTab = tab;
        lastRealTab = tab; // "다음 Tab키에 돌아갈 탭"도 갱신
        menuRoot.SetActive(true);
        ShowOnly(tab);
    }

    // E키 전용: 선택 대기 상태로 열기 (lastRealTab은 건드리지 않음)
    public void OpenSelection()
    {
        IsOpen = true;
        currentTab = MenuTab.None;
        menuRoot.SetActive(true);
        ShowOnly(MenuTab.None);
    }

    public void Close()
    {
        IsOpen = false;
        currentTab = MenuTab.None;
        menuRoot.SetActive(false);
    }

    private void ShowOnly(MenuTab tab)
    {
        equipmentPanel.SetActive(tab == MenuTab.Equipment);
        inventoryPanel.SetActive(tab == MenuTab.Inventory);
        questPanel.SetActive(tab == MenuTab.Quest);
        mapPanel.SetActive(tab == MenuTab.Map);
        codexPanel.SetActive(tab == MenuTab.Codex);
        // tab이 None이면 위 조건이 전부 false가 되어 자동으로 다 꺼짐
    }

    // UI 버튼용 함수 (Sidebar의 5개 버튼 OnClick에 연결)
    public void OpenEquipment() => Open(MenuTab.Equipment);
    public void OpenInventory() => Open(MenuTab.Inventory);
    public void OpenQuest() => Open(MenuTab.Quest);
    public void OpenMap() => Open(MenuTab.Map);
    public void OpenCodex() => Open(MenuTab.Codex);
}