using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 태블릿 메뉴 '도감' 탭. 플레이어가 얻어 본 배달품만 목록에 보여줍니다.
public class CodexPanelController : MonoBehaviour
{
    [Header("목록")]
    public Transform listParent;
    public CodexEntryUI entryPrefab;
    public GameObject emptyListText;

    [Header("상세 정보")]
    public GameObject detailRoot;
    public TMP_Text categoryLabel;
    public TMP_Text nameText;
    public Image iconImage;
    public TMP_Text iconFallbackText;
    public TMP_Text descriptionText;

    private PlayerCodex codex;
    private readonly List<CodexEntryUI> entries = new List<CodexEntryUI>();
    private ItemData selectedItem;

    void OnEnable()
    {
        selectedItem = null; // 열 때마다 첫 번째 항목부터
        FindCodex();
        Refresh();
    }

    void OnDisable()
    {
        if (codex != null) codex.OnCodexChanged -= Refresh;
        codex = null;
    }

    void FindCodex()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        codex = player != null ? player.GetComponent<PlayerCodex>() : null;
        if (codex != null) codex.OnCodexChanged += Refresh;
    }

    void Refresh()
    {
        int count = codex != null ? codex.Discovered.Count : 0;

        if (selectedItem == null && count > 0) selectedItem = codex.Discovered[0];

        while (entries.Count < count)
            entries.Add(Instantiate(entryPrefab, listParent));

        for (int i = 0; i < entries.Count; i++)
        {
            bool active = i < count;
            entries[i].gameObject.SetActive(active);
            if (!active) continue;

            entries[i].Setup(codex.Discovered[i], this);
            entries[i].SetSelected(codex.Discovered[i] == selectedItem);
        }

        emptyListText.SetActive(count == 0);
        ShowDetail(selectedItem);
    }

    public void Select(CodexEntryUI entry)
    {
        selectedItem = entry.Item;
        foreach (var e in entries) e.SetSelected(e == entry);
        ShowDetail(selectedItem);
    }

    void ShowDetail(ItemData item)
    {
        bool has = item != null;
        detailRoot.SetActive(has);
        if (!has) return;

        categoryLabel.text = "배달품";
        nameText.text = item.itemName;

        bool hasIcon = item.icon != null;
        iconImage.enabled = hasIcon;
        iconImage.sprite = hasIcon ? item.icon : null;
        iconFallbackText.gameObject.SetActive(!hasIcon);
        iconFallbackText.text = item.itemName;

        descriptionText.text = string.IsNullOrEmpty(item.description) ? "설명이 없습니다." : item.description;
    }
}