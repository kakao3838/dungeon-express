using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// 던전 입장 문입니다. Box Collider 2D + Is Trigger를 추가하고 배치하세요.
public class DungeonDoorTrigger : MonoBehaviour
{
    public string sceneToLoad = "JungleDungeonScene";

    private bool playerInRange = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInRange = false;
    }

    void Update()
    {
        if (!playerInRange) return;

        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.fKey.wasPressedThisFrame)
        {
            // 던전 선택창(TutorialManager)을 거쳐서 입장. 튜토리얼 중에는 의뢰를 접수해야 열림
            if (TutorialManager.Instance != null && TutorialManager.Instance.HandleDoorInteract(sceneToLoad)) return;

            SceneManager.LoadScene(sceneToLoad);
        }
    }
}
