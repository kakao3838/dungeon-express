using System;
using System.Collections.Generic;
using System.IO;

public static class SaveDataRestorer
{
    // Call only after the destination scene's player and managers exist.
    // Resolve everything before touching live state; no StartQuest/CompleteQuest calls.
    public static void Restore(SaveData data, Inventory inventory, PlayerWallet wallet,
        QuestManager quests, TutorialManager tutorial, ItemCatalog items = null, QuestCatalog catalog = null)
    {
        if (inventory == null) throw new ArgumentNullException(nameof(inventory));
        if (wallet == null) throw new ArgumentNullException(nameof(wallet));
        if (quests == null) throw new ArgumentNullException(nameof(quests));
        if (tutorial == null) throw new ArgumentNullException(nameof(tutorial));
        SaveDataValidation.Validate(data);
        items = items != null ? items : ItemCatalog.Load();
        catalog = catalog != null ? catalog : QuestCatalog.Load();
        if (items == null || catalog == null) throw new InvalidOperationException("Save catalogs are missing.");
        items.ValidateIds();
        catalog.ValidateIds();

        List<LostItem> bag = ResolveItems(data.items, items);
        List<LostItem> lost = ResolveItems(data.lostItems, items);
        List<QuestData> active = ResolveQuests(data.activeQuestIds, catalog);
        List<QuestData> completed = ResolveQuests(data.completedQuestIds, catalog);
        var qualityRecords = new Dictionary<string, int[]>(StringComparer.Ordinal);
        foreach (var entry in data.deliveredQualities)
        {
            QuestData quest = catalog.FindById(entry.questId);
            if (quest == null || entry.qualities.Length != quest.requiredItems.Count)
                throw new InvalidDataException("Delivered quality record does not match quest: " + entry.questId);
            qualityRecords.Add(entry.questId, entry.qualities);
        }

        inventory.RestoreSaveState(data.maxSlots, data.qualityLossMultiplier, bag, lost);
        wallet.RestoreSaveState(data.gold);
        quests.RestoreSaveState(active, completed, data.flags, qualityRecords);
        tutorial.RestoreSaveState(data.tutorialCompleted, data.dungeonGuideShown);
        SavePlayTime.Begin(data.playTimeSeconds);

        // Publish after all state is restored. These are refresh events, not quest rewards.
        Notify(inventory.NotifySaveRestored);
        Notify(wallet.NotifySaveRestored);
        Notify(quests.NotifySaveRestored);
    }

    private static void Notify(Action refresh)
    {
        try { refresh(); }
        catch (Exception e) { UnityEngine.Debug.LogException(e); }
    }

    private static List<LostItem> ResolveItems(List<SavedItem> saved, ItemCatalog catalog)
    {
        var result = new List<LostItem>(saved.Count);
        foreach (var entry in saved)
        {
            ItemData item = catalog.FindById(entry.itemId);
            if (item == null) throw new InvalidDataException("Unknown item ID: " + entry.itemId);
            result.Add(new LostItem { item = item, quality = entry.quality });
        }
        return result;
    }

    private static List<QuestData> ResolveQuests(List<string> ids, QuestCatalog catalog)
    {
        var result = new List<QuestData>(ids.Count);
        foreach (string id in ids)
        {
            QuestData quest = catalog.FindById(id);
            if (quest == null) throw new InvalidDataException("Unknown quest ID: " + id);
            result.Add(quest);
        }
        return result;
    }
}
