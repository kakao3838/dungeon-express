using UnityEngine;
using UnityEngine.InputSystem;

// 퀘스트를 받는 지점입니다. 사장 NPC(메인 퀘스트)나 PC(서브/일반 퀘스트) 모두 이 스크립트를 재사용하면 됩니다.
public class QuestGiverTrigger : MonoBehaviour
{
    [Tooltip("이 지점에서 받을 퀘스트")]
    public QuestData questToGive;

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
        if (!playerInRange) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.fKey.wasPressedThisFrame)
        {
            GiveQuest();
        }
    }

    void GiveQuest()
    {
        if (questToGive == null || QuestManager.Instance == null) return;

        if (QuestManager.Instance.IsQuestActive(questToGive))
        {
            Debug.Log("이미 진행 중인 퀘스트입니다: " + questToGive.questName);
            return;
        }

        if (QuestManager.Instance.IsQuestCompleted(questToGive))
        {
            Debug.Log("이미 완료한 퀘스트입니다: " + questToGive.questName);
            return;
        }

        QuestManager.Instance.StartQuest(questToGive);

        // Town 씬에 실제 물품 지급 연출이 아직 없어서, 임시로 즉시 지급합니다.
        if (playerInventory != null)
        {
            foreach (var req in questToGive.requiredItems)
            {
                for (int i = 0; i < req.quantity; i++)
                {
                    playerInventory.AddItem(req.item);
                }
            }
        }
    }
}
