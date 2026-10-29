using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// 열려 있는 TownScene에 튜토리얼용 오브젝트(PC, TutorialManager, 튜토리얼 UI)를 만듭니다.
// 메뉴: Dungeon Express > Build Tutorial (TownScene을 연 상태에서 실행)
// 레이아웃을 바꾸고 싶으면 이 스크립트의 좌표를 수정하고 다시 실행하세요. (이미 있으면 지우고 다시 만듦)
public static class TutorialBuilder
{
    const string FontAssetPath = "Assets/Fonts/MalgunGothic SDF.asset";
    const string QuestPath = "Assets/Data_Quests/Tutorial_기본배달.asset";

    static readonly Color Ink = new Color(0.1f, 0.1f, 0.1f);
    static readonly Color Muted = new Color(0.45f, 0.45f, 0.45f);
    static readonly Color PanelGray = new Color(0.87f, 0.87f, 0.87f, 0.97f);
    static readonly Color TabGray = new Color(0.35f, 0.35f, 0.35f);
    static readonly Color BtnGray = new Color(0.8f, 0.8f, 0.8f);
    static readonly Color Dim = new Color(0f, 0f, 0f, 0.55f);

    static TMP_FontAsset font;
    static Sprite round;

    [MenuItem("Dungeon Express/Build Tutorial")]
    public static void Build()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name != "TownScene")
        {
            EditorUtility.DisplayDialog("튜토리얼 만들기", "TownScene을 연 상태에서 실행해 주세요.", "확인");
            return;
        }

        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (font == null) throw new System.InvalidOperationException("한글 TMP 폰트를 찾을 수 없습니다: " + FontAssetPath);
        round = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        // 다시 실행하면 이전 결과를 지우고 새로 만듦
        DestroyIfExists("TutorialManager");
        DestroyIfExists("TutorialPC");

        BuildPC();
        BuildManager();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[TutorialBuilder] 완료: TownScene에 TutorialPC / TutorialManager 생성");
    }

    static void DestroyIfExists(string name)
    {
        var go = GameObject.Find(name);
        if (go != null) Object.DestroyImmediate(go);
    }

    // ---------- PC 오브젝트 (월드) ----------

    static void BuildPC()
    {
        var root = new GameObject("TutorialPC");
        root.transform.position = new Vector3(-1f, -2f, 0f);

        var col = root.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(4f, 3f);
        col.offset = new Vector2(0f, 1f);
        var pc = root.AddComponent<TutorialPC>();

        // 책상 + 모니터 (시안처럼 단순한 회색 도형, 크기는 월드 단위)
        Block(root.transform, "Desk", new Vector3(0f, -0.02f, 0f), new Vector2(3.2f, 0.96f), new Color(0.72f, 0.72f, 0.72f));
        Block(root.transform, "MonitorStand", new Vector3(0f, 0.73f, 0f), new Vector2(0.4f, 0.54f), new Color(0.45f, 0.45f, 0.45f));
        Block(root.transform, "Monitor", new Vector3(0f, 1.6f, 0f), new Vector2(1.8f, 1.2f), new Color(0.3f, 0.3f, 0.32f), 1);
        Block(root.transform, "Screen", new Vector3(0f, 1.6f, 0f), new Vector2(1.5f, 0.95f), new Color(0.55f, 0.85f, 0.9f), 2);

        // 가까이 가면 뜨는 'F' 안내 말풍선 (월드 스페이스 캔버스)
        var bubbleGo = new GameObject("Prompt", typeof(RectTransform));
        bubbleGo.transform.SetParent(root.transform, false);
        bubbleGo.transform.localPosition = new Vector3(1.6f, 2.0f, 0f); // 모니터 오른쪽 위 (위쪽 키 안내 창에 가려지지 않게)
        bubbleGo.transform.localScale = Vector3.one * 0.01f;
        var canvas = bubbleGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 50;
        var rt = (RectTransform)bubbleGo.transform;
        rt.sizeDelta = new Vector2(90f, 90f);
        var bg = bubbleGo.AddComponent<Image>();
        bg.sprite = round;
        bg.type = Image.Type.Sliced;
        bg.color = Color.white;
        var outline = bubbleGo.AddComponent<Outline>();
        outline.effectColor = Ink;
        outline.effectDistance = new Vector2(3f, -3f);
        var label = Txt("Label", bubbleGo.transform, "F", 56, Ink, TextAlignmentOptions.Center, 0, 0, 90, 90);
        label.fontStyle = FontStyles.Bold;
        bubbleGo.SetActive(false);

        pc.promptRoot = bubbleGo;
    }

    static void Block(Transform parent, string name, Vector3 localPos, Vector2 size, Color color, int order = 0)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = round;
        // 스프라이트 원래 크기(월드 단위)에 맞춰 배율을 계산해서 원하는 월드 크기로 맞춤
        Vector2 baseSize = round.bounds.size;
        go.transform.localScale = new Vector3(size.x / baseSize.x, size.y / baseSize.y, 1f);
        sr.color = color;
        sr.sortingOrder = order;
    }

    // ---------- 매니저 + UI ----------

    static void BuildManager()
    {
        var root = new GameObject("TutorialManager");
        var manager = root.AddComponent<TutorialManager>();

        var canvasGo = new GameObject("TutorialCanvas", typeof(RectTransform));
        canvasGo.transform.SetParent(root.transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        var so = new SerializedObject(manager);
        so.FindProperty("tutorialQuest").objectReferenceValue = AssetDatabase.LoadAssetAtPath<QuestData>(QuestPath);
        if (so.FindProperty("tutorialQuest").objectReferenceValue == null)
            Debug.LogWarning("[TutorialBuilder] 튜토리얼 퀘스트를 찾지 못했습니다: " + QuestPath);

        // PC / 던전 선택 / 키 안내 / 선택지 / 토스트 / 대사창 (대사창이 가장 위에 오도록 마지막에)
        BuildPCPanel(canvasGo.transform, so);
        BuildDungeonPanel(canvasGo.transform, so);
        BuildKeyGuide(canvasGo.transform, so);
        BuildChoice(canvasGo.transform, so);
        BuildToast(canvasGo.transform, so);
        BuildDialogue(canvasGo.transform, so);

        so.ApplyModifiedPropertiesWithoutUndo();

        // 씬을 열었을 때 화면을 가리지 않게 패널과 견본은 꺼둠 (TutorialManager가 필요할 때 켬)
        foreach (var field in new[]
        {
            "dialoguePanel", "keyGuidePanel", "pcPanel", "dungeonPanel", "choicePanel", "toastPanel",
            "keyGuideRowTemplate", "keyCapTemplate"
        })
        {
            ((GameObject)so.FindProperty(field).objectReferenceValue).SetActive(false);
        }
    }

    static void Set(SerializedObject so, string field, Object value)
    {
        var p = so.FindProperty(field);
        if (p == null) { Debug.LogWarning("[TutorialBuilder] 필드를 찾을 수 없음: " + field); return; }
        p.objectReferenceValue = value;
    }

    // ----- 대사창 (화면 아래, 사장과 대화할 때 쓰는 창과 같은 모양) -----
    static void BuildDialogue(Transform canvas, SerializedObject so)
    {
        var panel = Bottom("DialoguePanel", canvas, 0, 0, 0, 260);
        panel.anchorMin = new Vector2(0, 0);
        panel.anchorMax = new Vector2(1, 0);
        var bg = Img(panel, PanelGray, null, false, true);
        var button = MakeButton(panel, bg);
        button.transition = Selectable.Transition.None;

        // 말하는 사람 이름표 (창 위쪽 모서리에 걸침)
        var tab = Rect("SpeakerBox", panel, 60, -64, 260, 64);
        tab.anchorMin = tab.anchorMax = new Vector2(0, 1);
        tab.pivot = new Vector2(0, 0);
        tab.anchoredPosition = new Vector2(60, 0);
        Img(tab, TabGray);
        var speaker = Txt("SpeakerText", tab, "사장", 34, Color.white, TextAlignmentOptions.Center, 0, 0, 260, 64);

        var line = Txt("LineText", panel, "", 38, Ink, TextAlignmentOptions.TopLeft, 0, 0, 0, 0);
        var lrt = line.rectTransform;
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = new Vector2(90, 40);
        lrt.offsetMax = new Vector2(-90, -40);

        var hint = Txt("NextHint", panel, "▼  클릭 / F", 28, Muted, TextAlignmentOptions.BottomRight, 0, 0, 260, 50);
        hint.rectTransform.anchorMin = hint.rectTransform.anchorMax = new Vector2(1, 0);
        hint.rectTransform.pivot = new Vector2(1, 0);
        hint.rectTransform.anchoredPosition = new Vector2(-50, 20);

        Set(so, "dialoguePanel", panel.gameObject);
        Set(so, "speakerText", speaker);
        Set(so, "lineText", line);
        Set(so, "nextHint", hint.gameObject);
        Set(so, "dialogueButton", button);
    }

    // ----- 키 안내 (이동/상호작용/조작키) -----
    static void BuildKeyGuide(Transform canvas, SerializedObject so)
    {
        var panelGo = new GameObject("KeyGuidePanel", typeof(RectTransform));
        panelGo.transform.SetParent(canvas, false);
        var panel = (RectTransform)panelGo.transform;
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 1f);
        panel.pivot = new Vector2(0.5f, 1f);
        panel.anchoredPosition = new Vector2(0, -90);
        Img(panel, new Color(1f, 1f, 1f, 0.96f));
        var outline = panelGo.AddComponent<Outline>();
        outline.effectColor = Muted;
        outline.effectDistance = new Vector2(3f, -3f);

        var vlg = panelGo.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(50, 50, 30, 30);
        vlg.spacing = 20;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = false;
        vlg.childForceExpandHeight = false;
        var fitter = panelGo.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var title = Txt("Title", panel, "조작 안내", 44, Ink, TextAlignmentOptions.Center, 0, 0, 600, 60);
        title.fontStyle = FontStyles.Bold;

        // 줄들이 들어가는 곳
        var rowsGo = new GameObject("Rows", typeof(RectTransform));
        rowsGo.transform.SetParent(panel, false);
        var rowsVlg = rowsGo.AddComponent<VerticalLayoutGroup>();
        rowsVlg.spacing = 16;
        rowsVlg.childAlignment = TextAnchor.UpperCenter;
        rowsVlg.childControlWidth = true;
        rowsVlg.childControlHeight = true;
        rowsVlg.childForceExpandWidth = false;
        rowsVlg.childForceExpandHeight = false;
        var rowsLe = rowsGo.AddComponent<LayoutElement>();
        rowsLe.minWidth = 760;

        // 줄 견본: [Keys][Label]
        var rowGo = new GameObject("RowTemplate", typeof(RectTransform));
        rowGo.transform.SetParent(rowsGo.transform, false);
        var rowH = rowGo.AddComponent<HorizontalLayoutGroup>();
        rowH.spacing = 24;
        rowH.childAlignment = TextAnchor.MiddleCenter;
        rowH.childControlWidth = true;
        rowH.childControlHeight = true;
        rowH.childForceExpandWidth = false;
        rowH.childForceExpandHeight = false;
        var rowLe = rowGo.AddComponent<LayoutElement>();
        rowLe.minHeight = 76;

        var keysGo = new GameObject("Keys", typeof(RectTransform));
        keysGo.transform.SetParent(rowGo.transform, false);
        var keysH = keysGo.AddComponent<HorizontalLayoutGroup>();
        keysH.spacing = 10;
        keysH.childAlignment = TextAnchor.MiddleCenter;
        keysH.childControlWidth = true;
        keysH.childControlHeight = true;
        keysH.childForceExpandWidth = false;
        keysH.childForceExpandHeight = false;

        var label = Txt("Label", rowGo.transform, "", 34, Ink, TextAlignmentOptions.MidlineLeft, 0, 0, 100, 60);
        label.textWrappingMode = TextWrappingModes.NoWrap;

        // 키캡 견본
        var capGo = new GameObject("KeyCapTemplate", typeof(RectTransform));
        capGo.transform.SetParent(panel, false);
        var capImg = capGo.AddComponent<Image>();
        capImg.sprite = round;
        capImg.type = Image.Type.Sliced;
        capImg.color = new Color(0.94f, 0.94f, 0.94f);
        var capOutline = capGo.AddComponent<Outline>();
        capOutline.effectColor = Ink;
        capOutline.effectDistance = new Vector2(3f, -3f);
        var capLe = capGo.AddComponent<LayoutElement>();
        capLe.preferredWidth = 64;
        capLe.preferredHeight = 64;
        var capLabel = Txt("Label", capGo.transform, "W", 34, Ink, TextAlignmentOptions.Center, 0, 0, 64, 64);
        capLabel.fontStyle = FontStyles.Bold;
        var clrt = capLabel.rectTransform;
        clrt.anchorMin = Vector2.zero;
        clrt.anchorMax = Vector2.one;
        clrt.offsetMin = clrt.offsetMax = Vector2.zero;

        // 확인 버튼 (정글 진입 안내에서만 사용)
        var okRt = Rect("ConfirmButton", panel, 0, 0, 240, 70);
        var okImg = Img(okRt, BtnGray, round, true, true);
        var okBtn = MakeButton(okRt, okImg);
        Txt("Label", okRt, "확인", 34, Ink, TextAlignmentOptions.Center, 0, 0, 240, 70);
        var okLe = okRt.gameObject.AddComponent<LayoutElement>();
        okLe.preferredWidth = 240;
        okLe.preferredHeight = 70;

        Set(so, "keyGuidePanel", panel.gameObject);
        Set(so, "keyGuideTitle", title);
        Set(so, "keyGuideRows", rowsGo.GetComponent<RectTransform>());
        Set(so, "keyGuideRowTemplate", rowGo);
        Set(so, "keyCapTemplate", capGo);
        Set(so, "keyGuideConfirmButton", okBtn);
    }

    // ----- PC 화면 -----
    static void BuildPCPanel(Transform canvas, SerializedObject so)
    {
        var panel = Full("PCPanel", canvas);
        Img(panel, Dim, null, false, true); // 뒤쪽 클릭 막기

        // 모니터 외곽선 (1400x640, 대사창 위쪽에 위치)
        var frame = Rect("Monitor", panel, 0, 0, 1400, 640);
        frame.anchorMin = frame.anchorMax = new Vector2(0.5f, 0.5f);
        frame.pivot = new Vector2(0.5f, 0.5f);
        frame.anchoredPosition = new Vector2(0, 130);
        Img(frame, new Color(0.12f, 0.14f, 0.16f), round, true);

        var screen = Rect("Screen", frame, 24, 24, 1352, 592);
        Img(screen, new Color(0.95f, 0.97f, 0.98f), null, false, true);
        Txt("Header", screen, "DUNGEON EXPRESS", 26, Muted, TextAlignmentOptions.MidlineLeft, 24, 12, 600, 40);

        // 홈 화면: 의뢰 접수 아이콘 버튼
        var home = Rect("HomeView", screen, 0, 0, 1352, 592);
        var iconRt = Rect("RequestButton", home, 546, 160, 260, 260);
        var iconImg = Img(iconRt, new Color(0.9f, 0.9f, 0.9f), round, true, true);
        var requestBtn = MakeButton(iconRt, iconImg);
        Txt("Label", iconRt, "의뢰 접수", 40, Ink, TextAlignmentOptions.Center, 0, 0, 260, 260);

        // 의뢰 접수 화면
        var quest = Rect("QuestView", screen, 0, 0, 1352, 592);
        quest.gameObject.SetActive(false);

        var tabs = Rect("Tabs", quest, 24, 64, 240, 80);
        Img(tabs, new Color(0.82f, 0.82f, 0.82f));
        Txt("Label", tabs, "의뢰 접수", 32, Ink, TextAlignmentOptions.Center, 0, 0, 240, 80);

        Txt("Region", quest, "정글 던전", 34, Ink, TextAlignmentOptions.MidlineLeft, 300, 64, 400, 56);

        var row = Rect("QuestRow", quest, 300, 140, 520, 84);
        Img(row, new Color(0.88f, 0.88f, 0.88f));
        var nameText = Txt("QuestName", row, "", 32, Ink, TextAlignmentOptions.MidlineLeft, 24, 0, 360, 84);
        var acceptRt = Rect("AcceptButton", row, 390, 14, 110, 56);
        var acceptImg = Img(acceptRt, new Color(0.85f, 0.15f, 0.15f), round, true, true);
        var acceptBtn = MakeButton(acceptRt, acceptImg);
        Txt("Label", acceptRt, "접수", 30, Color.white, TextAlignmentOptions.Center, 0, 0, 110, 56);

        var detailBox = Rect("DetailBox", quest, 860, 64, 470, 500);
        Img(detailBox, new Color(1f, 1f, 1f));
        Txt("DetailTitle", detailBox, "의뢰 정보", 32, Ink, TextAlignmentOptions.MidlineLeft, 24, 16, 420, 50).fontStyle = FontStyles.Bold;
        var detail = Txt("DetailText", detailBox, "", 28, Ink, TextAlignmentOptions.TopLeft, 24, 80, 422, 400);

        Set(so, "pcPanel", panel.gameObject);
        Set(so, "pcHomeView", home.gameObject);
        Set(so, "pcRequestButton", requestBtn);
        Set(so, "pcQuestView", quest.gameObject);
        Set(so, "pcQuestNameText", nameText);
        Set(so, "pcDetailText", detail);
        Set(so, "pcAcceptButton", acceptBtn);
    }

    // ----- 던전 선택 (문) -----
    static void BuildDungeonPanel(Transform canvas, SerializedObject so)
    {
        var panel = Full("DungeonPanel", canvas);
        Img(panel, Dim, null, false, true);

        var card = Rect("Card", panel, 0, 0, 760, 560);
        card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
        card.pivot = new Vector2(0.5f, 0.5f);
        card.anchoredPosition = Vector2.zero;
        Img(card, new Color(0.95f, 0.95f, 0.95f), round, true, true);

        Txt("Title", card, "던전 선택", 48, Ink, TextAlignmentOptions.Center, 0, 36, 760, 70).fontStyle = FontStyles.Bold;

        var jungleRt = Rect("JungleButton", card, 80, 150, 600, 100);
        var jungleImg = Img(jungleRt, new Color(0.87f, 0.87f, 0.87f), round, true, true);
        var jungleBtn = MakeButton(jungleRt, jungleImg);
        jungleBtn.transition = Selectable.Transition.None;
        Txt("Label", jungleRt, "정글 던전", 40, Ink, TextAlignmentOptions.Center, 0, 0, 600, 100);

        var enterRt = Rect("EnterButton", card, 420, 420, 260, 80);
        var enterImg = Img(enterRt, new Color(0.85f, 0.15f, 0.15f), round, true, true);
        var enterBtn = MakeButton(enterRt, enterImg);
        Txt("Label", enterRt, "입장", 38, Color.white, TextAlignmentOptions.Center, 0, 0, 260, 80);

        var closeRt = Rect("CloseButton", card, 80, 420, 220, 80);
        var closeImg = Img(closeRt, BtnGray, round, true, true);
        var closeBtn = MakeButton(closeRt, closeImg);
        Txt("Label", closeRt, "닫기", 38, Ink, TextAlignmentOptions.Center, 0, 0, 220, 80);

        Set(so, "dungeonPanel", panel.gameObject);
        Set(so, "dungeonJungleButton", jungleBtn);
        Set(so, "dungeonJungleImage", jungleImg);
        Set(so, "dungeonEnterButton", enterBtn);
        Set(so, "dungeonCloseButton", closeBtn);
    }

    // ----- 예 / 아니요 -----
    static void BuildChoice(Transform canvas, SerializedObject so)
    {
        var panel = Rect("ChoicePanel", canvas, 0, 0, 340, 190);
        panel.anchorMin = panel.anchorMax = new Vector2(1, 0);
        panel.pivot = new Vector2(1, 0);
        panel.anchoredPosition = new Vector2(-80, 290);
        Img(panel, PanelGray, round, true, true);

        var yesRt = Rect("YesButton", panel, 20, 20, 300, 66);
        var yesImg = Img(yesRt, new Color(0.95f, 0.95f, 0.95f), round, true, true);
        var yes = MakeButton(yesRt, yesImg);
        Txt("Label", yesRt, "예", 36, Ink, TextAlignmentOptions.Center, 0, 0, 300, 66);

        var noRt = Rect("NoButton", panel, 20, 104, 300, 66);
        var noImg = Img(noRt, new Color(0.95f, 0.95f, 0.95f), round, true, true);
        var no = MakeButton(noRt, noImg);
        Txt("Label", noRt, "아니요", 36, Ink, TextAlignmentOptions.Center, 0, 0, 300, 66);

        Set(so, "choicePanel", panel.gameObject);
        Set(so, "choiceYesButton", yes);
        Set(so, "choiceNoButton", no);
    }

    // ----- 토스트 (짧은 알림) -----
    static void BuildToast(Transform canvas, SerializedObject so)
    {
        var panel = Rect("Toast", canvas, 0, 0, 900, 80);
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0);
        panel.pivot = new Vector2(0.5f, 0);
        panel.anchoredPosition = new Vector2(0, 330);
        Img(panel, new Color(0.1f, 0.1f, 0.1f, 0.85f), round, true);
        var text = Txt("Text", panel, "", 32, Color.white, TextAlignmentOptions.Center, 0, 0, 900, 80);

        Set(so, "toastPanel", panel.gameObject);
        Set(so, "toastText", text);
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

    // 아래쪽에 붙는 가로 가득 찬 패널 (left/right 여백, bottom 위치, 높이)
    static RectTransform Bottom(string name, Transform parent, float left, float right, float bottom, float height)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 0);
        rt.pivot = new Vector2(0.5f, 0);
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, bottom + height);
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
