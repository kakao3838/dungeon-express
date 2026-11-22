using System;
using UnityEngine;

// Begin on new game/load, Pause on returning to title. Menus count as play time.
public static class SavePlayTime
{
    private static double accumulated;
    private static double startedAt;
    private static bool running;
    public static bool IsInitialized { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        accumulated = 0;
        startedAt = 0;
        running = false;
        IsInitialized = false;
    }

    public static void Begin(float savedSeconds = 0f)
    {
        if (float.IsNaN(savedSeconds) || float.IsInfinity(savedSeconds) || savedSeconds < 0f)
            throw new ArgumentOutOfRangeException(nameof(savedSeconds));
        accumulated = savedSeconds;
        startedAt = Time.realtimeSinceStartupAsDouble;
        running = true;
        IsInitialized = true;
    }

    public static float Seconds
    {
        get
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Call SavePlayTime.Begin when a game starts or loads.");
            return (float)(accumulated + (running ? Time.realtimeSinceStartupAsDouble - startedAt : 0));
        }
    }

    public static void Pause()
    {
        if (!running) return;
        accumulated += Time.realtimeSinceStartupAsDouble - startedAt;
        running = false;
    }

    public static void Resume()
    {
        if (!IsInitialized || running) return;
        startedAt = Time.realtimeSinceStartupAsDouble;
        running = true;
    }
}
