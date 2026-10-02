using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("체력 설정 (하트 개수 단위)")]
    public int maxHearts = 3;
    public float invincibilityDuration = 1f;

    [Header("부활")]
    [Tooltip("죽은 뒤 이 시간이 지나면 첫 스폰 위치에서 부활 (Die 애니메이션 길이가 약 1.8초)")]
    public float respawnDelay = 2.2f;
    public float respawnInvincibilityDuration = 1.5f;

    [Header("낙사")]
    [Tooltip("씬에서 가장 낮은 바닥(콜라이더)보다 이만큼 더 아래로 떨어지면 죽음")]
    public float fallDeathMargin = 12f;

    public int CurrentHearts { get; private set; }

    // UI(하트 아이콘)가 이 이벤트를 구독해서 currentHearts, maxHearts를 받아 갱신하면 됨
    public event Action<int, int> OnHealthChanged;
    public event Action OnDeath;

    private bool isInvincible;
    private float invincibilityTimer;
    private Animator animator;
    private bool isDead;
    private Vector3 respawnPosition;
    private float fallDeathY = float.NegativeInfinity;
    private float originalGravityScale;

    void Awake()
    {
        CurrentHearts = maxHearts;
        animator = GetComponent<Animator>();
        respawnPosition = transform.position;
        var body = GetComponent<Rigidbody2D>();
        if (body != null) originalGravityScale = body.gravityScale;
        CalculateFallDeathY();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // 씬이 바뀔 때마다 그 씬의 첫 스폰 위치(SpawnPoint)를 부활 위치로 기억
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        GameObject spawnPoint = GameObject.Find("SpawnPoint");
        respawnPosition = spawnPoint != null ? spawnPoint.transform.position : transform.position;

        // 씬이 막 로드된 시점에는 콜라이더 범위가 아직 비어 있을 수 있어서 한 프레임 뒤에 다시 계산
        fallDeathY = float.NegativeInfinity;
        CalculateFallDeathY();
        StartCoroutine(RecalculateFallDeathYNextFrame());
    }

    IEnumerator RecalculateFallDeathYNextFrame()
    {
        yield return null;
        CalculateFallDeathY();
    }

    // 씬에서 가장 낮은 바닥을 기준으로 낙사 높이를 정함
    // 타일맵은 콜라이더가 만들어지기 전에도 알 수 있는 타일 범위(localBounds)를 사용
    void CalculateFallDeathY()
    {
        float lowest = float.PositiveInfinity;

        foreach (var tilemap in FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
        {
            if (tilemap.GetComponent<TilemapCollider2D>() == null) continue;

            tilemap.CompressBounds();
            Bounds local = tilemap.localBounds;
            if (local.size == Vector3.zero) continue;

            lowest = Mathf.Min(lowest, tilemap.transform.TransformPoint(local.min).y);
        }

        foreach (var c in FindObjectsByType<Collider2D>(FindObjectsSortMode.None))
        {
            if (c is TilemapCollider2D || c is CompositeCollider2D) continue;
            if (c.isTrigger || c.gameObject == gameObject) continue;
            if (c.attachedRigidbody != null && c.attachedRigidbody.bodyType != RigidbodyType2D.Static) continue;
            lowest = Mathf.Min(lowest, c.bounds.min.y);
        }

        fallDeathY = float.IsInfinity(lowest) ? float.NegativeInfinity : lowest - fallDeathMargin;
    }

    void Update()
    {
        if (!isDead && transform.position.y < fallDeathY)
        {
            FallDeath();
            return;
        }

        if (!isInvincible) return;

        invincibilityTimer -= Time.deltaTime;
        if (invincibilityTimer <= 0f)
        {
            isInvincible = false;
        }
    }

    public void TakeDamage(int amount)
    {
        if (isDead || isInvincible || CurrentHearts <= 0) return;

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
        if (isDead || CurrentHearts <= 0) return;

        CurrentHearts = Mathf.Min(maxHearts, CurrentHearts + amount);
        OnHealthChanged?.Invoke(CurrentHearts, maxHearts);
    }

    // 맵 밖으로 떨어지면 무적/남은 체력과 상관없이 바로 사망
    void FallDeath()
    {
        CurrentHearts = 0;
        OnHealthChanged?.Invoke(CurrentHearts, maxHearts);
        Die();

        // 계속 추락하지 않도록 그 자리에 멈춰 둠 (부활할 때 중력 복구)
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.gravityScale = 0f;
    }

    void Die()
    {
        isDead = true;

        if (animator != null)
        {
            // 공중에서 죽으면 IsGrounded=false 때문에 Any State -> Jump 전환이 Die를 덮어쓰므로 미리 정리
            animator.ResetTrigger("Attack");
            animator.SetBool("IsMoving", false);
            animator.SetBool("IsGrounded", true);
            animator.SetTrigger("Die");
        }

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        var controller = GetComponent<PlayerController>();
        if (controller != null) controller.enabled = false;

        var attack = GetComponent<PlayerAttack>();
        if (attack != null) attack.enabled = false;

        OnDeath?.Invoke();
        StartCoroutine(RespawnRoutine());
    }

    IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);

        transform.position = respawnPosition;

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        var controller = GetComponent<PlayerController>();
        var attack = GetComponent<PlayerAttack>();
        if (rb != null) rb.gravityScale = originalGravityScale;
        if (controller != null) controller.enabled = true;
        if (attack != null) attack.enabled = true;

        CurrentHearts = maxHearts;
        isDead = false;
        isInvincible = true;
        invincibilityTimer = respawnInvincibilityDuration;

        if (animator != null)
        {
            // Die 상태에서는 나가는 전환이 없어서 직접 Idle로 되돌림
            bool facingRight = controller == null || controller.IsFacingRight;
            animator.ResetTrigger("Die");
            animator.Play(facingRight ? "Idle_R" : "Idle_L", 0, 0f);
        }

        OnHealthChanged?.Invoke(CurrentHearts, maxHearts);
    }
}
