using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Owns scene transitions, separately from disk storage and slot presentation.
public sealed class SaveFlowCoordinator : MonoBehaviour
{
    public const string OfficeScene = "TownScene";
    private static SaveFlowCoordinator instance;
    public static bool IsLoading => instance != null && instance.loading;
    public static bool IsBusy => instance != null && (instance.loading || instance.entryWaiting || instance.directEntry);
    private bool loading, entryWaiting, directEntry, holdingLoadPause;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState() { instance = null; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap() { Ensure(); }

    private static SaveFlowCoordinator Ensure()
    {
        if (instance != null) return instance;
        var root = new GameObject("SaveFlowCoordinator");
        DontDestroyOnLoad(root);
        instance = root.AddComponent<SaveFlowCoordinator>();
        return instance;
    }

    private void Awake() { SaveLoadRequest.Requested += OnLoadRequested; }
    private void OnDestroy()
    {
        SaveLoadRequest.Requested -= OnLoadRequested;
        ReleaseLoadPause();
        if (instance == this) instance = null;
    }

    private void OnLoadRequested()
    {
        if (IsBusy) throw new InvalidOperationException("A save transition is already in progress.");
        try
        {
            SaveDataRestorer.ValidateForLoad(SaveLoadRequest.Data);
            if (!Application.CanStreamedLevelBeLoaded(OfficeScene))
                throw new InvalidOperationException("TownScene is missing from build settings.");
        }
        catch { SaveLoadRequest.Clear(); throw; }
        loading = true;
        SaveSlotWindow.ShowLoadProgress();
        StartCoroutine(RunLoad());
    }

    private IEnumerator RunLoad()
    {
        holdingLoadPause = true;
        GamePause.Push();
        Exception error = null;
        try
        {
            yield return Guarded(LoadOffice(), e => error = e);
        }
        finally
        {
            loading = false;
            ReleaseLoadPause();
        }
        if (error != null)
        {
            Debug.LogException(error);
            SaveSlotWindow.ShowLoadError(ReturnToTitle);
        }
    }

    private IEnumerator LoadOffice()
    {
        bool backup = SaveLoadRequest.UsedBackup;
        ClearOldSessionObjects();
        yield return null; // Destroy old singletons before destination Awake runs.
        AsyncOperation operation = SceneManager.LoadSceneAsync(OfficeScene);
        if (operation == null) throw new InvalidOperationException("Could not load TownScene.");
        while (!operation.isDone) yield return null;
        yield return null; // Allow destination Start/spawners to finish before restoration.

        float deadline = Time.realtimeSinceStartup + 10f;
        Inventory inventory = null;
        PlayerWallet wallet = null;
        PlayerController player = null;
        while (Time.realtimeSinceStartup < deadline)
        {
            player = PersistentPlayer.Instance != null
                ? PersistentPlayer.Instance.GetComponent<PlayerController>()
                : FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                inventory = player.GetComponent<Inventory>();
                wallet = player.GetComponent<PlayerWallet>();
            }
            if (inventory != null && wallet != null && QuestManager.Instance != null && TutorialManager.Instance != null)
                break;
            yield return null;
        }
        if (player == null || inventory == null || wallet == null || QuestManager.Instance == null || TutorialManager.Instance == null)
            throw new InvalidOperationException("Office player/save managers did not become ready.");

        SaveLoadRequest.Apply(inventory, wallet, QuestManager.Instance, TutorialManager.Instance);
        PlaceAtOfficeSavePoint(player);
        if (CameraFollow.Instance != null) CameraFollow.Instance.SnapToTarget();
        SaveSlotWindow.CompleteLoad(backup);
        while (SaveSlotWindow.IsShowing) yield return null;
        TutorialManager.Instance.ResumeAfterSaveLoad();
    }

    private static void ClearOldSessionObjects()
    {
        if (PersistentPlayer.Instance != null) Destroy(PersistentPlayer.Instance.gameObject);
        if (QuestManager.Instance != null) Destroy(QuestManager.Instance.gameObject);
        if (TutorialManager.Instance != null) Destroy(TutorialManager.Instance.gameObject);
        if (StoryManager.Instance != null) Destroy(StoryManager.Instance.transform.root.gameObject);
    }

