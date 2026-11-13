using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class PlayerVineClimb : MonoBehaviour
{
    [Header("Climb Settings")]
    [SerializeField] private float climbSpeed = 3f;
    [SerializeField] private float centerSnapSpeed = 12f;
    [SerializeField] private float exitJumpForce = 8f;
    [SerializeField] private float regrabDelay = 0.3f;
    [SerializeField] private float topExitReach = 0.8f;

    private readonly HashSet<Collider2D> nearbyVines = new HashSet<Collider2D>();
    private Collider2D bodyCollider;
    private float regrabAfter;

    private Rigidbody2D rb;
    private PlayerController playerController;
    private Animator animator;
    private ClimbableVine currentVine;
    private float normalGravityScale;
    private float verticalInput;
    private bool isClimbing;
    private bool hasClimbingParameter;

    private float GripY => bodyCollider.bounds.min.y + bodyCollider.bounds.size.y * 0.9f;

    public bool IsClimbing => isClimbing;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        foreach (Collider2D candidate in GetComponents<Collider2D>())
            if (candidate.enabled && !candidate.isTrigger) { bodyCollider = candidate; break; }
        playerController = GetComponent<PlayerController>();
        animator = GetComponent<Animator>();
        normalGravityScale = rb.gravityScale;
        hasClimbingParameter = HasAnimatorParameter("IsClimbing");
    }

    private void Update()
    {
        if (SaveSlotWindow.BlocksGameplayInput) { verticalInput = 0f; return; }
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            verticalInput = 0f;
            return;
        }

        verticalInput = 0f;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            verticalInput = 1f;
        else if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            verticalInput = -1f;

        if (!isClimbing) RefreshVine();
        if (!isClimbing && Time.time >= regrabAfter && playerController != null &&
            playerController.enabled && !playerController.InputLocked &&
            !(NPCDialogueMenu.Instance != null && NPCDialogueMenu.Instance.IsOpen) &&
            currentVine != null &&
            Mathf.Abs(verticalInput) > 0.01f)
        {
            StartClimbing();
        }

        if (isClimbing && keyboard.spaceKey.wasPressedThisFrame)
        {
            StopClimbing(true);
            regrabAfter = Time.time + regrabDelay;
        }

        if (isClimbing && animator != null)
            animator.speed = Mathf.Abs(verticalInput) > 0.01f ? 1f : 0f;
    }

    private void FixedUpdate()
    {
        if (!isClimbing)
            return;

        if (currentVine == null) { StopClimbing(false); return; }

        if (verticalInput > 0f && TryExitOntoPlatform()) return;

        // Continue onto a neighbouring segment only if it also reaches the hands.
        RefreshVine();
        if (currentVine == null) { StopClimbing(false); return; }

        // 발 피벗 대신 몸통의 겹침으로 판단하고, 연결된 다른 덩굴도 확인합니다.
        if (bodyCollider == null || !bodyCollider.bounds.Intersects(currentVine.Bounds))
        {
            RefreshVine();
            if (currentVine != null) return;
            StopClimbing(false);
            return;
        }

        float horizontalCorrection = Mathf.Clamp(
            (currentVine.CenterX - rb.position.x) * centerSnapSpeed,
            -centerSnapSpeed,
            centerSnapSpeed
        );

        float verticalSpeed = verticalInput * climbSpeed;
        if (verticalSpeed > 0f)
            verticalSpeed = Mathf.Min(verticalSpeed,
                Mathf.Max(0f, currentVine.Bounds.max.y - GripY) / Time.fixedDeltaTime);

        rb.linearVelocity = new Vector2(
            horizontalCorrection,
            verticalSpeed
        );
        if (animator != null) animator.speed = Mathf.Abs(verticalSpeed) > 0.01f ? 1f : 0f;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        ClimbableVine vine = other.GetComponent<ClimbableVine>();
        if (vine == null)
            vine = other.GetComponentInParent<ClimbableVine>();

        if (vine != null) nearbyVines.Add(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.GetComponentInParent<ClimbableVine>() != null) nearbyVines.Add(other);
    }

    private void RefreshVine()
    {
        currentVine = null;
        float nearest = float.PositiveInfinity;
        foreach (Collider2D candidate in nearbyVines)
        {
            if (candidate == null || !candidate.enabled || !candidate.gameObject.activeInHierarchy ||
                bodyCollider == null || !candidate.bounds.Intersects(bodyCollider.bounds)) continue;
            ClimbableVine vine = candidate.GetComponentInParent<ClimbableVine>();
            if (vine == null || !vine.isActiveAndEnabled) continue;
            if (GripY > vine.Bounds.max.y + 0.03f || GripY < vine.Bounds.min.y) continue;
            float distance = Mathf.Abs(candidate.bounds.center.x - rb.position.x);
            if (distance < nearest || (Mathf.Abs(distance - nearest) < 0.01f &&
                currentVine != null && verticalInput > 0f && vine.Bounds.max.y > currentVine.Bounds.max.y))
            { nearest = distance; currentVine = vine; }
        }
    }

    // Only step onto a nearby, upward-facing surface with enough room for the body.
    private bool TryExitOntoPlatform()
    {
        if (bodyCollider == null || playerController == null ||
            bodyCollider.bounds.max.y < currentVine.Bounds.max.y - topExitReach) return false;
        Bounds body = bodyCollider.bounds;
        float direction = playerController.IsFacingRight ? 1f : -1f;
        // Search outward from the vine, preferring the smallest sideways adjustment.
        float centerX = currentVine.CenterX;
        float reach = body.extents.x + topExitReach * 0.5f;
        for (int sample = 0; sample <= 12; sample++)
        {
            float offset = sample == 0 ? 0f : ((sample + 1) / 2) * reach / 6f;
            float x = centerX + offset * direction * (sample % 2 == 1 ? 1f : -1f);
            Vector2 origin = new Vector2(x, currentVine.Bounds.max.y + topExitReach);
            foreach (RaycastHit2D hit in Physics2D.RaycastAll(origin, Vector2.down,
                topExitReach * 2f, playerController.groundLayer))
            {
                if (hit.collider.isTrigger || hit.collider.attachedRigidbody == rb || hit.normal.y < 0.7f ||
                    hit.point.y < body.min.y - 0.1f || hit.point.y > body.max.y + topExitReach) continue;
                Vector2 targetCenter = new Vector2(x, hit.point.y + body.extents.y + 0.05f);
                bool blocked = false;
                foreach (Collider2D obstacle in Physics2D.OverlapBoxAll(targetCenter,
                    (Vector2)body.size * 0.98f, 0f))
                {
                    if (!obstacle.isTrigger && obstacle.attachedRigidbody != rb &&
                        !Physics2D.GetIgnoreLayerCollision(gameObject.layer, obstacle.gameObject.layer) &&
                        !Physics2D.GetIgnoreCollision(bodyCollider, obstacle)) { blocked = true; break; }
                }
                if (blocked) continue;
                // Preserve the body's offset from its root and place its feet on the surface.
                rb.position += targetCenter - (Vector2)body.center;
                StopClimbing(false);
                playerController.StopForMenu();
                rb.linearVelocity = Vector2.zero;
                regrabAfter = Time.time + regrabDelay;
                return true;
            }
        }
        return false;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        nearbyVines.Remove(other);
        ClimbableVine vine = other.GetComponent<ClimbableVine>();
        if (vine == null)
            vine = other.GetComponentInParent<ClimbableVine>();

        if (vine == null || vine != currentVine)
            return;

        RefreshVine();
        if (isClimbing && currentVine == null)
            StopClimbing(false);
    }

    private void StartClimbing()
    {
        isClimbing = true;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;

        if (playerController != null)
            playerController.enabled = false;

        SetClimbingAnimation(true);
    }

    private void StopClimbing(bool jumpAway)
    {
        isClimbing = false;
        rb.gravityScale = normalGravityScale;

        if (animator != null)
            animator.speed = 1f;

        if (playerController != null)
            playerController.enabled = true;

        SetClimbingAnimation(false);

        if (jumpAway)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, exitJumpForce);
    }

    private void OnDisable()
    {
        if (isClimbing)
            StopClimbing(false);
    }

    public void CancelClimbForKnockback()
    {
        if (isClimbing)
            StopClimbing(false);

        // 넉백 직후 재잡기를 잠시 막습니다. 조작기가 다시 켜지기 전에는 잡지 않습니다.
        regrabAfter = Time.time + regrabDelay;
    }

    private void SetClimbingAnimation(bool value)
    {
        if (animator != null && hasClimbingParameter)
            animator.SetBool("IsClimbing", value);
    }

    private bool HasAnimatorParameter(string parameterName)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return false;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == parameterName)
                return true;
        }

        return false;
    }
}
