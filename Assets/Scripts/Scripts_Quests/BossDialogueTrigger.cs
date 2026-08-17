using UnityEngine;
using UnityEngine.InputSystem;

public class BossDialogueTrigger : MonoBehaviour
{
    public BossDialogueUI dialogueUI;

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
        if (!playerInRange || dialogueUI == null) return;

        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.fKey.wasPressedThisFrame)
        {
            isOpen = !isOpen;
            if (isOpen) dialogueUI.OpenDialogue();
            else dialogueUI.CloseDialogue();
        }
    }
}
