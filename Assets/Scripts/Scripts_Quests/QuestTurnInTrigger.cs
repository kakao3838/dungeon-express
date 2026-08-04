using UnityEngine;
using UnityEngine.InputSystem;

// 퀘스트 납품 지점입니다. Box Collider 2D + Is Trigger를 추가하고 배치하세요.
public class QuestTurnInTrigger : MonoBehaviour
{
    private bool playerInRange = false;
    private Inventory playerInventory;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = true;
        playerInventory = other.GetComponent<Inventory>();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
        playerInventory = null;
    }

    void Update()
    {
        if (!playerInRange || playerInventory == null) return;

        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.fKey.wasPressedThisFrame)
        {
            if (QuestManager.Instance.HasRequiredItems(playerInventory))
            {
                QuestManager.Instance.CompleteCurrentQuest(playerInventory);
            }
            else
            {
                Debug.Log("필요한 아이템이 부족합니다.");
            }
        }
    }
}
