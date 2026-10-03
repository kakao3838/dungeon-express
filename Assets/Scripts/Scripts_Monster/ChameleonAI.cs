using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
public class ChameleonAI : MonoBehaviour
{
    [Header("���� ����")]
    public float detectionRange = 2f;
    public LayerMask playerLayer;

    [Header("���� ����")]
    public int attackDamage = 1;
    public float attackCooldown = 2f;
    public float attackHitDelay = 0.6f; // ���� �ִϸ��̼� �� �������� ���� ����

    [Header("�̵� ����")]
    public float moveSpeed = 1.5f;
    public float patrolDistance = 2f; // ���� ��ġ ���� �¿�� �̵��ϴ� �Ÿ�

    [Header("����")]
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

        // ���� �ִϸ��̼� �� �������� ���� �������� ���
        yield return new WaitForSeconds(attackHitDelay);

        if (target != null)
        {
            PlayerDamageReceiver damageReceiver = target.GetComponent<PlayerDamageReceiver>();
            if (damageReceiver == null)
                damageReceiver = target.GetComponentInParent<PlayerDamageReceiver>();

            if (damageReceiver != null)
            {
                damageReceiver.Hit(attackDamage, transform.position);
            }
            else
            {
                IDamageable damageable = target.GetComponent<IDamageable>();
                if (damageable != null)
                    damageable.TakeDamage(attackDamage);
            }
        }

        // ��ٿ�
        yield return new WaitForSeconds(attackCooldown);

        isAttacking = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        isAttacking = false;
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }
}
