using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(BoxCollider2D))]
[DisallowMultipleComponent]
public sealed class GoldPickup : MonoBehaviour
{
    [Min(1)] public int goldAmount = 500;
    public TMP_FontAsset promptFont;
    public Vector3 promptOffset = new Vector3(0f, 1.1f, 0f);

    private readonly HashSet<Collider2D> playerColliders = new HashSet<Collider2D>();
    private PlayerWallet playerWallet;
    private GameObject prompt;
    private bool reportedMissingWallet;

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;

        var promptObject = new GameObject("Prompt", typeof(RectTransform));
        promptObject.transform.SetParent(transform, false);
        promptObject.transform.localPosition = promptOffset;
        var label = promptObject.AddComponent<TextMeshPro>();
        if (promptFont != null) label.font = promptFont;
        label.text = $"줍기";
        label.fontSize = 3f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.outlineWidth = 0.15f;
        label.rectTransform.sizeDelta = new Vector2(3f, 0.6f);
        label.GetComponent<MeshRenderer>().sortingOrder = 20;
        prompt = promptObject;
        prompt.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other) => TrackPlayer(other);
    private void OnTriggerStay2D(Collider2D other) => TrackPlayer(other);

    private void OnTriggerExit2D(Collider2D other)
    {
        playerColliders.Remove(other);
        if (playerColliders.Count == 0) playerWallet = null;
    }

    private void TrackPlayer(Collider2D other)
    {
        if (other.GetComponentInParent<Inventory>() == null) return;
        if (!other.CompareTag("Player") &&
            (other.attachedRigidbody == null || !other.attachedRigidbody.CompareTag("Player")))
            return;

        PlayerWallet wallet = other.GetComponentInParent<PlayerWallet>();
        if (wallet == null)
        {
            if (!reportedMissingWallet)
            {
                Debug.LogError("골드 얻음.", this);
                reportedMissingWallet = true;
            }
            return;
        }

        playerColliders.Add(other);
        playerWallet = wallet;
    }

    private void Update()
    {
        playerColliders.RemoveWhere(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy);
        if (playerColliders.Count == 0) playerWallet = null;

        bool available = playerColliders.Count > 0 && playerWallet != null &&
            !GamePause.IsPaused && !SaveSlotWindow.BlocksGameplayInput &&
            !MenuManager.IsMenuOpen && !TutorialManager.InputLocked && !TutorialManager.BlocksMenu &&
            !(NPCDialogueMenu.Instance != null && NPCDialogueMenu.Instance.IsOpen);

        if (prompt != null) prompt.SetActive(available);
        if (!available) return;

        var keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.fKey.wasPressedThisFrame) return;

        playerWallet.AddGold(goldAmount);
        Destroy(gameObject);
    }

    private void OnDisable()
    {
        playerColliders.Clear();
        playerWallet = null;
        if (prompt != null) prompt.SetActive(false);
    }
}
