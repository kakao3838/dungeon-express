using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapArea : MonoBehaviour
{
    [SerializeField] private string mapId;

    // 씬에 있는 모든 구간 (카메라가 플레이어가 있는 구간을 찾을 때 사용)
    public static readonly List<MapArea> All = new List<MapArea>();

    private Bounds worldBounds;
    private bool hasBounds;

    public static MapArea Current { get; private set; }
    public static event Action<MapArea> CurrentChanged;

    public string MapId => mapId;

    // 이 구간 아래에 있는 충돌 타일맵(TilemapCollider2D)을 모두 감싼 월드 좌표 범위
    public Bounds WorldBounds
    {
        get
        {
            if (!hasBounds) RecalculateBounds();
            return worldBounds;
        }
    }

    public void RecalculateBounds()
    {
        hasBounds = false;
        foreach (var tilemapCollider in GetComponentsInChildren<TilemapCollider2D>())
        {
            Bounds b = tilemapCollider.bounds;
            if (b.size == Vector3.zero) continue;

            if (!hasBounds)
            {
                worldBounds = b;
                hasBounds = true;
            }
            else
            {
                worldBounds.Encapsulate(b);
            }
        }
    }

    // 기준 위치(플레이어)가 들어 있거나 가장 가까운 구간
    public static MapArea FindClosest(Vector3 position)
    {
        MapArea closest = null;
        float closestSqrDistance = float.MaxValue;

        foreach (var area in All)
        {
            Bounds b = area.WorldBounds;
            if (b.size == Vector3.zero) continue;

            float sqrDistance = b.SqrDistance(new Vector3(position.x, position.y, b.center.z));
            if (sqrDistance < closestSqrDistance)
            {
                closestSqrDistance = sqrDistance;
                closest = area;
            }
        }

        return closest;
    }

    void OnEnable()
    {
        if (!All.Contains(this)) All.Add(this);
    }

    void OnDisable()
    {
        All.Remove(this);
    }

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