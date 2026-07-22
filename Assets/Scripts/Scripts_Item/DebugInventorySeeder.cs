using UnityEngine;
using UnityEngine.InputSystem;

// 테스트 전용 스크립트입니다.
// Town 씬(의뢰 시스템)이 아직 없을 때, I키로 가방에 테스트 아이템을 넣어서
// 인벤토리 UI와 로직이 잘 동작하는지 확인하는 용도입니다.
// 나중에 진짜 의뢰 시스템이 생기면 이 컴포넌트는 지우고,
// 그 시스템이 Inventory.AddItem()을 직접 호출하면 됩니다.
public class DebugInventorySeeder : MonoBehaviour
{
    public Inventory inventory;
    public ItemData testItem;

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || inventory == null || testItem == null) return;

        if (keyboard.iKey.wasPressedThisFrame)
        {
            inventory.AddItem(testItem);
        }
    }
}
