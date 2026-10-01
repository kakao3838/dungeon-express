using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

// 태블릿 메뉴(Resources/TabletMenu.prefab)를 시안(1920x1080 기준 좌표)대로 생성합니다.
// 메뉴: Dungeon Express > Build Tablet Menu
// 레이아웃을 바꾸고 싶으면 이 스크립트의 좌표를 수정하고 다시 실행하세요.
public static class TabletMenuBuilder
{
    const string FontAssetPath = "Assets/Fonts/MalgunGothic SDF.asset";
    const string ArtDir = "Assets/artsources/menu_ temporary/";
    const string PrefabDir = "Assets/Prefab/menu UI/";
    const string MenuPrefabPath = "Assets/Resources/TabletMenu.prefab";

    static readonly Color Ink = new Color(0.1f, 0.1f, 0.1f);
    static readonly Color Muted = new Color(0.45f, 0.45f, 0.45f);
    static readonly Color SlotGray = new Color(0.86f, 0.86f, 0.86f);
    static readonly Color PillGray = new Color(0.87f, 0.87f, 0.87f);
    static readonly Color Selected = new Color(0.85f, 0.15f, 0.15f);

    static TMP_FontAsset font;
    static Sprite round;

    [MenuItem("Dungeon Express/Build Tablet Menu")]
    public static void Build()
    {
        font = EnsureKoreanFont();
        round = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        InventorySlotUI slotPrefab = BuildSlotPrefab();
        QuestEntryUI entryPrefab = BuildQuestEntryPrefab();
        BuildMenuPrefab(slotPrefab, entryPrefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[TabletMenuBuilder] 완료: " + MenuPrefabPath);
    }

    // ---------- 폰트 ----------

    // 프로젝트 공용 한글 TMP 폰트(Assets/Fonts/MalgunGothic SDF)를 사용
    static TMP_FontAsset EnsureKoreanFont()
    {
        var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (asset == null)
            throw new System.InvalidOperationException("한글 TMP 폰트를 찾을 수 없습니다: " + FontAssetPath);
        return asset;
    }

    // ---------- 헬퍼 ----------

    static Sprite Art(string file)
    {
        return AssetDatabase.LoadAllAssetsAtPath(ArtDir + file).OfType<Sprite>().FirstOrDefault();
    }

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

    static RectTransform Stretch(string name, Transform parent, float inset = 0)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
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
        TextAlignmentOptions align, float x, float y, float w, float h, float minSize = 0)
    {
        var rt = Rect(name, parent, x, y, w, h);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.overflowMode = TextOverflowModes.Overflow; // Ellipsis는 박스 높이가 한 줄보다 작으면 글자가 통째로 사라짐
        t.raycastTarget = false;
        if (minSize > 0)
        {
            t.enableAutoSizing = true;
            t.fontSizeMin = minSize;
            t.fontSizeMax = size;
        }
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

    // 선택 표시용 테두리(가운데가 비어 있는 슬라이스 이미지)
    static GameObject OutlineFrame(Transform parent, float outset, Color color)
    {
        var rt = Stretch("SelectedFrame", parent, -outset);
        var img = Img(rt, color, round, true);
        img.fillCenter = false;
        img.pixelsPerUnitMultiplier = 3f;
        return rt.gameObject;
    }

    static GameObject Pill(Transform parent, string label, float x, float y, float w, float h, float fontSize)
    {
        var rt = Rect("Pill_" + label, parent, x, y, w, h);
        Img(rt, PillGray, round, true);
        Txt("Label", rt, label, fontSize, Ink, TextAlignmentOptions.Center, 0, 0, w, h);
        return rt.gameObject;
    }

    static T SavePrefab<T>(GameObject root, string path) where T : Component
    {
        var asset = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return asset.GetComponent<T>();
    }

    // ---------- 개별 프리팹 ----------

    static InventorySlotUI BuildSlotPrefab()
    {
        var rt = Rect("TabletInventorySlot", null, 0, 0, 110, 110);
        var bg = Img(rt, SlotGray, round, true);
        var button = MakeButton(rt, bg);
        var slot = rt.gameObject.AddComponent<InventorySlotUI>();

        var icon = Img(Stretch("Icon", rt, 14), Color.white);
        icon.preserveAspect = true;
        var fallback = Txt("FallbackLabel", rt, "", 22, Ink, TextAlignmentOptions.Center, 0, 0, 0, 0, 12);
        var fr = (RectTransform)fallback.transform;
        fr.pivot = new Vector2(0.5f, 0.5f); // 피벗을 먼저 바꿔야 아래 오프셋이 어긋나지 않음
        fr.anchorMin = Vector2.zero; fr.anchorMax = Vector2.one;
        fr.offsetMin = new Vector2(8, 8); fr.offsetMax = new Vector2(-8, -8);

        var lockRt = Rect("LockIcon", rt, 27, 27, 56, 56);
        Img(lockRt, Color.white, Art("icon_lock.png.png"));
        var selected = OutlineFrame(rt, 5, Selected);

        slot.iconImage = icon;
        slot.fallbackLabel = fallback;
        slot.lockIcon = lockRt.gameObject;
        slot.selectedFrame = selected;
        slot.button = button;
        UnityEventTools.AddVoidPersistentListener(button.onClick, slot.OnClick);

        return SavePrefab<InventorySlotUI>(rt.gameObject, PrefabDir + "TabletInventorySlot.prefab");
    }

    static QuestEntryUI BuildQuestEntryPrefab()
    {
        var rt = Rect("TabletQuestEntry", null, 0, 0, 650, 72);
        var bg = Img(rt, new Color(0.93f, 0.93f, 0.93f), round, true);
        var button = MakeButton(rt, bg);
        var entry = rt.gameObject.AddComponent<QuestEntryUI>();

        var badge = Pill(rt, "일반", 16, 20, 76, 32, 20);
        var name = Txt("Name", rt, "", 28, Ink, TextAlignmentOptions.MidlineLeft, 112, 0, 520, 72, 16);
        var selected = OutlineFrame(rt, 4, Selected);

        entry.badgeText = badge.GetComponentInChildren<TMP_Text>();
        entry.nameText = name;
        entry.selectedFrame = selected;
        UnityEventTools.AddVoidPersistentListener(button.onClick, entry.OnClick);

        return SavePrefab<QuestEntryUI>(rt.gameObject, PrefabDir + "TabletQuestEntry.prefab");
    }

    // ---------- 메뉴 프리팹 ----------

    static void BuildMenuPrefab(InventorySlotUI slotPrefab, QuestEntryUI entryPrefab)
    {
        var root = new GameObject("TabletMenu", typeof(RectTransform));
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        root.AddComponent<GraphicRaycaster>();
        var manager = root.AddComponent<MenuManager>();

        var menuRoot = Stretch("MenuRoot", root.transform);
        Img(Stretch("Backdrop", menuRoot), Color.white, null, false, true);

        var frame = Rect("Frame", menuRoot, 0, 0, 1920, 1080);
        frame.anchorMin = frame.anchorMax = new Vector2(0.5f, 0.5f);
        frame.pivot = new Vector2(0.5f, 0.5f);
        frame.anchoredPosition = Vector2.zero;
        Img(Stretch("TabletBackground", frame), Color.white, Art("bg_tablet_frame.jpg.jpg"), false, true);

        var highlights = BuildSidebar(frame, manager);

        var content = Stretch("ContentRoot", frame);
        var equipment = BuildEquipmentPanel(content);
        var inventory = BuildInventoryPanel(content, slotPrefab);
        var quest = BuildQuestPanel(content, entryPrefab);
        var map = BuildPlaceholderPanel(content, "MapPanel", "지도 (준비 중)");
        var codex = BuildPlaceholderPanel(content, "CodexPanel", "도감 (준비 중)");

        var so = new SerializedObject(manager);
        so.FindProperty("menuRoot").objectReferenceValue = menuRoot.gameObject;
        so.FindProperty("equipmentPanel").objectReferenceValue = equipment;
        so.FindProperty("inventoryPanel").objectReferenceValue = inventory;
        so.FindProperty("questPanel").objectReferenceValue = quest;
        so.FindProperty("mapPanel").objectReferenceValue = map;
        so.FindProperty("codexPanel").objectReferenceValue = codex;
        var hl = so.FindProperty("tabHighlights");
        hl.arraySize = highlights.Length;
        for (int i = 0; i < highlights.Length; i++) hl.GetArrayElementAtIndex(i).objectReferenceValue = highlights[i];
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, MenuPrefabPath);
        Object.DestroyImmediate(root);
    }

