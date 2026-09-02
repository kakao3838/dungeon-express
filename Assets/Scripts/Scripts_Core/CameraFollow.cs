using UnityEngine;

// Main Camera�� ���̼���.
// �÷��̾ �� ���� �� �ٷ� ���� �ʰ� ���߿� �����Ǵ� ���(ScenePlayerSpawner ��)����
// �ڵ����� ã�Ƽ� ���󰩴ϴ�.
public class CameraFollow : MonoBehaviour
{
    [Header("���󰡱� ����")]
    public float smoothSpeed = 5f;
    public Vector3 offset = new Vector3(0f, 0f, -10f); // 2D�� ���� Z�� -10

    private Transform target;
    private bool hasSnappedToTarget;

    public static CameraFollow Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    void LateUpdate()
    {
        // ���� Ÿ��(�÷��̾�)�� �� ã������ ��� ã�ƺ�
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
                SnapToTarget();
            }
            else
            {
                return; // ���� �÷��̾ ������ �̹� �������� ���
            }
        }

        Vector3 desiredPosition = target.position + offset;
        if (!hasSnappedToTarget)
        {
            transform.position = desiredPosition;
            hasSnappedToTarget = true;
            return;
        }

        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
    }

    public void SnapToTarget()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }

        if (target != null)
        {
            transform.position = target.position + offset;
            hasSnappedToTarget = true;
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}