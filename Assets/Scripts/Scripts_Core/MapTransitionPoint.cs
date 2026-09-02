using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MapTransitionPoint : MonoBehaviour
{
    [SerializeField] private string connectionId;
    [SerializeField] private Transform arrivalPoint;
    [SerializeField] private MapArea mapArea;
    [SerializeField] private float transitionLockDuration = 0.2f;

    private Collider2D transitionCollider;
    private float nextAllowedTransitionTime;

    public string ConnectionId => connectionId;
    public Transform ArrivalPoint => arrivalPoint != null ? arrivalPoint : transform;
    public MapArea MapArea => mapArea;

    void Awake()
    {
        transitionCollider = GetComponent<Collider2D>();
        transitionCollider.isTrigger = true;

        if (mapArea == null)
        {
            mapArea = GetComponentInParent<MapArea>();
        }
    }

    void OnEnable()
    {
        MapTransitionRegistry.Register(this);
    }

    void OnDisable()
    {
        MapTransitionRegistry.Unregister(this);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        GameObject player = other.attachedRigidbody != null
            ? other.attachedRigidbody.gameObject
            : other.transform.root.gameObject;

        if (!player.CompareTag("Player") || Time.time < nextAllowedTransitionTime) return;

        MapTransitionPoint destination = MapTransitionRegistry.GetDestination(this);
        if (destination == null)
        {
            Debug.LogWarning($"[MapTransition] '{name}' cannot transition because Connection ID '{connectionId}' does not have exactly 2 points.", this);
            return;
        }

        MovePlayer(player, destination);
    }

    void MovePlayer(GameObject player, MapTransitionPoint destination)
    {
        Transform target = destination.ArrivalPoint;
        Rigidbody2D playerRigidbody = player.GetComponent<Rigidbody2D>();

        if (playerRigidbody != null)
        {
            playerRigidbody.position = target.position;
            playerRigidbody.rotation = target.eulerAngles.z;
            playerRigidbody.linearVelocity = Vector2.zero;
            playerRigidbody.angularVelocity = 0f;
        }
        else
        {
            player.transform.SetPositionAndRotation(target.position, target.rotation);
        }

        nextAllowedTransitionTime = Time.time + transitionLockDuration;
        destination.nextAllowedTransitionTime = Time.time + transitionLockDuration;
        destination.mapArea?.SetCurrent();
        CameraFollow.Instance?.SnapToTarget();
    }

    void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(connectionId))
        {
            Debug.LogWarning($"[MapTransition] '{name}' has an empty Connection ID.", this);
        }

        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null && !collider.isTrigger)
        {
            Debug.LogWarning($"[MapTransition] '{name}' requires a Trigger Collider2D.", this);
        }

        if (!Application.isPlaying)
        {
            MapTransitionRegistry.ValidateScene();
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(ArrivalPoint.position, 0.15f);
    }
}