using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 플레이어가 죽었을 때의 흐름을 맡습니다. (기획서: 체력 소진 → 사망 메시지 → 세이브 포인트에서 리스폰)
//   사망 모션이 끝나면 → 가방 아이템 일부를 랜덤으로 잃고(DeathRules) → "YOU DIE" 화면과 잃은 아이템을 보여주고
//   → 확인하면 사무실로 이동해서 부활 (진행 중인 의뢰, 골드, 장비, 남은 아이템은 그대로)
// 사무실의 'RespawnPoint' 오브젝트(나중에 세이브 포인트 의자)가 있으면 그 자리에서, 없으면 사무실 문 옆에서 부활합니다.
// DeathDirector 오브젝트(씬 간 유지)에 둡니다. Dungeon Express > Build Death Screen 으로 만들어집니다.
public class DeathManager : MonoBehaviour
{
    public static DeathManager Instance { get; private set; }

    [Header("설정")]
    public string townSceneName = "TownScene";
    [Tooltip("사망 화면이 뜬 뒤 이 시간(초) 동안은 확인 키를 눌러도 넘어가지 않음 (실수로 바로 넘기지 않게)")]
    public float minimumShowSeconds = 1.5f;
    public float fadeSeconds = 0.6f;

    [Header("개발용 (임시)")]
    [Tooltip("켜면 F9 키로 바로 죽을 수 있음 (사망 흐름 테스트용). 테스트가 끝나면 끄거나 지우세요")]
    public bool debugDieKey = true;

    [Header("연결")]
    public CanvasGroup screen;
    public TMP_Text titleText;
    public TMP_Text lostHeaderText;
    public RectTransform lostRowsParent;
    public GameObject lostRowTemplate; // 자식: Icon(Image), Name(TMP), Quality(TMP). 꺼둔 견본
    public TMP_Text hintText;

    private readonly List<GameObject> spawnedRows = new List<GameObject>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(transform.root.gameObject); // 사무실로 돌아오면 씬에 있는 사본은 필요 없음
            return;
        }
        Instance = this;
        DontDestroyOnLoad(transform.root.gameObject);

        if (lostRowTemplate != null) lostRowTemplate.SetActive(false);
        HideImmediate();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (!debugDieKey || Instance != this) return;

        var keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.f9Key.wasPressedThisFrame) return;

        var health = FindFirstObjectByType<PlayerHealth>();
        if (health != null && !health.IsDead) health.TakeDamage(health.maxHearts + 1);
    }

    // PlayerHealth가 사망 모션 뒤에 부름. 끝나면 플레이어는 사무실에서 부활해 있음
    public IEnumerator RunDeath(PlayerHealth health)
    {
        GamePause.Push(); // 사망 화면과 씬 이동 동안 몬스터 등이 움직이지 않게

        // 1) 아이템 손실
        var inventory = health.GetComponent<Inventory>();
        List<LostItem> lost = inventory != null ? inventory.RemoveRandomOnDeath() : new List<LostItem>();

        // 2) YOU DIE 화면과 잃은 아이템
        BuildLostRows(lost);
        yield return Fade(0f, 1f);

        float shownAt = Time.realtimeSinceStartup;
        if (hintText != null) hintText.gameObject.SetActive(false);
        while (Time.realtimeSinceStartup - shownAt < minimumShowSeconds) yield return null;

        if (hintText != null) hintText.gameObject.SetActive(true);
        while (!ConfirmPressed()) yield return null;

        // 3) 사무실로 이동 (이미 사무실이면 그대로)
        if (SceneManager.GetActiveScene().name != townSceneName)
        {
            var load = SceneManager.LoadSceneAsync(townSceneName);
            while (!load.isDone) yield return null;
            yield return null; // 새 씬 오브젝트가 자리 잡을 시간
        }

        // 4) 사무실에서 부활
        Vector3 spawn;
        var player = health.GetComponent<PlayerController>();
        FindRespawnPosition(player, out spawn);
        health.RespawnAt(spawn);

        yield return Fade(1f, 0f);
        ClearRows();
        GamePause.Pop();
    }

    // 사무실의 RespawnPoint가 있으면 그 위치, 없으면 문 왼쪽 (튜토리얼에서 돌아올 때 서는 자리)
    void FindRespawnPosition(PlayerController player, out Vector3 position)
    {
        var point = GameObject.Find("RespawnPoint");
        if (point != null)
        {
            position = point.transform.position;
            return;
        }

        position = player != null ? player.transform.position : Vector3.zero;
        var door = FindFirstObjectByType<DungeonDoorTrigger>();
        if (player != null && door != null)
        {
            TutorialManager.PlaceNextToDoor(player, door.transform);
            position = player.transform.position;
        }
    }

    static bool ConfirmPressed()
    {
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        return (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
            || (mouse != null && mouse.leftButton.wasPressedThisFrame);
    }

    // ---------- 화면 ----------

    void BuildLostRows(List<LostItem> lost)
    {
        ClearRows();

        if (lostHeaderText != null)
            lostHeaderText.text = lost.Count > 0 ? "사라진 아이템" : "사라진 아이템 없음";

        if (lostRowTemplate == null || lostRowsParent == null) return;

        foreach (var entry in lost)
        {
            var row = Instantiate(lostRowTemplate, lostRowsParent);
            row.SetActive(true);
            spawnedRows.Add(row);

            var name = row.transform.Find("Name");
            var quality = row.transform.Find("Quality");
            var icon = row.transform.Find("Icon");
            if (name != null) name.GetComponent<TMP_Text>().text = entry.item != null ? entry.item.itemName : "?";
            if (quality != null) quality.GetComponent<TMP_Text>().text = entry.quality + "%";
            if (icon != null)
            {
                var image = icon.GetComponent<Image>();
                bool has = entry.item != null && entry.item.icon != null;
                image.enabled = has;
                image.sprite = has ? entry.item.icon : null;
            }
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(lostRowsParent);
    }

    void ClearRows()
    {
        foreach (var row in spawnedRows) if (row != null) Destroy(row);
        spawnedRows.Clear();
    }

    void HideImmediate()
    {
        if (screen == null) return;
        screen.alpha = 0f;
        screen.blocksRaycasts = false;
        screen.gameObject.SetActive(false);
    }

    // 시간이 멈춘 상태에서도 돌도록 실제 시간(unscaled)으로 서서히 밝기를 바꿈
    IEnumerator Fade(float from, float to)
    {
        if (screen == null) yield break;

        screen.gameObject.SetActive(true);
        screen.blocksRaycasts = true;

        float t = 0f;
        while (t < fadeSeconds)
        {
            t += Time.unscaledDeltaTime;
            screen.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / fadeSeconds));
            yield return null;
        }
        screen.alpha = to;

        if (to <= 0f) HideImmediate();
    }
}
