using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    [Header("공격 설정")]
    public float attackRange = 0.6f;
    public float hitboxRadius = 0.35f;
    public int attackDamage = 1;
    public LayerMask enemyLayer;

    private PlayerController controller;
    private Vector2 attackDirection = Vector2.right;

    void Awake()
    {
        controller = GetComponent<PlayerController>();
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
            Attack();
        }
    }

    void Attack()
    {
        Vector2 origin = transform.position;
        Vector2 hitCenter = origin + attackDirection * attackRange;

        Collider2D[] hits = Physics2D.OverlapCircleAll(hitCenter, hitboxRadius, enemyLayer);
        foreach (var hit in hits)
        {
            IDamageable damageable = hit.GetComponent<IDamageable>();
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
