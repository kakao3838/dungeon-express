using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
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
    [SerializeField] private float attackAnimationDuration = 0.6f;
    [Tooltip("공격 모션 중 타격 판정이 나가는 시점 (0 = 시작, 1 = 끝). 배트를 휘두르는 순간에 맞게 합니다")]
    [Range(0f, 1f)] [SerializeField] private float hitTimeNormalized = 0.35f;
    [SerializeField] private AnimationClip attackLeftClip;
    [SerializeField] private AnimationClip attackRightClip;

    private PlayerController controller;
    private PlayerVineClimb vineClimb;
    private Animator animator;
    private Vector2 attackDirection = Vector2.right;
    private bool isAttacking;
    private int ignoreAttackUntilFrame = -1;
    private bool wasBlockedLastFrame;

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

        // 대화창·결과창·메뉴를 닫으려고 누른 클릭이 공격으로 나가지 않게,
        // 창이 열려 있던 직전 프레임에도 입력을 무시함
        bool blocked = IsBlockedByUI();
        bool ignore = blocked || wasBlockedLastFrame || Time.frameCount <= ignoreAttackUntilFrame;
        wasBlockedLastFrame = blocked;

        if (attackPressed && !ignore)
        {
            if (vineClimb != null && vineClimb.IsClimbing)
                return;

            if (!allowAirAttack && controller != null && !controller.IsGrounded)
                return;

            if (isAttacking)
                return;

            if (animator != null)
                StartCoroutine(PlayAttackAnimation());
            else
                Attack();
        }
    }

    // UI(대화창·결과창·메뉴 등)가 떠 있거나 UI 위를 클릭 중이면 공격하지 않음
    private bool IsBlockedByUI()
    {
        if (GamePause.IsPaused) return true;
        if (NPCDialogueMenu.Instance != null && NPCDialogueMenu.Instance.IsOpen) return true;
        var mouse = Mouse.current;
        return mouse != null && mouse.leftButton.wasPressedThisFrame
            && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private void OnEnable()
    {
        // 일시정지가 풀리는 클릭과 같은 프레임에 공격이 나가지 않게 함
        ignoreAttackUntilFrame = Time.frameCount;
        wasBlockedLastFrame = false;
    }

    private IEnumerator PlayAttackAnimation()
    {
        isAttacking = true;
        bool facingRight = controller != null && controller.IsFacingRight;
        AnimationClip attackClip = facingRight ? attackRightClip : attackLeftClip;
        float elapsed = 0f;
        bool hitDone = false;
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

            // 배트가 휘둘러지는 시점에 타격 판정 (시작 순간에 판정하면 몬스터가 모션보다 먼저 맞음)
            if (!hitDone && elapsed >= attackAnimationDuration * hitTimeNormalized)
            {
                hitDone = true;
                Attack();
            }
            yield return null;
        }
        if (!hitDone) Attack();

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
        // 컴포넌트를 꺼도 코루틴은 계속 돌아서, 멈추지 않으면 공격 모션이 겹치거나 반복됨
        StopAllCoroutines();
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
