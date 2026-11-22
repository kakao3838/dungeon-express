using System;
using System.Collections.Generic;

// 저장 슬롯 하나의 전체 데이터
[Serializable]
public class SaveData
{
    public int version = 1;
    public string savedAtUtc;
    public float playTimeSeconds;
    // Slot summary. Chapter 0 means tutorial / before the main story.
    public int chapter;
    public string mainQuestId;
    public string mainQuestName;

    // PlayerWallet.Gold
    public int gold;

    // Inventory
    public int maxSlots = 2;
    public float qualityLossMultiplier = 1f;

    // 목록 순서가 가방 칸 순서. 한 항목은 아이템 한 개.
    public List<SavedItem> items = new List<SavedItem>();

    // Inventory.LostItems
    public List<SavedItem> lostItems = new List<SavedItem>();

    // PlayerEquipment
    public List<SavedEquipment> equipped = new List<SavedEquipment>();

    // QuestManager
    // 진행 목록 순서를 유지해야 마지막 의뢰가 추적 대상으로 복원됨.
    public List<string> activeQuestIds = new List<string>();
    public List<string> completedQuestIds = new List<string>();
    public List<string> flags = new List<string>();

    // QuestManager가 보관하는 의뢰별 납품 품질 기록
    public List<SavedQuestDelivery> deliveredQualities = new List<SavedQuestDelivery>();

    // TutorialManager의 세션 내 상태
    public bool tutorialCompleted;
    public bool dungeonGuideShown;
}

// 가방 아이템과 분실물에 공통으로 사용
[Serializable]
public class SavedItem
{
    // ItemData.itemId
    public string itemId;

    // 가방: Inventory.GetQuality(index)
    // 분실물: LostItem.quality
    public int quality = 100;
}

// 장착 중인 장비
[Serializable]
public class SavedEquipment
{
    // EquipmentData에는 아직 없는 필드.
    public string equipmentId;

    public EquipSlotType slotType;
    public UpgradeGrade grade;
    public string magicEffect;
}

// 의뢰별 납품 물품의 평균 품질
[Serializable]
public class SavedQuestDelivery
{
    public string questId;

    // QuestData.requiredItems 순서와 같음
    public int[] qualities = new int[0];
}