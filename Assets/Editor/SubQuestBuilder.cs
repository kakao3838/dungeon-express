using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// 서브(일반) 퀘스트 4개를 QuestData 에셋으로 만들고, 퀘스트/아이템에 고유 ID를 채우고,
// Resources/QuestCatalog.asset(전체 퀘스트 + PC 의뢰 목록)을 만듭니다.
// 메뉴: Dungeon Express > Build Sub Quests   (여러 번 실행해도 같은 에셋을 갱신함)
// 서브 퀘스트 내용(보수, 설명 등)을 바꾸고 싶으면 이 스크립트의 표를 수정하고 다시 실행하세요.
public static class SubQuestBuilder
{
    const string QuestDir = "Assets/Data_Quests/";
    const string ItemDir = "Assets/Data_Items/";
    const string CatalogPath = "Assets/Resources/QuestCatalog.asset";

    // 기존 아이템 에셋 이름 → 고유 ID
    static readonly Dictionary<string, string> ItemIds = new Dictionary<string, string>
    {
        { "Item_긴급한전달사항", "urgent_notice" },
        { "Item_냉기결정", "frost_crystal" },
        { "Item_마력물약", "mana_potion" },
        { "Item_반창고", "bandage" },
        { "Item_비상약", "emergency_medicine" },
        { "Item_소금", "salt" },
        { "Item_신선한고기", "fresh_meat" },
        { "Item_신선한야채", "fresh_vegetables" },
        { "Item_아이스크림", "ice_cream" },
        { "Item_여행용식량꾸러미", "travel_rations" },
        { "Item_영약이담긴병", "elixir_bottle" },
        { "Item_와인", "wine" },
        { "Item_우산", "umbrella" },
        { "Item_유리병", "glass_bottle" },
        { "Item_응급치료키트", "first_aid_kit" },
        { "Item_장작묶음", "firewood_bundle" },
        { "Item_정령의이슬", "spirit_dew" },
        { "Item_편지", "letter" },
        { "Item_해독제", "antidote" },
        { "Item_화장지", "toilet_paper" },
    };

    // 이전 구상 기준으로 만들어 둔 퀘스트 에셋 → 고유 ID (새 기획에 맞춰 바꿀 때까지 legacy_ 접두어)
    static readonly Dictionary<string, string> ExistingQuestIds = new Dictionary<string, string>
    {
        { "Tutorial_기본배달", "tutorial" },
        { "Tutorial_스콜배달", "legacy_squall" },
        { "Quest_물병배달", "legacy_glass_bottle" },
        { "Quest_영약배달", "legacy_elixir" },
        { "Quest_보상수령", "legacy_reward" },
        { "Quest_해독제배달", "legacy_antidote" },
        { "Quest_냉기결정배달", "legacy_frost_crystal" },
    };

    class SubQuest
    {
        public string fileName, id, name, client, itemAsset, location, description, objective;
        public int reward;
    }

    static readonly SubQuest[] SubQuests =
    {
        new SubQuest
        {
            fileName = "Sub_사냥꾼의간식", id = "sub_hunter_snack", name = "사냥꾼의 간식", client = "사냥꾼",
            itemAsset = "Item_아이스크림", location = "정글 던전", reward = 50,
            description = "하루종일 사냥하느라 정말 힘들었어요. 열심히 한 보상으로 달달한 걸 먹고 싶네요.",
            objective = "사냥꾼 → 아이스크림 전달",
        },
        new SubQuest
        {
            fileName = "Sub_탐험가의두통", id = "sub_explorer_headache", name = "탐험가의 두통", client = "아픈 탐험가",
            itemAsset = "Item_비상약", location = "정글 던전", reward = 80,
            description = "아까부터 머리가 계속 지끈거리네요. 탐험을 계속하려면 약이 필요할 것 같아요.",
            objective = "아픈 탐험가 → 비상약 전달",
        },
        new SubQuest
        {
            fileName = "Sub_두근두근손편지", id = "sub_love_letter", name = "두근두근 손편지", client = "설레는 탐험가",
            itemAsset = "Item_편지", location = "정글 던전", reward = 60,
            description = "여자친구가 직접 쓴 손편지를 보내주고 싶대요! 소중한 마음이 담긴 편지인 만큼 조심히 배달해 주세요.",
            objective = "설레는 탐험가 → 편지 전달",
        },
        new SubQuest
        {
            fileName = "Sub_신비한폭포수", id = "sub_waterfall_water", name = "신비한 폭포수", client = "신난 여행객",
            itemAsset = "Item_유리병", location = "정글 던전", reward = 60,
            description = "여기 폭포에서 떨어지는 물이 몸에 좋대요! 가족들에게도 전해주고 싶어요.",
            objective = "신난 여행객 → 유리병 전달",
        },
    };

