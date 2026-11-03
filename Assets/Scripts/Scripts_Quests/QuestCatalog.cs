using System.Collections.Generic;
using UnityEngine;

// 게임에 있는 퀘스트 목록입니다. Resources/QuestCatalog.asset 하나만 만들어서 씁니다.
// - allQuests: 모든 퀘스트. questId로 QuestData를 찾을 때 씀 (나중에 저장 불러오기용)
// - pcQuests: 사무실 PC의 '의뢰 접수' 목록에 뜨는 일반/서브 퀘스트
[CreateAssetMenu(fileName = "QuestCatalog", menuName = "Dungeon Express/Quest Catalog")]
public class QuestCatalog : ScriptableObject
{
    public const string ResourcePath = "QuestCatalog";

    public List<QuestData> allQuests = new List<QuestData>();
    public List<QuestData> pcQuests = new List<QuestData>();

    public static QuestCatalog Load()
    {
        return Resources.Load<QuestCatalog>(ResourcePath);
    }

    public QuestData FindById(string questId)
    {
        if (string.IsNullOrEmpty(questId)) return null;
        foreach (var quest in allQuests)
        {
            if (quest != null && quest.questId == questId) return quest;
        }
        return null;
    }
}
