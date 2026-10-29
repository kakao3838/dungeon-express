using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// 열려 있는 씬의 NPC 대화창(NPCDialogueMenu, NPCDialogueBox)을 튜토리얼 대사창과 같은 모양으로 맞춥니다.
// 메뉴: Dungeon Express > Restyle NPC Dialogue
// 위치/크기/색만 바꾸고 연결된 컴포넌트와 참조는 그대로 두므로 여러 번 실행해도 안전합니다.
public static class NPCDialogueStyler
{
    static readonly Color Ink = new Color(0.1f, 0.1f, 0.1f);
    static readonly Color Muted = new Color(0.45f, 0.45f, 0.45f);
    static readonly Color PanelGray = new Color(0.87f, 0.87f, 0.87f, 0.97f);
    static readonly Color TabGray = new Color(0.35f, 0.35f, 0.35f);
    static readonly Color ButtonLight = new Color(0.95f, 0.95f, 0.95f);

    const float BarHeight = 260f;

    [MenuItem("Dungeon Express/Restyle NPC Dialogue")]
    public static void Restyle()
    {
        var scene = EditorSceneManager.GetActiveScene();
        int touched = 0;

        foreach (var menu in Object.FindObjectsByType<NPCDialogueMenu>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            StyleMenu(menu);
            Record(menu.gameObject);
            touched++;
        }
        foreach (var box in Object.FindObjectsByType<NPCDialogueBox>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            StyleBox(box);
            // 프리팹 인스턴스(카일 등)는 바꾼 값을 오버라이드로 기록해야 씬에 저장됨
            Record(box.panel != null ? box.panel : box.gameObject);
            touched++;
        }

        if (touched == 0)
        {
            Debug.LogWarning("[NPCDialogueStyler] 이 씬에서 NPC 대화창을 찾지 못했습니다: " + scene.name);
            return;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[NPCDialogueStyler] " + scene.name + ": 대화창 " + touched + "개를 튜토리얼 스타일로 맞춤");
    }

    // ---------- 사장 대화 메뉴 (선택지 / 대화 / 의뢰 물품 카드) ----------
    static void StyleMenu(NPCDialogueMenu menu)
    {
        // 선택지 메뉴: [이름표] + 선택지 버튼 3개
        if (menu.menuPanel != null)
        {
            var panel = (RectTransform)menu.menuPanel.transform;
            Bar(panel);
            Tab(panel.Find("NPCNameBox"), menu.npcNameText);

            var choices = panel.Find("Choices") as RectTransform;
            if (choices != null)
            {
                Inset(choices, 90, 30, 90, 40);
                var layout = choices.GetComponent<VerticalLayoutGroup>();
                if (layout != null) layout.spacing = 12;
                foreach (var b in choices.GetComponentsInChildren<Button>(true)) LightButton(b, 62f, 32f, TextAlignmentOptions.MidlineLeft);
            }
        }

        // 대화: [이름표] + 대사 + 닫기
        if (menu.dialoguePanel != null)
        {
            var panel = (RectTransform)menu.dialoguePanel.transform;
            Bar(panel);
            Tab(panel.Find("SpeakerBox"), menu.speakerText);

            var area = panel.Find("TextArea") as RectTransform;
            if (area != null) Inset(area, 90, 40, 260, 40);
            if (menu.dialogueText != null)
            {
                // 씬에 꺼진 채로 들어 있던 대사 텍스트를 켬 (안 켜면 2번 '대화'를 눌러도 아무것도 안 보임)
                menu.dialogueText.gameObject.SetActive(true);
                Body(menu.dialogueText, 38f);
            }
            BottomRightButton(panel.Find("CloseButtonAnchor"));
        }

        // 의뢰 물품 카드: [이름표] + 설명 + 물품 카드 + 확인
        if (menu.questDetailPanel != null)
        {
            var panel = (RectTransform)menu.questDetailPanel.transform;
            Bar(panel);
            Tab(panel.Find("SpeakerBox"), menu.questSpeakerText);

            var area = panel.Find("DescriptionArea") as RectTransform;
            if (area != null) Inset(area, 90, 40, 260, 40);
            Body(menu.questDescriptionText, 36f);

            StyleItemCard(panel.Find("ItemCard"));
            BottomRightButton(panel.Find("CloseButtonAnchor"));
        }
    }

    static void StyleItemCard(Transform card)
    {
        if (card == null) return;

        // 대사창 위쪽 가운데에 뜨는 작은 카드 (원래 1000x670으로 너무 커서 화면을 가렸음)
        var rt = (RectTransform)card;
        rt.sizeDelta = new Vector2(560, 420);
        rt.anchoredPosition = new Vector2(-280, 200);

        var img = card.GetComponent<Image>();
        if (img != null) img.color = new Color(0.96f, 0.96f, 0.96f, 0.98f);

        foreach (var t in card.GetComponentsInChildren<TMP_Text>(true))
        {
            switch (t.name)
            {
                case "CardHeader": t.color = Muted; t.fontSize = 28; break;
                case "TraitLabel": t.color = Muted; t.fontSize = 24; break;
                case "ItemTraitText": t.color = new Color(0.75f, 0.3f, 0.05f); t.fontSize = 32; break;
                default: t.color = Ink; t.fontSize = 40; break; // ItemNameText
            }
        }

        // 글자가 커진 만큼 안쪽 칸도 넓힘 (위에서부터 쌓이는 구조)
        SetStrip(card.Find("CardHeader"), -16, 36);
        var icon = card.Find("IconBox") as RectTransform;
        if (icon != null)
        {
            icon.anchoredPosition = new Vector2(0, -64);
            icon.sizeDelta = new Vector2(120, 120);
            var iconImg = icon.GetComponent<Image>();
            if (iconImg != null) iconImg.color = new Color(0.85f, 0.85f, 0.85f);
        }
        SetStrip(card.Find("ItemNameText"), -200, 52);
        SetStrip(card.Find("TraitRow"), -268, 110);
        SetStrip(card.Find("TraitRow/TraitLabel"), 0, 34);
        SetStrip(card.Find("TraitRow/ItemTraitText"), -36, 44);
    }

    // 위쪽에 붙은 가로 가득 찬 줄의 위치(위에서 얼마나 내려오는지)와 높이
    static void SetStrip(Transform tr, float y, float height)
    {
        var rt = tr as RectTransform;
        if (rt == null) return;
        rt.anchoredPosition = new Vector2(0, y);
        rt.sizeDelta = new Vector2(0, height);
    }

    // ---------- 납품 대사창 (카일) ----------
    static void StyleBox(NPCDialogueBox box)
    {
        if (box.panel == null) return;
        var panel = (RectTransform)box.panel.transform;
        Bar(panel);

        // 이름표 / 대사 / 닫기 버튼은 이름으로 찾음 (SpeakerBox, CloseButtonAnchor는 사장 대화창과 같은 구조)
        Tab(panel.Find("SpeakerBox"), box.speakerText);

        var area = panel.Find("TextArea") as RectTransform;
        if (area != null) Inset(area, 90, 40, 260, 40);
        Body(box.dialogueText, 38f);

        BottomRightButton(panel.Find("CloseButtonAnchor"));
    }

    // ---------- 공통 ----------

    // root 아래 모든 오브젝트/컴포넌트의 변경을 저장 대상으로 표시 (프리팹 인스턴스면 오버라이드로 기록)
    static void Record(GameObject root)
    {
        foreach (var tr in root.GetComponentsInChildren<Transform>(true))
        {
            EditorUtility.SetDirty(tr.gameObject);
            if (PrefabUtility.IsPartOfPrefabInstance(tr.gameObject))
                PrefabUtility.RecordPrefabInstancePropertyModifications(tr.gameObject);

            foreach (var c in tr.GetComponents<Component>())
            {
                if (c == null) continue;
                EditorUtility.SetDirty(c);
                if (PrefabUtility.IsPartOfPrefabInstance(c)) PrefabUtility.RecordPrefabInstancePropertyModifications(c);
            }
        }
    }

    // 화면 아래 가로 가득 찬 대사창
    static void Bar(RectTransform panel)
    {
        panel.anchorMin = new Vector2(0, 0);
        panel.anchorMax = new Vector2(1, 0);
        panel.pivot = new Vector2(0.5f, 0);
        panel.anchoredPosition = Vector2.zero;
        panel.sizeDelta = new Vector2(0, BarHeight);

        var img = panel.GetComponent<Image>();
        if (img == null) img = panel.gameObject.AddComponent<Image>();
        img.sprite = null;
        img.color = PanelGray;
        img.raycastTarget = true;
    }

    // 말하는 사람 이름표: 대사창 위쪽 모서리에 걸친 회색 탭
    static void Tab(Transform boxTr, TMP_Text speaker)
    {
        if (boxTr == null) return;
        var box = (RectTransform)boxTr;
        box.anchorMin = box.anchorMax = new Vector2(0, 1);
        box.pivot = new Vector2(0, 0);
        box.anchoredPosition = new Vector2(60, 0);
        box.sizeDelta = new Vector2(260, 64);

        var img = box.GetComponent<Image>();
        if (img == null) img = box.gameObject.AddComponent<Image>();
        img.color = TabGray;
        img.raycastTarget = false;

        if (speaker != null)
        {
            var rt = speaker.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            speaker.color = Color.white;
            speaker.fontSize = 34;
            speaker.alignment = TextAlignmentOptions.Center;
        }
    }

    static void Body(TMP_Text text, float size)
    {
        if (text == null) return;
        var rt = text.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        text.color = Ink;
        text.fontSize = size;
        text.alignment = TextAlignmentOptions.TopLeft;
    }

    static void Inset(RectTransform rt, float left, float bottom, float right, float top)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }

    static void LightButton(Button b, float height, float fontSize, TextAlignmentOptions align)
    {
        var img = b.GetComponent<Image>();
        if (img != null) img.color = ButtonLight;

        var le = b.GetComponent<LayoutElement>();
        if (le == null) le = b.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.minHeight = height;

        var text = b.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
        {
            text.color = Ink;
            text.fontSize = fontSize;
            text.alignment = align;
        }
    }

    // 오른쪽 아래 닫기/확인 버튼 (CloseButtonAnchor 안의 버튼)
    static void BottomRightButton(Transform anchorTr)
    {
        if (anchorTr == null) return;
        var anchor = (RectTransform)anchorTr;
        anchor.anchorMin = anchor.anchorMax = new Vector2(1, 0);
        anchor.pivot = new Vector2(1, 0);
        anchor.anchoredPosition = new Vector2(-60, 24);
        anchor.sizeDelta = new Vector2(180, 64);

        var b = anchor.GetComponentInChildren<Button>(true);
        if (b != null) LightButton(b, 64f, 32f, TextAlignmentOptions.Center);
    }
}
