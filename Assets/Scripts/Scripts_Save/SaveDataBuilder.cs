using System;
using UnityEngine;

public static class SaveDataBuilder
{
    public static SaveData Capture(Inventory inventory, PlayerWallet wallet, QuestManager questManager)
    {
        return Capture(inventory, wallet, questManager, SavePlayTime.Seconds);
    }

    public static SaveData Capture(
        Inventory inventory,
        PlayerWallet wallet,
        QuestManager questManager,
        float playTimeSeconds)
    {
        // 연결이 없는데 빈 데이터로 저장되는 것을 방지
        if (inventory == null)
            throw new ArgumentNullException(nameof(inventory));

        if (wallet == null)
            throw new ArgumentNullException(nameof(wallet));

        if (questManager == null)
            throw new ArgumentNullException(nameof(questManager));

        if (float.IsNaN(playTimeSeconds) || float.IsInfinity(playTimeSeconds) || playTimeSeconds < 0f)
            throw new ArgumentOutOfRangeException(nameof(playTimeSeconds));
        ItemCatalog catalog = ItemCatalog.Load();
        if (catalog == null)
            throw new InvalidOperationException("Resources/ItemCatalog is missing.");
        catalog.ValidateIds();
        SaveData data = new SaveData
        {
            savedAtUtc = DateTime.UtcNow.ToString("O"),
            playTimeSeconds = playTimeSeconds,

            gold = wallet.Gold,

            maxSlots = inventory.maxSlots,
            qualityLossMultiplier = inventory.qualityLossMultiplier,

            tutorialCompleted = questManager.HasFlag(
                QuestManager.FlagTutorialDone)
        };

        // 가방: 목록 순서를 유지하면서 아이템별 품질 기록
        for (int i = 0; i < inventory.Items.Count; i++)
        {
            ItemData item = inventory.Items[i];

            data.items.Add(new SavedItem
            {
                itemId = RequireItemId(item, catalog),
                quality = inventory.GetQuality(i)
            });
        }

        // 분실물: 아이템 종류와 손실 당시 품질 기록
        foreach (LostItem lost in inventory.LostItems)
        {
            data.lostItems.Add(new SavedItem
            {
                itemId = RequireItemId(lost.item, catalog),
                quality = lost.quality
            });
        }

        // 진행 중 의뢰: 순서 유지
        foreach (QuestData quest in questManager.activeQuests)
        {
            data.activeQuestIds.Add(RequireQuestId(quest));
        }

        // 완료한 의뢰
        foreach (QuestData quest in questManager.CompletedQuests)
        {
            data.completedQuestIds.Add(RequireQuestId(quest));
            int count = quest.requiredItems.Count;
            int[] qualities = new int[count];
            bool hasRecord = count > 0;
            for (int i = 0; i < count; i++)
            {
                qualities[i] = questManager.GetDeliveredQuality(quest, i, -1);
                if (qualities[i] < 0) hasRecord = false;
            }
            if (hasRecord)
                data.deliveredQualities.Add(new SavedQuestDelivery
                {
                    questId = quest.questId,
                    qualities = qualities
                });
        }

        // 스토리 진행·기능 해금 기록
        foreach (string flag in questManager.Flags)
        {
            data.flags.Add(flag);
        }

        data.flags.Sort(StringComparer.Ordinal);
        data.dungeonGuideShown = TutorialManager.Instance != null && TutorialManager.Instance.DungeonGuideShown;
        CaptureChapter(data, questManager);
        return data;
    }

    private static string RequireItemId(ItemData item, ItemCatalog catalog)
    {
        if (item == null)
            throw new InvalidOperationException(
                "저장할 아이템 데이터가 없습니다.");

        if (string.IsNullOrWhiteSpace(item.itemId))
            throw new InvalidOperationException(
                $"아이템 '{item.itemName}'의 Item Id가 비어 있습니다.");

        if (catalog.FindById(item.itemId) != item)
            throw new InvalidOperationException($"Item '{item.itemId}' is not registered in ItemCatalog.");
        return item.itemId;
    }

    private static string RequireQuestId(QuestData quest)
    {
        if (quest == null)
            throw new InvalidOperationException(
                "저장할 의뢰 데이터가 없습니다.");

        if (string.IsNullOrWhiteSpace(quest.questId))
            throw new InvalidOperationException(
                $"의뢰 '{quest.questName}'의 Quest Id가 비어 있습니다.");

        return quest.questId;
    }
    private static void CaptureChapter(SaveData data, QuestManager manager)
    {
        QuestData current = null;
        int[] best = null;
        // Prefer the furthest active main quest. Otherwise show completed progress.
        foreach (QuestData quest in manager.activeQuests)
            SelectMainQuest(quest, ref current, ref best);
        if (current == null)
            foreach (QuestData quest in manager.CompletedQuests)
                SelectMainQuest(quest, ref current, ref best);
        if (current == null) return;
        data.chapter = best[0];
        data.mainQuestId = current.questId;
        data.mainQuestName = current.questName;
    }
    private static void SelectMainQuest(QuestData quest, ref QuestData current, ref int[] best)
    {
        if (quest == null || quest.category != QuestCategory.Main ||
            string.IsNullOrEmpty(quest.questId) || !quest.questId.StartsWith("main_", StringComparison.Ordinal))
            return;
        string[] parts = quest.questId.Substring(5).Split('_');
        int[] progress = new int[parts.Length];
        for (int i = 0; i < parts.Length; i++)
            if (!int.TryParse(parts[i], out progress[i]) || progress[i] < 1) return;
        if (best != null)
        {
            int comparison = 0;
            for (int i = 0; i < Math.Max(best.Length, progress.Length); i++)
            {
                int next = i < progress.Length ? progress[i] : 0;
                int previous = i < best.Length ? best[i] : 0;
                comparison = next.CompareTo(previous);
                if (comparison != 0) break;
            }
            if (comparison <= 0) return;
        }
        current = quest;
        best = progress;
    }
}