    private static void PlaceAtOfficeSavePoint(PlayerController player)
    {
        SavePoint point = FindFirstObjectByType<SavePoint>();
        GameObject respawn = GameObject.Find("RespawnPoint");
        if (point != null) player.transform.position = point.transform.TransformPoint(point.loadSpawnOffset);
        else if (respawn != null) player.transform.position = respawn.transform.position;
        else
        {
            var door = FindFirstObjectByType<DungeonDoorTrigger>();
            if (door != null) TutorialManager.PlaceNextToDoor(player, door.transform);
        }

        Physics2D.SyncTransforms();
        Collider2D bodyCollider = player.GetComponent<Collider2D>();
        Vector3 position = player.transform.position;
        foreach (var hit in Physics2D.RaycastAll(position + Vector3.up * 3f, Vector2.down, 20f, player.groundLayer))
        {
            if (hit.collider.isTrigger || hit.collider.transform.root == player.transform.root) continue;
            if (bodyCollider != null)
                position.y = hit.point.y + (player.transform.position.y - bodyCollider.bounds.min.y) + 0.02f;
            player.transform.position = position;
            break;
        }
        player.StopForMenu();
        var body = player.GetComponent<Rigidbody2D>();
        if (body != null) body.angularVelocity = 0f;
        Physics2D.SyncTransforms();
    }

    private static IEnumerator Guarded(IEnumerator routine, Action<Exception> failed)
    {
        try
        {
            while (true)
            {
                bool more;
                object current = null;
                Exception error = null;
                try { more = routine.MoveNext(); if (more) current = routine.Current; }
                catch (Exception e) { error = e; more = false; }
                if (error != null) { failed(error); yield break; }
                if (!more) yield break;
                yield return current;
            }
        }
        finally { (routine as IDisposable)?.Dispose(); }
    }

    private void ReleaseLoadPause()
    {
        if (!holdingLoadPause) return;
        holdingLoadPause = false;
        GamePause.Pop();
    }

    private static void ReturnToTitle()
    {
        SaveLoadRequest.Clear();
        SavePlayTime.Pause();
        if (SceneManager.GetActiveScene().name == "TitleScene") return;
        ClearOldSessionObjects();
        SceneManager.LoadScene("TitleScene");
    }

    public static IEnumerator ConfirmDungeonEntry(Action<bool> finished)
    {
        var owner = Ensure();
        if (owner.entryWaiting || owner.loading) { finished(false); yield break; }
        string sourceScene = SceneManager.GetActiveScene().name;
        if (!SaveScenePolicy.IsAllowed(sourceScene, OfficeScene, Application.isEditor, true))
        {
            finished(true); // Non-office exits cannot save and retain their original behavior.
            yield break;
        }
        owner.entryWaiting = true;
        GamePause.Push();
        bool closed = false, enter = false;
        try
        {
            SaveSlotWindow window = null;
            try { window = SaveSlotWindow.OpenDungeonEntry(result => { enter = result; closed = true; }, true); }
            catch (Exception e) { Debug.LogException(e); closed = true; }
            while (!closed && window != null) yield return null;
            finished(enter && sourceScene == SceneManager.GetActiveScene().name);
        }
        finally { owner.entryWaiting = false; GamePause.Pop(); }
    }

    public static void EnterDungeon(string sceneName)
    {
        var owner = Ensure();
        if (IsBusy) return;
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError("Dungeon scene is missing from build settings: " + sceneName);
            return;
        }
        owner.StartCoroutine(owner.EnterDirect(sceneName));
    }

    private IEnumerator EnterDirect(string sceneName)
    {
        directEntry = true;
        GamePause.Push();
        try
        {
            bool enter = false;
            yield return ConfirmDungeonEntry(result => enter = result);
            if (!enter) yield break;
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
            if (operation != null) while (!operation.isDone) yield return null;
        }
        finally { directEntry = false; GamePause.Pop(); }
    }
}
