using UnityEngine;
using UnityEngine.InputSystem;

// 사무실의 PC 오브젝트입니다. Box Collider 2D + Is Trigger와 함께 배치하세요.
// - 튜토리얼에서 PC 차례가 되면 F 안내가 떠오르고, F키를 누르면 TutorialManager가 PC 화면을 엽니다.
// - 튜토리얼이 아닐 때는 가까이 가서 F키를 누르면 PC 화면(PCBoardUI)이 열립니다.
public class OfficePC : MonoBehaviour
{
    [Tooltip("플레이어가 가까이 있을 때만 보이는 'F' 안내 말풍선")]
    public GameObject promptRoot;
    [Tooltip("튜토리얼이 아닐 때 F키로 여는 PC 화면")]
    public PCBoardUI board;

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
        if (SaveSlotWindow.BlocksGameplayInput) return;
        var manager = TutorialManager.Instance;
        var story = StoryManager.Instance;
        bool tutorialTurn = manager != null && manager.WaitingForPC;
        // 메인 퀘스트 스토리가 PC를 쓸 차례이면 그 연출이 PC 화면을 엶 (플레이어가 직접 접수하지 않음)
        bool storyTurn = !tutorialTurn && story != null && story.WaitingForPC;
        // 튜토리얼이 진행 중인 동안에는 튜토리얼이 정한 차례에만 열림
        bool freeUse = !storyTurn && !TutorialManager.BlocksMenu && board != null && !board.IsOpen && !GamePause.IsPaused;

        bool show = inRange && !GamePause.IsPaused && (tutorialTurn || storyTurn || freeUse);
        if (tutorialTurn) show = inRange; // 튜토리얼 차례는 이미 정지 상태가 아님
        if (promptRoot != null && promptRoot.activeSelf != show) promptRoot.SetActive(show);
        if (!show) return;

        var keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.fKey.wasPressedThisFrame) return;

        if (tutorialTurn) manager.OnPCInteract();
        else if (storyTurn) story.OnPCInteract();
        else board.Open();
    }
}
