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

    private int currentHealth;
    private SpriteRenderer sr;
    private Color originalColor;

    void Awake()
    {
        currentHealth = maxHealth;
        sr = GetComponent<SpriteRenderer>();
        originalColor = sr.color;
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        Debug.Log(gameObject.name + " 피격! 남은 체력: " + currentHealth);

        // 맞았을 때 색깔이 잠깐 번쩍이도록
        StopAllCoroutines();
        StartCoroutine(FlashHit());

        if (currentHealth <= 0)
        {
            Debug.Log(gameObject.name + " 사망!");
            Destroy(gameObject);
        }
    }

    private IEnumerator FlashHit()
    {
        sr.color = hitFlashColor;
        yield return new WaitForSeconds(hitFlashDuration);
        sr.color = originalColor;
    }
}