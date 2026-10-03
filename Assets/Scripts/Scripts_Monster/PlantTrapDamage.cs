using System.Collections;
using UnityEngine;

public class PlantTrapDamage : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private float angryDuration = 1.17f;

    private Collider2D damageZone;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Sprite idleSprite;
    private bool isReacting;

    private void Awake()
    {
        damageZone = GetComponent<Collider2D>();
        animator = GetComponentInParent<Animator>();
        spriteRenderer = GetComponentInParent<SpriteRenderer>();

        if (spriteRenderer != null)
            idleSprite = spriteRenderer.sprite;

        // This controller currently contains only the angry animation.
        // Keep it stopped until the player actually stomps the mushroom.
        if (animator != null)
            animator.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerDamageReceiver receiver = other.GetComponent<PlayerDamageReceiver>();
        if (receiver == null)
            receiver = other.GetComponentInParent<PlayerDamageReceiver>();

        if (receiver == null)
            return;

        Rigidbody2D playerBody = other.attachedRigidbody;
        if (playerBody == null)
            playerBody = receiver.GetComponent<Rigidbody2D>();

        // Ignore upward jumps and side contacts. Only a downward stomp counts.
        if (playerBody == null || playerBody.linearVelocity.y > 0.05f)
            return;

        // HeadDamageZone 자체가 머리 위의 얇은 트리거이므로 플레이어의 발 위치만 확인합니다.
        // 기존 bounds.min 검사는 플레이어의 큰 몸통 콜라이더 때문에 정상 착지도 차단했습니다.
        if (damageZone != null && playerBody.position.y < damageZone.bounds.center.y)
            return;

        receiver.Hit(damage, transform.position);

        if (!isReacting)
            StartCoroutine(PlayAngryOnce());
    }

    private IEnumerator PlayAngryOnce()
    {
        isReacting = true;

        if (animator != null)
        {
            animator.enabled = true;
            animator.Play("mushroom_angry", 0, 0f);
            animator.Update(0f);
        }

        yield return new WaitForSeconds(angryDuration);

        if (animator != null)
            animator.enabled = false;

        if (spriteRenderer != null && idleSprite != null)
            spriteRenderer.sprite = idleSprite;

        isReacting = false;
    }
}
