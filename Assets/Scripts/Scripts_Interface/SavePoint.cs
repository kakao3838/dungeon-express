using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(BoxCollider2D))]
[DisallowMultipleComponent]
[DefaultExecutionOrder(-200)]
public sealed class SavePoint : MonoBehaviour
{
    [Header("사용 장소")]
    public string officeSceneName = "TownScene";
    [Tooltip("에디터의 PlayerMonsterTest 씬에서만 테스트를 허용합니다. 빌드에서는 무시됩니다.")]
    public bool allowEditorTestScene;

    [Header("안내 표시")]
    public TMP_FontAsset promptFont;
    public Vector3 promptOffset = new Vector3(0f, 1.8f, 0f);
    [Tooltip("불러오기 후 플레이어가 서 있을 위치입니다.")]
    public Vector3 loadSpawnOffset = new Vector3(-1.2f, 0.1f, 0f);

    private readonly HashSet<Collider2D> players = new HashSet<Collider2D>();
    private GameObject prompt;
    private SaveSlotWindow openedWindow;

    public bool IsSceneAllowed => SaveScenePolicy.IsAllowed(
        SceneManager.GetActiveScene().name, officeSceneName, Application.isEditor, allowEditorTestScene);

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
        var labelObject = new GameObject("Prompt", typeof(RectTransform));
        labelObject.transform.SetParent(transform, false);
        labelObject.transform.localPosition = promptOffset;
        var label = labelObject.AddComponent<TextMeshPro>();
        if (promptFont != null) label.font = promptFont;
        label.text = "F  저장";
        label.fontSize = 3f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.outlineWidth = 0.15f;
        label.rectTransform.sizeDelta = new Vector2(3f, 0.6f);
        label.GetComponent<MeshRenderer>().sortingOrder = 20;
        prompt = labelObject;
        prompt.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other) { TrackPlayer(other); }
    private void OnTriggerStay2D(Collider2D other) { TrackPlayer(other); }
    private void OnTriggerExit2D(Collider2D other) { players.Remove(other); }

    private void TrackPlayer(Collider2D other)
    {
        if (other.GetComponentInParent<Inventory>() == null) return;
        if (other.CompareTag("Player") ||
            (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag("Player")))
            players.Add(other);
    }

    private void Update()
    {
        players.RemoveWhere(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy);
        bool doorHasPriority = false;
        if (players.Count > 0)
        {
            foreach (var door in FindObjectsByType<DungeonDoorTrigger>(FindObjectsSortMode.None))
            {
                if (!door.IsPlayerInRange) continue;
                doorHasPriority = true;
                break;
            }
        }
        bool available = players.Count > 0 && IsSceneAllowed && !GamePause.IsPaused &&
            !doorHasPriority &&
            !SaveSlotWindow.BlocksGameplayInput && !MenuManager.IsMenuOpen &&
            !TutorialManager.InputLocked && !TutorialManager.BlocksMenu &&
            !(NPCDialogueMenu.Instance != null && NPCDialogueMenu.Instance.IsOpen);
        if (prompt != null) prompt.SetActive(available);
        var keyboard = Keyboard.current;
        if (!available || keyboard == null || !keyboard.fKey.wasPressedThisFrame) return;
        openedWindow = SaveSlotWindow.OpenSave(() => openedWindow = null, allowEditorTestScene, officeSceneName);
        if (prompt != null) prompt.SetActive(false);
    }

    private void OnDisable()
    {
        players.Clear();
        if (prompt != null) prompt.SetActive(false);
        SaveSlotWindow owned = openedWindow;
        openedWindow = null;
        if (owned != null && owned.IsOpen) owned.Close();
    }

    private void OnDrawGizmosSelected()
    {
        var box = GetComponent<BoxCollider2D>();
        if (box == null) return;
        Matrix4x4 previous = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(box.offset, box.size);
        Gizmos.matrix = previous;
    }
}
