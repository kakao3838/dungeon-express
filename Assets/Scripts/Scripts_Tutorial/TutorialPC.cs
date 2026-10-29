using UnityEngine;
using UnityEngine.InputSystem;

// 사무실의 PC 오브젝트입니다. Box Collider 2D + Is Trigger와 함께 배치하세요.
// 튜토리얼에서 PC 차례가 되면 F 안내가 떠오르고, F키를 누르면 TutorialManager가 PC 화면을 엽니다.
public class TutorialPC : MonoBehaviour
{
    [Tooltip("플레이어가 가까이 있을 때만 보이는 'F' 안내 말풍선")]
    public GameObject promptRoot;

    private bool inRange;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) inRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) inRange = false;
    }

    void Update()
    {
        var manager = TutorialManager.Instance;
        bool show = inRange && manager != null && manager.WaitingForPC;
        if (promptRoot != null && promptRoot.activeSelf != show) promptRoot.SetActive(show);
        if (!show) return;

        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.fKey.wasPressedThisFrame) manager.OnPCInteract();
    }
}
