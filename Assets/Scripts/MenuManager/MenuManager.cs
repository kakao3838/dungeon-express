using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;

public enum MenuTab
{
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

    private MenuTab lastTab = MenuTab.Equipment;

    private void Awake()
    {
        Instance = this;

        IsOpen = false;
        menuRoot.SetActive(false);
    }

    private void ToggleTab(MenuTab tab)
    {
        if (IsOpen && lastTab == tab)
        {
            Close();
        }
        else
        {
            Open(tab);
        }
    }

    private void Update()
    {
        // Tab : 메뉴 열기 / 닫기
        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            if (IsOpen)
                Close();
            else
                Open(lastTab);
        }

        // 1 : 장비
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            ToggleTab(MenuTab.Equipment);
        }

        // 2 : 가방
        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            ToggleTab(MenuTab.Inventory);
        }

        // 3 : 의뢰
        if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            ToggleTab(MenuTab.Quest);
        }

        // 4 : 지도
        if (Keyboard.current.digit4Key.wasPressedThisFrame)
        {
            ToggleTab(MenuTab.Map);
        }

        // 5 : 도감
        if (Keyboard.current.digit5Key.wasPressedThisFrame)
        {
            ToggleTab(MenuTab.Codex);
        }

        // M : 지도
        if (Keyboard.current.mKey.wasPressedThisFrame)
        {
            ToggleTab(MenuTab.Map);
        }
    }

    public void Open(MenuTab tab)
    {
        IsOpen = true;
        lastTab = tab;

        menuRoot.SetActive(true);

        ShowOnly(tab);
    }

    public void Close()
    {
        IsOpen = false;
        menuRoot.SetActive(false);
    }

    private void ShowOnly(MenuTab tab)
    {
        equipmentPanel.SetActive(tab == MenuTab.Equipment);
        inventoryPanel.SetActive(tab == MenuTab.Inventory);
        questPanel.SetActive(tab == MenuTab.Quest);
        mapPanel.SetActive(tab == MenuTab.Map);
        codexPanel.SetActive(tab == MenuTab.Codex);
    }

    // UI 버튼용 함수

    public void OpenEquipment()
    {
        Open(MenuTab.Equipment);
    }

    public void OpenInventory()
    {
        Open(MenuTab.Inventory);
    }

    public void OpenQuest()
    {
        Open(MenuTab.Quest);
    }

    public void OpenMap()
    {
        Open(MenuTab.Map);
    }

    public void OpenCodex()
    {
        Open(MenuTab.Codex);
    }

    
}