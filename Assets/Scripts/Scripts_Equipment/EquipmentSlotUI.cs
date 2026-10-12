using UnityEngine;
using UnityEngine.UI;

public class EquipmentSlotUI : MonoBehaviour
{
    public Image iconImage;
    public GameObject selectedFrame;
    public Button button;

    public EquipmentData Item { get; private set; }
    private EquipmentPanelController panel;
    private EquipSlotType slotType;

    public void Setup(EquipmentData item, EquipSlotType slot, EquipmentPanelController controller)
    {
        Item = item;
        slotType = slot;
        panel = controller;

        bool hasIcon = item != null && item.icon != null;
        iconImage.enabled = hasIcon;
        if (hasIcon) iconImage.sprite = item.icon;
    }

    public void SetSelected(bool on) => selectedFrame.SetActive(on);

    public void OnClick() => panel.Select(this, slotType);
}