using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[DisallowMultipleComponent]
public sealed class TrapDamage : MonoBehaviour
{
    [Min(1)]
    [SerializeField] private int damage = 1;
    [SerializeField] private GameObject visual;
    [SerializeField] private GameObject visualPushed;
    [Min(0f)]
    [SerializeField] private float pushedDuration = 0.3f;

    private Coroutine restoreVisualRoutine;

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
        if (visual == null) visual = transform.Find("Visual")?.gameObject;
        if (visualPushed == null) visualPushed = transform.Find("Visual(Pushed)")?.gameObject;
        SetPushed(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerDamageReceiver receiver = other.GetComponentInParent<PlayerDamageReceiver>();
        if (receiver == null) return;

        receiver.Hit(damage, transform.position);

        SetPushed(true);
        if (restoreVisualRoutine != null)
            StopCoroutine(restoreVisualRoutine);
        restoreVisualRoutine = StartCoroutine(RestoreVisualAfterHit());
    }

    private IEnumerator RestoreVisualAfterHit()
    {
        yield return new WaitForSeconds(pushedDuration);
        SetPushed(false);
        restoreVisualRoutine = null;
    }

    private void SetPushed(bool pushed)
    {
        if (visual != null) visual.SetActive(!pushed);
        if (visualPushed != null) visualPushed.SetActive(pushed);
    }

    private void OnDisable()
    {
        if (restoreVisualRoutine != null)
        {
            StopCoroutine(restoreVisualRoutine);
            restoreVisualRoutine = null;
        }
        SetPushed(false);
    }
}
