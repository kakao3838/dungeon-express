using UnityEngine;

// 용 머리 불 뿜기 함정입니다. 주기적으로 입을 벌리고 불을 뿜었다가 다시 입을 닫습니다.
// 닫힘 -> 벌어짐 -> 불 뿜기(닿으면 플레이어가 피해를 입음) -> 닫힘 순서로 반복합니다.
// 프리팹(DragonFireTrap)을 씬에 놓고, 주기와 불 길이는 인스펙터에서 조절하세요.
// 왼쪽을 보게 하려면 프리팹의 Y 회전을 180으로 돌리세요.
public class DragonFireTrap : MonoBehaviour
{
    [Header("부품 (프리팹에 이미 연결돼 있음)")]
    [Tooltip("윗머리 회전축 (턱 경첩 위치)")]
    public Transform headPivot;
    [Tooltip("아랫턱 회전축 (턱 경첩 위치)")]
    public Transform jawPivot;
    [Tooltip("불 그림")]
    public SpriteRenderer fire;
    [Tooltip("불이 나가는 위치 (포신 끝)")]
    public Transform muzzle;

    [Header("주기 (초)")]
    public float closedSeconds = 2f;   // 입 닫고 쉬는 시간
    public float openSeconds = 0.35f;  // 입 벌리는 데 걸리는 시간
    public float fireSeconds = 1.5f;   // 불 뿜는 시간
    public float closeSeconds = 0.3f;  // 입 닫는 데 걸리는 시간
    [Tooltip("처음 시작을 늦춤. 함정이 여러 개일 때 서로 다르게 주면 박자가 엇갈림")]
    public float startDelay = 0f;

    [Header("입 벌리는 각도 (도)")]
    public float headOpenAngle = 15f;  // 윗머리가 들리는 각도 (+ = 위로)
    public float jawOpenAngle = -30f;  // 아랫턱이 내려가는 각도 (- = 아래로)

    [Header("불")]
    [Tooltip("불 프레임 (순서대로). 프리팹에 이미 들어 있음")]
    public Sprite[] flameFrames;
    public float flameFps = 14f;
    [Tooltip("맨 앞 몇 장은 불이 커지는 장면 (한 번만 재생)")]
    public int growFrames = 3;
    [Tooltip("맨 뒤 몇 장은 불이 꺼지는 장면 (끝날 때 재생)")]
    public int endFrames = 1;

    [Header("불 피해 (불에 닿는 순간 한 번, 한 번 뿜을 때마다 최대 한 번)")]
    [Tooltip("불에 닿는 범위 (월드 단위). 포신 끝에서 앞쪽으로 뻗음")]
    public Vector2 hitSize = new Vector2(5f, 1.4f);
    [Tooltip("한 번 뿜을 때 깎이는 체력 (하트 개수)")]
    public int damage = 1;
    public LayerMask hitLayers = ~0;

    private enum State { Closed, Opening, Firing, Closing }

    private State state = State.Closed;
    private float timer;
    private bool firstCycle = true;
    private bool hitThisBreath; // 이번에 뿜은 불로 이미 피해를 줬는지

    void Awake()
    {
        SetMouth(0f);
        SetFire(false);
    }

    void OnEnable()
    {
        state = State.Closed;
        timer = 0f;
        firstCycle = true;
        SetMouth(0f);
        SetFire(false);
    }

    void Update()
    {
        // 게임 시간이 멈추면(대화창, 일시정지) Time.deltaTime이 0이라 함정도 같이 멈춤
        timer += Time.deltaTime;

        switch (state)
        {
            case State.Closed:
                float wait = closedSeconds + (firstCycle ? startDelay : 0f);
                if (timer >= wait) Enter(State.Opening);
                break;

            case State.Opening:
                SetMouth(Smooth(timer / Mathf.Max(0.01f, openSeconds)));
                if (timer >= openSeconds) Enter(State.Firing);
                break;

            case State.Firing:
                UpdateFlame(timer);
                DealDamage();
                if (timer >= fireSeconds) Enter(State.Closing);
                break;

            case State.Closing:
                SetMouth(1f - Smooth(timer / Mathf.Max(0.01f, closeSeconds)));
                if (timer >= closeSeconds) Enter(State.Closed);
                break;
        }
    }

