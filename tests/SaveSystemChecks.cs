// Standalone regression harness. Compile only with the production files listed
// in run-save-checks.ps1, never add this file under Assets.
// Unity lifecycle/JsonUtility still need an Editor play-mode check.
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace UnityEngine
{
    public class ScriptableObject { }
    public class GameObject { }
    public class Sprite { }
    public class Behaviour { public bool enabled = true; }
    public class Object
    {
        public static readonly Dictionary<Type, object> Scene = new Dictionary<Type, object>();
        public static T FindFirstObjectByType<T>() where T : class => Scene.ContainsKey(typeof(T)) ? Scene[typeof(T)] as T : null;
    }
    public class MonoBehaviour : Behaviour
    {
        public GameObject gameObject = new GameObject();
        protected static void Destroy(object value) { }
        protected static void DontDestroyOnLoad(object value) { }
    }
    public class CreateAssetMenuAttribute : Attribute { public string fileName, menuName; }
    public class HeaderAttribute : Attribute { public HeaderAttribute(string value) { } }
    public class TooltipAttribute : Attribute { public TooltipAttribute(string value) { } }
    public class RangeAttribute : Attribute { public RangeAttribute(float a, float b) { } }
    public class TextAreaAttribute : Attribute { public TextAreaAttribute() { } public TextAreaAttribute(int a, int b) { } }
    public class SerializeField : Attribute { }
    public enum RuntimeInitializeLoadType { SubsystemRegistration }
    public class RuntimeInitializeOnLoadMethodAttribute : Attribute
    { public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType type) { } }
    public static class Time { public static double realtimeSinceStartupAsDouble; public static float timeScale = 1f; }
    public static class Application { public static string persistentDataPath; }
    public static class Debug
    {
        public static void Log(object value) { }
        public static void LogException(Exception value) { throw new Exception("Refresh callback failed", value); }
    }
    public static class Mathf
    {
        public static int Clamp(int x, int min, int max) => Math.Min(max, Math.Max(min, x));
        public static int Max(int a, int b) => Math.Max(a, b);
        public static int Min(int a, int b) => Math.Min(a, b);
        public static int RoundToInt(float value) => (int)Math.Round(value);
    }
    public static class Random { public static int Range(int a, int b) => a; }
    public static class Resources
    {
        public static readonly Dictionary<string, object> Values = new Dictionary<string, object>();
        public static T Load<T>(string key) where T : class => Values.ContainsKey(key) ? Values[key] as T : null;
    }
    public static class JsonUtility
    {
        public static string ToJson(object value, bool pretty = false)
        {
            using (var stream = new MemoryStream())
            {
                new DataContractJsonSerializer(value.GetType()).WriteObject(stream, value);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }
        public static T FromJson<T>(string json)
        {
            try
            {
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                    return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(stream);
            }
            catch (Exception e) { throw new ArgumentException("Bad JSON", e); }
        }
    }
}

public class TutorialManager
{
    public static TutorialManager Instance;
    public bool DungeonGuideShown;
    public bool Completed;
    public void RestoreSaveState(bool completed, bool shown) { Completed = completed; DungeonGuideShown = shown; }
}

public class PlayerController : UnityEngine.Behaviour
{
    public int StopCount;
    public void StopForMenu() { StopCount++; }
}
public class PlayerAttack : UnityEngine.Behaviour { }
public class PlayerVineClimb : UnityEngine.Behaviour { }

internal static class SaveSystemChecks
{
    private static int checks;
    private static void Check(bool condition, string label)
    {
        if (!condition) throw new Exception("FAIL: " + label);
        checks++;
    }
    private static void Throws<T>(Action action, string label) where T : Exception
    {
        try { action(); }
        catch (T) { checks++; return; }
        throw new Exception("Expected " + typeof(T).Name + ": " + label);
    }

