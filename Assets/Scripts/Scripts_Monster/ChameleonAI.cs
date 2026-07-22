using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ChameleonAI : MonoBehaviour
{
    private enum State { Idle, Attack, Cooldown }

    [Header("감지 설정")]
    public float detectionRange = 2f;
    public LayerMask playerLayer;

    [Header("공격 설정")]
    public int attackDamage = 1;
    public float attackCooldown = 2f;

    [Header("혀 공격 시각화")]
    public LineRenderer tongueRenderer; // 같은 오브젝트에 LineRenderer 컴포넌트를 추가해서 연결하세요
    public float tongueExtendTime = 0.15f;  // 혀가 뻗어나가는 시간
    public float tongueRetractTime = 0.1f;  // 혀가 돌아오는 시간

    [Header("임시 시각화 색상 (나중에 애니메이션으로 교체)")]
    public Color idleColor = Color.green;
    public Color attackColor = Color.red;
    public Color cooldownColor = Color.gray;

    private State currentState = State.Idle;
    private SpriteRenderer sr;
    private bool isAttacking = false;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();

        if (tongueRenderer != null)
        {
            tongueRenderer.positionCount = 2;
            tongueRenderer.enabled = false; // 평소엔 숨겨둠
        }
    }

    void Start()
    {
        SetState(State.Idle);
    }

    void Update()
    {
        if (currentState == State.Idle)
        {
            CheckForPlayer();
        }
    }

    void CheckForPlayer()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, detectionRange, playerLayer);
        if (hit != null && !isAttacking)
        {
            StartCoroutine(AttackSequence(hit));
        }
    }

    IEnumerator AttackSequence(Collider2D target)
    {
        isAttacking = true;
        SetState(State.Attack);

        Vector3 startPos = transform.position;
        Vector3 targetPos = target.transform.position;

        // 1. 혀가 플레이어 쪽으로 쭉 뻗어나감
        if (tongueRenderer != null)
        {
            tongueRenderer.enabled = true;
            float t = 0f;
            while (t < tongueExtendTime)
            {
                t += Time.deltaTime;
                float ratio = t / tongueExtendTime;
                Vector3 currentEnd = Vector3.Lerp(startPos, targetPos, ratio);
                tongueRenderer.SetPosition(0, startPos);
                tongueRenderer.SetPosition(1, currentEnd);
                yield return null;
            }
        }

        // 2. 혀가 닿는 순간 데미지 적용
        IDamageable damageable = target.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(attackDamage);
        }

        // 3. 혀가 다시 돌아옴
        if (tongueRenderer != null)
        {
            float t = 0f;
            while (t < tongueRetractTime)
            {
                t += Time.deltaTime;
                float ratio = t / tongueRetractTime;
                Vector3 currentEnd = Vector3.Lerp(targetPos, startPos, ratio);
                tongueRenderer.SetPosition(0, startPos);
                tongueRenderer.SetPosition(1, currentEnd);
                yield return null;
            }
            tongueRenderer.enabled = false;
        }

        // 4. 쿨다운
        SetState(State.Cooldown);
        yield return new WaitForSeconds(attackCooldown);

        // 5. 다시 대기 상태로
        SetState(State.Idle);
        isAttacking = false;
    }

    void SetState(State newState)
    {
        currentState = newState;

        switch (newState)
        {
            case State.Idle:
                sr.color = idleColor;
                break;
            case State.Attack:
                sr.color = attackColor;
                break;
            case State.Cooldown:
                sr.color = cooldownColor;
                break;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}