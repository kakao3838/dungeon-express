using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("이동 설정")]
    public float moveSpeed = 6f;
    public float jumpForce = 12f;

    [Header("바닥 체크")]
    [Tooltip("발밑에 빈 오브젝트를 자식으로 만들어서 여기에 연결하세요.")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.15f;
    public LayerMask groundLayer;

    private Rigidbody2D rb;
    private float moveInput;
    private bool isGrounded;
    private bool facingRight = true;

    public bool IsFacingRight => facingRight;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // 좌우 이동 입력 (WASD + 화살표 둘 다 지원)
        moveInput = 0f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) moveInput = -1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) moveInput = 1f;

        // 이동 방향에 따라 스프라이트 좌우 반전
        if (moveInput > 0f && !facingRight) Flip();
        else if (moveInput < 0f && facingRight) Flip();

        // 바닥에 닿아있는지 체크
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }

        // 디버그용 로그: 스페이스를 누른 순간 isGrounded 값을 콘솔에 출력
        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            Debug.Log("스페이스 눌림! isGrounded = " + isGrounded);
        }

        // 점프 (바닥에 있을 때만)
        if (keyboard.spaceKey.wasPressedThisFrame && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }

    void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
    }

    void Flip()
    {
        facingRight = !facingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1f;
        transform.localScale = scale;
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}