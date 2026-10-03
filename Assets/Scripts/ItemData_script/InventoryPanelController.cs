using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 태블릿 메뉴 '가방' 탭. 플레이어(Player 태그)의 Inventory를 그대로 보여줍니다.
public class InventoryPanelController : MonoBehaviour
{
    [Header("슬롯 그리드")]
    public Transform slotGridParent;
    public InventorySlotUI slotPrefab;
    public int columns = 5;
    public int rows = 4;

    [Header("카테고리 탭 (0: 배달품, 1: 소모품)")]
    public Button[] tabButtons;
    public GameObject[] tabUnderlines;

    [Header("재화")]
    public TMP_Text coinText;

    [Header("상세 정보")]
    public GameObject detailRoot;
    public GameObject emptyDetailText;
    public TMP_Text categoryLabel;
    public TMP_Text nameText;
    public Image iconImage;
    public TMP_Text iconFallbackText;
    public GameObject qualityRow;
    public RectTransform qualityFill;
    public TMP_Text qualityText;
    public TMP_Text traitText;
    public TMP_Text descriptionText;

    private static readonly string[] TabNames = { "배달품", "소모품" };

    private Inventory inventory;
    private PlayerWallet wallet;
    private readonly List<InventorySlotUI> slots = new List<InventorySlotUI>();
    private int currentTab = 0;
    private ItemData selectedItem;

    void OnEnable()
    {
        currentTab = 0; // 열 때마다 배달품 탭부터
        selectedItem = null;
        FindInventory();
        Refresh();
    }

    void OnDisable()
    {
        if (inventory != null) inventory.OnInventoryChanged -= Refresh;
        if (wallet != null) wallet.OnGoldChanged -= OnGoldChanged;
        inventory = null;
        wallet = null;
    }

    void FindInventory()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        inventory = player != null ? player.GetComponent<Inventory>() : null;
        wallet = player != null ? player.GetComponent<PlayerWallet>() : null;
        if (inventory != null) inventory.OnInventoryChanged += Refresh;
        if (wallet != null) wallet.OnGoldChanged += OnGoldChanged;
    }

    void OnGoldChanged(int gold)
    {
        coinText.text = gold.ToString("N0");
    }

    // 현재는 모든 아이템이 배달품입니다. 소모품 데이터가 생기면 여기서 분류하세요.
    static bool BelongsToTab(ItemData item, int tab) => tab == 0;

    public void SelectTab(int tab)
    {
        currentTab = tab;
        selectedItem = null;
        Refresh();
    }

    void Refresh()
    {
        var shown = new List<ItemData>();
        if (inventory != null)
        {
            foreach (var item in inventory.Items)
            {
                if (BelongsToTab(item, currentTab)) shown.Add(item);
            }
        }

        // 선택한 아이템이 사라졌으면(퀘스트 완료 등) 첫 번째 아이템을 선택
        if (selectedItem == null || !shown.Contains(selectedItem))
            selectedItem = shown.Count > 0 ? shown[0] : null;

        int capacity = inventory != null ? inventory.maxSlots : 0;
        int total = Mathf.Max(columns * rows, capacity);
        BuildSlots(total);

        bool selectedMarked = false;
        for (int i = 0; i < slots.Count; i++)
        {
            ItemData item = i < shown.Count ? shown[i] : null;
            bool locked = i >= capacity; // 가방 용량 밖의 칸은 잠김
            slots[i].Setup(item, locked, this);

            if (item != null && !selectedMarked && item == selectedItem)
            {
                slots[i].SetSelected(true);
                selectedMarked = true;
            }
        }

        for (int i = 0; i < tabUnderlines.Length; i++)
            tabUnderlines[i].SetActive(i == currentTab);

        coinText.text = (wallet != null ? wallet.Gold : 0).ToString("N0");
        ShowDetail(selectedItem);
    }

    void BuildSlots(int count)
    {
        while (slots.Count < count)
            slots.Add(Instantiate(slotPrefab, slotGridParent));
        for (int i = 0; i < slots.Count; i++)
            slots[i].gameObject.SetActive(i < count);
    }

    public void Select(InventorySlotUI slot)
    {
        selectedItem = slot.Item;
        foreach (var s in slots) s.SetSelected(s == slot);
        ShowDetail(selectedItem);
    }

    void ShowDetail(ItemData item)
    {
        bool has = item != null;
        detailRoot.SetActive(has);
        emptyDetailText.SetActive(!has);
        if (!has) return;

        categoryLabel.text = TabNames[currentTab];
        nameText.text = item.itemName;

        bool hasIcon = item.icon != null;
        iconImage.enabled = hasIcon;
        iconImage.sprite = hasIcon ? item.icon : null;
        iconFallbackText.gameObject.SetActive(!hasIcon);
        iconFallbackText.text = item.itemName;

        // 품질 값이 설정된 아이템만 게이지 표시 (미설정 0은 '품질 0%'로 오해되므로 숨김)
        bool showQuality = item.quality > 0;
        qualityRow.SetActive(showQuality);
        if (showQuality)
        {
            qualityFill.anchorMax = new Vector2(item.quality / 100f, 1f);
            qualityText.text = item.quality + "%";
        }

        traitText.text = ItemData.GetTraitLabel(item.itemType);
        descriptionText.text = string.IsNullOrEmpty(item.description) ? "설명이 없습니다." : item.description;
    }
}
