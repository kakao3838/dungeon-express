using UnityEngine;

// Main Camera�� ���̼���.
// �÷��̾ �� ���� �� �ٷ� ���� �ʰ� ���߿� �����Ǵ� ���(ScenePlayerSpawner ��)����
// �ڵ����� ã�Ƽ� ���󰩴ϴ�.
public class CameraFollow : MonoBehaviour
{
    [Header("���󰡱� ����")]
    public float smoothSpeed = 5f;
    public Vector3 offset = new Vector3(0f, 0f, -10f); // 2D�� ���� Z�� -10

    [Header("범위 제한 (플레이어가 있는 MapArea 기준)")]
    [Tooltip("켜면 화면 왼쪽/오른쪽/아래쪽이 현재 구간의 타일 바깥을 비추지 않도록 막습니다")]
    public bool clampToMapArea = true;

    private Camera cam;
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
            transform.position = ClampToMapArea(desiredPosition);
            hasSnappedToTarget = true;
            return;
        }

        Vector3 smoothed = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = ClampToMapArea(smoothed);
    }

    // 플레이어가 있는 구간(MapArea)의 타일 범위 밖이 화면에 보이지 않게 카메라 위치를 제한
    // 좌우: 구간 양 끝, 아래: 가장 낮은 타일 바닥까지. 위쪽은 점프 공간이 필요해서 제한하지 않음
    Vector3 ClampToMapArea(Vector3 position)
    {
        if (!clampToMapArea || target == null || MapArea.All.Count == 0) return position;

        if (cam == null) cam = GetComponent<Camera>();
        if (cam == null || !cam.orthographic) return position;

        MapArea area = MapArea.FindClosest(target.position);
        if (area == null) return position;

        Bounds bounds = area.WorldBounds;
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        // 구간이 화면보다 좁으면 가운데에 고정
        if (bounds.size.x <= halfWidth * 2f)
        {
            position.x = bounds.center.x;
        }
        else
        {
            position.x = Mathf.Clamp(position.x, bounds.min.x + halfWidth, bounds.max.x - halfWidth);
        }

        position.y = Mathf.Max(position.y, bounds.min.y + halfHeight);
        return position;
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
            transform.position = ClampToMapArea(target.position + offset);
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