    void Enter(State next)
    {
        state = next;
        timer = 0f;
        if (next == State.Opening) firstCycle = false;

        if (next == State.Firing)
        {
            hitThisBreath = false; // 새로 뿜을 때마다 다시 한 번 피해를 줄 수 있음
            SetMouth(1f);
            SetFire(true);
            UpdateFlame(0f);
        }
        else if (next == State.Closing)
        {
            SetFire(false);
        }
        else if (next == State.Closed)
        {
            SetMouth(0f);
        }
    }

    // amount: 0 = 입 닫힘, 1 = 활짝 벌어짐
    void SetMouth(float amount)
    {
        if (headPivot != null) headPivot.localRotation = Quaternion.Euler(0f, 0f, headOpenAngle * amount);
        if (jawPivot != null) jawPivot.localRotation = Quaternion.Euler(0f, 0f, jawOpenAngle * amount);
    }

    void SetFire(bool on)
    {
        if (fire != null) fire.enabled = on;
    }

    void UpdateFlame(float elapsed)
    {
        if (fire == null || flameFrames == null || flameFrames.Length == 0) return;

        int total = flameFrames.Length;
        int grow = Mathf.Clamp(growFrames, 0, total);
        int end = Mathf.Clamp(endFrames, 0, total - grow);
        int loop = total - grow - end;

        float ticks = elapsed * flameFps;
        int index;

        if (ticks < grow)
        {
            index = (int)ticks;                                   // 불이 커지는 장면
        }
        else if (end > 0 && elapsed >= fireSeconds - end / flameFps)
        {
            index = total - end + Mathf.Min(end - 1, (int)((elapsed - (fireSeconds - end / flameFps)) * flameFps)); // 꺼지는 장면
        }
        else if (loop > 0)
        {
            index = grow + (int)(ticks - grow) % loop;            // 이글거리는 장면 반복
        }
        else
        {
            index = Mathf.Min(total - 1, (int)ticks);
        }

        fire.sprite = flameFrames[Mathf.Clamp(index, 0, total - 1)];
    }

    // 불에 닿는 순간 피해를 주고, 이번 불이 끝날 때까지는 더 주지 않음 (한 번 뿜을 때 체력 한 번만 깎임)
    void DealDamage()
    {
        if (hitThisBreath) return;

        Vector2 center, size;
        GetHitBox(out center, out size);

        var hits = Physics2D.OverlapBoxAll(center, size, transform.eulerAngles.z, hitLayers);
        foreach (var hit in hits)
        {
            var health = hit.GetComponentInParent<PlayerHealth>();
            if (health == null) continue;

            // 무적 시간 중이라 피해가 안 들어갔으면 맞은 걸로 치지 않고, 불이 계속되는 동안 다시 시도함
            int before = health.CurrentHearts;
            health.TakeDamage(damage);
            if (health.CurrentHearts < before)
            {
                hitThisBreath = true;
                break;
            }
        }
    }

    // 포신 끝에서 앞쪽(입이 향한 방향)으로 뻗는 피해 범위
    void GetHitBox(out Vector2 center, out Vector2 size)
    {
        Vector3 origin = muzzle != null ? muzzle.position : transform.position;
        Vector3 dir = transform.right * Mathf.Sign(transform.lossyScale.x); // 좌우 뒤집어도 입 방향을 따라감
        center = origin + dir * (hitSize.x * 0.5f);
        size = hitSize;
    }

    static float Smooth(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    void OnDrawGizmosSelected()
    {
        Vector2 center, size;
        GetHitBox(out center, out size);
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.9f);
        Gizmos.DrawWireCube(center, size);
    }
}
