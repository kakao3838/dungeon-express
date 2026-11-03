using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// 열려 있는 TownScene에 사무실 PC 화면(PCBoardUI)을 만들고, PC 오브젝트(OfficePC)와 튜토리얼에 연결합니다.
// 메뉴: Dungeon Express > Build PC Board  (Build Tutorial을 실행하면 자동으로 함께 만들어짐)
// 레이아웃을 바꾸고 싶으면 이 스크립트의 좌표를 수정하고 다시 실행하세요. (이미 있으면 지우고 다시 만듦)
public static class PCBoardBuilder
{
    const string FontAssetPath = "Assets/Fonts/MalgunGothic SDF.asset";

    static readonly Color Ink = new Color(0.1f, 0.1f, 0.1f);
    static readonly Color Muted = new Color(0.45f, 0.45f, 0.45f);
    static readonly Color Dim = new Color(0f, 0f, 0f, 0.55f);
    static readonly Color Red = new Color(0.85f, 0.15f, 0.15f);

    static TMP_FontAsset font;
    static Sprite round;

    [MenuItem("Dungeon Express/Build PC Board")]
    public static void Build()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name != "TownScene")
        {
            EditorUtility.DisplayDialog("PC 화면 만들기", "TownScene을 연 상태에서 실행해 주세요.", "확인");
            return;
        }

        BuildAndLink();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // 씬에 PC 화면을 새로 만들고 OfficePC / TutorialManager가 가리키게 함 (씬 저장은 호출한 쪽에서)
    public static void BuildAndLink()
    {
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (font == null) throw new System.InvalidOperationException("한글 TMP 폰트를 찾을 수 없습니다: " + FontAssetPath);
        round = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        var old = GameObject.Find("PCBoardCanvas");
        if (old != null) Object.DestroyImmediate(old);

        var board = BuildBoard();

        foreach (var pc in Object.FindObjectsByType<OfficePC>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var so = new SerializedObject(pc);
            so.FindProperty("board").objectReferenceValue = board;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        foreach (var manager in Object.FindObjectsByType<TutorialManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var so = new SerializedObject(manager);
            so.FindProperty("pcBoard").objectReferenceValue = board;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        Debug.Log("[PCBoardBuilder] 완료: PCBoardCanvas 생성 및 연결");
    }

    static PCBoardUI BuildBoard()
    {
        var root = new GameObject("PCBoardCanvas", typeof(RectTransform));
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150; // 튜토리얼 대사창(200)보다 아래
        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        root.AddComponent<GraphicRaycaster>();
        var ui = root.AddComponent<PCBoardUI>();
        var so = new SerializedObject(ui);

        // 뒤쪽 클릭을 막는 어두운 배경 + 모니터 (대사창 위쪽에 위치)
        var panel = Full("Panel", root.transform);
        Img(panel, Dim, null, false, true);

        var frame = Rect("Monitor", panel, 0, 0, 1400, 640);
        frame.anchorMin = frame.anchorMax = new Vector2(0.5f, 0.5f);
        frame.pivot = new Vector2(0.5f, 0.5f);
        frame.anchoredPosition = new Vector2(0, 130);
        Img(frame, new Color(0.12f, 0.14f, 0.16f), round, true);

        var screen = Rect("Screen", frame, 24, 24, 1352, 592);
        Img(screen, new Color(0.95f, 0.97f, 0.98f), null, false, true);
        Txt("Header", screen, "DUNGEON EXPRESS", 26, Muted, TextAlignmentOptions.MidlineLeft, 24, 12, 600, 40);

        // ----- 홈 화면: 아이콘 3개 -----
        var home = Rect("HomeView", screen, 0, 0, 1352, 592);
        string[] iconNames = { "의뢰 접수", "장비 강화", "가방 강화" };
        Button[] icons = new Button[3];
        GameObject[] locks = new GameObject[3];
        for (int i = 0; i < 3; i++)
        {
            var rt = Rect("Icon" + (i + 1), home, 206 + i * 340, 150, 260, 260);
            var img = Img(rt, new Color(0.9f, 0.9f, 0.9f), round, true, true);
            icons[i] = MakeButton(rt, img);
            Txt("Label", rt, iconNames[i], 40, Ink, TextAlignmentOptions.Center, 0, 0, 260, 260);

            if (i > 0)
            {
                var lockRt = Rect("LockMark", rt, 0, 0, 260, 260);
                Img(lockRt, new Color(0.3f, 0.3f, 0.3f, 0.45f), round, true, false);
                Txt("Text", lockRt, "잠김", 34, Color.white, TextAlignmentOptions.Center, 0, 190, 260, 50);
                locks[i] = lockRt.gameObject;
            }
        }
        var hint = Txt("Hint", home, "", 28, new Color(0.7f, 0.1f, 0.1f), TextAlignmentOptions.Center, 0, 440, 1352, 60);

        // ----- 탭 화면 -----
        var tabs = Rect("TabView", screen, 0, 0, 1352, 592);
        Button[] tabButtons = new Button[3];
        Image[] tabImages = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            var rt = Rect("Tab" + (i + 1), tabs, 24, 64 + i * 84, 240, 72);
            tabImages[i] = Img(rt, new Color(0.93f, 0.93f, 0.93f), null, false, true);
            tabButtons[i] = MakeButton(rt, tabImages[i]);
            Txt("Label", rt, iconNames[i], 32, Ink, TextAlignmentOptions.Center, 0, 0, 240, 72);
        }

        // 의뢰 접수 내용: 목록 + 상세
        var request = Rect("RequestContent", tabs, 0, 0, 1352, 592);
        Txt("ListTitle", request, "의뢰 목록", 34, Ink, TextAlignmentOptions.MidlineLeft, 300, 64, 400, 56);

        var list = Rect("List", request, 300, 136, 520, 420);
        var vlg = list.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 12;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var rowRt = Rect("RowTemplate", list, 0, 0, 520, 84);
        var rowImg = Img(rowRt, new Color(0.88f, 0.88f, 0.88f), null, false, true);
        var rowBtn = MakeButton(rowRt, rowImg);
        rowBtn.transition = Selectable.Transition.None;
        var rowLe = rowRt.gameObject.AddComponent<LayoutElement>();
        rowLe.preferredHeight = 84;
        rowLe.minHeight = 84;
        Txt("Label", rowRt, "", 32, Ink, TextAlignmentOptions.MidlineLeft, 24, 0, 480, 84);

        var empty = Txt("EmptyText", request, "접수할 수 있는 의뢰가 없습니다.", 30, Muted, TextAlignmentOptions.TopLeft, 300, 150, 520, 60);

        var detail = Rect("DetailBox", request, 860, 64, 470, 500);
        Img(detail, Color.white);
        var detailName = Txt("DetailName", detail, "", 32, Ink, TextAlignmentOptions.MidlineLeft, 24, 16, 422, 50);
        detailName.fontStyle = FontStyles.Bold;
        var detailBody = Txt("DetailBody", detail, "", 26, Ink, TextAlignmentOptions.TopLeft, 24, 76, 422, 340);
        var acceptHint = Txt("AcceptHint", detail, "", 24, new Color(0.7f, 0.1f, 0.1f), TextAlignmentOptions.BottomLeft, 24, 410, 270, 76);
        var acceptRt = Rect("AcceptButton", detail, 300, 428, 146, 56);
        var acceptImg = Img(acceptRt, Red, round, true, true);
        var acceptBtn = MakeButton(acceptRt, acceptImg);
        Txt("Label", acceptRt, "접수", 30, Color.white, TextAlignmentOptions.Center, 0, 0, 146, 56);

        // 장비 강화 / 가방 강화 내용 (아직 준비 중)
        var equip = Rect("EquipContent", tabs, 0, 0, 1352, 592);
        var equipText = Txt("Text", equip, "", 36, Muted, TextAlignmentOptions.Center, 300, 200, 1030, 120);
        var bag = Rect("BagContent", tabs, 0, 0, 1352, 592);
        var bagText = Txt("Text", bag, "", 36, Muted, TextAlignmentOptions.Center, 300, 200, 1030, 120);

        // 닫기 (홈/탭 어느 화면에서나 보임)
        var closeRt = Rect("CloseButton", screen, 24, 516, 160, 56);
        var closeImg = Img(closeRt, new Color(0.8f, 0.8f, 0.8f), round, true, true);
        var closeBtn = MakeButton(closeRt, closeImg);
        Txt("Label", closeRt, "닫기", 30, Ink, TextAlignmentOptions.Center, 0, 0, 160, 56);

        Set(so, "panel", panel.gameObject);
        Set(so, "closeButton", closeBtn);
        Set(so, "hintText", hint);
        Set(so, "homeView", home.gameObject);
        Set(so, "requestIconButton", icons[0]);
        Set(so, "equipIconButton", icons[1]);
        Set(so, "bagIconButton", icons[2]);
        Set(so, "equipLockMark", locks[1]);
        Set(so, "bagLockMark", locks[2]);
        Set(so, "tabView", tabs.gameObject);
        Set(so, "requestTabButton", tabButtons[0]);
        Set(so, "equipTabButton", tabButtons[1]);
        Set(so, "bagTabButton", tabButtons[2]);
        Set(so, "requestContent", request.gameObject);
        Set(so, "equipContent", equip.gameObject);
        Set(so, "bagContent", bag.gameObject);
        Set(so, "equipContentText", equipText);
        Set(so, "bagContentText", bagText);
        Set(so, "listParent", list);
        Set(so, "rowTemplate", rowBtn);
        Set(so, "emptyText", empty.gameObject);
        Set(so, "detailRoot", detail.gameObject);
        Set(so, "detailNameText", detailName);
        Set(so, "detailBodyText", detailBody);
        Set(so, "acceptButton", acceptBtn);
        Set(so, "acceptHintText", acceptHint);

        var highlights = so.FindProperty("tabHighlights");
        highlights.arraySize = tabImages.Length;
        for (int i = 0; i < tabImages.Length; i++) highlights.GetArrayElementAtIndex(i).objectReferenceValue = tabImages[i];

        so.ApplyModifiedPropertiesWithoutUndo();

        // 화면을 가리지 않게 패널과 견본은 꺼둠 (PCBoardUI.Awake에서도 꺼짐)
        panel.gameObject.SetActive(false);
        rowRt.gameObject.SetActive(false);
        hint.gameObject.SetActive(false);
        acceptHint.gameObject.SetActive(false);
        tabs.gameObject.SetActive(false);
        equip.gameObject.SetActive(false);
        bag.gameObject.SetActive(false);
        return ui;
    }

    static void Set(SerializedObject so, string field, Object value)
    {
        var p = so.FindProperty(field);
        if (p == null) { Debug.LogWarning("[PCBoardBuilder] 필드를 찾을 수 없음: " + field); return; }
        p.objectReferenceValue = value;
    }

    // ---------- 헬퍼 (TabletMenuBuilder와 같은 방식) ----------

    // 좌상단 기준 좌표(x, y 아래로 증가) 배치
    static RectTransform Rect(string name, Transform parent, float x, float y, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);
        return rt;
    }

    static RectTransform Full(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    static Image Img(RectTransform rt, Color color, Sprite sprite = null, bool sliced = false, bool raycast = false)
    {
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        img.sprite = sprite;
        if (sliced) img.type = Image.Type.Sliced;
        img.raycastTarget = raycast;
        return img;
    }

    static TextMeshProUGUI Txt(string name, Transform parent, string text, float size, Color color,
        TextAlignmentOptions align, float x, float y, float w, float h)
    {
        var rt = Rect(name, parent, x, y, w, h);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.overflowMode = TextOverflowModes.Overflow;
        t.raycastTarget = false;
        return t;
    }

    static Button MakeButton(RectTransform rt, Graphic target)
    {
        var b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = target;
        target.raycastTarget = true;
        var colors = b.colors;
        colors.highlightedColor = new Color(0.93f, 0.93f, 0.93f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
        b.colors = colors;
        return b;
    }
}
