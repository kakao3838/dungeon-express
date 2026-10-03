using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class PlayerVineClimb : MonoBehaviour
{
    [Header("Climb Settings")]
    [SerializeField] private float climbSpeed = 3f;
    [SerializeField] private float centerSnapSpeed = 12f;
    [SerializeField] private float exitJumpForce = 8f;

    private Rigidbody2D rb;
    private PlayerController playerController;
    private Animator animator;
    private ClimbableVine currentVine;
    private float normalGravityScale;
    private float verticalInput;
    private bool isClimbing;
    private bool hasClimbingParameter;

    public bool IsClimbing => isClimbing;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerController = GetComponent<PlayerController>();
        animator = GetComponent<Animator>();
        normalGravityScale = rb.gravityScale;
        hasClimbingParameter = HasAnimatorParameter("IsClimbing");
    }

    private void Update()
    {
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

        if (!isClimbing && currentVine != null && currentVine.ContainsPoint(rb.position) &&
            Mathf.Abs(verticalInput) > 0.01f)
        {
            StartClimbing();
        }

        if (isClimbing && keyboard.spaceKey.wasPressedThisFrame)
        {
            StopClimbing(true);
        }

        if (isClimbing && animator != null)
            animator.speed = Mathf.Abs(verticalInput) > 0.01f ? 1f : 0f;
    }

    private void FixedUpdate()
    {
        if (!isClimbing || currentVine == null)
            return;

        // 플레이어의 발 기준점이 실제 덩굴 범위를 벗어나면 매달림을 즉시 해제합니다.
        if (!currentVine.ContainsPoint(rb.position))
        {
            StopClimbing(false);
            currentVine = null;
            return;
        }

        float horizontalCorrection = Mathf.Clamp(
            (currentVine.CenterX - rb.position.x) * centerSnapSpeed,
            -centerSnapSpeed,
            centerSnapSpeed
        );

        rb.linearVelocity = new Vector2(
            horizontalCorrection,
            verticalInput * climbSpeed
        );
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        ClimbableVine vine = other.GetComponent<ClimbableVine>();
        if (vine == null)
            vine = other.GetComponentInParent<ClimbableVine>();

        if (vine != null)
            currentVine = vine;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        ClimbableVine vine = other.GetComponent<ClimbableVine>();
        if (vine == null)
            vine = other.GetComponentInParent<ClimbableVine>();

        if (vine == null || vine != currentVine)
            return;

        currentVine = null;
        if (isClimbing)
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

        // 방향키를 계속 누르고 있어도 같은 프레임에 덩굴을 다시 잡아
        // 넉백 속도를 덮어쓰지 않도록, 덩굴에서 완전히 떨어질 때까지 재진입을 막습니다.
        currentVine = null;
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