    private static void Main(string[] args)
    {
        string root = Path.Combine(args[0], "save-regression-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        UnityEngine.Application.persistentDataPath = root;
        try { Run(root); Console.WriteLine("PASS: " + checks + " save-system checks"); }
        finally { Directory.Delete(root, true); }
    }

    private static void Run(string root)
    {
        var item = new ItemData { itemId = "medicine", itemName = "Medicine" };
        var items = new ItemCatalog(); items.allItems.Add(item);
        var quest = new QuestData { questId = "main_1_2", questName = "Main", giveItemsOnStart = true };
        quest.requiredItems.Add(new QuestItemRequirement { item = item, quantity = 1 });
        var quests = new QuestCatalog(); quests.allQuests.Add(quest);
        UnityEngine.Resources.Values[ItemCatalog.ResourcePath] = items;
        UnityEngine.Resources.Values[QuestCatalog.ResourcePath] = quests;
        var inventory = new Inventory(); inventory.maxSlots = 4;
        inventory.AddItem(item, 85); inventory.AddItem(item, 30);
        var wallet = new PlayerWallet(); wallet.AddGold(123);
        var manager = new QuestManager(); manager.activeQuests.Add(quest);
        manager.SetFlag(QuestManager.FlagTutorialDone);
        var tutorial = new TutorialManager(); TutorialManager.Instance = tutorial;
        tutorial.DungeonGuideShown = true;
        var data = SaveDataBuilder.Capture(inventory, wallet, manager, 90061);
        var store = new SaveFileStore(Path.Combine(root, "slots"));

        Throws<ArgumentOutOfRangeException>(() => store.GetSlotPath(0), "slot zero");
        Throws<ArgumentOutOfRangeException>(() => store.GetSlotPath(4), "slot four");
        Check(!store.Exists(1), "empty slot");
        Throws<FileNotFoundException>(() => { bool b; store.Load(1, out b); }, "load empty");
        store.Save(1, data);
        bool backup;
        var loaded = store.Load(1, out backup);
        Check(!backup && loaded.gold == 123, "round trip gold");
        Check(loaded.items.Count == 2 && loaded.items[0].quality == 85 && loaded.items[1].quality == 30, "quality/order");
        Check(loaded.mainQuestId == "main_1_2" && loaded.chapter == 1, "chapter");
        Check(SaveSlotDisplay.PlayTime(loaded) == "25:01:01", "time over 24 hours");
        Throws<IOException>(() => store.Save(1, data), "overwrite requires confirmation");
        data.gold = 456; store.Save(1, data, true);
        Check(File.Exists(store.GetSlotPath(1) + ".bak"), "backup created");
        Check(store.Load(1, out backup).gold == 456 && !backup, "latest primary");
        File.WriteAllText(store.GetSlotPath(1), "broken");
        Check(store.Load(1, out backup).gold == 123 && backup, "corrupt primary uses backup");
        data.gold = 789; store.Save(1, data, true);
        File.WriteAllText(store.GetSlotPath(1), "broken again");
        Check(store.Load(1, out backup).gold == 123 && backup, "healthy backup survives damaged primary overwrite");
        store.Save(2, data); store.Save(3, data);
        Check(store.Exists(2) && store.Exists(3), "independent slots");
        store.Delete(1); Check(!store.Exists(1), "delete primary and backup");
        Check(store.Exists(2), "delete preserves other slots");
        store.Delete(1); // Idempotent.
        File.WriteAllText(store.GetSlotPath(2), "{}");
        Throws<InvalidDataException>(() => store.Load(2, out backup), "reject empty object");

        var bad = SaveDataBuilder.Capture(inventory, wallet, manager, 10);
        bad.items[0].quality = 101;
        Throws<InvalidDataException>(() => store.Save(3, bad, true), "reject invalid quality");
        Check(store.Load(3, out backup).gold == 789, "bad save preserves old file");
        bad.items[0].quality = 85; bad.version = 99;
        Throws<NotSupportedException>(() => store.Save(3, bad, true), "future schema");
        bad.version = 1; bad.completedQuestIds.Add(quest.questId);
        Throws<InvalidDataException>(() => SaveDataValidation.Validate(bad), "quest overlap");

        int started = 0, finished = 0, refreshed = 0;
        manager.OnQuestStarted += q => started++;
        manager.OnQuestCompleted += q => finished++;
        manager.OnStateRestored += () => refreshed++;
        inventory.RemoveRandomOnDeath();
        Check(inventory.LostItems.Count == 1, "preexisting losses");
        inventory.ClearAll(); wallet.AddGold(999); manager.activeQuests.Clear();
        SaveDataRestorer.Restore(loaded, inventory, wallet, manager, tutorial, items, quests);
        Check(inventory.Items.Count == 2 && inventory.GetQuality(1) == 30, "restore bag and quality");
        Check(inventory.LostItems.Count == 0, "replace loss history");
        Check(wallet.Gold == 123 && manager.TrackedQuest == quest, "restore wallet/quest");
        Check(started == 0 && finished == 0 && refreshed == 1, "no quest replay or rewards");
        Check(tutorial.Completed && tutorial.DungeonGuideShown, "tutorial flags");
        Check(SavePlayTime.Seconds >= 90061, "restore accumulated play time");
        SaveDataRestorer.Restore(loaded, inventory, wallet, manager, tutorial, items, quests);
        Check(inventory.Items.Count == 2 && wallet.Gold == 123, "repeat load no duplication");
        loaded.items[1].itemId = "missing";
        Throws<InvalidDataException>(() => SaveDataRestorer.Restore(loaded, inventory, wallet, manager, tutorial, items, quests), "unknown item");
        Check(inventory.Items.Count == 2 && wallet.Gold == 123, "failed resolution does not mutate");
        loaded.items[1].itemId = item.itemId; loaded.activeQuestIds[0] = "missing_quest";
        Throws<InvalidDataException>(() => SaveDataRestorer.Restore(loaded, inventory, wallet, manager, tutorial, items, quests), "unknown quest");
        Check(manager.TrackedQuest == quest, "failed quest resolution preserves state");
        loaded.activeQuestIds[0] = quest.questId;
        items.allItems.Add(new ItemData { itemId = item.itemId });
        Throws<InvalidOperationException>(() => SaveDataRestorer.Restore(loaded, inventory, wallet, manager, tutorial, items, quests), "duplicate catalog ID");
        items.allItems.RemoveAt(1);

        SaveGameService.BeginNewGame();
        SaveGameService.Save(1, inventory, wallet, manager);
        Check(SaveGameService.CurrentSlot == 1, "active slot follows save");
        var read = SaveGameService.Read(1, out backup);
        SaveGameService.RestoreReadData(1, read, inventory, wallet, manager, tutorial);
        Check(SaveGameService.CurrentSlot == 1, "active slot follows restore");
        SaveGameService.Delete(1); Check(SaveGameService.CurrentSlot == 0, "delete clears selected slot");

        Check(SaveSlotInfo.Read(store, 1).State == SaveSlotState.Empty, "browser empty slot");
        Check(SaveSlotInfo.Read(store, 2).State == SaveSlotState.Unreadable, "browser corrupt slot");
        var occupied = SaveSlotInfo.Read(store, 3);
        Check(occupied.CanLoad && occupied.Occupied, "browser ready slot");
        var empty = SaveSlotInfo.Read(store, 1);
        Check(!empty.CanLoad && !empty.Occupied, "empty slots cannot load/delete");
        store.Save(3, data, true);
        File.WriteAllText(store.GetSlotPath(3), "broken");
        Check(SaveSlotInfo.Read(store, 3).State == SaveSlotState.Backup, "browser marks backup recovery");

        int requested = 0;
        Action handler = () => requested++;
        SaveLoadRequest.Requested += handler;
        SaveLoadRequest.Select(1, read, false);
        Check(requested == 1 && SaveLoadRequest.HasPending && SaveLoadRequest.Slot == 1, "title load request without scene");
        SaveLoadRequest.Apply(inventory, wallet, manager, tutorial);
        Check(!SaveLoadRequest.HasPending && SaveGameService.CurrentSlot == 1, "apply pending data after scene setup");
        SaveLoadRequest.Requested -= handler;

        var player = new PlayerController();
        var attack = new PlayerAttack();
        var vine = new PlayerVineClimb { enabled = false };
        UnityEngine.Object.Scene[typeof(PlayerController)] = player;
        UnityEngine.Object.Scene[typeof(PlayerAttack)] = attack;
        UnityEngine.Object.Scene[typeof(PlayerVineClimb)] = vine;
        UnityEngine.Time.timeScale = 0.5f;
        GamePause.Push();
        Check(GamePause.IsPaused && UnityEngine.Time.timeScale == 0f && !player.enabled && !attack.enabled,
            "slot/modal pause blocks controls");
        Check(player.StopCount == 1, "opening pause stops existing movement");
        GamePause.Push();
        GamePause.Pop();
        Check(GamePause.IsPaused && !player.enabled && UnityEngine.Time.timeScale == 0f,
            "closing child window retains parent menu pause");
        GamePause.Pop();
        Check(!GamePause.IsPaused && player.enabled && attack.enabled && UnityEngine.Time.timeScale == 0.5f,
            "last window restores previous controls/time scale");
        Check(!vine.enabled, "originally disabled input stays disabled");
        GamePause.Pop();
        Check(!GamePause.IsPaused, "extra release cannot underflow pause count");

        Check(SaveScenePolicy.IsAllowed("TownScene", "TownScene", false, false), "office save enabled in builds");
        Check(!SaveScenePolicy.IsAllowed("JungleDungeonScene", "TownScene", false, true), "dungeon cannot save");
        Check(!SaveScenePolicy.IsAllowed("TitleScene", "TownScene", true, true), "title cannot save");
        Check(SaveScenePolicy.IsAllowed("PlayerMonsterTest", "TownScene", true, true), "explicit editor test exception");
        Check(!SaveScenePolicy.IsAllowed("PlayerMonsterTest", "TownScene", true, false), "test scene denied by default");
        Check(!SaveScenePolicy.IsAllowed("PlayerMonsterTest", "TownScene", false, true), "test exception stripped in builds");

        SaveDataRestorer.ValidateForLoad(read, items, quests);
        Check(wallet.Gold == read.gold, "title preflight does not grant/change gold");
        string knownId = read.items[0].itemId;
        read.items[0].itemId = "unavailable";
        Throws<InvalidDataException>(() => SaveDataRestorer.ValidateForLoad(read, items, quests), "reject invalid ID before scene change");
        read.items[0].itemId = knownId;

        var gate = new DungeonEntrySession();
        Check(!gate.CanEnter, "opening prompt cannot enter");
        gate.ChooseSave();
        Check(!gate.CanEnter, "selecting a slot / failed writes cannot enter");
        Throws<InvalidOperationException>(() => gate.SkipSave(), "cannot bypass failed save with hidden skip action");
        gate.Cancel();
        Check(!gate.CanEnter && gate.State == DungeonEntryState.Cancelled, "cancel slot window stays in office");
        Throws<InvalidOperationException>(() => gate.Saved(), "late save completion cannot enter after cancellation");
        gate = new DungeonEntrySession(); gate.SkipSave();
        Check(gate.CanEnter, "No permits entry without writing a save");
        gate = new DungeonEntrySession(); gate.ChooseSave(); gate.Saved(); gate.Cancel();
        Check(gate.CanEnter, "successful save permits entry when success window closes");
        gate = new DungeonEntrySession(); gate.Cancel();
        Check(!gate.CanEnter, "cancel initial prompt stays in office");
    }
}
