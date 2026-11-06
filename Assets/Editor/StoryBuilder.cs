using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// 메인 퀘스트 1-1 이야기에 필요한 데이터와 씬 오브젝트를 만듭니다.
//   1) Dungeon Express > Build Story Data         : 대사 에셋(Assets/Data_Story)과 메인 퀘스트 에셋(메인 1-1, 1-2 x3) 생성/갱신
//   2) Dungeon Express > Build Story (Town)       : TownScene을 연 상태에서. StoryDirector(대사창 + 스토리 진행기) 생성
//   3) Dungeon Express > Build Story Objects (Jungle) : JungleDungeonScene을 연 상태에서. 1-2-2에 레온과 영약 우물 배치
// 대사나 퀘스트 내용을 바꾸고 싶으면 이 스크립트의 문장을 고치고 1번을 다시 실행하세요. (같은 에셋을 갱신함)
public static class StoryBuilder
{
    const string FontAssetPath = "Assets/Fonts/MalgunGothic SDF.asset";
    const string DialogueDir = "Assets/Data_Story/";
    const string QuestDir = "Assets/Data_Quests/";
    const string ItemDir = "Assets/Data_Items/";
    const string CatalogPath = "Assets/Resources/QuestCatalog.asset";
    const string StoryMapId = "1-3-3"; // 시안의 1-2-2 유적 (2). 레온/영약 우물을 둘 맵 구역 (보물방이 생길 때까지 임시)

    static readonly Color Ink = new Color(0.1f, 0.1f, 0.1f);
    static readonly Color Muted = new Color(0.45f, 0.45f, 0.45f);
    static readonly Color PanelGray = new Color(0.87f, 0.87f, 0.87f, 0.97f);
    static readonly Color TabGray = new Color(0.35f, 0.35f, 0.35f);

    static TMP_FontAsset font;
    static Sprite round;

    // ---------- 대사 (기획서 그대로. "화자: 대사", 화자가 없는 줄은 설명 문장) ----------

    const string BossIntro =
@"사장: 아, 왔군.
사장: 이상한 의뢰가 들어와서 말이야. PC를 확인해 보게나.";

    // 확인하지 못한 임시 문구: 기획서에 없는 '다시 말을 걸었을 때' 대사
    const string BossRemind =
@"사장: PC를 확인해 보게나.";

    const string PcAccept =
@"사장: 이상한 의뢰이지 않은가? 고작 유리병 3개에 5000만 골드라니!
사장: 새 PC 장만한 지 얼마나 됐다고 벌써 고장난건 아니겠지?
사장: 자네가 확인하러 가줄 수 있겠나?
플레이어: (고개를 끄덕인다.)
사장: 좋아, 그럼… 일단 의뢰는 접수해놓겠네. 가서 확인해 보게.";

    const string PcAfter =
@"사장: 음… 혹시 진짜 의뢰일지도 모르니 물품도 같이 가져가고.";

    const string LeonMeet =
@"레온: 혹시 던전 익스프레스에서 오셨나요…?
플레이어: (고개를 끄덕이며 의뢰 물품을 건넨다)";

    const string LeonTalk =
@"레온: 드디어…! 감사합니다, 감사합니다…! 문 앞에 있던 몬스터는… 해치우신 건가요?
플레이어: (고개를 끄덕인다.)
레온: 약속드린 보수는… 모든 의뢰가 끝난 후, 이 방에 있는 보물들로 드릴게요. 아무거나 집어 가셔도 5000만 골드 값은 할 겁니다.
플레이어: ‘모든 의뢰?’
레온: (잠시 숨을 고른 뒤, 뒤편에 있는 액체를 유리병에 담기 시작한다.)
레온: 이건… 아무리 큰 부상을 입은 사람이라도 치료할 수 있는 영약입니다.
레온: 저는 며칠 전, 우연히 보물 지도를 발견해서… 이 영약을 찾기 위해 동료들과 함께 던전에 들어왔어요.
레온: 하지만 보스에게 습격받는 바람에… 동료들과 뿔뿔이 흩어지고 말았어요. 분명 모두 크게 다쳤을 겁니다.
레온: 저는 어떻게든 이곳까지 도망쳐 왔지만… 이 던전의 몬스터들은 제 실력으로는 감당하기가 어렵더군요.
레온: 동료들에게 직접 전달하러 나가는 건 무리일 것 같아요…
레온: 염치없는 부탁인 건 알지만… 이 영약을 담은 유리병을 제 동료들에게 전달해 주실 수 있을까요?
레온: 그들이 아직 살아 있다면… 이 영약이 분명 큰 도움이 될 겁니다.
플레이어: (고개를 끄덕인다.)
레온: 정말 감사합니다!
플레이어: (유리병에 액체를 모두 담는다.)";

