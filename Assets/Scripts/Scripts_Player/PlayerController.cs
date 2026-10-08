using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("이동 설정")]
    [Tooltip("걷기 속도")]
    public float moveSpeed = 3.5f;
    [Tooltip("Shift를 누르고 있을 때의 달리기 속도")]
    public float runSpeed = 6f;
    public float jumpForce = 12f;
    [Tooltip("떨어질 때 중력 배율. 클수록 체공 시간이 짧아집니다")]
    public float fallGravityMultiplier = 2f;
    [Tooltip("점프 키를 일찍 떼면 올라가는 속도를 이 배율만큼 더 빨리 줄입니다 (짧은 점프)")]
    public float lowJumpGravityMultiplier = 2.5f;

    [Header("바닥 체크")]
    [Tooltip("발밑에 빈 오브젝트를 자식으로 만들어서 여기에 연결하세요.")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.15f;
    public LayerMask groundLayer;

    private Rigidbody2D rb;
    private Animator animator;
    private float moveInput;
    private bool isGrounded;
    private bool isRunning;
    private bool jumpHeld;
    private bool facingRight = true;

    public bool IsFacingRight => facingRight;
    public bool IsGrounded => isGrounded;
    public bool IsMoving => Mathf.Abs(moveInput) > 0.01f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // 좌우 이동 입력 (WASD + 화살표 둘 다 지원)
        moveInput = 0f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) moveInput = -1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) moveInput = 1f;
        isRunning = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
        jumpHeld = keyboard.spaceKey.isPressed;

        // 이동 방향에 따라 바라보는 방향 갱신
        if (moveInput > 0f && !facingRight) Flip();
        else if (moveInput < 0f && facingRight) Flip();

        if (animator != null) animator.SetBool("IsMoving", moveInput != 0f);

        // 바닥에 닿아있는지 체크
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }

        if (animator != null) animator.SetBool("IsGrounded", isGrounded);

        // 디버그용 로그: 스페이스를 누른 순간 isGrounded 값을 콘솔에 출력
        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            Debug.Log("Jump input. Grounded: " + isGrounded);
        }

        // 점프 (바닥에 있을 때만)
        if (keyboard.spaceKey.wasPressedThisFrame && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }

    void FixedUpdate()
    {
        float speed = isRunning ? runSpeed : moveSpeed;
        Vector2 velocity = new Vector2(moveInput * speed, rb.linearVelocity.y);

        // 덩굴을 탈 때(중력 0)는 건드리지 않고, 일반 중력일 때만 점프를 더 짧고 즉각적으로 만듦
        if (rb.gravityScale > 0f)
        {
            if (velocity.y < 0f)
                velocity.y += Physics2D.gravity.y * rb.gravityScale * (fallGravityMultiplier - 1f) * Time.fixedDeltaTime;
            else if (velocity.y > 0f && !jumpHeld)
                velocity.y += Physics2D.gravity.y * rb.gravityScale * (lowJumpGravityMultiplier - 1f) * Time.fixedDeltaTime;
        }

        rb.linearVelocity = velocity;
    }

    void Flip()
    {
        facingRight = !facingRight;
        if (animator != null) animator.SetBool("FacingRight", facingRight);
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
