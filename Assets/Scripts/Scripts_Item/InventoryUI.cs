using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [Header("연결")]
    public GameObject inventoryPanel;
    public Image slotPrefab;
    public Transform slotsContainer;

    private Inventory inventory; // 드래그 연결 대신 자동으로 찾음
    private readonly List<Image> slotIcons = new List<Image>();
    private bool isOpen = false;

    void Start()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
        }
    }

    void Update()
    {
        // 아직 인벤토리를 못 찾았으면 계속 찾아봄 (Player가 나중에 생기는 경우 대응)
        if (inventory == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                inventory = player.GetComponent<Inventory>();
                if (inventory != null)
                {
                    inventory.OnInventoryChanged += RefreshUI;
                    BuildSlots();
                    RefreshUI();
                }
            }
        }

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

    void OnDestroy()
    {
        if (inventory != null)
        {
            inventory.OnInventoryChanged -= RefreshUI;
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

        if (slotIcons.Count != inventory.maxSlots)
        {
            BuildSlots();
        }

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
                    slotIcons[i].color = new Color(0.6f, 0.85f, 0.6f);
                }

                if (label != null)
                {
                    label.text = item.itemName;
                }
            }
            else
            {
                slotIcons[i].color = new Color(0.3f, 0.3f, 0.3f);
                if (label != null)
                {
                    label.text = "";
                }
            }
        }
    }
}
