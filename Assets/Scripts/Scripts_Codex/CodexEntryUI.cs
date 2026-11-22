using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CodexEntryUI : MonoBehaviour
{
    public Image iconImage;
    public TMP_Text nameText;
    public GameObject selectedFrame;

    public ItemData Item { get; private set; }
    private CodexPanelController panel;

    public void Setup(ItemData item, CodexPanelController controller)
    {
        Item = item;
        panel = controller;
        nameText.text = item.itemName;

        bool hasIcon = item.icon != null;
        iconImage.enabled = hasIcon;
        iconImage.sprite = hasIcon ? item.icon : null;
        SetSelected(false);
    }

    public void SetSelected(bool on) => selectedFrame.SetActive(on);

    public void OnClick() => panel.Select(this);
}