using UnityEngine;
using UnityEngine.InputSystem;

// 테스트 전용 스크립트입니다.
// 이제 QuestGiverTrigger가 퀘스트에 필요한 아이템을 정확히 지급해주므로,
// 임의로 아이템을 넣는 기능은 제거했습니다. 가방을 빠르게 비우는 기능만 남겨뒀어요.
public class DebugInventorySeeder : MonoBehaviour
{
    public Inventory inventory;

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || inventory == null) return;

        // 테스트 중 가방+퀘스트 상태를 한 번에 초기화하고 싶을 때 O키
        if (keyboard.oKey.wasPressedThisFrame)
        {
            inventory.ClearAll();
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.ResetAll();
            }
            Debug.Log("가방과 퀘스트 상태를 초기화했습니다.");
        }
    }
}