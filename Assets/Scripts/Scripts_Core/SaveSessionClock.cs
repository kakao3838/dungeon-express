using UnityEngine;
using UnityEngine.SceneManagement;

// Starts once on entering gameplay; title time does not count toward a slot.
public sealed class SaveSessionClock : MonoBehaviour
{
    private bool suspended;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        var root = new GameObject("SaveSessionClock");
        DontDestroyOnLoad(root);
        root.AddComponent<SaveSessionClock>();
    }

    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        Sync();
    }

    private void OnDestroy() { SceneManager.sceneLoaded -= OnSceneLoaded; }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) { Sync(); }
    private void OnApplicationPause(bool paused) { suspended = paused; Sync(); }
    private void OnApplicationQuit() { SavePlayTime.Pause(); }

    private void Sync()
    {
        string scene = SceneManager.GetActiveScene().name;
        if (suspended || scene == "TitleScene" || scene == "BootScene" || SaveLoadRequest.HasPending)
        {
            SavePlayTime.Pause();
            return;
        }
        if (!SavePlayTime.IsInitialized) SaveGameService.BeginNewGame();
        else SavePlayTime.Resume();
    }
}
