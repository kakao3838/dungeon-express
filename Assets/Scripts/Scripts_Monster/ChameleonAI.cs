using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
public class ChameleonAI : MonoBehaviour
{
    [Header("감지 설정")]
    public float detectionRange = 2f;
    public LayerMask playerLayer;

    [Header("공격 설정")]
    public int attackDamage = 1;
    public float attackCooldown = 2f;
    public float attackAnimDuration = 1.35f; // 공격 애니메이션 전체 길이 (이 동안 혀가 닿았는지 계속 검사)

    [Header("이동 설정")]
    public float moveSpeed = 1.5f;
    public float patrolDistance = 2f; // 시작 위치 기준 좌우로 이동하는 거리

    [Header("방향")]
    public bool facingRight = true;

    private Animator animator;
    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private Vector3 spawnPosition;
    private float moveDir;
    private bool isAttacking = false;

    void Awake()
    {
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        spawnPosition = transform.position;
        moveDir = facingRight ? 1f : -1f;

        UpdateAnimatorFacing();
    }

    void Update()
    {
        if (!isAttacking)
        {
            CheckForPlayer();
        }

        if (!isAttacking)
        {
            Patrol();
        }
    }

    void Patrol()
    {
        float offsetFromSpawn = transform.position.x - spawnPosition.x;
        if (moveDir > 0f && offsetFromSpawn >= patrolDistance) moveDir = -1f;
        else if (moveDir < 0f && offsetFromSpawn <= -patrolDistance) moveDir = 1f;

        rb.linearVelocity = new Vector2(moveDir * moveSpeed, rb.linearVelocity.y);

        facingRight = moveDir > 0f;
        UpdateAnimatorFacing();
        if (animator != null) animator.SetBool("IsMoving", true);
    }

    void CheckForPlayer()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, detectionRange, playerLayer);
        if (hit != null)
        {
            FacePlayer(hit.transform);
            StartCoroutine(AttackSequence(hit));
        }
    }

    void FacePlayer(Transform target)
    {
        facingRight = target.position.x >= transform.position.x;
        UpdateAnimatorFacing();
    }

    void UpdateAnimatorFacing()
    {
        if (animator != null) animator.SetBool("FacingRight", facingRight);
    }

    IEnumerator AttackSequence(Collider2D target)
    {
        isAttacking = true;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        if (animator != null)
        {
            animator.SetBool("IsMoving", false);
            animator.SetTrigger("Attack");
        }

        // 공격 애니메이션 동안 혀가 실제로 플레이어에 닿았을 때만 데미지 (한 번의 공격에 한 번)
        bool hitLanded = false;
        float elapsed = 0f;
        while (elapsed < attackAnimDuration)
        {
            if (!hitLanded) hitLanded = TryHitWithTongue();
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 쿨다운
        yield return new WaitForSeconds(attackCooldown);

        isAttacking = false;
    }

    // 혀 모양 (x, y, 반지름) - 오른쪽을 볼 때 몬스터 중심 기준, 스프라이트 로컬 단위
    // 공격 스프라이트에서 분홍색(혀) 픽셀을 측정해서 만든 값. 왼쪽을 볼 때는 x를 뒤집어서 사용
    static readonly Vector3[] TongueFull =
    {
        new Vector3(1.6f, 0.62f, 0.2f), new Vector3(3.2f, 0.09f, 0.3f), new Vector3(4.8f, -0.72f, 0.4f),
        new Vector3(5.6f, -1.25f, 0.5f), new Vector3(6.4f, -2.3f, 0.9f), new Vector3(7.2f, -2.85f, 0.75f),
    };
    static readonly Vector3[] TongueHalf =
    {
        new Vector3(3.2f, 0.69f, 0.1f), new Vector3(4.0f, 0.35f, 0.2f), new Vector3(4.8f, -0.18f, 0.35f),
        new Vector3(5.6f, -0.98f, 0.45f),
    };
    static readonly Vector3[] TongueTip =
    {
        new Vector3(3.2f, 1.2f, 0.3f), new Vector3(4.0f, 0.48f, 0.2f), new Vector3(4.8f, 0.25f, 0.2f),
    };

    // 혀가 나와 있는 공격 프레임 번호 (스프라이트 이름 끝 숫자)
    static readonly int[] RightFullFrames = { 6, 10 };
    static readonly int[] RightHalfFrames = { 8, 11 };
    static readonly int[] RightTipFrames = { 4, 15 };
    static readonly int[] LeftFullFrames = { 5, 9 };
    static readonly int[] LeftHalfFrames = { 8, 11 };
    static readonly int[] LeftTipFrames = { 7, 12 };

    // 지금 재생 중인 공격 프레임의 혀 모양을 구해서 플레이어와 닿았는지 검사
    bool TryHitWithTongue()
    {
        if (sr == null || sr.sprite == null) return false;

        string spriteName = sr.sprite.name;
        if (!spriteName.Contains("attack")) return false;

        bool left = spriteName.Contains("_left_");
        int underscore = spriteName.LastIndexOf('_');
        if (underscore < 0 || !int.TryParse(spriteName.Substring(underscore + 1), out int frame)) return false;

        Vector3[] shape;
        if (System.Array.IndexOf(left ? LeftFullFrames : RightFullFrames, frame) >= 0) shape = TongueFull;
        else if (System.Array.IndexOf(left ? LeftHalfFrames : RightHalfFrames, frame) >= 0) shape = TongueHalf;
        else if (System.Array.IndexOf(left ? LeftTipFrames : RightTipFrames, frame) >= 0) shape = TongueTip;
        else return false;

        float scale = Mathf.Abs(transform.lossyScale.x);
        float side = left ? -1f : 1f;

        // 혀를 따라 일정 간격으로 원을 겹쳐서 닿았는지 확인
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
                if (hit == null) continue;

                IDamageable damageable = hit.GetComponent<IDamageable>();
                if (damageable == null) continue;

                damageable.TakeDamage(attackDamage);
                return true;
            }
        }

        return false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
