using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// 던전 입장 문입니다. Box Collider 2D + Is Trigger를 추가하고 배치하세요.
public class DungeonDoorTrigger : MonoBehaviour
{
    public string sceneToLoad = "JungleDungeonScene";

    private readonly HashSet<Collider2D> playerColliders = new HashSet<Collider2D>();

    public bool IsPlayerInRange
    {
        get
        {
            playerColliders.RemoveWhere(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy);
            return playerColliders.Count > 0;
        }
    }

    private void TrackPlayer(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() == null) return;
        if (other.CompareTag("Player") ||
            (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag("Player")))
            playerColliders.Add(other);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TrackPlayer(other);
    }

    private void OnTriggerStay2D(Collider2D other) { TrackPlayer(other); }
    private void OnDisable() { playerColliders.Clear(); }

    private void OnTriggerExit2D(Collider2D other)
    {
        playerColliders.Remove(other);
    }

    void Update()
    {
        if (SaveSlotWindow.BlocksGameplayInput) return;
        if (GamePause.IsPaused || MenuManager.IsMenuOpen || TutorialManager.InputLocked) return;
        if (NPCDialogueMenu.Instance != null && NPCDialogueMenu.Instance.IsOpen) return;
        if (!IsPlayerInRange) return;

        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.fKey.wasPressedThisFrame)
        {
            // 던전 선택창(TutorialManager)을 거쳐서 입장. 튜토리얼 중에는 의뢰를 접수해야 열림
            if (TutorialManager.Instance != null && TutorialManager.Instance.HandleDoorInteract(sceneToLoad)) return;

            SaveFlowCoordinator.EnterDungeon(sceneToLoad);
        }
    }
}
