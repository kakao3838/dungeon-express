using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 사망으로 잃은 물품을 화면 좌하단에 쌓아서 보여주는 알림 모음.
// 아이템 하나당 알림 하나가 뜨고, X 버튼을 눌러야 사라집니다 (게임은 멈추지 않음).
// 알림 아래쪽의 "'의뢰 이름' 포기"는 누를 수 있는 버튼입니다. 누르면 그 의뢰를 포기하고 알림이 사라집니다.
// 견본(template)은 자식으로 Title, Sub, CloseButton을 가진 꺼둔 오브젝트입니다.
// Dungeon Express > Build Death Screen 으로 만들어집니다.
public class DeathNoticeStack : MonoBehaviour
{
    public RectTransform container;
    public GameObject template;

    [Tooltip("의뢰 포기 줄이 있을 때 / 없을 때의 알림 높이")]
    public float heightWithSub = 180f;
    public float heightWithoutSub = 110f;

    private class Entry
    {
        public GameObject root;
        public Func<string> subLine;  // 포기 줄에 보일 글 (비어 있으면 줄을 숨김). 알림이 갱신될 때마다 다시 물어봄
        public Action onSubClick;     // 포기 줄을 눌렀을 때
    }

    private readonly List<Entry> entries = new List<Entry>();

    public int Count => entries.Count;

    // title: "사라진 물품: 케이크"
    // subLine: "'의뢰 이름' 포기" (없으면 빈 문자열), onSubClick: 포기 줄을 눌렀을 때 할 일
    public void Show(string title, Func<string> subLine, Action onSubClick)
    {
        if (template == null || container == null) return;

        var notice = Instantiate(template, container);
        notice.name = "DeathNotice";
        notice.SetActive(true);

        var entry = new Entry { root = notice, subLine = subLine, onSubClick = onSubClick };
        entries.Add(entry);

        var titleText = notice.transform.Find("Title");
        if (titleText != null) titleText.GetComponent<TMP_Text>().text = title;

        var close = notice.transform.Find("CloseButton");
        var closeButton = close != null ? close.GetComponent<Button>() : null;
        if (closeButton != null) closeButton.onClick.AddListener(() => Dismiss(entry));

        // 포기 줄을 눌러서 쓰는 버튼으로 만듦 (견본에 버튼이 없어도 여기서 붙임)
        var sub = notice.transform.Find("Sub");
        if (sub != null)
        {
            var text = sub.GetComponent<TMP_Text>();
            text.raycastTarget = true;
            text.fontStyle |= FontStyles.Underline; // 눌러야 하는 글자라는 걸 알 수 있게

            var button = sub.GetComponent<Button>();
            if (button == null) button = sub.gameObject.AddComponent<Button>();
            button.targetGraphic = text;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                if (entry.onSubClick != null) entry.onSubClick();
                Dismiss(entry); // 눌렀다는 걸 바로 알 수 있게 이 알림은 사라짐
            });
        }

        Apply(entry);
        LayoutRebuilder.ForceRebuildLayoutImmediate(container);
    }

    // 모든 알림의 포기 줄을 다시 계산함 (다른 알림에서 같은 의뢰를 이미 포기했으면 그 줄이 사라지게)
    public void RefreshAll()
    {
        foreach (var entry in entries) Apply(entry);
        if (container != null) LayoutRebuilder.ForceRebuildLayoutImmediate(container);
    }

    void Apply(Entry entry)
    {
        if (entry.root == null) return;

        string line = entry.subLine != null ? entry.subLine() : string.Empty;
        bool hasSub = !string.IsNullOrEmpty(line);

        var sub = entry.root.transform.Find("Sub");
        if (sub != null)
        {
            sub.gameObject.SetActive(hasSub);
            if (hasSub) sub.GetComponent<TMP_Text>().text = line;
        }

        var layout = entry.root.GetComponent<LayoutElement>();
        if (layout != null)
        {
            float height = hasSub ? heightWithSub : heightWithoutSub;
            layout.preferredHeight = height;
            layout.minHeight = height;
        }
    }

    void Dismiss(Entry entry)
    {
        entries.Remove(entry);
        if (entry.root != null)
        {
            entry.root.SetActive(false); // Destroy는 프레임 끝에 일어나므로, 쌓이는 칸이 바로 다시 정렬되게 먼저 끔
            Destroy(entry.root);
        }
        RefreshAll();
    }

    public void Clear()
    {
        foreach (var entry in entries) if (entry.root != null) Destroy(entry.root);
        entries.Clear();
    }
}
