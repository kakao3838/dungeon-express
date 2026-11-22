using System;
using UnityEngine;

/// <summary>Persistent checkpoint identity stored in the active save profile.</summary>
public static class SaveCheckpointState
{
    public static string SceneName { get; private set; } = string.Empty;
    public static string CheckpointId { get; private set; } = string.Empty;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState() => Clear();

    public static void SetCurrent(string sceneName, string checkpointId)
    {
        if (string.IsNullOrWhiteSpace(sceneName)) throw new ArgumentException("Scene name is required.", nameof(sceneName));
        if (string.IsNullOrWhiteSpace(checkpointId)) throw new ArgumentException("Checkpoint ID is required.", nameof(checkpointId));
        SceneName = sceneName;
        CheckpointId = checkpointId;
    }

    public static void Restore(string sceneName, string checkpointId)
    {
        if (string.IsNullOrWhiteSpace(sceneName) && string.IsNullOrWhiteSpace(checkpointId))
        {
            Clear();
            return;
        }
        SetCurrent(sceneName, checkpointId);
    }

    public static void Clear()
    {
        SceneName = string.Empty;
        CheckpointId = string.Empty;
    }
}
