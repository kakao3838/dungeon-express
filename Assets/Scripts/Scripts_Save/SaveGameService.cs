using System;
using UnityEngine;

// Entry points for the future slot UI and scene/death coordinator.
// Does not create a GameObject or initiate a scene transition.
public static class SaveGameService
{
    public static int CurrentSlot { get; private set; } // 0 until first save/load
    public static event Action<int> Saved;
    public static event Action<int> Loaded;
    public static SaveFileStore Files => new SaveFileStore();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        CurrentSlot = 0;
        Saved = null;
        Loaded = null;
    }

    // Call after the new game's own initialization; does not clear gameplay data.
    public static void BeginNewGame()
    {
        CurrentSlot = 0;
        SavePlayTime.Begin();
    }

    public static SaveData Save(int slot, Inventory inventory, PlayerWallet wallet,
        QuestManager quests, bool overwrite = false)
    {
        SaveData data = SaveDataBuilder.Capture(inventory, wallet, quests);
        Files.Save(slot, data, overwrite);
        CurrentSlot = slot;
        Notify(Saved, slot);
        return data;
    }

    // Reading lets the title UI inspect a slot before any scene/player exists.
    public static SaveData Read(int slot, out bool usedBackup) => Files.Load(slot, out usedBackup);

    // Invoke after scene setup, when gameplay conversations/result windows are closed.
    // Callers can use Read first, load the office, then call RestoreReadData.
    public static void RestoreReadData(int slot, SaveData data, Inventory inventory,
        PlayerWallet wallet, QuestManager quests, TutorialManager tutorial)
    {
        Files.GetSlotPath(slot); // Validate slot before touching gameplay state.
        SaveDataRestorer.Restore(data, inventory, wallet, quests, tutorial);
        CurrentSlot = slot;
        Notify(Loaded, slot);
    }

    public static void Delete(int slot)
    {
        Files.Delete(slot);
        if (CurrentSlot == slot) CurrentSlot = 0;
    }

    private static void Notify(Action<int> callbacks, int slot)
    {
        if (callbacks == null) return;
        foreach (Action<int> callback in callbacks.GetInvocationList())
        {
            try { callback(slot); }
            catch (Exception e) { Debug.LogException(e); }
        }
    }
}
