using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MapTransitionRegistry
{
    private static readonly Dictionary<string, List<MapTransitionPoint>> PointsById = new(StringComparer.Ordinal);
    private static bool initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        if (initialized) return;

        initialized = true;
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    public static void Register(MapTransitionPoint point)
    {
        if (point == null || string.IsNullOrWhiteSpace(point.ConnectionId)) return;

        if (!PointsById.TryGetValue(point.ConnectionId, out List<MapTransitionPoint> points))
        {
            points = new List<MapTransitionPoint>();
            PointsById.Add(point.ConnectionId, points);
        }

        if (!points.Contains(point))
        {
            points.Add(point);
        }
    }

    public static void Unregister(MapTransitionPoint point)
    {
        if (point == null || !PointsById.TryGetValue(point.ConnectionId, out List<MapTransitionPoint> points)) return;

        points.Remove(point);
        if (points.Count == 0)
        {
            PointsById.Remove(point.ConnectionId);
        }
    }

    public static MapTransitionPoint GetDestination(MapTransitionPoint point)
    {
        if (point == null || string.IsNullOrWhiteSpace(point.ConnectionId)) return null;

        if (!PointsById.ContainsKey(point.ConnectionId))
        {
            ValidateScene();
        }

        if (!PointsById.TryGetValue(point.ConnectionId, out List<MapTransitionPoint> points)) return null;
        if (points.Count != 2) return null;

        return points[0] == point ? points[1] : points[0];
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PointsById.Clear();

        MapTransitionPoint[] points = UnityEngine.Object.FindObjectsByType<MapTransitionPoint>(FindObjectsSortMode.None);
        foreach (MapTransitionPoint point in points)
        {
            if (string.IsNullOrWhiteSpace(point.ConnectionId))
            {
                Debug.LogWarning($"[MapTransition] '{point.name}' has an empty Connection ID.", point);
            }
            Register(point);
        }

        ValidateConnections();
    }

    public static void ValidateScene()
    {
        PointsById.Clear();

        MapTransitionPoint[] points = UnityEngine.Object.FindObjectsByType<MapTransitionPoint>(FindObjectsSortMode.None);
        foreach (MapTransitionPoint point in points)
        {
            Register(point);
        }

        ValidateConnections();
    }

    static void OnSceneUnloaded(Scene scene)
    {
        PointsById.Clear();
    }

    static void ValidateConnections()
    {
        foreach (KeyValuePair<string, List<MapTransitionPoint>> pair in PointsById)
        {
            if (pair.Value.Count == 1)
            {
                Debug.LogWarning($"[MapTransition] Connection ID '{pair.Key}' has 1 point. Exactly 2 are required.");
            }
            else if (pair.Value.Count > 2)
            {
                Debug.LogError($"[MapTransition] Connection ID '{pair.Key}' has {pair.Value.Count} points. Exactly 2 are required.");
            }
        }
    }
}