    // 확인하지 못한 임시 문구: 기획서에 없는 '다시 말을 걸었을 때' 대사
    const string LeonRemind =
@"레온: 제 동료들을 부탁드립니다…";

    // ---------- 1) 데이터 ----------

    [MenuItem("Dungeon Express/Build Story Data")]
    public static void BuildData()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Data_Story")) AssetDatabase.CreateFolder("Assets", "Data_Story");

        CreateDialogue("Story_BossIntro", BossIntro);
        CreateDialogue("Story_BossRemind", BossRemind);
        CreateDialogue("Story_PcAccept", PcAccept);
        CreateDialogue("Story_PcAfter", PcAfter);
        CreateDialogue("Story_LeonMeet", LeonMeet);
        CreateDialogue("Story_LeonTalk", LeonTalk);
        CreateDialogue("Story_LeonRemind", LeonRemind);

        // 물품 특성: 표에 '충격 주의'로 적힌 유리병과 영약
        SetFragile("Item_유리병");
        SetFragile("Item_영약이담긴병");

        var glass = Load<ItemData>(ItemDir + "Item_유리병.asset");
        var elixir = Load<ItemData>(ItemDir + "Item_영약이담긴병.asset");

        var main11 = CreateQuest("Main_1-1_5000만골드의의뢰", "main_1_1", "5000만 골드의 의뢰", "레온", glass, 3, 50000000,
            "유적 안쪽에서 발이 묶여버렸습니다. 시간이 없으니 빨리 와주세요. 유리병 3개가 급하게 필요해요. 자세한 이야기는 나중에 말씀드릴게요.",
            "레온 → 유리병 전달", true);

        // 영약은 우물에서 직접 얻으므로 퀘스트를 시작해도 가방에 자동으로 넣지 않음
        // 보수: 표에는 '5000만 (1, 2, 3 모두 완료했을 경우)'라서 개별 퀘스트는 0으로 둠
        var main121 = CreateQuest("Main_1-2_레온의동료구하기1", "main_1_2_1", "레온의 동료 구하기 (1)", "레온", elixir, 1, 0,
            "제 동료들을 구해주세요. 아마 정글 깊숙한 곳에 흩어져 있을 거예요.", "레온의 동료 → 영약 전달", false);
        var main122 = CreateQuest("Main_1-2_레온의동료구하기2", "main_1_2_2", "레온의 동료 구하기 (2)", "레온", elixir, 1, 0,
            "제 동료들을 구해주세요. 아마 정글 깊숙한 곳에 흩어져 있을 거예요.", "레온의 동료 → 영약 전달", false);
        var main123 = CreateQuest("Main_1-2_레온의동료구하기3", "main_1_2_3", "레온의 동료 구하기 (3)", "레온", elixir, 1, 0,
            "제 동료들을 구해주세요. 아마 정글 깊숙한 곳에 흩어져 있을 거예요.", "레온의 동료 → 영약 전달", false);

        // 카탈로그(전체 퀘스트)에 등록
        var catalog = AssetDatabase.LoadAssetAtPath<QuestCatalog>(CatalogPath);
        if (catalog != null)
        {
            foreach (var quest in new[] { main11, main121, main122, main123 })
            {
                if (quest != null && !catalog.allQuests.Contains(quest)) catalog.allQuests.Add(quest);
            }
            EditorUtility.SetDirty(catalog);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[StoryBuilder] 완료: 대사 7개, 메인 퀘스트 4개 (Assets/Data_Story, Assets/Data_Quests)");
    }

