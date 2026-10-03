using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class FlowertreeAI : MonoBehaviour
{
    [Header("Player detection")]
    [SerializeField] private float detectionRange = 6f;
    [SerializeField] private float attackRange = 1.6f;
    [SerializeField] private float verticalTolerance = 3f;
    [SerializeField] private float maxRoamDistance = 4f;

    [Header("Charge")]
    [SerializeField] private float windupDuration = 0.3f;
    [SerializeField] private float chargeSpeed = 6f;
    [SerializeField] private float maxChargeDuration = 1.2f;

    [Header("Attack")]
    [SerializeField] private int damage = 1;
    [SerializeField] private float attackHitDelay = 0.25f;
    [SerializeField] private float attackAnimationDuration = 1.33f;
    [SerializeField] private float recoveryDuration = 0.6f;

    private Transform player;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Sprite idleSprite;
    private bool isBusy;
    private Vector2 homePosition;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        homePosition = transform.position;
        rb.constraints |= RigidbodyConstraints2D.FreezeRotation;

        if (spriteRenderer != null)
            idleSprite = spriteRenderer.sprite;

        if (animator != null)
            animator.enabled = false;
    }

    private void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
            Collider2D ownCollider = GetComponent<Collider2D>();
            if (ownCollider != null)
            {
                foreach (Collider2D playerCollider in playerObject.GetComponentsInChildren<Collider2D>())
                    Physics2D.IgnoreCollision(ownCollider, playerCollider, true);
            }
        }
        else
            Debug.LogWarning("FlowertreeAI: a GameObject tagged Player was not found.");
    }

    private void Update()
    {
        if (player == null || isBusy)
            return;

        float horizontalDistance = Mathf.Abs(player.position.x - transform.position.x);
        float verticalDistance = Mathf.Abs(player.position.y - transform.position.y);

        if (horizontalDistance <= detectionRange && verticalDistance <= verticalTolerance)
            StartCoroutine(ChargeSequence());
        else
            StopAndIdle();
    }

    private IEnumerator ChargeSequence()
    {
        isBusy = true;
        StopAndIdle();
        FacePlayer();

        // Short warning pause before committing to one direction.
        yield return new WaitForSeconds(windupDuration);

        if (player == null)
        {
            isBusy = false;
            yield break;
        }

        float chargeDirection = player.position.x >= transform.position.x ? 1f : -1f;
        SetFacingDirection(chargeDirection);
        PlayAnimation("flowertree_walk_right");

        float chargeTimer = 0f;
        bool reachedPlayer = false;

        while (chargeTimer < maxChargeDuration)
        {
            if (player == null)
                break;

            float horizontalDistance = Mathf.Abs(player.position.x - transform.position.x);
            float verticalDistance = Mathf.Abs(player.position.y - transform.position.y);

            if (horizontalDistance <= attackRange && verticalDistance <= verticalTolerance)
            {
                reachedPlayer = true;
                break;
            }

            rb.linearVelocity = new Vector2(chargeDirection * chargeSpeed, rb.linearVelocity.y);

            float nextX = rb.position.x + chargeDirection * chargeSpeed * Time.fixedDeltaTime;
            if (Mathf.Abs(nextX - homePosition.x) > maxRoamDistance)
                break;

            chargeTimer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        StopMoving();

        if (reachedPlayer)
            yield return AttackSequence();
        else
            ShowIdle();

        yield return new WaitForSeconds(recoveryDuration);
        isBusy = false;
    }

    private IEnumerator AttackSequence()
    {
        PlayAnimation("flowertree_attack_right");
        yield return new WaitForSeconds(attackHitDelay);

        if (player != null)
        {
            float horizontalDistance = Mathf.Abs(player.position.x - transform.position.x);
            float verticalDistance = Mathf.Abs(player.position.y - transform.position.y);

            if (horizontalDistance <= attackRange + 0.5f && verticalDistance <= verticalTolerance)
            {
                PlayerDamageReceiver receiver = player.GetComponent<PlayerDamageReceiver>();
                if (receiver == null)
                    receiver = player.GetComponentInParent<PlayerDamageReceiver>();

                if (receiver != null)
                    receiver.Hit(damage, transform.position);
            }
        }

        float remainingAnimation = Mathf.Max(0f, attackAnimationDuration - attackHitDelay);
        yield return new WaitForSeconds(remainingAnimation);
        ShowIdle();
    }

    private void FacePlayer()
    {
        if (player != null)
            SetFacingDirection(player.position.x >= transform.position.x ? 1f : -1f);
    }

    private void SetFacingDirection(float direction)
    {
        if (spriteRenderer != null)
            spriteRenderer.flipX = direction < 0f;
    }

    private void PlayAnimation(string stateName)
    {
        if (animator == null)
            return;

        animator.enabled = true;
        animator.Play(stateName, 0, 0f);
        animator.Update(0f);
    }

    private void StopAndIdle()
    {
        StopMoving();
        ShowIdle();
    }

    private void StopMoving()
    {
        if (rb != null)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    private void ShowIdle()
    {
        if (animator != null)
            animator.enabled = false;

        if (spriteRenderer != null && idleSprite != null)
            spriteRenderer.sprite = idleSprite;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        StopMoving();
        isBusy = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.cyan;
        Vector3 center = Application.isPlaying ? (Vector3)homePosition : transform.position;
        Gizmos.DrawLine(center + Vector3.left * maxRoamDistance, center + Vector3.right * maxRoamDistance);
    }
}
