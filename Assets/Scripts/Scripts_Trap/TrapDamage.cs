using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[DisallowMultipleComponent]
public sealed class TrapDamage : MonoBehaviour
{
    [Min(1)]
    [SerializeField] private int damage = 1;

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerDamageReceiver receiver = other.GetComponentInParent<PlayerDamageReceiver>();
        if (receiver != null)
            receiver.Hit(damage, transform.position);
    }
}
