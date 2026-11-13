using System;
using System.Collections.Generic;
using System.IO;

public static class SaveDataValidation
{
    public const int CurrentVersion = 1;

    public static void Validate(SaveData data)
    {
        if (data == null) Fail("Save data is empty.");
        if (data.version != CurrentVersion)
            throw new NotSupportedException("Unsupported save version: " + data.version);
        DateTimeOffset timestamp;
        if (!DateTimeOffset.TryParse(data.savedAtUtc, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out timestamp)) Fail("Invalid save timestamp.");
        if (!Finite(data.playTimeSeconds) || data.playTimeSeconds < 0f || data.gold < 0 ||
            data.maxSlots < 1 || data.chapter < 0 || !Finite(data.qualityLossMultiplier) ||
            data.qualityLossMultiplier < 0f || data.qualityLossMultiplier > 1f)
            Fail("Invalid player values.");
        if (data.items == null || data.lostItems == null || data.equipped == null ||
            data.activeQuestIds == null || data.completedQuestIds == null ||
            data.flags == null || data.deliveredQualities == null) Fail("Missing save lists.");
        if (data.items.Count > data.maxSlots) Fail("Inventory exceeds bag capacity.");
        // Equipment restore is intentionally deferred; never silently discard it.
        if (data.equipped.Count > 0)
            throw new NotSupportedException("Equipment saving/loading is not implemented yet.");
        ValidateItems(data.items);
        ValidateItems(data.lostItems);
        var active = UniqueIds(data.activeQuestIds);
        var completed = UniqueIds(data.completedQuestIds);
        if (active.Overlaps(completed)) Fail("A quest is both active and completed.");
        var flags = UniqueIds(data.flags);
        if (flags.Contains(QuestManager.FlagTutorialDone) != data.tutorialCompleted)
            Fail("Tutorial completion flag does not match saved state.");
        var records = new HashSet<string>(StringComparer.Ordinal);
        foreach (var record in data.deliveredQualities)
        {
            if (record == null || string.IsNullOrWhiteSpace(record.questId) ||
                !completed.Contains(record.questId) || !records.Add(record.questId) || record.qualities == null)
                Fail("Invalid delivered quality record.");
            foreach (int quality in record.qualities)
                if (quality < 0 || quality > Inventory.FullQuality) Fail("Invalid delivered quality.");
        }
    }

    private static void ValidateItems(List<SavedItem> items)
    {
        foreach (var item in items)
            if (item == null || string.IsNullOrWhiteSpace(item.itemId) ||
                item.quality < 0 || item.quality > Inventory.FullQuality) Fail("Invalid inventory item.");
    }

    private static HashSet<string> UniqueIds(List<string> ids)
    {
        var found = new HashSet<string>(StringComparer.Ordinal);
        foreach (string id in ids)
            if (string.IsNullOrWhiteSpace(id) || !found.Add(id)) Fail("Empty or duplicate ID in save.");
        return found;
    }

    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    private static void Fail(string message) => throw new InvalidDataException(message);
}
