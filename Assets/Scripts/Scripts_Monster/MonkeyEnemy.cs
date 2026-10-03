using System.Collections;
using UnityEngine;

public class MonkeyEnemy : MonoBehaviour
{
    [Header("player detection")]
    [SerializeField] private float detectionRange = 4.5f;
    [SerializeField] private float verticalDetectionRange = 3f;

    [Header("attack")]
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float throwDelay = 0.4f;
    [SerializeField] private float attackAnimationDuration = 0.75f;
    [SerializeField] private float attackVisualScale = 1.07f;
    [SerializeField] private bool facesRight;
    [SerializeField] private AnimationClip attackClip;
    [SerializeField] private Sprite[] attackFrames;
    [SerializeField] private Sprite alertSprite;
    [SerializeField] private Vector2 alertLocalPosition = new Vector2(-4f, 4f);

    [Header("ground")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundSnapDistance = 10f;
    [SerializeField] private float groundVisualOffset = -0.2f;

    [Header("banana")]
    [SerializeField] private BananaProjectile bananaPrefab;
    [SerializeField] private Transform firePoint;
    [Tooltip("왼쪽을 보는 원숭이 기준 등 뒤 바나나 생성 위치")]
    [SerializeField] private Vector2 bananaSpawnOffset = new Vector2(4f, 1.5f);

    [SerializeField] private float bananaHorizontalSpeed = 5f;
    [SerializeField] private float bananaVerticalSpeed = 6f;

    private Transform player;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Collider2D bodyCollider;
    private Sprite idleSprite;
    private SpriteRenderer visualRenderer;
    private Transform visualTransform;
    private bool isAttacking;
    private GameObject alertObject;

    private float attackTimer;

    private Vector3 fixedPosition;
    private bool fixedFlipX;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        fixedPosition = transform.position;
        if (spriteRenderer != null)
        {
            fixedFlipX = facesRight;
            spriteRenderer.flipX = fixedFlipX;
        }
        animator = GetComponent<Animator>();
        bodyCollider = GetComponent<Collider2D>();
        if (spriteRenderer != null) idleSprite = spriteRenderer.sprite;
        if (animator != null) animator.enabled = false;

        CreateAnchoredVisual();

        if (firePoint != null)
            firePoint.localPosition = new Vector2(
                facesRight ? -Mathf.Abs(bananaSpawnOffset.x) : Mathf.Abs(bananaSpawnOffset.x),
                bananaSpawnOffset.y
            );

        CreateAlertObject();
    }

    private void LateUpdate()
    {
        // 원숭이는 추격하지 않고 처음 배치된 자리에서 바나나만 던집니다.
        transform.position = fixedPosition;
    }

