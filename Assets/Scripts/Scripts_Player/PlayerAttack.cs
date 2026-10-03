using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    [Header("공격 설정")]
    public float attackRange = 0.6f;
    public float hitboxRadius = 0.35f;
    public int attackDamage = 1;
    public LayerMask enemyLayer;
    [Tooltip("끄면 공중에서는 공격 입력을 무시합니다.")]
    public bool allowAirAttack = true;
    [SerializeField] private float attackAnimationDuration = 1.17f;
    [SerializeField] private AnimationClip attackLeftClip;
    [SerializeField] private AnimationClip attackRightClip;

    private PlayerController controller;
    private PlayerVineClimb vineClimb;
    private Animator animator;
    private Vector2 attackDirection = Vector2.right;
    private bool isAttacking;

    void Awake()
    {
        controller = GetComponent<PlayerController>();
        vineClimb = GetComponent<PlayerVineClimb>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        if (keyboard == null) return;

        // 공격 방향 결정: 위/아래를 누르고 있으면 그쪽 우선, 아니면 캐릭터가 보는 좌우 방향
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            attackDirection = Vector2.up;
        else if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            attackDirection = Vector2.down;
        else
            attackDirection = (controller != null && controller.IsFacingRight) ? Vector2.right : Vector2.left;

        bool attackPressed = keyboard.jKey.wasPressedThisFrame
            || (mouse != null && mouse.leftButton.wasPressedThisFrame);

        if (attackPressed)
        {
            if (vineClimb != null && vineClimb.IsClimbing)
                return;

            if (!allowAirAttack && controller != null && !controller.IsGrounded)
                return;

            if (isAttacking)
                return;

            if (animator != null)
                StartCoroutine(PlayAttackAnimation());
            Attack();
        }
    }

    private IEnumerator PlayAttackAnimation()
    {
        isAttacking = true;
        bool facingRight = controller != null && controller.IsFacingRight;
        AnimationClip attackClip = facingRight ? attackRightClip : attackLeftClip;
        float elapsed = 0f;
        animator.enabled = false;

        // 이동/점프 전환이 공격을 덮어쓰지 못하도록 클립을 직접 샘플링합니다.
        while (elapsed < attackAnimationDuration)
        {
            if (attackClip != null)
            {
                float clipTime = attackAnimationDuration > 0f
                    ? elapsed / attackAnimationDuration * attackClip.length
                    : attackClip.length;
                attackClip.SampleAnimation(gameObject, Mathf.Min(clipTime, attackClip.length));
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        string returnState;
        if (controller != null && !controller.IsGrounded)
            returnState = controller.IsFacingRight ? "Jump_R" : "Jump_L";
        else if (controller != null && controller.IsMoving)
            returnState = controller.IsFacingRight ? "Walk_R" : "Walk_L";
        else
            returnState = controller != null && controller.IsFacingRight ? "Idle_R" : "Idle_L";

        animator.enabled = true;
        animator.speed = 1f;
        animator.Play(returnState, 0, 0f);
        animator.Update(0f);
        isAttacking = false;

    }

    private void OnDisable()
    {
        isAttacking = false;
        if (animator != null)
        {
            animator.enabled = true;
            animator.speed = 1f;
        }
    }

    void Attack()
    {
        Vector2 origin = transform.position;
        Vector2 hitCenter = origin + attackDirection * attackRange;

        Collider2D[] hits = Physics2D.OverlapCircleAll(hitCenter, hitboxRadius, enemyLayer);
        foreach (var hit in hits)
        {
            Monster monster = hit.GetComponent<Monster>();
            if (monster == null)
                monster = hit.GetComponentInParent<Monster>();

            if (monster != null)
            {
                monster.TakeDamage(attackDamage, transform.position);
                continue;
            }

            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable == null)
                damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(attackDamage);
            }
        }
    }

    // 씬 뷰에서 공격 판정 범위를 눈으로 확인하기 위한 기즈모
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector2 origin = transform.position;
        Gizmos.DrawWireSphere(origin + attackDirection * attackRange, hitboxRadius);
    }
}
