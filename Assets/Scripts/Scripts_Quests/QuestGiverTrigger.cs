using UnityEngine;
using UnityEngine.InputSystem;

// 퀘스트를 주는 NPC/오브젝트입니다. Box Collider 2D + Is Trigger를 추가하고 배치하세요.
public class QuestGiverTrigger : MonoBehaviour
{
    [Header("대화")]
    public string npcName = "사장";
    [TextArea] public string talkLine = "ㅎㅇ 나 사장";

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
        if (MenuManager.IsMenuOpen) return; // 태블릿 메뉴가 열려 있을 때는 대화 시작 안 함
        if (NPCDialogueMenu.Instance != null && NPCDialogueMenu.Instance.IsOpen) return;

        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.fKey.wasPressedThisFrame)
        {
            if (NPCDialogueMenu.Instance != null)
            {
                NPCDialogueMenu.Instance.Open(this);
            }
            else if (questToGive != null)
            {
                // 대화 메뉴가 씬에 없을 때를 위한 예전 동작 (안전장치)
                QuestManager.Instance.StartQuest(questToGive, playerInventory);
            }
        }
    }

    // "1. 메인 퀘스트" 선택 시 NPCDialogueMenu가 호출
    public void GiveMainQuest()
    {
        if (questToGive == null)
        {
            Debug.Log(npcName + ": 지금은 줄 퀘스트가 없어요.");
            return;
        }
        QuestManager.Instance.StartQuest(questToGive, playerInventory);
    }
}