    private void Start()
    {
        SnapToGround();

        GameObject playerObject =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    private void Update()
    {
        if (player == null)
            return;

        if (attackTimer > 0f)
        {
            attackTimer -= Time.deltaTime;
        }

        if (!CanDetectPlayer())
            return;

        if (attackTimer <= 0f && !isAttacking)
        {
            StartCoroutine(AttackSequence());
        }
    }

    private IEnumerator AttackSequence()
    {
        isAttacking = true;
        attackTimer = attackCooldown;
        if (alertObject != null) alertObject.SetActive(true);

        if (animator != null) animator.enabled = false;

        float elapsed = 0f;
        bool bananaThrown = false;
        while (elapsed < attackAnimationDuration)
        {
            ShowAttackFrame(elapsed);

            if (!bananaThrown && elapsed >= throwDelay)
            {
                if (alertObject != null) alertObject.SetActive(false);
                if (player != null && player.gameObject.activeInHierarchy) ThrowBanana();
                bananaThrown = true;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (animator != null) animator.enabled = false;
        if (alertObject != null) alertObject.SetActive(false);
        ShowVisualSprite(idleSprite);
        isAttacking = false;
    }

    private void CreateAnchoredVisual()
    {
        if (spriteRenderer == null || idleSprite == null)
            return;

        GameObject visualObject = new GameObject("Visual");
        visualTransform = visualObject.transform;
        visualTransform.SetParent(transform, false);

        visualRenderer = visualObject.AddComponent<SpriteRenderer>();
        visualRenderer.sharedMaterial = spriteRenderer.sharedMaterial;
        visualRenderer.color = spriteRenderer.color;
        visualRenderer.flipX = fixedFlipX;
        visualRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
        visualRenderer.sortingOrder = spriteRenderer.sortingOrder;
        visualRenderer.maskInteraction = spriteRenderer.maskInteraction;

        ShowVisualSprite(idleSprite);
        spriteRenderer.enabled = false;
    }

    private void ShowAttackFrame(float elapsed)
    {
        if (attackFrames == null || attackFrames.Length == 0)
            return;

        float normalized = Mathf.Clamp01(elapsed / Mathf.Max(attackAnimationDuration, 0.01f));
        int index = Mathf.Min(Mathf.FloorToInt(normalized * attackFrames.Length), attackFrames.Length - 1);
        if (visualTransform != null)
            visualTransform.localScale = Vector3.one * attackVisualScale;
        ShowVisualSprite(attackFrames[index]);
    }

    private void ShowVisualSprite(Sprite sprite)
    {
        if (visualRenderer == null || visualTransform == null || sprite == null)
            return;

        visualRenderer.sprite = sprite;
        visualRenderer.flipX = fixedFlipX;

        if (sprite == idleSprite)
            visualTransform.localScale = Vector3.one;

        // Sprite pivots are authored per frame to keep the seated body fixed.
        // Bounds-based correction changes every frame because transparent
        // margins differ, which makes the monkey jump left and right.
        visualTransform.localPosition = Vector3.zero;
    }

    private void CreateAlertObject()
    {
        if (alertSprite == null)
            return;

        alertObject = new GameObject("Alert");
        alertObject.transform.SetParent(transform, false);
        alertObject.transform.localPosition = alertLocalPosition;

        SpriteRenderer alertRenderer = alertObject.AddComponent<SpriteRenderer>();
        alertRenderer.sprite = alertSprite;
        if (spriteRenderer != null)
        {
            alertRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
            alertRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;
        }

        alertObject.SetActive(false);
    }

    private bool CanDetectPlayer()
    {
        if (player == null || !player.gameObject.activeInHierarchy)
            return false;

        Vector2 offset = player.position - transform.position;
        bool playerIsInFront = facesRight ? offset.x >= 0f : offset.x <= 0f;
        return playerIsInFront && Mathf.Abs(offset.x) <= detectionRange &&
               Mathf.Abs(offset.y) <= verticalDetectionRange;
    }

    private void SnapToGround()
    {
        if (bodyCollider == null)
            return;

        Vector2 rayOrigin = (Vector2)bodyCollider.bounds.center + Vector2.up * 2f;
        // Ground 레이어만 검사한다. 다른 몬스터나 장식용 콜라이더를 바닥으로
        // 잘못 잡으면 원숭이가 실제 타일보다 위에 고정될 수 있다.
        RaycastHit2D[] hits = Physics2D.RaycastAll(
            rayOrigin,
            Vector2.down,
            groundSnapDistance,
            groundLayer);
        RaycastHit2D groundHit = default;
        bool foundGround = false;

        foreach (RaycastHit2D candidate in hits)
        {
            if (candidate.collider == null || candidate.collider == bodyCollider || candidate.collider.isTrigger)
                continue;
            if (candidate.collider.GetComponentInParent<PlayerController>() != null ||
                candidate.collider.GetComponentInParent<Monster>() != null)
                continue;

            groundHit = candidate;
            foundGround = true;
            break;
        }

        if (!foundGround)
            return;

        // The source images contain a large transparent canvas, so sprite bounds
        // would make the visible feet float. Use the stable body collider instead.
        float correction = groundHit.point.y - bodyCollider.bounds.min.y;
        transform.position += Vector3.up * (correction + groundVisualOffset);
        fixedPosition = transform.position;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        isAttacking = false;
    }

    public void PrepareForDeath()
    {
        StopAllCoroutines();
        if (alertObject != null) alertObject.SetActive(false);
        if (visualRenderer != null) visualRenderer.enabled = false;
        // Death sprites are about 7% smaller than the idle artwork. Apply the
        // same scale compensation used by the attack frames before the root
        // SpriteRenderer is handed back to the Animator.
        transform.localScale = new Vector3(
            transform.localScale.x * attackVisualScale,
            transform.localScale.y * attackVisualScale,
            transform.localScale.z);
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.sprite = idleSprite;
            spriteRenderer.flipX = fixedFlipX;
        }
    }

    private void ThrowBanana()
    {
        if (bananaPrefab == null || firePoint == null)
            return;

        float direction = facesRight ? 1f : -1f;

        BananaProjectile banana =
            Instantiate(
                bananaPrefab,
                firePoint.position,
                Quaternion.identity
            );

        banana.Launch(
            direction,
            bananaHorizontalSpeed,
            bananaVerticalSpeed
        );
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(
            transform.position,
            detectionRange
        );
    }
}
