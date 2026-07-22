using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [Header("연결")]
    public Inventory inventory;
    public GameObject inventoryPanel; // E키로 켜고 끌 패널 전체 (평소엔 꺼져있음)
    public Image slotPrefab;          // 슬롯 하나짜리 프리팹 (Image + 자식 Text)
    public Transform slotsContainer;  // Horizontal/Grid Layout Group이 붙은 부모

    private readonly List<Image> slotIcons = new List<Image>();
    private bool isOpen = false;

    void OnEnable()
    {
        if (inventory != null)
        {
            inventory.OnInventoryChanged += RefreshUI;
        }
    }

    void OnDisable()
    {
        if (inventory != null)
        {
            inventory.OnInventoryChanged -= RefreshUI;
        }
    }

    void Start()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
        }
        BuildSlots();
        RefreshUI();
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.eKey.wasPressedThisFrame)
        {
            isOpen = !isOpen;
            if (inventoryPanel != null)
            {
                inventoryPanel.SetActive(isOpen);
            }
        }
    }

    void BuildSlots()
    {
        if (inventory == null || slotPrefab == null || slotsContainer == null) return;

        foreach (Transform child in slotsContainer)
        {
            Destroy(child.gameObject);
        }
        slotIcons.Clear();

        for (int i = 0; i < inventory.maxSlots; i++)
        {
            Image slot = Instantiate(slotPrefab, slotsContainer);
            slotIcons.Add(slot);
        }
    }

    void RefreshUI()
    {
        if (inventory == null) return;

        var items = inventory.Items;

        for (int i = 0; i < slotIcons.Count; i++)
        {
            Text label = slotIcons[i].GetComponentInChildren<Text>();

            if (i < items.Count)
            {
                ItemData item = items[i];

                if (item.icon != null)
                {
                    slotIcons[i].sprite = item.icon;
                    slotIcons[i].color = Color.white;
                }
                else
                {
                    // 아이콘 없을 때 임시로 연두색 표시
                    slotIcons[i].color = new Color(0.6f, 0.85f, 0.6f);
                }

                if (label != null)
                {
                    label.text = item.itemName;
                }
            }
            else
            {
                // 빈 슬롯
                slotIcons[i].color = new Color(0.3f, 0.3f, 0.3f);
                if (label != null)
                {
                    label.text = "";
                }
            }
        }
    }
}
