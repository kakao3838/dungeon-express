using UnityEngine;

public class BananaProjectile : MonoBehaviour
{
    [Header("damage")]
    [SerializeField] private int damage = 1;

    [Header("lifeTime")]
    [SerializeField] private float lifeTime = 2.5f;
    [SerializeField] private float maxTravelDistance = 9f;
    [SerializeField] private LayerMask groundLayer;

    [Header("rotation")]
    [SerializeField] private float rotationSpeed = 360f;

    private Rigidbody2D rb;
    private Vector2 spawnPosition;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        spawnPosition = transform.position;
        Destroy(gameObject, lifeTime);
    }

    public void Launch(float direction, float horizontalSpeed, float verticalSpeed)
    {
        rb.linearVelocity = new Vector2(
            direction * horizontalSpeed,
            verticalSpeed
        );
    }

    private void Update()
    {
        transform.Rotate(
            0f,
            0f,
            rotationSpeed * Time.deltaTime
        );

        if (Vector2.SqrMagnitude((Vector2)transform.position - spawnPosition) >=
            maxTravelDistance * maxTravelDistance)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerDamageReceiver receiver =
            other.GetComponent<PlayerDamageReceiver>();

        if (receiver == null)
        {
            receiver =
                other.GetComponentInParent<PlayerDamageReceiver>();
        }

        if (receiver != null)
        {
            receiver.Hit(damage, transform.position);

            Destroy(gameObject);
            return;
        }

        // Ignore enemies and trigger-only detection volumes. Destroy the
        // banana as soon as it reaches a real ground collider so it cannot
        // remain lying on top of the tilemap.
        if (other.GetComponentInParent<Monster>() != null || other.isTrigger)
            return;

        if ((groundLayer.value & (1 << other.gameObject.layer)) != 0)
            Destroy(gameObject);
    }
}