    [MenuItem("Dungeon Express/Build Sub Quests")]
    public static void Build()
    {
        FillItemIds();

        var subAssets = new List<QuestData>();
        foreach (var s in SubQuests) subAssets.Add(CreateOrUpdate(s));

        FillExistingQuestIds();

        // 카탈로그: 전체 퀘스트(Data_Quests 폴더) + PC 의뢰 목록(서브 퀘스트)
        var catalog = AssetDatabase.LoadAssetAtPath<QuestCatalog>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<QuestCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }

        catalog.allQuests.Clear();
        foreach (string guid in AssetDatabase.FindAssets("t:QuestData", new[] { "Assets/Data_Quests" }))
        {
            var quest = AssetDatabase.LoadAssetAtPath<QuestData>(AssetDatabase.GUIDToAssetPath(guid));
            if (quest != null) catalog.allQuests.Add(quest);
        }
        catalog.pcQuests.Clear();
        catalog.pcQuests.AddRange(subAssets);

        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SubQuestBuilder] 완료: 서브 퀘스트 " + subAssets.Count + "개, 전체 퀘스트 " + catalog.allQuests.Count + "개, " + CatalogPath);
    }

    static QuestData CreateOrUpdate(SubQuest s)
    {
        string path = QuestDir + s.fileName + ".asset";
        var quest = AssetDatabase.LoadAssetAtPath<QuestData>(path);
        if (quest == null)
        {
            quest = ScriptableObject.CreateInstance<QuestData>();
            AssetDatabase.CreateAsset(quest, path);
        }

        quest.questId = s.id;
        quest.questName = s.name;
        quest.category = QuestCategory.Regular;
        quest.client = s.client;
        quest.targetLocation = s.location;
        quest.reward = s.reward;
        quest.description = s.description;
        quest.objectives = new List<string> { s.objective };
        quest.unlocksQuests = new List<QuestData>();

        var item = AssetDatabase.LoadAssetAtPath<ItemData>(ItemDir + s.itemAsset + ".asset");
        if (item == null) Debug.LogWarning("[SubQuestBuilder] 아이템 에셋을 찾지 못했습니다: " + s.itemAsset);
        quest.requiredItems = new List<QuestItemRequirement>
        {
            new QuestItemRequirement { item = item, quantity = 1 },
        };

        EditorUtility.SetDirty(quest);
        return quest;
    }

    static void FillItemIds()
    {
        foreach (var pair in ItemIds)
        {
            var item = AssetDatabase.LoadAssetAtPath<ItemData>(ItemDir + pair.Key + ".asset");
            if (item == null) continue;
            item.itemId = pair.Value;
            EditorUtility.SetDirty(item);
        }

        var cake = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Items/cake.asset");
        if (cake != null)
        {
            cake.itemId = "cake";
            EditorUtility.SetDirty(cake);
        }
    }

    static void FillExistingQuestIds()
    {
        foreach (var pair in ExistingQuestIds)
        {
            var quest = AssetDatabase.LoadAssetAtPath<QuestData>(QuestDir + pair.Key + ".asset");
            if (quest == null) continue;
            quest.questId = pair.Value;
            if (pair.Key == "Tutorial_기본배달") quest.client = "카일";
            EditorUtility.SetDirty(quest);
        }
    }
}
