using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Monster : MonoBehaviour, IDamageable
{
    [Header("몬스터 체력")]
    public int maxHealth = 3;

    [Header("피격 연출")]
    public Color hitFlashColor = Color.white;
    public float hitFlashDuration = 0.1f;
    public float deathAnimDuration = 1f; // Die 애니메이션이 끝날 때까지 기다렸다가 파괴

    private int currentHealth;
    private SpriteRenderer sr;
    private Color originalColor;
    private Animator animator;
    private Collider2D col;
    private Rigidbody2D rb;
    private bool isDead = false;

    void Awake()
    {
        currentHealth = maxHealth;
        sr = GetComponent<SpriteRenderer>();
        originalColor = sr.color;
        animator = GetComponent<Animator>();
        col = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        Debug.Log(gameObject.name + " 피격! 남은 체력: " + currentHealth);

        // 맞았을 때 색깔이 잠깐 번쩍이도록
        StopCoroutine(nameof(FlashHit));
        StartCoroutine(FlashHit());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator FlashHit()
    {
        sr.color = hitFlashColor;
        yield return new WaitForSeconds(hitFlashDuration);
        if (!isDead) sr.color = originalColor;
    }

    private void Die()
    {
        isDead = true;
        Debug.Log(gameObject.name + " 사망!");

        var ai = GetComponent<ChameleonAI>();
        if (ai != null) ai.enabled = false;
        if (col != null) col.enabled = false;
        if (rb != null) rb.linearVelocity = Vector2.zero;

        if (animator != null) animator.SetTrigger("Die");

        StartCoroutine(DestroyAfterDeathAnim());
    }

    private IEnumerator DestroyAfterDeathAnim()
    {
        yield return new WaitForSeconds(deathAnimDuration);
        Destroy(gameObject);
    }
}
