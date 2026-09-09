using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour
{
    public Image borderImage;
    public Image iconImage;
    public GameObject lockIcon;

    private ItemData currentItem;
    private InventoryPanelController panelController;

    public void Setup(ItemData item, InventoryPanelController controller)
    {
        currentItem = item;
        panelController = controller;

        iconImage.enabled = item != null;
        if (item == null) { lockIcon.SetActive(false); return; }

        iconImage.sprite = item.icon;
        lockIcon.SetActive(item.isLocked);
        borderImage.color = GetColorByRarity(item.rarity);
    }

    Color GetColorByRarity(ItemData.Rarity rarity)
    {
        switch (rarity)
        {
            case ItemData.Rarity.Rare: return Color.blue;
            case ItemData.Rarity.Epic: return new Color(0.6f, 0f, 0.8f);
            case ItemData.Rarity.Legendary: return new Color(1f, 0.6f, 0f);
            default: return Color.white;
        }
    }

    public void OnClick()
    {
        panelController.ShowDetail(currentItem);
    }
}