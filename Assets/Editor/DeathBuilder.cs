using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// 열려 있는 TownScene에 사망 처리(DeathManager)와 "YOU DIE" 화면을 만듭니다.
// 메뉴: Dungeon Express > Build Death Screen  (이미 있으면 지우고 다시 만듦)
// 화면 모양을 바꾸고 싶으면 이 스크립트의 좌표와 색을 고치고 다시 실행하세요.
public static class DeathBuilder
{
    const string FontAssetPath = "Assets/Fonts/MalgunGothic SDF.asset";

    static readonly Color Ink = Color.white;
    static readonly Color Muted = new Color(0.75f, 0.75f, 0.75f);
    static readonly Color Blood = new Color(0.8f, 0.12f, 0.12f);

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

        // 화면 전체를 덮는 어두운 배경 (페이드는 CanvasGroup으로)
        var screen = Full("DeathScreen", canvasGo.transform);
        var group = screen.gameObject.AddComponent<CanvasGroup>();
        var bg = screen.gameObject.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.92f);
        bg.raycastTarget = true;

        // 제목
        var title = Txt("Title", screen, "YOU DIE", 150, Blood, TextAlignmentOptions.Center, 0, 150, 1920, 200);
        title.fontStyle = FontStyles.Bold;

        // 사라진 아이템
        var header = Txt("LostHeader", screen, "사라진 아이템", 48, Ink, TextAlignmentOptions.Center, 0, 400, 1920, 70);

        var rows = Rect("LostRows", screen, 560, 490, 800, 360);
        var vlg = rows.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 14;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // 줄 견본: [아이콘] 이름 ........ 품질%
        var rowRt = Rect("LostRowTemplate", rows, 0, 0, 800, 76);
        var rowBg = rowRt.gameObject.AddComponent<Image>();
        rowBg.color = new Color(1f, 1f, 1f, 0.1f);
        rowBg.raycastTarget = false;
        var rowLe = rowRt.gameObject.AddComponent<LayoutElement>();
        rowLe.preferredHeight = 76;
        rowLe.minHeight = 76;
        var iconRt = Rect("Icon", rowRt, 14, 6, 64, 64);
        var iconImg = iconRt.gameObject.AddComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;
        Txt("Name", rowRt, "", 38, Ink, TextAlignmentOptions.MidlineLeft, 100, 0, 520, 76);
        Txt("Quality", rowRt, "", 38, Muted, TextAlignmentOptions.MidlineRight, 620, 0, 160, 76);

        // 안내
        var hint = Txt("Hint", screen, "아무 키나 눌러서 사무실로 돌아가기", 36, Muted, TextAlignmentOptions.Center, 0, 940, 1920, 60);

        var so = new SerializedObject(manager);
        Set(so, "screen", group);
        Set(so, "titleText", title);
        Set(so, "lostHeaderText", header);
        Set(so, "lostRowsParent", rows);
        Set(so, "lostRowTemplate", rowRt.gameObject);
        Set(so, "hintText", hint);
        so.ApplyModifiedPropertiesWithoutUndo();

        // 화면을 가리지 않게 꺼둠 (DeathManager가 필요할 때 켬)
        rowRt.gameObject.SetActive(false);
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
