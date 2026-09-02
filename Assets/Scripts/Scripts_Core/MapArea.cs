using System;
using UnityEngine;

public class MapArea : MonoBehaviour
{
    [SerializeField] private string mapId;

    public static MapArea Current { get; private set; }
    public static event Action<MapArea> CurrentChanged;

    public string MapId => mapId;

    public void SetCurrent()
    {
        if (Current == this) return;

        Current = this;
        CurrentChanged?.Invoke(this);
    }

    void OnDestroy()
    {
        if (Current == this)
        {
            Current = null;
        }
    }
}