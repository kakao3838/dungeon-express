using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Monster : MonoBehaviour, IDamageable
{
    [Header("���� ü��")]
    public int maxHealth = 3;

    [Header("�ǰ� ����")]
    public Color hitFlashColor = new Color(1f, 0.25f, 0.25f, 1f);
    public float hitFlashDuration = 0.1f;
    public float deathAnimDuration = 1f; // Die �ִϸ��̼��� ���� ������ ��ٷȴٰ� �ı�
    [Header("Knockback")]
    public float knockbackDistance = 0.8f;
    public float knockbackDuration = 0.18f;

    private int currentHealth;
    private SpriteRenderer sr;
    private Color originalColor;
    private Animator animator;
    private Collider2D col;
    private Rigidbody2D rb;
    private bool isDead = false;
    private Coroutine hitFlashCoroutine;
    private Coroutine knockbackCoroutine;

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
        TakeDamage(amount, null);
    }

    public void TakeDamage(int amount, Vector2? attackerPosition)
    {
        if (isDead) return;

        currentHealth -= amount;
        Debug.Log($"{gameObject.name} took {amount} damage. HP: {currentHealth}/{maxHealth}");

        // �¾��� �� ������ ��� ��½�̵���
        if (hitFlashCoroutine != null)
            StopCoroutine(hitFlashCoroutine);
        hitFlashCoroutine = StartCoroutine(FlashHit());

        if (currentHealth > 0 && attackerPosition.HasValue &&
            (GetComponent<ChameleonAI>() != null || GetComponent<FlowertreeAI>() != null))
        {
            if (knockbackCoroutine != null)
                StopCoroutine(knockbackCoroutine);
            knockbackCoroutine = StartCoroutine(ApplyKnockback(attackerPosition.Value));
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator ApplyKnockback(Vector2 attackerPosition)
    {
        ChameleonAI chameleonAI = GetComponent<ChameleonAI>();
        FlowertreeAI flowertreeAI = GetComponent<FlowertreeAI>();
        if (chameleonAI != null) chameleonAI.enabled = false;
        if (flowertreeAI != null) flowertreeAI.enabled = false;

        if (rb != null) rb.linearVelocity = Vector2.zero;

        Vector2 start = transform.position;
        float direction = transform.position.x >= attackerPosition.x ? 1f : -1f;
        Vector2 target = start + Vector2.right * direction * knockbackDistance;
        float elapsed = 0f;

        while (elapsed < knockbackDuration && !isDead)
        {
            elapsed += Time.deltaTime;
            Vector2 next = Vector2.Lerp(start, target, Mathf.Clamp01(elapsed / knockbackDuration));
            if (rb != null) rb.position = next;
            else transform.position = next;
            yield return null;
        }

        if (!isDead)
        {
            if (chameleonAI != null) chameleonAI.enabled = true;
            if (flowertreeAI != null) flowertreeAI.enabled = true;
        }

        knockbackCoroutine = null;
    }

    private IEnumerator FlashHit()
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        Color[] originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalColors[i] = renderers[i].color;
            if (renderers[i].gameObject.name != "Alert")
                renderers[i].color = hitFlashColor;
        }
        yield return new WaitForSeconds(hitFlashDuration);
        if (!isDead)
        {
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null) renderers[i].color = originalColors[i];
        }
        hitFlashCoroutine = null;
    }

    private void Die()
    {
        isDead = true;
        Debug.Log($"{gameObject.name} defeated.");

        var ai = GetComponent<ChameleonAI>();
        if (ai != null) ai.enabled = false;
        var flowertreeAI = GetComponent<FlowertreeAI>();
        if (flowertreeAI != null) flowertreeAI.enabled = false;
        var monkeyAI = GetComponent<MonkeyEnemy>();
        if (monkeyAI != null)
        {
            monkeyAI.PrepareForDeath();
            monkeyAI.enabled = false;
        }
        var flyingAI = GetComponent<FlyingEnemy>();
        if (flyingAI != null) flyingAI.enabled = false;
        if (col != null) col.enabled = false;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        if (animator != null)
        {
            if (flowertreeAI != null)
            {
                animator.enabled = true;
                animator.Play("flowertree_die_right", 0, 0f);
                animator.Update(0f);
            }
            else if (monkeyAI != null)
            {
                animator.enabled = true;
                animator.Play("monkey_die_left", 0, 0f);
                animator.Update(0f);
            }
            else if (flyingAI != null)
            {
                animator.enabled = true;
                animator.Play("pulfly_die", 0, 0f);
                animator.Update(0f);
            }
            else
            {
                animator.SetTrigger("Die");
            }
        }

        StartCoroutine(DestroyAfterDeathAnim());
    }

    private IEnumerator DestroyAfterDeathAnim()
    {
        yield return new WaitForSeconds(deathAnimDuration);
        Destroy(gameObject);
    }
}
