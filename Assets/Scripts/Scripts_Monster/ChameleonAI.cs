using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
public class ChameleonAI : MonoBehaviour
{
    [Header("감지 설정")]
    public float detectionRange = 2f;
    public LayerMask playerLayer;

    [Header("공격 설정")]
    public int attackDamage = 1;
    public float attackCooldown = 2f;
    public float attackHitDelay = 0.6f; // 공격 애니메이션 중 데미지가 들어가는 시점

    [Header("이동 설정")]
    public float moveSpeed = 1.5f;
    public float patrolDistance = 2f; // 시작 위치 기준 좌우로 이동하는 거리

    [Header("방향")]
    public bool facingRight = true;

    private Animator animator;
    private Rigidbody2D rb;
    private Vector3 spawnPosition;
    private float moveDir;
    private bool isAttacking = false;

    void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        spawnPosition = transform.position;
        moveDir = facingRight ? 1f : -1f;

        UpdateAnimatorFacing();
    }

    void Update()
    {
        if (!isAttacking)
        {
            CheckForPlayer();
        }

        if (!isAttacking)
        {
            Patrol();
        }
    }

    void Patrol()
    {
        float offsetFromSpawn = transform.position.x - spawnPosition.x;
        if (moveDir > 0f && offsetFromSpawn >= patrolDistance) moveDir = -1f;
        else if (moveDir < 0f && offsetFromSpawn <= -patrolDistance) moveDir = 1f;

        rb.linearVelocity = new Vector2(moveDir * moveSpeed, rb.linearVelocity.y);

        facingRight = moveDir > 0f;
        UpdateAnimatorFacing();
        if (animator != null) animator.SetBool("IsMoving", true);
    }

    void CheckForPlayer()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, detectionRange, playerLayer);
        if (hit != null)
        {
            FacePlayer(hit.transform);
            StartCoroutine(AttackSequence(hit));
        }
    }

    void FacePlayer(Transform target)
    {
        facingRight = target.position.x >= transform.position.x;
        UpdateAnimatorFacing();
    }

    void UpdateAnimatorFacing()
    {
        if (animator != null) animator.SetBool("FacingRight", facingRight);
    }

    IEnumerator AttackSequence(Collider2D target)
    {
        isAttacking = true;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        if (animator != null)
        {
            animator.SetBool("IsMoving", false);
            animator.SetTrigger("Attack");
        }

        // 공격 애니메이션 중 데미지가 들어가는 시점까지 대기
        yield return new WaitForSeconds(attackHitDelay);

        if (target != null)
        {
            IDamageable damageable = target.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(attackDamage);
            }
        }

        // 쿨다운
        yield return new WaitForSeconds(attackCooldown);

        isAttacking = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
