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

    [Header("수동 배치")]
    [Tooltip("켜면 자동 생성 대신 씬에 구워둔(Bake) 자식 오브젝트를 그대로 쓰고 시차 이동만 적용합니다. Scene 뷰에서 직접 옮길 수 있습니다")]
    public bool manualLayout;
    [Tooltip("Bake 할 때 카메라 위치 기준 좌우로 깔 전체 폭(월드 유닛)")]
    public float bakeWidth = 80f;

    private Camera cam;
    private readonly List<List<SpriteRenderer>> pools = new List<List<SpriteRenderer>>();
    private readonly List<float> tileWidths = new List<float>();

    // 수동 모드에서 각 배경 오브젝트가 시작 시점에 놓여 있던 위치와 시차 계수
    private struct ManualItem
    {
        public Transform t;
        public float follow;
        public Vector3 basePos;
    }
    private readonly List<ManualItem> manualItems = new List<ManualItem>();
    [Tooltip("수동 배치한 위치가 '정확히 보이는' 기준 카메라 위치 (Bake 할 때 자동 기록). 플레이 중 카메라가 여기서 벗어난 만큼 시차로 따라옵니다")]
    public Vector2 manualCamOrigin;
    private bool manualReady;

    void Start()
    {
        cam = Camera.main;
        if (manualLayout) return; // 수동 모드는 LateUpdate 첫 프레임에 초기화
        BuildPools();
    }

    void InitManual()
    {
        manualItems.Clear();
        foreach (Transform holder in transform)
        {
            var layer = layers.Find(l => l.name == holder.name);
            float follow = layer != null ? layer.cameraFollow : 0.5f;
            foreach (Transform item in holder)
                manualItems.Add(new ManualItem { t = item, follow = follow, basePos = item.position });
        }
        manualReady = true;
    }

    void UpdateManual()
    {
        Vector2 d = (Vector2)cam.transform.position - manualCamOrigin;
        foreach (var m in manualItems)
        {
            if (m.t == null) continue;
            m.t.position = new Vector3(m.basePos.x + d.x * m.follow, m.basePos.y + d.y, m.basePos.z);
        }
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
            if (!manualLayout) BuildPools();
        }

        if (manualLayout)
        {
            if (!manualReady) InitManual();
            UpdateManual();
            return;
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

#if UNITY_EDITOR
    // 에디터에서 배경을 실제 오브젝트로 구워서 Scene 뷰에서 손으로 옮길 수 있게 합니다.
    // 이미 구워둔 배경이 있으면 지우고 다시 만들므로, 손으로 고친 뒤에는 다시 실행하지 마세요.
    [ContextMenu("Bake To Scene (수동 편집용)")]
    void BakeToScene()
    {
        var bakeCam = Camera.main;
        if (bakeCam == null) { Debug.LogWarning("[ParallaxBackground] Main Camera가 없어 Bake 할 수 없습니다."); return; }

        UnityEditor.Undo.RegisterFullObjectHierarchyUndo(gameObject, "Bake ParallaxBackground");
        for (int i = transform.childCount - 1; i >= 0; i--) DestroyImmediate(transform.GetChild(i).gameObject);

        Vector3 camPos = bakeCam.transform.position;
        float viewHeight = bakeCam.orthographicSize * 2f;
        float viewWidth = viewHeight * bakeCam.aspect;
        float viewBottom = camPos.y - bakeCam.orthographicSize;

        foreach (var layer in layers)
        {
            if (layer.sprites == null || layer.sprites.Length == 0 || layer.sprites[0] == null) continue;

            var holder = new GameObject(layer.name).transform;
            holder.SetParent(transform, false);

            if (layer.coverCamera)
            {
                var sky = CreateTile(holder, layer, layer.sprites[0], layer.name + "_0");
                Vector2 size = layer.sprites[0].bounds.size;
                float s = Mathf.Max(viewWidth / size.x, viewHeight / size.y) * 1.02f;
                sky.transform.localScale = new Vector3(s, s, 1f);
                sky.transform.position = new Vector3(camPos.x, camPos.y + layer.yOffset, 0f);
                continue;
            }

            float tileWidth = layer.sprites[0].bounds.size.x * layer.scale + layer.gap;
            int count = Mathf.CeilToInt(Mathf.Max(bakeWidth, viewWidth) / tileWidth);
            int first = Mathf.FloorToInt(camPos.x / tileWidth) - count / 2;
            for (int i = 0; i < count; i++)
            {
                int index = first + i;
                Sprite sprite = layer.sprites[((index % layer.sprites.Length) + layer.sprites.Length) % layer.sprites.Length];
                var sr = CreateTile(holder, layer, sprite, layer.name + "_" + i);
                float tileY = layer.alignToBottom
                    ? viewBottom + sprite.bounds.extents.y * layer.scale + layer.yOffset
                    : camPos.y + layer.yOffset;
                sr.transform.localScale = new Vector3(layer.scale, layer.scale, 1f);
                sr.transform.position = new Vector3(index * tileWidth, tileY, 0f);
            }
        }

        manualLayout = true;
        manualCamOrigin = camPos;
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }

    SpriteRenderer CreateTile(Transform holder, Layer layer, Sprite sprite, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(holder, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = sortingLayerName;
        sr.sortingOrder = layer.sortingOrder;
        sr.color = layer.color;
        sr.sprite = sprite;
        return sr;
    }

    [ContextMenu("Clear Baked (자동 생성으로 되돌리기)")]
    void ClearBaked()
    {
        UnityEditor.Undo.RegisterFullObjectHierarchyUndo(gameObject, "Clear ParallaxBackground");
        for (int i = transform.childCount - 1; i >= 0; i--) DestroyImmediate(transform.GetChild(i).gameObject);
        manualLayout = false;
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }
#endif
}
