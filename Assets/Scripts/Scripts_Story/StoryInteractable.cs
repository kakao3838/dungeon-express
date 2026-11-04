using UnityEngine;
using UnityEngine.InputSystem;

// 스토리에 쓰이는 NPC/오브젝트(레온, 영약 우물 등)입니다.
// StoryManager가 "지금 상호작용할 수 있다"고 할 때만 가까이 가면 F 안내가 뜨고, F를 누르면 이야기가 진행됩니다.
// 가까움의 기준은 이 오브젝트(와 자식)의 스프라이트가 실제로 보이는 위치입니다.
// 그래서 씬에서 스프라이트가 있는 자식 오브젝트만 옮겨도 상호작용 위치와 F 안내가 같이 따라갑니다.
public class StoryInteractable : MonoBehaviour
{
    [Tooltip("StoryManager의 상호작용 ID (예: leon, elixir_well)")]
    public string interactionId;
    [Tooltip("F 안내 말풍선 (스프라이트 위쪽에 자동으로 따라다님)")]
    public GameObject promptRoot;
    [Tooltip("스프라이트 가장자리에서 이 거리 안으로 가까이 가면 상호작용할 수 있음")]
    public float interactRange = 1.8f;
    [Tooltip("F 안내 말풍선이 스프라이트 위쪽에서 떨어진 거리")]
    public float promptHeight = 0.6f;

    private PlayerController player;

    void Update()
    {
        var story = StoryManager.Instance;
        if (player == null) player = FindFirstObjectByType<PlayerController>();

        Bounds bounds = GetVisualBounds();
        bool near = player != null && bounds.SqrDistance(new Vector3(player.transform.position.x, player.transform.position.y, bounds.center.z)) <= interactRange * interactRange;
        bool show = near && story != null && !GamePause.IsPaused && story.CanInteract(interactionId);

        if (promptRoot != null)
        {
            if (promptRoot.activeSelf != show) promptRoot.SetActive(show);
            if (show) promptRoot.transform.position = new Vector3(bounds.center.x, bounds.max.y + promptHeight, promptRoot.transform.position.z);
        }
        if (!show) return;

        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.fKey.wasPressedThisFrame) story.HandleInteract(interactionId);
    }

    // 이 오브젝트와 자식들의 스프라이트를 모두 감싼 범위 (스프라이트가 없으면 오브젝트 위치)
    Bounds GetVisualBounds()
    {
        bool has = false;
        Bounds bounds = new Bounds(transform.position, Vector3.zero);
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
        {
            if (!sr.enabled) continue;
            if (!has) { bounds = sr.bounds; has = true; }
            else bounds.Encapsulate(sr.bounds);
        }
        return bounds;
    }
}
