using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
public class ChameleonAI : MonoBehaviour
{
    [Header("Player Detection")]
    public float detectionRange = 2f;
    public LayerMask playerLayer;

    [Header("Attack")]
    public int attackDamage = 1;
    public float attackCooldown = 2f;
    public float attackAnimDuration = 1.35f;

    [Header("Movement")]
    public float moveSpeed = 1.5f;
    public float patrolDistance = 2f;

    [Header("Direction")]
    public bool facingRight = true;

    private Animator animator;
    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private Vector3 spawnPosition;
    private float moveDir;
    private bool isAttacking;

    private static readonly Vector3[] TongueFull =
    {
        new Vector3(1.6f, 0.62f, 0.2f), new Vector3(3.2f, 0.09f, 0.3f), new Vector3(4.8f, -0.72f, 0.4f),
        new Vector3(5.6f, -1.25f, 0.5f), new Vector3(6.4f, -2.3f, 0.9f), new Vector3(7.2f, -2.85f, 0.75f),
    };

    private static readonly Vector3[] TongueHalf =
    {
        new Vector3(3.2f, 0.69f, 0.1f), new Vector3(4.0f, 0.35f, 0.2f), new Vector3(4.8f, -0.18f, 0.35f),
        new Vector3(5.6f, -0.98f, 0.45f),
    };

    private static readonly Vector3[] TongueTip =
    {
        new Vector3(3.2f, 1.2f, 0.3f), new Vector3(4.0f, 0.48f, 0.2f), new Vector3(4.8f, 0.25f, 0.2f),
    };

    private static readonly int[] RightFullFrames = { 6, 10 };
    private static readonly int[] RightHalfFrames = { 8, 11 };
    private static readonly int[] RightTipFrames = { 4, 15 };
    private static readonly int[] LeftFullFrames = { 5, 9 };
    private static readonly int[] LeftHalfFrames = { 8, 11 };
    private static readonly int[] LeftTipFrames = { 7, 12 };

    private void Awake()
    {
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        spawnPosition = transform.position;
        moveDir = facingRight ? 1f : -1f;
        UpdateAnimatorFacing();
    }

    private void Update()
    {
        if (isAttacking)
            return;

        CheckForPlayer();
        if (!isAttacking)
            Patrol();
    }

    private void Patrol()
    {
        float offsetFromSpawn = transform.position.x - spawnPosition.x;
        if (moveDir > 0f && offsetFromSpawn >= patrolDistance)
            moveDir = -1f;
        else if (moveDir < 0f && offsetFromSpawn <= -patrolDistance)
            moveDir = 1f;

        rb.linearVelocity = new Vector2(moveDir * moveSpeed, rb.linearVelocity.y);
        facingRight = moveDir > 0f;
        UpdateAnimatorFacing();

        if (animator != null)
            animator.SetBool("IsMoving", true);
    }

    private void CheckForPlayer()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, detectionRange, playerLayer);
        if (hit == null)
            return;

        FacePlayer(hit.transform);
        StartCoroutine(AttackSequence());
    }

    private void FacePlayer(Transform target)
    {
        facingRight = target.position.x >= transform.position.x;
        UpdateAnimatorFacing();
    }

    private void UpdateAnimatorFacing()
    {
        if (animator != null)
            animator.SetBool("FacingRight", facingRight);
    }

    private IEnumerator AttackSequence()
    {
        isAttacking = true;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (animator != null)
        {
            animator.SetBool("IsMoving", false);
            animator.SetTrigger("Attack");
        }

        bool hitLanded = false;
        float elapsed = 0f;
        while (elapsed < attackAnimDuration)
        {
            if (!hitLanded)
                hitLanded = TryHitWithTongue();

            elapsed += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(attackCooldown);
        isAttacking = false;
    }

    private bool TryHitWithTongue()
    {
        if (sr == null || sr.sprite == null)
            return false;

        string spriteName = sr.sprite.name;
        if (!spriteName.Contains("attack"))
            return false;

        bool left = spriteName.Contains("_left_");
        int underscore = spriteName.LastIndexOf('_');
        if (underscore < 0 || !int.TryParse(spriteName.Substring(underscore + 1), out int frame))
            return false;

        Vector3[] shape;
        if (System.Array.IndexOf(left ? LeftFullFrames : RightFullFrames, frame) >= 0)
            shape = TongueFull;
        else if (System.Array.IndexOf(left ? LeftHalfFrames : RightHalfFrames, frame) >= 0)
            shape = TongueHalf;
        else if (System.Array.IndexOf(left ? LeftTipFrames : RightTipFrames, frame) >= 0)
            shape = TongueTip;
        else
            return false;

        float scale = Mathf.Abs(transform.lossyScale.x);
        float side = left ? -1f : 1f;

        for (int i = 0; i < shape.Length; i++)
        {
            Vector3 a = shape[i];
            Vector3 b = i + 1 < shape.Length ? shape[i + 1] : shape[i];
            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b) / 0.3f));

            for (int s = 0; s <= steps; s++)
            {
                Vector3 point = Vector3.Lerp(a, b, s / (float)steps);
                Vector2 world = (Vector2)transform.position + new Vector2(point.x * side, point.y) * scale;
                Collider2D hit = Physics2D.OverlapCircle(world, point.z * scale, playerLayer);
                if (hit == null)
                    continue;

                PlayerDamageReceiver receiver = hit.GetComponentInParent<PlayerDamageReceiver>();
                if (receiver != null)
                {
                    receiver.Hit(attackDamage, transform.position);
                    return true;
                }

                IDamageable damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable == null)
                    continue;

                damageable.TakeDamage(attackDamage);
                return true;
            }
        }

        return false;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        isAttacking = false;
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
