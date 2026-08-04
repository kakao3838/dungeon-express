using UnityEngine;
using UnityEngine.InputSystem;

// 퀘스트를 주는 NPC/오브젝트입니다. Box Collider 2D + Is Trigger를 추가하고 배치하세요.
public class QuestGiverTrigger : MonoBehaviour
{
    public QuestData questToGive;

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
        if (!playerInRange || questToGive == null) return;

        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.fKey.wasPressedThisFrame)
        {
            QuestManager.Instance.StartQuest(questToGive);
        }
    }
}
