using System.Collections;
using UnityEngine;

public class FlyingEnemy : MonoBehaviour
{
    [Header("player detection")]
    [SerializeField] private float detectionRange = 6f;
    [SerializeField] private float attackRange = 1f;

    [Header("movement")]
    [SerializeField] private float patrolSpeed = 1.5f;
    [SerializeField] private float chaseSpeed = 3f;
    [SerializeField] private float patrolDistance = 3f;
    [SerializeField] private float maxPursuitDistance = 6f;
    [SerializeField] private float hoverHeight = 0.7f;

    [Header("attack")]
    [SerializeField] private int damage = 1;
    [SerializeField] private float attackCooldown = 1.2f;
    [SerializeField] private float hitDelay = 0.3f;
    [SerializeField] private float attackAnimationDuration = 0.6f;

    private Transform player;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private PlayerVineClimb playerVineClimb;
    private bool isAttacking;

    private Vector2 startPosition;
    private int patrolDirection = 1;

    private float attackTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        startPosition = transform.position;
        if (animator != null) animator.Play("pulfly_fly", 0, 0f);
    }

    private void Start()
    {
        GameObject playerObject =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
            playerVineClimb = playerObject.GetComponent<PlayerVineClimb>();
            if (playerVineClimb == null)
                playerVineClimb = playerObject.GetComponentInParent<PlayerVineClimb>();
        }
    }

    private void Update()
    {
        if (attackTimer > 0f)
        {
            attackTimer -= Time.deltaTime;
        }

        if (player == null)
            return;

        if (isAttacking)
            return;

        // Keep the flying enemy at its own flight height while the player climbs.
        // It may still attack if the player comes within the normal attack range.
        if (playerVineClimb != null && playerVineClimb.IsClimbing)
        {
            Patrol();
            if (Vector2.Distance(transform.position, player.position) <= attackRange)
                StartCoroutine(AttackSequence());
            return;
        }

        float distance =
            Vector2.Distance(transform.position, player.position);
        float playerDistanceFromHome =
            Vector2.Distance(startPosition, player.position);

        if (distance <= detectionRange && playerDistanceFromHome <= maxPursuitDistance)
        {
            ChasePlayer(distance);
        }
        else
        {
            Patrol();
        }
    }

    private void Patrol()
    {
        float leftLimit =
            startPosition.x - patrolDistance;

        float rightLimit =
            startPosition.x + patrolDistance;

        if (transform.position.x >= rightLimit)
        {
            patrolDirection = -1;
        }
        else if (transform.position.x <= leftLimit)
        {
            patrolDirection = 1;
        }

        float verticalReturn = Mathf.Clamp(
            (startPosition.y - rb.position.y) * patrolSpeed,
            -patrolSpeed,
            patrolSpeed
        );

        rb.linearVelocity = new Vector2(patrolDirection * patrolSpeed, verticalReturn);

        FaceDirection(patrolDirection);
    }

    private void ChasePlayer(float distance)
    {
        Vector2 hoverTarget = (Vector2)player.position + Vector2.up * hoverHeight;
        Vector2 direction = (hoverTarget - rb.position).normalized;

        rb.linearVelocity =
            direction * chaseSpeed;

        FaceDirection(direction.x);

        if (distance <= attackRange)
        {
            StartCoroutine(AttackSequence());
        }
    }

    private IEnumerator AttackSequence()
    {
        if (attackTimer > 0f)
            yield break;

        isAttacking = true;
        attackTimer = attackCooldown;
        rb.linearVelocity = Vector2.zero;
        if (animator != null) animator.Play("pulfly_attack", 0, 0f);

        yield return new WaitForSeconds(hitDelay);

        PlayerDamageReceiver receiver =
            player.GetComponent<PlayerDamageReceiver>();

        if (receiver == null)
        {
            receiver =
                player.GetComponentInParent<PlayerDamageReceiver>();
        }

        if (receiver != null)
        {
            receiver.Hit(
                damage,
                transform.position
            );
        }

        yield return new WaitForSeconds(Mathf.Max(0f, attackAnimationDuration - hitDelay));
        if (animator != null) animator.Play("pulfly_fly", 0, 0f);
        isAttacking = false;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        isAttacking = false;
    }

    private void FaceDirection(float direction)
    {
        if (spriteRenderer == null)
            return;

        spriteRenderer.flipX = direction < 0f;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(
            transform.position,
            detectionRange
        );

        Gizmos.DrawWireSphere(
            transform.position,
            attackRange
        );

        Gizmos.color = Color.cyan;
        Vector3 home = Application.isPlaying ? (Vector3)startPosition : transform.position;
        Gizmos.DrawWireSphere(home, maxPursuitDistance);
    }
}