    // 배경 이미지에 사이드바 글자(장비/가방/의뢰/지도/도감)가 그려져 있어서, 그 위에 투명 버튼 + 선택 표시만 얹음
    static GameObject[] BuildSidebar(Transform frame, MenuManager manager)
    {
        var sidebar = Stretch("Sidebar", frame);
        float[] centerY = { 267, 339, 411, 483, 555 };
        UnityEngine.Events.UnityAction[] actions =
        {
            manager.OpenEquipment, manager.OpenInventory, manager.OpenQuest, manager.OpenMap, manager.OpenCodex
        };
        string[] names = { "Equipment", "Inventory", "Quest", "Map", "Codex" };

        var highlights = new GameObject[5];
        for (int i = 0; i < 5; i++)
        {
            var hrt = Rect(names[i] + "Highlight", sidebar, 160, centerY[i] - 26, 200, 52);
            // 글자가 배경 이미지에 그려져 있어서 선택 표시가 글자를 가리지 않도록 반투명으로 덮음
            Img(hrt, new Color(0.55f, 0.55f, 0.55f, 0.35f), round, true);
            highlights[i] = hrt.gameObject;
        }
        for (int i = 0; i < 5; i++)
        {
            var brt = Rect(names[i] + "Button", sidebar, 160, centerY[i] - 26, 200, 52);
            var clear = Img(brt, new Color(1, 1, 1, 0));
            var button = MakeButton(brt, clear);
            UnityEventTools.AddVoidPersistentListener(button.onClick, actions[i]);
        }
        return highlights;
    }

