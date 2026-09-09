using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class InventoryPanelController : MonoBehaviour
{
    public Transform slotGridParent;
    public GameObject slotPrefab;
    public List<ItemData> items;

    [Header("상세 정보")]
    public Image detailIcon;
    public TMP_Text detailName;
    public Slider qualityBar;
    public TMP_Text detailDescription;

    void OnEnable() => RefreshSlots();

    void RefreshSlots()
    {
        foreach (Transform child in slotGridParent)
            Destroy(child.gameObject);

        foreach (var item in items)
        {
            var slotObj = Instantiate(slotPrefab, slotGridParent);
            slotObj.GetComponent<InventorySlotUI>().Setup(item, this);
        }
    }

    public void ShowDetail(ItemData item)
    {
        if (item == null) return;
        detailIcon.sprite = item.icon;
        detailName.text = item.itemName;
        qualityBar.value = item.quality / 100f;
        detailDescription.text = item.description;
    }
}