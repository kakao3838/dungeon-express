using UnityEngine;

/// <summary>
/// Marks a trigger collider as a vine that the player can climb.
/// Add this component and a Collider2D with Is Trigger enabled to a vine object.
/// </summary>
public sealed class ClimbableVine : MonoBehaviour
{
    private Collider2D vineCollider;

    public Bounds Bounds
    {
        get
        {
            Bounds area = vineCollider != null ? vineCollider.bounds : new Bounds(transform.position, Vector3.zero);
            // Scene overrides may leave the trigger larger than the visible vine.
            bool found = false;
            Bounds visual = default;
            foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>())
            {
                if (!renderer.enabled || renderer.sprite == null) continue;
                if (!found) { visual = renderer.bounds; found = true; }
                else visual.Encapsulate(renderer.bounds);
            }
            if (found)
            {
                Vector3 min = area.min;
                Vector3 max = area.max;
                min.y = Mathf.Max(min.y, visual.min.y);
                max.y = Mathf.Max(min.y, Mathf.Min(max.y, visual.max.y));
                area.SetMinMax(min, max);
            }
            return area;
        }
    }

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