    static void CreateDialogue(string name, string source)
    {
        string path = DialogueDir + name + ".asset";
        var asset = AssetDatabase.LoadAssetAtPath<DialogueSequence>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<DialogueSequence>();
            AssetDatabase.CreateAsset(asset, path);
        }
        asset.SetFromText(source);
        EditorUtility.SetDirty(asset);
    }

    static QuestData CreateQuest(string fileName, string id, string name, string client, ItemData item, int quantity,
        int reward, string description, string objective, bool giveItemsOnStart)
    {
        string path = QuestDir + fileName + ".asset";
        var quest = AssetDatabase.LoadAssetAtPath<QuestData>(path);
        if (quest == null)
        {
            quest = ScriptableObject.CreateInstance<QuestData>();
            AssetDatabase.CreateAsset(quest, path);
        }

        quest.questId = id;
        quest.questName = name;
        quest.category = QuestCategory.Main;
        quest.client = client;
        quest.targetLocation = "정글 던전";
        quest.reward = reward;
        quest.description = description;
        quest.objectives = new List<string> { objective };
        quest.unlocksQuests = new List<QuestData>(); // 1-2 시작 시점은 StoryManager가 직접 정함
        quest.giveItemsOnStart = giveItemsOnStart;
        quest.requiredItems = new List<QuestItemRequirement> { new QuestItemRequirement { item = item, quantity = quantity } };

        EditorUtility.SetDirty(quest);
        return quest;
    }

    static void SetFragile(string itemAsset)
    {
        var item = AssetDatabase.LoadAssetAtPath<ItemData>(ItemDir + itemAsset + ".asset");
        if (item == null) return;
        item.itemType = ItemType.Fragile;
        EditorUtility.SetDirty(item);
    }

    static T Load<T>(string path) where T : Object
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null) Debug.LogWarning("[StoryBuilder] 에셋을 찾지 못했습니다: " + path);
        return asset;
    }

    // ---------- 2) 사무실: StoryDirector ----------

    [MenuItem("Dungeon Express/Build Story (Town)")]
    public static void BuildTown()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name != "TownScene")
        {
            EditorUtility.DisplayDialog("스토리 만들기", "TownScene을 연 상태에서 실행해 주세요.", "확인");
            return;
        }

        BuildData(); // 대사/퀘스트 에셋이 없거나 오래됐으면 갱신
        PrepareHelpers();

        var old = GameObject.Find("StoryDirector");
        if (old != null) Object.DestroyImmediate(old);

        var root = new GameObject("StoryDirector");
        var story = root.AddComponent<StoryManager>();

        var canvasGo = new GameObject("DialogueCanvas", typeof(RectTransform));
        canvasGo.transform.SetParent(root.transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 190; // 튜토리얼 대사창(200) 아래, PC 화면(150) 위
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();
        var runner = canvasGo.AddComponent<DialogueRunner>();

        BuildDialogueBar(canvasGo.transform, runner);
        Wire(story);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[StoryBuilder] 완료: TownScene에 StoryDirector 생성");
    }

    static void Wire(StoryManager story)
    {
        var so = new SerializedObject(story);
        SetRef(so, "bossIntro", Load<DialogueSequence>(DialogueDir + "Story_BossIntro.asset"));
        SetRef(so, "bossRemind", Load<DialogueSequence>(DialogueDir + "Story_BossRemind.asset"));
        SetRef(so, "pcAccept", Load<DialogueSequence>(DialogueDir + "Story_PcAccept.asset"));
        SetRef(so, "pcAfter", Load<DialogueSequence>(DialogueDir + "Story_PcAfter.asset"));
        SetRef(so, "leonMeet", Load<DialogueSequence>(DialogueDir + "Story_LeonMeet.asset"));
        SetRef(so, "leonTalk", Load<DialogueSequence>(DialogueDir + "Story_LeonTalk.asset"));
        SetRef(so, "leonRemind", Load<DialogueSequence>(DialogueDir + "Story_LeonRemind.asset"));
        SetRef(so, "main11", Load<QuestData>(QuestDir + "Main_1-1_5000만골드의의뢰.asset"));
        SetRef(so, "elixir", Load<ItemData>(ItemDir + "Item_영약이담긴병.asset"));

        var list = so.FindProperty("main12");
        string[] files = { "Main_1-2_레온의동료구하기1", "Main_1-2_레온의동료구하기2", "Main_1-2_레온의동료구하기3" };
        list.arraySize = files.Length;
        for (int i = 0; i < files.Length; i++)
            list.GetArrayElementAtIndex(i).objectReferenceValue = Load<QuestData>(QuestDir + files[i] + ".asset");

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // 화면 아래 대사창 (튜토리얼 대사창과 같은 모양)
    static void BuildDialogueBar(Transform canvas, DialogueRunner runner)
    {
        var panel = Bottom("DialoguePanel", canvas, 260);
        var bg = Img(panel, PanelGray, null, false, true);
        var button = MakeButton(panel, bg);
        button.transition = Selectable.Transition.None;

        var tab = Rect("SpeakerBox", panel, 60, -64, 260, 64);
        tab.anchorMin = tab.anchorMax = new Vector2(0, 1);
        tab.pivot = new Vector2(0, 0);
        tab.anchoredPosition = new Vector2(60, 0);
        Img(tab, TabGray);
        var speaker = Txt("SpeakerText", tab, "", 34, Color.white, TextAlignmentOptions.Center, 0, 0, 260, 64);

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

        var so = new SerializedObject(runner);
        SetRef(so, "panel", panel.gameObject);
        SetRef(so, "speakerBox", tab.gameObject);
        SetRef(so, "speakerText", speaker);
        SetRef(so, "lineText", line);
        SetRef(so, "nextHint", hint.gameObject);
        SetRef(so, "panelButton", button);
        so.ApplyModifiedPropertiesWithoutUndo();

        panel.gameObject.SetActive(false);
    }

    // ---------- 3) 정글 1-2-2: 레온과 영약 우물 ----------

    [MenuItem("Dungeon Express/Build Story Objects (Jungle)")]
    public static void BuildJungle()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name != "JungleDungeonScene")
        {
            EditorUtility.DisplayDialog("스토리 오브젝트 만들기", "JungleDungeonScene을 연 상태에서 실행해 주세요.", "확인");
            return;
        }

        PrepareHelpers();

        MapArea area = null;
        foreach (var a in Object.FindObjectsByType<MapArea>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var so = new SerializedObject(a);
            if (so.FindProperty("mapId").stringValue == StoryMapId) { area = a; break; }
        }
        if (area == null)
        {
            EditorUtility.DisplayDialog("스토리 오브젝트 만들기", StoryMapId + " 구역(MapArea)을 찾지 못했습니다.", "확인");
            return;
        }

        // 이전에 만든 레온/우물은 지우고 다시 만듦. 맵 담당이 둔 NPC 자리표시자는 건드리지 않고,
        // 예전 방식으로 내가 붙였던 레온 상호작용만 떼어냄
        RestorePlaceholders();
        Vector3? keptWellPosition = null;
        foreach (string name in new[] { "Leon", "ElixirWell" })
        {
            var existing = GameObject.Find(name);
            if (existing == null) continue;
            if (name == "ElixirWell")
            {
                // 스프라이트가 있는 자식을 옮겼을 수도 있으니 보이는 위치(스프라이트 중심)를 기준으로 기억
                var sr = existing.GetComponentInChildren<SpriteRenderer>();
                keptWellPosition = sr != null ? sr.bounds.center : existing.transform.position;
            }
            Object.DestroyImmediate(existing);
        }

        area.RecalculateBounds();
        Bounds bounds = area.WorldBounds;

        // 레온과 영약 우물은 시안의 '1-2-2 유적 (보물방)'에 있음: 유적 (1) → 유적 (2) 끝의 보스룸 → 숨겨진 길 → 맨 오른쪽 보물방
        // 정글 씬에서는 시안의 '유적 (2)'가 '1-3-3'이고, 보스룸/숨겨진 길/보물방은 아직 만들어지지 않았음
        // 그래서 보물방이 생길 때까지 임시로 이 구역의 가장 오른쪽 평평한 바닥에 둠 (보물방이 만들어지면 씬에서 옮기면 됨)
        float anchorX, floorY;
        bool foundFlat = FindRightmostFlat(bounds, out anchorX, out floorY);

        float leonX = anchorX - 3.2f;
        float leonY = LowestFloorTop(bounds, leonX);
        BuildLeon(area.transform, new Vector3(leonX, leonY, 0f));

        float wellX = anchorX - 0.8f;
        float wellY = LowestFloorTop(bounds, wellX);
        // 사용자가 씬에서 옮겨 둔 우물이 이 구역 오른쪽 끝 근처에 있으면 그 위치를 유지 (BuildWell은 바닥 높이 + 0.7에 만듦)
        if (keptWellPosition.HasValue && keptWellPosition.Value.x > bounds.max.x - 40f && keptWellPosition.Value.x <= bounds.max.x)
        {
            wellX = keptWellPosition.Value.x;
            wellY = keptWellPosition.Value.y - 0.7f;
        }
        BuildWell(area.transform, new Vector3(wellX, wellY, 0f));

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[StoryBuilder] 완료: " + StoryMapId + " 오른쪽 끝(" + (foundFlat ? "평평한 바닥" : "평평한 곳을 못 찾아 대체 위치") + ")에 레온 (" + leonX + ", " + leonY + "), 영약 우물 (" + wellX + ", " + wellY + ")");
    }

    // 예전에 맵 담당의 NPC 자리표시자(NPCs 아래)에 붙였던 레온 상호작용(컴포넌트, 감지 범위, 'F' 안내)을 떼어냄
    static void RestorePlaceholders()
    {
        foreach (var interact in Object.FindObjectsByType<StoryInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var tr = interact.transform;
            if (tr.parent == null || tr.parent.name != "NPCs") continue;

            var prompt = tr.Find("Prompt");
            if (prompt != null) Object.DestroyImmediate(prompt.gameObject);
            foreach (var col in tr.GetComponents<BoxCollider2D>())
            {
                if (col.isTrigger) Object.DestroyImmediate(col);
            }
            Object.DestroyImmediate(interact);
            EditorUtility.SetDirty(tr.gameObject);
        }
    }

    // 구역 맨 아래 바닥에서 가장 오른쪽에 있는 평평하고 머리 위가 트인 자리(너비 5칸)를 찾음. x는 그 오른쪽 끝
    static bool FindRightmostFlat(Bounds bounds, out float x, out float floorY)
    {
        for (float cx = bounds.max.x - 2f; cx > bounds.min.x + 6f; cx -= 0.5f)
        {
            float f0 = LowestFloorTop(bounds, cx);
            bool ok = true;
            for (float dx = -5f; dx <= 0f && ok; dx += 1f)
            {
                float f = LowestFloorTop(bounds, cx + dx);
                if (Mathf.Abs(f - f0) > 0.2f) { ok = false; break; }
                for (float h = 1f; h <= 3f; h += 1f)
                {
                    if (IsSolid(cx + dx, f + h)) { ok = false; break; }
                }
            }
            if (!ok) continue;

            x = cx;
            floorY = f0;
            return true;
        }

        x = bounds.max.x - 8f;
        floorY = LowestFloorTop(bounds, x);
        return false;
    }

    // x 한 줄을 위에서 아래로 훑어서, 빈 곳에서 단단한 곳으로 바뀌는 지점 중 가장 낮은 곳(바닥 윗면)의 높이
    // 못 찾으면 구역 맨 아래에서 바닥 한 칸 두께만큼 위를 돌려줌
    static float LowestFloorTop(Bounds bounds, float x)
    {
        Physics2D.SyncTransforms(); // 방금 지우거나 옮긴 오브젝트가 충돌 검사에 남아 있지 않게

        float lowest = float.MaxValue;
        bool prevSolid = IsSolid(x, bounds.max.y + 0.5f);
        for (float y = bounds.max.y + 0.3f; y > bounds.min.y - 0.3f; y -= 0.1f)
        {
            bool solid = IsSolid(x, y);
            if (solid && !prevSolid) lowest = y;
            prevSolid = solid;
        }
        return lowest == float.MaxValue ? bounds.min.y + 0.9f : lowest;
    }

    static bool IsSolid(float x, float y)
    {
        foreach (var c in Physics2D.OverlapPointAll(new Vector2(x, y)))
        {
            if (!c.isTrigger) return true;
        }
        return false;
    }

    static GameObject BuildLeon(Transform parent, Vector3 groundPos)
    {
        var root = NewInteractable("Leon", parent, groundPos + new Vector3(0f, 0.85f, 0f), StoryManager.IdLeon, new Vector2(3.5f, 3f));
        // 시안처럼 파란 타원(의뢰인). 아트가 나오면 이 스프라이트만 바꾸면 됨
        Block(root.transform, "Body", Vector3.zero, new Vector2(0.9f, 1.7f), new Color(0.1f, 0.1f, 1f), 1);
        return root;
    }

    static GameObject BuildWell(Transform parent, Vector3 groundPos)
    {
        var root = new GameObject("ElixirWell");
        root.transform.SetParent(parent, true);
        root.transform.position = groundPos + new Vector3(0f, 0.7f, 0f);
        var col = root.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(3.5f, 3f);
        var interact = root.AddComponent<StoryInteractable>();
        interact.interactionId = StoryManager.IdElixirWell;
        interact.promptRoot = BuildPrompt(root.transform, new Vector3(0f, 1.7f, 0f));

        Block(root.transform, "Base", Vector3.zero, new Vector2(1.6f, 1.4f), new Color(0.72f, 0.72f, 0.72f), 0);
        Block(root.transform, "Rim", new Vector3(0f, 0.65f, 0f), new Vector2(1.8f, 0.3f), new Color(0.6f, 0.6f, 0.6f), 1);
        return root;
    }

    static GameObject NewInteractable(string name, Transform parent, Vector3 worldPos, string id, Vector2 triggerSize)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent, true);
        root.transform.position = worldPos;
        var col = root.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = triggerSize;
        var interact = root.AddComponent<StoryInteractable>();
        interact.interactionId = id;
        interact.promptRoot = BuildPrompt(root.transform, new Vector3(0f, 1.6f, 0f));
        return root;
    }

    // 가까이 가면 뜨는 'F' 안내 말풍선 (월드 스페이스 캔버스)
    static GameObject BuildPrompt(Transform parent, Vector3 localPos)
    {
        var go = new GameObject("Prompt", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = Vector3.one * 0.01f;
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 50;
        ((RectTransform)go.transform).sizeDelta = new Vector2(90f, 90f);
        var bg = go.AddComponent<Image>();
        bg.sprite = round;
        bg.type = Image.Type.Sliced;
        bg.color = Color.white;
        var outline = go.AddComponent<Outline>();
        outline.effectColor = Ink;
        outline.effectDistance = new Vector2(3f, -3f);
        var label = Txt("Label", go.transform, "F", 56, Ink, TextAlignmentOptions.Center, 0, 0, 90, 90);
        label.fontStyle = FontStyles.Bold;
        go.SetActive(false);
        return go;
    }

    // 월드 크기(단위)에 맞춘 단색 도형
    static void Block(Transform parent, string name, Vector3 localPos, Vector2 size, Color color, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = round;
        Vector2 baseSize = round.bounds.size;
        go.transform.localScale = new Vector3(size.x / baseSize.x, size.y / baseSize.y, 1f);
        sr.color = color;
        sr.sortingOrder = order;
    }

    // ---------- 헬퍼 (TabletMenuBuilder와 같은 방식) ----------

    static void PrepareHelpers()
    {
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (font == null) throw new System.InvalidOperationException("한글 TMP 폰트를 찾을 수 없습니다: " + FontAssetPath);
        round = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
    }

    static void SetRef(SerializedObject so, string field, Object value)
    {
        var p = so.FindProperty(field);
        if (p == null) { Debug.LogWarning("[StoryBuilder] 필드를 찾을 수 없음: " + field); return; }
        p.objectReferenceValue = value;
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

    // 아래쪽에 붙는 가로 가득 찬 패널
    static RectTransform Bottom(string name, Transform parent, float height)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 0);
        rt.pivot = new Vector2(0.5f, 0);
        rt.offsetMin = new Vector2(0, 0);
        rt.offsetMax = new Vector2(0, height);
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
        return b;
    }
}
