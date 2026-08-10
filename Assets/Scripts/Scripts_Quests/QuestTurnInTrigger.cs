using UnityEngine;
using UnityEngine.InputSystem;

// 퀘스트 납품 지점입니다. Box Collider 2D + Is Trigger를 추가하고 배치하세요.
public class QuestTurnInTrigger : MonoBehaviour
{
    [Tooltip("이 지점이 받는 퀘스트. 현재 진행 중인 퀘스트가 이거랑 다르면 무시함 (다른 퀘스트가 연쇄로 완료되는 걸 방지)")]
    public QuestData questToComplete;

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
        if (questToComplete == null || !QuestManager.Instance.IsQuestActive(questToComplete)) return;

        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.fKey.wasPressedThisFrame)
        {
            if (QuestManager.Instance.HasRequiredItems(questToComplete, playerInventory))
            {
                QuestManager.Instance.CompleteQuest(questToComplete, playerInventory);
            }
            else
            {
                Debug.Log("필요한 아이템이 부족합니다.");
            }
        }
    }
}
