using System.Collections;
using UnityEngine;

public class PlayerKnockback : MonoBehaviour
{
    [Header("�˹� ����")]
    [SerializeField] private float horizontalForce = 5f;
    [SerializeField] private float verticalForce = 3f;
    [SerializeField] private float knockbackDuration = 0.2f;

    private Rigidbody2D rb;
    private PlayerController playerController;
    private PlayerVineClimb playerVineClimb;

    private Coroutine knockbackCoroutine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerController = GetComponent<PlayerController>();
        playerVineClimb = GetComponent<PlayerVineClimb>();
    }

    public void Knockback(Vector2 attackerPosition)
    {
        if (knockbackCoroutine != null)
        {
            StopCoroutine(knockbackCoroutine);
        }

        knockbackCoroutine = StartCoroutine(
            KnockbackCoroutine(attackerPosition)
        );
    }

    private IEnumerator KnockbackCoroutine(Vector2 attackerPosition)
    {
        if (playerVineClimb != null)
            playerVineClimb.CancelClimbForKnockback();

        // ������ �ݴ� ���� ���
        float direction = transform.position.x >= attackerPosition.x ? 1f : -1f;

        // �Ϲ� �̵��� �˹� �ӵ��� ����� �ʵ��� ��� ����
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        rb.linearVelocity = Vector2.zero;

        rb.linearVelocity = new Vector2(
            direction * horizontalForce,
            verticalForce
        );

        yield return new WaitForSeconds(knockbackDuration);

        if (playerController != null)
        {
            playerController.enabled = true;
        }

        knockbackCoroutine = null;
    }
}
