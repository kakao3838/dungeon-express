#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Preview/debug entry points until save-point objects and doors are wired up.
public static class SaveSlotPreview
{
    [MenuItem("Dungeon Express/Save/Open Save Slots (Play Mode)")]
    private static void Save()
    {
        if (!EditorApplication.isPlaying)
            Debug.LogWarning("Enter Play Mode in TownScene or PlayerMonsterTest first.");
        else if (!SaveScenePolicy.IsAllowed(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
            "TownScene", true, true))
            Debug.LogWarning("Saving is only allowed in TownScene or the editor's PlayerMonsterTest scene.");
        else SaveSlotWindow.OpenSave(allowEditorTestScene: true);
    }

    [MenuItem("Dungeon Express/Save/Open Load Slots (Play Mode)")]
    private static void Load()
    {
        if (EditorApplication.isPlaying) SaveSlotWindow.OpenLoad();
        else Debug.LogWarning("Enter Play Mode first.");
    }
}
#endif