    static GameObject BuildPlaceholderPanel(Transform content, string name, string label)
    {
        var panel = Stretch(name, content);
        Txt("Text", panel, label, 40, Muted, TextAlignmentOptions.Center, 400, 470, 1400, 80);
        return panel.gameObject;
    }

    static GameObject BuildEquipmentPanel(Transform content)
    {
        var panel = Stretch("EquipmentPanel", content);

        var character = Rect("CharacterArea", panel, 440, 190, 300, 640);
        Img(character, PillGray, round, true);
        Txt("Label", character, "PC\n전신 or 반신", 30, Ink, TextAlignmentOptions.Center, 0, 0, 300, 640);

        string[] slotNames = { "무기", "머리", "상의", "신발" };
        for (int i = 0; i < slotNames.Length; i++)
        {
            float y = 190 + i * 165;
            var slot = Rect(slotNames[i] + "Slot", panel, 800, y, 120, 120);
            Img(slot, SlotGray, round, true);
            Txt("Label", panel, slotNames[i], 22, Muted, TextAlignmentOptions.Center, 800, y + 124, 120, 30);
        }

        Txt("Empty", panel, "장착한 장비가 없습니다.", 30, Muted, TextAlignmentOptions.Center, 1230, 450, 560, 60);
        return panel.gameObject;
    }

    static GameObject BuildInventoryPanel(Transform content, InventorySlotUI slotPrefab)
    {
        var panel = Stretch("InventoryPanel", content);
        var controller = panel.gameObject.AddComponent<InventoryPanelController>();
        controller.slotPrefab = slotPrefab;

        // 카테고리 탭 (아이콘)
        string[] tabIcons = { "icon_tab_delivery.png.png", "icon_tab_consumable.png.png" };
        controller.tabButtons = new Button[2];
        controller.tabUnderlines = new GameObject[2];
        Img(Rect("TabLine", panel, 470, 212, 654, 2), new Color(0.8f, 0.8f, 0.8f));
        for (int i = 0; i < 2; i++)
        {
            var tab = Rect("Tab" + i, panel, 470 + i * 84, 140, 64, 64);
            var icon = Img(tab, Color.white, Art(tabIcons[i]));
            icon.preserveAspect = true;
            var button = MakeButton(tab, icon);
            UnityEventTools.AddIntPersistentListener(button.onClick, controller.SelectTab, i);
            controller.tabButtons[i] = button;

            var underline = Rect("Underline", panel, 470 + i * 84, 210, 64, 5);
            Img(underline, Ink);
            controller.tabUnderlines[i] = underline.gameObject;
        }

        // 재화
        Img(Rect("CoinIcon", panel, 1012, 150, 44, 44), Color.white, Art("icon_coin.png.png"));
        controller.coinText = Txt("CoinText", panel, "0", 34, Ink, TextAlignmentOptions.MidlineLeft, 1066, 146, 110, 52, 18);

        // 슬롯 그리드 (5열 x 4행)
        var grid = Rect("SlotGrid", panel, 470, 250, 654, 518);
        var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(110, 110);
        layout.spacing = new Vector2(26, 26);
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 5;
        controller.slotGridParent = grid;

        // 상세 정보
        controller.emptyDetailText = Txt("EmptyDetail", panel, "가방이 비어 있습니다.", 30, Muted,
            TextAlignmentOptions.Center, 1230, 450, 560, 60).gameObject;

        var detail = Rect("ItemDetail", panel, 0, 0, 1920, 1080);
        detail.anchorMin = Vector2.zero; detail.anchorMax = Vector2.one;
        detail.offsetMin = detail.offsetMax = Vector2.zero; detail.pivot = new Vector2(0.5f, 0.5f);
        controller.detailRoot = detail.gameObject;

        controller.categoryLabel = Txt("Category", detail, "배달품", 24, Muted, TextAlignmentOptions.Center, 1230, 132, 560, 40);
        controller.nameText = Txt("Name", detail, "", 44, Ink, TextAlignmentOptions.Center, 1230, 172, 560, 60, 24);

        var iconBox = Rect("IconBox", detail, 1400, 255, 220, 220);
        Img(iconBox, SlotGray, round, true);
        var icon2 = Img(Stretch("Icon", iconBox, 14), Color.white);
        icon2.preserveAspect = true;
        controller.iconImage = icon2;
        controller.iconFallbackText = Txt("IconFallback", iconBox, "", 32, Ink, TextAlignmentOptions.Center, 10, 10, 200, 200, 16);

        var quality = Rect("QualityRow", detail, 1260, 510, 500, 36);
        Pill(quality, "품질", 0, 2, 80, 32, 22);
        var barBg = Rect("QualityBar", quality, 100, 8, 270, 20);
        Img(barBg, PillGray, round, true);
        var fill = Stretch("Fill", barBg);
        Img(fill, new Color(0.2f, 0.78f, 0.2f), round, true);
        fill.anchorMax = new Vector2(0, 1); // 너비는 InventoryPanelController가 품질 값에 맞춰 조절
        fill.offsetMin = fill.offsetMax = Vector2.zero;
        controller.qualityFill = fill;
        controller.qualityRow = quality.gameObject;
        controller.qualityText = Txt("QualityText", quality, "", 24, Ink, TextAlignmentOptions.MidlineLeft, 384, 0, 110, 36);

        var trait = Rect("TraitRow", detail, 1260, 560, 500, 36);
        Pill(trait, "특성", 0, 2, 80, 32, 22);
        controller.traitText = Txt("TraitText", trait, "", 24, Ink, TextAlignmentOptions.MidlineLeft, 100, 0, 400, 36, 16);

        controller.descriptionText = Txt("Description", detail, "", 26, Ink, TextAlignmentOptions.TopLeft, 1260, 640, 500, 300, 16);

        return panel.gameObject;
    }

