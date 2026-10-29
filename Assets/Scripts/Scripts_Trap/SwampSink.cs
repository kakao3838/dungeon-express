using UnityEngine;

public class SwampSink : MonoBehaviour
{
    [SerializeField] private float sinkSpeed = 0.5f;
    [SerializeField] private float sinkAcceleration = 30f;

    private void OnTriggerStay2D(Collider2D other)
    {
        Rigidbody2D body = other.attachedRigidbody;
        if (body == null || body.GetComponentInParent<PlayerController>() == null)
            return;

        Vector2 velocity = body.linearVelocity;
        float gravityStep = Physics2D.gravity.y * body.gravityScale * Time.fixedDeltaTime;
        velocity.y = Mathf.MoveTowards(
            velocity.y,
            -sinkSpeed,
            sinkAcceleration * Time.fixedDeltaTime) - gravityStep;
        body.linearVelocity = velocity;
    }
}
