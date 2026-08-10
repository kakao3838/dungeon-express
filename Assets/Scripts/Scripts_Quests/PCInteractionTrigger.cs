using UnityEngine;
using UnityEngine.InputSystem;

public class PCInteractionTrigger : MonoBehaviour
{
    public QuestBoardUI questBoard;

    private bool playerInRange = false;
    private bool isOpen = false;

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
        if (!playerInRange || questBoard == null) return;

        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.fKey.wasPressedThisFrame)
        {
            isOpen = !isOpen;
            if (isOpen) questBoard.OpenBoard();
            else questBoard.CloseBoard();
        }
    }
}
