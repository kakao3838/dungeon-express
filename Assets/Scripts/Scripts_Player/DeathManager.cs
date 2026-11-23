using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

// 플레이어가 죽었을 때의 흐름을 맡습니다.
//   사망 모션이 끝나면 → 가방 아이템 일부를 랜덤으로 잃고(DeathRules)
//   → 현재 프로필에 자동 저장(손실이 반영된 상태)
//   → 마지막으로 닿은 세이브 포인트(없으면 사무실)에서 부활 → 좌하단에 사라진 물품 알림.
// 의뢰는 자동으로 포기되지 않습니다. 잃은 물품이 필요한 진행 중 의뢰가 있으면 알림에 "'의뢰 이름' 포기" 버튼이 뜨고,
// 플레이어가 그걸 눌렀을 때만 그 의뢰를 포기합니다 (그 의뢰에서 받아 가방에 남은 물품도 함께 회수).
// 사망 창은 없고, 화면은 장면 이동을 가리는 짧은 암전만 씁니다.
// DeathDirector 오브젝트(씬 간 유지)에 둡니다. Dungeon Express > Build Death Screen 으로 만들어집니다.
public class DeathManager : MonoBehaviour
{
    public static DeathManager Instance { get; private set; }

    [Header("설정")]
    public string townSceneName = "TownScene";
    public float fadeSeconds = 0.4f;

    [Header("개발용 (임시)")]
    [Tooltip("켜면 F9 키로 바로 죽을 수 있음 (사망 흐름 테스트용). 테스트가 끝나면 끄거나 지우세요")]
    public bool debugDieKey = true;

