using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 가방 탭의 슬롯 한 칸. 상태는 3가지: 아이템 있음 / 빈 칸 / 잠김(가방 용량 밖)
public class InventorySlotUI : MonoBehaviour
{
    public Image iconImage;
    public TMP_Text fallbackLabel;   // 아이콘 스프라이트가 없는 아이템은 이름을 대신 표시
    public GameObject lockIcon;
    public GameObject selectedFrame;
    public Button button;

    private ItemData currentItem;
    private InventoryPanelController panelController;

    public ItemData Item => currentItem;

    public void Setup(ItemData item, bool locked, InventoryPanelController controller)
    {
        currentItem = item;
        panelController = controller;

        bool hasItem = item != null;
        bool hasIcon = hasItem && item.icon != null;

        iconImage.enabled = hasIcon;
        iconImage.sprite = hasIcon ? item.icon : null;

        fallbackLabel.gameObject.SetActive(hasItem && !hasIcon);
        fallbackLabel.text = hasItem ? item.itemName : "";

        lockIcon.SetActive(locked && !hasItem);
        button.interactable = hasItem;
        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        selectedFrame.SetActive(selected);
    }

    public void OnClick()
    {
        if (currentItem != null) panelController.Select(this);
    }
}
