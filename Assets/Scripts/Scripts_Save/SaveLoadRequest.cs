using System;

// Title UI can select a save before the destination scene/player exists.
// The scene coordinator owns the transition and calls Apply after setup/placement.
public static class SaveLoadRequest
{
    public static int Slot { get; private set; }
    public static SaveData Data { get; private set; }
    public static bool UsedBackup { get; private set; }
    public static bool HasPending => Data != null;
    public static event Action Requested;
    public static bool HasHandler => Requested != null;

    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState() { Clear(); Requested = null; }

    public static void Select(int slot, SaveData data, bool usedBackup)
    {
        if (slot < 1 || slot > SaveFileStore.SlotCount) throw new ArgumentOutOfRangeException(nameof(slot));
        SaveDataValidation.Validate(data);
        Slot = slot;
        Data = data;
        UsedBackup = usedBackup;
        Requested?.Invoke();
    }

    public static void Apply(Inventory inventory, PlayerWallet wallet, QuestManager quests, TutorialManager tutorial)
    {
        if (!HasPending) throw new InvalidOperationException("No selected save to load.");
        SaveGameService.RestoreReadData(Slot, Data, inventory, wallet, quests, tutorial);
        Clear();
    }

    public static void Clear() { Slot = 0; Data = null; UsedBackup = false; }
}
