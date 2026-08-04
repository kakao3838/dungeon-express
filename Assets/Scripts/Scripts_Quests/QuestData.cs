using System;
using System.Collections.Generic;
using UnityEngine;

public enum QuestCategory
{
    Main,    // 메인 퀘스트
    Side,    // 사이드 퀘스트
    Regular  // 일반 퀘스트
}

[Serializable]
public class QuestItemRequirement
{
    public ItemData item;
    public int quantity = 1;
}

[CreateAssetMenu(fileName = "NewQuest", menuName = "Dungeon Express/Quest Data")]
public class QuestData : ScriptableObject
{
    [Header("기본 정보")]
    public string questName;
    public QuestCategory category;

    [Header("의뢰 물품")]
    public List<QuestItemRequirement> requiredItems = new List<QuestItemRequirement>();

    [Header("의뢰 정보")]
    public string targetLocation; // 의뢰 장소 (예: "정글 던전")
    public int reward;            // 의뢰 보수 (골드)

    [Header("설명")]
    [TextArea(3, 6)]
    public string description;

    [Header("체크리스트 (목표)")]
    public List<string> objectives = new List<string>();

    [Header("연계 퀘스트 (선택)")]
    [Tooltip("이 퀘스트를 완료하면 해금되는 다음 퀘스트(들). 하나의 퀘스트가 여러 퀘스트로 분기될 수 있어서 리스트로 관리해요.")]
    public List<QuestData> unlocksQuests = new List<QuestData>();
}