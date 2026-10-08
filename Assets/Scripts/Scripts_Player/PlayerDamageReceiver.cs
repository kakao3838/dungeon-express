using System.Collections;
using UnityEngine;

public class PlayerDamageReceiver : MonoBehaviour
{
    [Header("Tint")]
    [SerializeField] private Color hitColor = Color.red;
    [SerializeField] private float hitColorDuration = 0.15f;

    private PlayerHealth playerHealth;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private PlayerKnockback playerKnockback;

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        playerKnockback = GetComponent<PlayerKnockback>();

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    private void OnEnable()
    {
        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();

        if (playerHealth != null)
            playerHealth.OnDamaged += PlayHitColor;
    }

    private void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.OnDamaged -= PlayHitColor;
    }

    public void Hit(int damage, Vector2 attackerPosition)
    {
        if (playerHealth == null)
            return;

        int beforeHealth = playerHealth.CurrentHearts;

        playerHealth.TakeDamage(damage);

        // 실제 피해가 안 들어갔으면 무적 상태
        if (playerHealth.CurrentHearts == beforeHealth)
            return;

        // 이번 피해로 죽었으면 넉백하지 않음 (Die 모션이 Jump로 덮이고 시체가 날아감)
        if (playerKnockback != null && !playerHealth.IsDead)
        {
            playerKnockback.Knockback(attackerPosition);
        }
    }

    private void PlayHitColor()
    {
        StopCoroutine(nameof(HitColorCoroutine));
        StartCoroutine(nameof(HitColorCoroutine));
    }

    private IEnumerator HitColorCoroutine()
    {
        if (spriteRenderer == null)
            yield break;

        spriteRenderer.color = hitColor;

        yield return new WaitForSeconds(hitColorDuration);

        spriteRenderer.color = originalColor;
    }
}