    [Header("연결")]
    public CanvasGroup fade;          // 장면 이동을 가리는 검은 화면
    public DeathNoticeStack notices;  // 좌하단 사라진 물품 알림

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(transform.root.gameObject); // 사무실로 돌아오면 씬에 있는 사본은 필요 없음
            return;
        }
        Instance = this;
        DontDestroyOnLoad(transform.root.gameObject);

        HideFadeImmediate();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Instance = null;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (notices == null) return;

        // 타이틀로 나가면 이전 판의 알림은 지움
        if (scene.name == "TitleScene") { notices.Clear(); return; }

        // 알림이 남아 있는 채로 장면이 바뀌어도 X를 누를 수 있게
        if (notices.Count > 0) EnsureEventSystem();
    }

    void Update()
    {
        if (SaveSlotWindow.BlocksGameplayInput) return;
        if (!debugDieKey || Instance != this) return;

        var keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.f9Key.wasPressedThisFrame) return;

        var health = FindFirstObjectByType<PlayerHealth>();
        if (health != null && !health.IsDead) health.TakeDamage(health.maxHearts + 1);
    }

    // PlayerHealth가 사망 모션 뒤에 부름. 끝나면 플레이어는 마지막 세이브 포인트(없으면 사무실)에서 부활해 있음
    public IEnumerator RunDeath(PlayerHealth health)
    {
        GamePause.Push(); // 암전과 씬 이동 동안 몬스터 등이 움직이지 않게

        var lost = new List<LostItem>();

        try
        {
            // 1) 아이템 손실 → 2) 손실이 반영된 상태로 자동 저장
            var inventory = health.GetComponent<Inventory>();
            if (inventory != null) lost = inventory.RemoveRandomOnDeath();
            AutoSave();

            // 4) 부활할 곳 정하고(세이브 포인트 또는 사무실) 필요하면 그 씬으로 이동
            yield return Fade(0f, 1f);

            string targetScene;
            string checkpointId;
            ResolveRespawnTarget(out targetScene, out checkpointId);

            if (SceneManager.GetActiveScene().name != targetScene)
            {
                var load = SceneManager.LoadSceneAsync(targetScene);
                while (load != null && !load.isDone) yield return null;
                yield return null; // 새 씬 오브젝트가 자리 잡을 시간
            }

            // 5) 부활
            Vector3 spawn;
            var player = health.GetComponent<PlayerController>();
            FindRespawnPosition(player, targetScene, checkpointId, out spawn);
            health.RespawnAt(spawn);
            Physics2D.SyncTransforms();
            if (CameraFollow.Instance != null) CameraFollow.Instance.SnapToTarget();

            yield return Fade(1f, 0f);
        }
        finally
        {
            GamePause.Pop();
        }

        // 3) 게임이 다시 움직이는 상태에서 알림 표시
        ShowNotices(lost);
    }

    // ---------- 의뢰 포기 (알림의 버튼을 눌렀을 때) ----------

    // 이 물품이 필요한 진행 중 의뢰들
    static List<QuestData> ActiveQuestsRequiring(ItemData item)
    {
        var result = new List<QuestData>();
        var quests = QuestManager.Instance;
        if (quests == null || item == null) return result;

        foreach (var quest in quests.activeQuests)
        {
            if (quest == null || quest.requiredItems == null) continue;
            foreach (var requirement in quest.requiredItems)
            {
                if (requirement.item == item) { result.Add(quest); break; }
            }
        }
        return result;
    }

    // 알림의 "'의뢰 이름' 포기"를 눌렀을 때: 그 물품이 필요한 진행 중 의뢰를 포기함.
    // 그 의뢰에서 받아서 가방에 남아 있는 물품도 함께 회수됨 (CancelQuest가 처리)
    static void AbandonQuestsRequiring(ItemData item)
    {
        var quests = QuestManager.Instance;
        if (quests == null) return;

        var inventory = FindFirstObjectByType<Inventory>();
        foreach (var quest in ActiveQuestsRequiring(item)) quests.CancelQuest(quest, inventory);
    }

    // ---------- 저장 ----------

    // 활성 프로필이 없으면(에디터에서 타이틀 없이 바로 시작 등) 저장만 건너뜀. 저장 실패가 사망 처리를 막지 않게 함
    static void AutoSave()
    {
        if (!SaveGameService.HasActiveProfile)
        {
            Debug.LogWarning("[DeathManager] 활성 프로필이 없어 사망 자동 저장을 건너뜁니다.");
            return;
        }

        try { SaveGameService.SaveCurrentProfile(); }
        catch (System.Exception e) { Debug.LogException(e); }
    }

    // ---------- 부활 위치 ----------

    // 마지막으로 닿은 세이브 포인트가 있고 그 씬을 불러올 수 있으면 거기, 아니면 사무실
    void ResolveRespawnTarget(out string sceneName, out string checkpointId)
    {
        sceneName = townSceneName;
        checkpointId = string.Empty;

        string savedScene = SaveCheckpointState.SceneName;
        string savedId = SaveCheckpointState.CheckpointId;
        if (string.IsNullOrWhiteSpace(savedScene) || string.IsNullOrWhiteSpace(savedId)) return;
        if (!Application.CanStreamedLevelBeLoaded(savedScene)) return;

        sceneName = savedScene;
        checkpointId = savedId;
    }

    // 세이브 포인트가 있으면 그 옆, 없으면 사무실의 RespawnPoint, 그것도 없으면 문 왼쪽 (튜토리얼에서 돌아올 때 서는 자리)
    void FindRespawnPosition(PlayerController player, string sceneName, string checkpointId, out Vector3 position)
    {
        if (!string.IsNullOrEmpty(checkpointId))
        {
            SavePoint point = SavePoint.FindCheckpoint(sceneName, checkpointId);
            if (point != null)
            {
                position = point.transform.TransformPoint(point.loadSpawnOffset);
                return;
            }
            Debug.LogWarning("[DeathManager] 세이브 포인트를 찾지 못해 기본 위치에서 부활합니다: " + sceneName + " / " + checkpointId);
        }

        var respawn = GameObject.Find("RespawnPoint");
        if (respawn != null)
        {
            position = respawn.transform.position;
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

    // ---------- 알림 ----------

    // 잃은 아이템마다 알림 하나. 그 물품이 필요한 진행 중 의뢰가 있으면 "'의뢰 이름' 포기" 버튼을 같이 보여줌
    void ShowNotices(List<LostItem> lost)
    {
        if (notices == null || lost.Count == 0) return;

        EnsureEventSystem(); // 버튼 클릭을 받으려면 필요

        foreach (var entry in lost)
        {
            ItemData item = entry.item;
            string itemName = item != null ? item.itemName : "?";

            // 줄 내용은 알림이 갱신될 때마다 다시 계산함 (다른 알림에서 이미 포기한 의뢰는 사라지게)
            notices.Show("사라진 물품: " + itemName,
                () => AbandonQuestLine(item),
                () => AbandonQuestsRequiring(item));
        }
    }

    // "'의뢰 이름' 포기" (아직 진행 중인 의뢰만)
    static string AbandonQuestLine(ItemData item)
    {
        var names = new List<string>();
        foreach (var quest in ActiveQuestsRequiring(item)) names.Add(quest.questName);
        if (names.Count == 0) return string.Empty;
        return "'" + string.Join("', '", names) + "' 포기";
    }

    // 씬에 EventSystem이 없으면(예: 정글 던전) 알림의 X 버튼을 누를 수 없으므로 하나 만들어 둠.
    // DontDestroyOnLoad를 걸지 않아서 씬이 바뀌면 사라지고, 다음 씬의 EventSystem과 겹치지 않음
    static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;

        var go = new GameObject("EventSystem (Death)");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
    }

    // ---------- 암전 ----------

    void HideFadeImmediate()
    {
        if (fade == null) return;
        fade.alpha = 0f;
        fade.blocksRaycasts = false;
        fade.gameObject.SetActive(false);
    }

    // 시간이 멈춘 상태에서도 돌도록 실제 시간(unscaled)으로 서서히 밝기를 바꿈
    IEnumerator Fade(float from, float to)
    {
        if (fade == null) yield break;

        fade.gameObject.SetActive(true);
        fade.blocksRaycasts = true;

        float t = 0f;
        while (t < fadeSeconds)
        {
            t += Time.unscaledDeltaTime;
            fade.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / fadeSeconds));
            yield return null;
        }
        fade.alpha = to;

        if (to <= 0f) HideFadeImmediate();
    }
}
