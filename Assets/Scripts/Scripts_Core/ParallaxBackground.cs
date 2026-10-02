using System;
using System.Collections.Generic;
using UnityEngine;

// 메인 카메라를 따라다니는 시차(Parallax) 배경입니다.
// 빈 오브젝트에 이 스크립트를 붙이고 layers에 뒤쪽 레이어부터 순서대로 채우세요.
// 각 레이어의 스프라이트는 카메라 화면을 덮을 만큼 가로로 자동 반복됩니다.
[DefaultExecutionOrder(100)] // CameraFollow(LateUpdate)가 카메라를 옮긴 뒤에 위치를 갱신
public class ParallaxBackground : MonoBehaviour
{
    [Serializable]
    public class Layer
    {
        public string name = "Layer";

        [Tooltip("여러 장이면 순서대로 번갈아 반복됩니다 (나무 모양을 섞고 싶을 때)")]
        public Sprite[] sprites;

        [Tooltip("1 = 화면에 고정(아주 먼 배경), 0 = 월드와 같이 움직임(가까운 배경). 값이 클수록 천천히 지나갑니다")]
        [Range(0f, 1f)] public float cameraFollow = 0.5f;

        [Tooltip("켜면 스프라이트를 화면 전체를 덮도록 늘려서 한 장만 사용 (하늘 같은 단색 배경)")]
        public bool coverCamera;

        [Tooltip("켜면 스프라이트 아랫면을 화면 아래쪽 끝에 맞춥니다 (덤불·나무처럼 바닥에 서 있는 배경)")]
        public bool alignToBottom;

        public float scale = 1f;
        public float yOffset = 0f;
        public float gap = 0f; // 반복되는 스프라이트 사이 간격
        public int sortingOrder = -100;
        public Color color = Color.white;
    }

    public List<Layer> layers = new List<Layer>();
    public string sortingLayerName = "Default";

    private Camera cam;
    private readonly List<List<SpriteRenderer>> pools = new List<List<SpriteRenderer>>();
    private readonly List<float> tileWidths = new List<float>();

    void Start()
    {
        cam = Camera.main;
        BuildPools();
    }

    void BuildPools()
    {
        foreach (Transform child in transform) Destroy(child.gameObject);
        pools.Clear();
        tileWidths.Clear();
        if (cam == null) return;

        float viewHeight = cam.orthographicSize * 2f;
        float viewWidth = viewHeight * cam.aspect;

        foreach (var layer in layers)
        {
            var pool = new List<SpriteRenderer>();
            pools.Add(pool);

            if (layer.sprites == null || layer.sprites.Length == 0 || layer.sprites[0] == null)
            {
                tileWidths.Add(1f);
                continue;
            }

            float spriteWidth = layer.sprites[0].bounds.size.x * layer.scale;
            float tileWidth = layer.coverCamera ? spriteWidth : spriteWidth + layer.gap;
            tileWidths.Add(tileWidth);

            int count = layer.coverCamera ? 1 : Mathf.CeilToInt(viewWidth / tileWidth) + 3;
            var holder = new GameObject(layer.name).transform;
            holder.SetParent(transform, false);

            for (int i = 0; i < count; i++)
            {
                var go = new GameObject(layer.name + "_" + i);
                go.transform.SetParent(holder, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingLayerName = sortingLayerName;
                sr.sortingOrder = layer.sortingOrder;
                sr.color = layer.color;
                sr.sprite = layer.sprites[0];

                if (layer.coverCamera)
                {
                    Vector2 size = layer.sprites[0].bounds.size;
                    float s = Mathf.Max(viewWidth / size.x, viewHeight / size.y) * 1.02f;
                    go.transform.localScale = new Vector3(s, s, 1f);
                }
                else
                {
                    go.transform.localScale = new Vector3(layer.scale, layer.scale, 1f);
                }
                pool.Add(sr);
            }
        }
    }

    void LateUpdate()
    {
        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null) return;
            BuildPools();
        }

        Vector3 camPos = cam.transform.position;

        for (int li = 0; li < layers.Count && li < pools.Count; li++)
        {
            var layer = layers[li];
            var pool = pools[li];
            if (pool.Count == 0) continue;

            float y = camPos.y + layer.yOffset;
            float viewBottom = camPos.y - cam.orthographicSize;

            if (layer.coverCamera)
            {
                pool[0].transform.position = new Vector3(camPos.x, y, 0f);
                continue;
            }

            float tileWidth = tileWidths[li];
            float anchor = camPos.x * layer.cameraFollow;
            int firstIndex = Mathf.FloorToInt((camPos.x - anchor) / tileWidth) - pool.Count / 2;

            for (int i = 0; i < pool.Count; i++)
            {
                int index = firstIndex + i;
                int spriteIndex = ((index % layer.sprites.Length) + layer.sprites.Length) % layer.sprites.Length;
                Sprite sprite = layer.sprites[spriteIndex];
                pool[i].sprite = sprite;

                // 스프라이트마다 높이가 다를 수 있어서 각자 아랫면을 기준으로 맞춤
                float tileY = layer.alignToBottom
                    ? viewBottom + sprite.bounds.extents.y * layer.scale + layer.yOffset
                    : y;
                pool[i].transform.position = new Vector3(anchor + index * tileWidth, tileY, 0f);
            }
        }
    }
}
