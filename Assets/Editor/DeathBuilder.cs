using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// 열려 있는 TownScene에 사망 처리(DeathManager)와 "Game Over" 창을 만듭니다.
// 메뉴: Dungeon Express > Build Death Screen  (이미 있으면 지우고 다시 만듦)
// 화면 모양을 바꾸고 싶으면 이 스크립트의 좌표와 색을 고치고 다시 실행하세요.
// 구성(기획서): 왼쪽 [Game Over / 불러오기 / 타이틀로 / 사라진 물품], 오른쪽 [진행 중이던 의뢰 목록 / 상세], 의뢰 포기 확인 창
public static class DeathBuilder
{
    const string FontAssetPath = "Assets/Fonts/MalgunGothic SDF.asset";
    const int LostSlotCount = 5;

    static readonly Color Panel = new Color(0.85f, 0.85f, 0.85f);
    static readonly Color Ink = new Color(0.1f, 0.1f, 0.1f);
    static readonly Color Muted = new Color(0.4f, 0.4f, 0.4f);
    static readonly Color Slot = new Color(0.72f, 0.72f, 0.72f);
    static readonly Color ButtonFill = new Color(0.97f, 0.97f, 0.97f);

    static TMP_FontAsset font;

    [MenuItem("Dungeon Express/Build Death Screen")]
    public static void Build()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name != "TownScene")
        {
            EditorUtility.DisplayDialog("사망 화면 만들기", "TownScene을 연 상태에서 실행해 주세요.", "확인");
            return;
        }

        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (font == null) throw new System.InvalidOperationException("한글 TMP 폰트를 찾을 수 없습니다: " + FontAssetPath);

        var old = GameObject.Find("DeathDirector");
        if (old != null) Object.DestroyImmediate(old);

        var root = new GameObject("DeathDirector");
        var manager = root.AddComponent<DeathManager>();

        var canvasGo = new GameObject("DeathCanvas", typeof(RectTransform));
        canvasGo.transform.SetParent(root.transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300; // 모든 UI 위 (저장 슬롯 창은 그보다 위)
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // 화면 전체를 덮는 어두운 배경 (페이드는 CanvasGroup으로)
        var screen = Full("DeathScreen", canvasGo.transform);
        var group = screen.gameObject.AddComponent<CanvasGroup>();
        var bg = screen.gameObject.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.85f);
        bg.raycastTarget = true;

        // ---------- 왼쪽: Game Over / 버튼 / 사라진 물품 ----------
        var left = Box("LeftPanel", screen, 210, 190, 520, 700, Panel);
        var title = Txt("Title", left, "Game Over", 64, Ink, TextAlignmentOptions.Center, 0, 40, 520, 90);
        title.fontStyle = FontStyles.Bold;

        var loadButton = Btn("LoadButton", left, "불러오기", 40, 170, 440, 84, 36);
        var titleButton = Btn("TitleButton", left, "타이틀로", 40, 274, 440, 84, 36);

        Txt("LostHeader", left, "사라진 물품", 30, Ink, TextAlignmentOptions.MidlineLeft, 40, 420, 440, 44);

        var slotIcons = new Image[LostSlotCount];
        var slotQualities = new TMP_Text[LostSlotCount];
        for (int i = 0; i < LostSlotCount; i++)
        {
            var slot = Box("LostSlot" + (i + 1), left, 40 + i * 91, 474, 76, 76, Slot);
            var icon = Rect("Icon", slot, 6, 6, 64, 64);
            slotIcons[i] = icon.gameObject.AddComponent<Image>();
            slotIcons[i].preserveAspect = true;
            slotIcons[i].raycastTarget = false;
            slotIcons[i].enabled = false;
            slotQualities[i] = Txt("Quality", slot, "", 20, Muted, TextAlignmentOptions.Center, -8, 80, 92, 28);
        }

        // ---------- 오른쪽: 진행 중이던 의뢰 목록 ----------
        var right = Box("RightPanel", screen, 750, 190, 960, 700, Panel);
        Txt("QuestHeader", right, "진행 중이던 의뢰 목록", 36, Ink, TextAlignmentOptions.Center, 0, 24, 960, 56);

        var dungeonButton = Btn("DungeonButton", right, "정글 던전", 30, 100, 340, 56, 30);
        var dungeonLabel = dungeonButton.GetComponentInChildren<TMP_Text>();
        dungeonLabel.alignment = TextAlignmentOptions.MidlineLeft;
        dungeonLabel.margin = new Vector4(18, 0, 10, 0);

        var list = Rect("QuestList", right, 30, 176, 380, 490);
        var vlg = list.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 10;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var empty = Txt("EmptyQuest", right, "진행 중이던 의뢰가 없습니다", 26, Muted, TextAlignmentOptions.Center, 30, 200, 380, 60);

        // 의뢰 줄 견본: [분류] 이름 ........ [포기]
        var rowRt = Rect("QuestRowTemplate", list, 0, 0, 380, 64);
        var rowImg = rowRt.gameObject.AddComponent<Image>();
        rowImg.color = Color.white;
        rowRt.gameObject.AddComponent<Button>().targetGraphic = rowImg;
        var rowLe = rowRt.gameObject.AddComponent<LayoutElement>();
        rowLe.preferredHeight = 64;
        rowLe.minHeight = 64;
        Txt("Badge", rowRt, "", 24, Muted, TextAlignmentOptions.Center, 8, 0, 84, 64);
        var rowName = Txt("Name", rowRt, "", 26, Ink, TextAlignmentOptions.MidlineLeft, 96, 0, 190, 64);
        rowName.textWrappingMode = TextWrappingModes.NoWrap;
        rowName.overflowMode = TextOverflowModes.Ellipsis;
        var give = Btn("Give", rowRt, "포기", 292, 14, 76, 36, 22);
        give.name = "Give";
        give.GetComponent<Image>().color = new Color(0.35f, 0.35f, 0.35f);
        give.GetComponentInChildren<TMP_Text>().color = Color.white;
        give.GetComponentInChildren<TMP_Text>().gameObject.name = "Label";

        Box("Divider", right, 430, 100, 3, 560, new Color(0.55f, 0.55f, 0.55f));

        // 의뢰 상세
        var detail = Rect("Detail", right, 460, 90, 470, 590);
        var detailName = Txt("QuestName", detail, "", 40, Ink, TextAlignmentOptions.Center, 0, 0, 470, 70);
        detailName.fontStyle = FontStyles.Bold;
        var client = DetailRow(detail, "의뢰인", 90);
        var items = DetailRow(detail, "의뢰 물품", 160);
        var location = DetailRow(detail, "의뢰 장소", 230);
        var reward = DetailRow(detail, "의뢰 보수", 300);
        Txt("DescLabel", detail, "의뢰 설명", 26, Muted, TextAlignmentOptions.MidlineLeft, 0, 385, 470, 40);
        var desc = Txt("DescText", detail, "", 24, Ink, TextAlignmentOptions.TopLeft, 0, 430, 470, 160);

        // ---------- 의뢰 포기 확인 ----------
        var confirm = Full("GiveUpConfirm", screen);
        var dim = confirm.gameObject.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.55f);
        dim.raycastTarget = true; // 뒤쪽 버튼이 눌리지 않게 막음
        var box = Box("Box", confirm, 610, 330, 700, 340, Panel);
        Txt("Question", box, "의뢰를 포기하시겠습니까?", 38, Ink, TextAlignmentOptions.Center, 0, 40, 700, 60);
        var pill = Box("QuestPill", box, 150, 120, 400, 64, ButtonFill);
        var confirmName = Txt("QuestName", pill, "", 28, Ink, TextAlignmentOptions.Center, 0, 0, 400, 64);
        var yes = Btn("Yes", box, "네", 100, 230, 220, 72, 34);
        var no = Btn("No", box, "아니오", 380, 230, 220, 72, 34);

        // ---------- 연결 ----------
        var so = new SerializedObject(manager);
        Set(so, "screen", group);
        Set(so, "loadButton", loadButton);
        Set(so, "titleButton", titleButton);
        SetArray(so, "lostSlotIcons", slotIcons);
        SetArray(so, "lostSlotQualities", slotQualities);
        Set(so, "dungeonButton", dungeonButton);
        Set(so, "dungeonLabel", dungeonLabel);
        Set(so, "questListParent", list);
        Set(so, "questRowTemplate", rowRt.gameObject);
        Set(so, "emptyQuestText", empty.gameObject);
        Set(so, "detailRoot", detail.gameObject);
        Set(so, "detailName", detailName);
        Set(so, "detailClient", client);
        Set(so, "detailItems", items);
        Set(so, "detailLocation", location);
        Set(so, "detailReward", reward);
        Set(so, "detailDescription", desc);
        Set(so, "confirmRoot", confirm.gameObject);
        Set(so, "confirmQuestName", confirmName);
        Set(so, "confirmYes", yes);
        Set(so, "confirmNo", no);
        so.ApplyModifiedPropertiesWithoutUndo();

        // 화면을 가리지 않게 꺼둠 (DeathManager가 필요할 때 켬)
        rowRt.gameObject.SetActive(false);
        confirm.gameObject.SetActive(false);
        screen.gameObject.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[DeathBuilder] 완료: TownScene에 DeathDirector 생성");
    }

    static void Set(SerializedObject so, string field, Object value)
    {
        var p = so.FindProperty(field);
        if (p == null) { Debug.LogWarning("[DeathBuilder] 필드를 찾을 수 없음: " + field); return; }
        p.objectReferenceValue = value;
    }

    static void SetArray(SerializedObject so, string field, Object[] values)
    {
        var p = so.FindProperty(field);
        if (p == null) { Debug.LogWarning("[DeathBuilder] 필드를 찾을 수 없음: " + field); return; }
        p.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }

    // 왼쪽 이름표 + 오른쪽 값 한 줄. 값 글자를 돌려줌
    static TMP_Text DetailRow(Transform parent, string label, float y)
    {
        Txt(label + "Label", parent, label, 26, Muted, TextAlignmentOptions.TopLeft, 0, y, 140, 60);
        return Txt(label + "Value", parent, "", 26, Ink, TextAlignmentOptions.TopLeft, 150, y, 320, 60);
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

    static RectTransform Box(string name, Transform parent, float x, float y, float w, float h, Color color)
    {
        var rt = Rect(name, parent, x, y, w, h);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return rt;
    }

    static Button Btn(string name, Transform parent, string label, float x, float y, float w, float h, float size)
    {
        var rt = Rect(name, parent, x, y, w, h);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = ButtonFill;
        var button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = img;
        var colors = button.colors;
        colors.highlightedColor = new Color(0.88f, 0.9f, 1f);
        colors.pressedColor = new Color(0.75f, 0.78f, 0.9f);
        colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.8f);
        button.colors = colors;
        Txt("Label", rt, label, size, Ink, TextAlignmentOptions.Center, 0, 0, w, h);
        return button;
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
}
