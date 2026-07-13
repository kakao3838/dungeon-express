using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("체력 설정 (하트 개수 단위)")]
    public int maxHearts = 3;
    public float invincibilityDuration = 1f;

    public int CurrentHearts { get; private set; }

    // UI(하트 아이콘)가 이 이벤트를 구독해서 currentHearts, maxHearts를 받아 갱신하면 됨
    public event Action<int, int> OnHealthChanged;
    public event Action OnDeath;

    private bool isInvincible;
    private float invincibilityTimer;

    void Awake()
    {
        CurrentHearts = maxHearts;
    }

    void Update()
    {
        if (!isInvincible) return;

        invincibilityTimer -= Time.deltaTime;
        if (invincibilityTimer <= 0f)
        {
            isInvincible = false;
        }
    }

    public void TakeDamage(int amount)
    {
        if (isInvincible || CurrentHearts <= 0) return;

        CurrentHearts = Mathf.Max(0, CurrentHearts - amount);
        OnHealthChanged?.Invoke(CurrentHearts, maxHearts);

        if (CurrentHearts <= 0)
        {
            Die();
        }
        else
        {
            isInvincible = true;
            invincibilityTimer = invincibilityDuration;
        }
    }

    public void Heal(int amount)
    {
        if (CurrentHearts <= 0) return;

        CurrentHearts = Mathf.Min(maxHearts, CurrentHearts + amount);
        OnHealthChanged?.Invoke(CurrentHearts, maxHearts);
    }

    void Die()
    {
        OnDeath?.Invoke();
        // TODO: 사망 처리 (리스폰, 가방 드롭 로직 등은 추후 구현)
    }
}
