using UnityEngine;

/// <summary>
/// Marks a trigger collider as a vine that the player can climb.
/// Add this component and a Collider2D with Is Trigger enabled to a vine object.
/// </summary>
public sealed class ClimbableVine : MonoBehaviour
{
    private Collider2D vineCollider;

    private void Awake()
    {
        vineCollider = GetComponent<Collider2D>();
    }

    public float CenterX
    {
        get
        {
            return vineCollider != null ? vineCollider.bounds.center.x : transform.position.x;
        }
    }

    public bool ContainsPoint(Vector2 worldPoint)
    {
        return vineCollider != null && vineCollider.bounds.Contains(worldPoint);
    }
}
