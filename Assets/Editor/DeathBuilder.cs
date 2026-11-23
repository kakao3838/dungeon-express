using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// 열려 있는 TownScene에 사망 처리(DeathManager)와 사망 알림 UI를 만듭니다.
//   - 장면 이동을 가리는 검은 암전 화면
//   - 화면 좌하단에 쌓이는 "사라진 물품" 알림 (알림 견본 + 쌓이는 칸)
// 메뉴: Dungeon Express > Build Death Screen  (이미 있으면 지우고 다시 만듦)
// 알림 모양을 바꾸고 싶으면 이 스크립트의 좌표와 색을 고치고 다시 실행하세요.
public static class DeathBuilder
{
    const string FontAssetPath = "Assets/Fonts/MalgunGothic SDF.asset";

    static readonly Color NoticeBg = new Color(0.55f, 0.55f, 0.55f, 0.96f);
    static readonly Color Ink = new Color(0.08f, 0.08f, 0.08f, 1f);

    // 알림 크기 (1920x1080 기준). 의뢰 포기 줄이 없을 때의 높이는 DeathNoticeStack이 줄임
    const float NoticeWidth = 780f;
    const float NoticeHeight = 180f;

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
        canvas.sortingOrder = 300; // 모든 UI 위
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // 장면 이동을 가리는 검은 화면 (페이드는 CanvasGroup으로)
        var fade = Full("Fade", canvasGo.transform);
        var group = fade.gameObject.AddComponent<CanvasGroup>();
        var fadeImage = fade.gameObject.AddComponent<Image>();
        fadeImage.color = Color.black;
        fadeImage.raycastTarget = true;

        // 좌하단에 쌓이는 알림 칸 (아래쪽 기준으로 쌓임)
        var stackRt = new GameObject("DeathNotices", typeof(RectTransform)).GetComponent<RectTransform>();
        stackRt.SetParent(canvasGo.transform, false);
        stackRt.anchorMin = stackRt.anchorMax = Vector2.zero;
        stackRt.pivot = Vector2.zero;
        stackRt.anchoredPosition = Vector2.zero;
        stackRt.sizeDelta = new Vector2(NoticeWidth, 1000f);
        var vlg = stackRt.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 0;
        vlg.childAlignment = TextAnchor.LowerLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        var stack = stackRt.gameObject.AddComponent<DeathNoticeStack>();

        // 알림 견본: 사라진 물품 / 의뢰 포기 / X 버튼
        var template = Rect("DeathNoticeTemplate", stackRt, 0, 0, NoticeWidth, NoticeHeight);
        var bg = template.gameObject.AddComponent<Image>();
        bg.color = NoticeBg;
        bg.raycastTarget = true;
        var le = template.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = NoticeWidth;
        le.preferredHeight = NoticeHeight;
        le.minHeight = NoticeHeight;

        var title = Txt("Title", template, "사라진 물품: 케이크", 46, Ink, TextAlignmentOptions.MidlineLeft,
            28, 16, 660, 70);
        var sub = Txt("Sub", template, "'의뢰 이름' 포기", 32, Ink, TextAlignmentOptions.MidlineRight,
            28, 112, NoticeWidth - 56, 50);

        var closeRt = Rect("CloseButton", template, NoticeWidth - 72, 10, 56, 56);
        var closeImage = closeRt.gameObject.AddComponent<Image>();
        closeImage.color = new Color(1f, 1f, 1f, 0.01f); // 클릭 영역만 (보이지 않게)
        var closeButton = closeRt.gameObject.AddComponent<Button>();
        closeButton.targetGraphic = closeImage;
        Txt("Label", closeRt, "X", 40, Ink, TextAlignmentOptions.Center, 0, 0, 56, 56);

        // 연결
        var so = new SerializedObject(manager);
        Set(so, "fade", group);
        Set(so, "notices", stack);
        so.ApplyModifiedPropertiesWithoutUndo();

        var stackSo = new SerializedObject(stack);
        Set(stackSo, "container", stackRt);
        Set(stackSo, "template", template.gameObject);
        stackSo.ApplyModifiedPropertiesWithoutUndo();

        // 화면을 가리지 않게 꺼둠 (DeathManager가 필요할 때 켬)
        template.gameObject.SetActive(false);
        fade.gameObject.SetActive(false);

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

    // ---------- 헬퍼 ----------

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