    static GameObject BuildQuestPanel(Transform content, QuestEntryUI entryPrefab)
    {
        var panel = Stretch("QuestPanel", content);
        var controller = panel.gameObject.AddComponent<QuestPanelController>();
        controller.entryPrefab = entryPrefab;

        // 상단 탭: 메인 / 일반 / 완료
        string[] tabNames = { "메인", "일반", "완료" };
        controller.tabUnderlines = new GameObject[3];
        Img(Rect("TabLine", panel, 470, 212, 654, 2), new Color(0.8f, 0.8f, 0.8f));
        for (int i = 0; i < 3; i++)
        {
            var tab = Rect("Tab" + i, panel, 470 + i * 110, 150, 100, 58);
            var clear = Img(tab, new Color(1, 1, 1, 0));
            var button = MakeButton(tab, clear);
            UnityEventTools.AddIntPersistentListener(button.onClick, controller.SelectTab, i);
            Txt("Label", tab, tabNames[i], 30, Ink, TextAlignmentOptions.Center, 0, 0, 100, 58);

            var underline = Rect("Underline", panel, 470 + i * 110, 210, 100, 5);
            Img(underline, Ink);
            controller.tabUnderlines[i] = underline.gameObject;
        }

        // 목록
        var list = Rect("QuestList", panel, 470, 250, 650, 700);
        var vlg = list.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 16;
        vlg.childControlWidth = false;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = false;
        vlg.childForceExpandHeight = false;
        controller.listParent = list;
        controller.emptyListText = Txt("EmptyList", panel, "해당하는 의뢰가 없습니다.", 28, Muted,
            TextAlignmentOptions.Center, 470, 300, 650, 60).gameObject;

        // 상세 정보
        var detail = Stretch("QuestDetail", panel);
        controller.detailRoot = detail.gameObject;
        controller.categoryLabel = Txt("Category", detail, "", 24, Muted, TextAlignmentOptions.Center, 1230, 132, 560, 40);
        controller.nameText = Txt("Name", detail, "", 44, Ink, TextAlignmentOptions.Center, 1230, 172, 560, 60, 24);

        controller.itemsText = InfoRow(detail, "의뢰 물품", 262);
        controller.locationText = InfoRow(detail, "의뢰 장소", 312);
        controller.rewardText = InfoRow(detail, "의뢰 보상", 362);

        Txt("DescHeader", detail, "의뢰 설명", 24, Muted, TextAlignmentOptions.TopLeft, 1230, 428, 560, 40);
        controller.descriptionText = Txt("Description", detail, "", 26, Ink, TextAlignmentOptions.TopLeft, 1230, 470, 560, 230, 16);
        controller.objectivesText = Txt("Objectives", detail, "", 26, Ink, TextAlignmentOptions.TopLeft, 1230, 720, 560, 200, 16);

        return panel.gameObject;
    }

    static TMP_Text InfoRow(Transform parent, string label, float y)
    {
        Pill(parent, label, 1230, y, 110, 36, 20);
        return Txt(label + "Value", parent, "", 26, Ink, TextAlignmentOptions.MidlineLeft, 1356, y, 440, 36, 14);
    }
}
