using UnityEngine;

// Main Camera에 붙이세요.
// 플레이어가 씬 시작 시 바로 있지 않고 나중에 생성되는 경우(ScenePlayerSpawner 등)에도
// 자동으로 찾아서 따라갑니다.
public class CameraFollow : MonoBehaviour
{
    [Header("따라가기 설정")]
    public float smoothSpeed = 5f;
    public Vector3 offset = new Vector3(0f, 0f, -10f); // 2D는 보통 Z만 -10

    private Transform target;

    void LateUpdate()
    {
        // 아직 타겟(플레이어)을 못 찾았으면 계속 찾아봄
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
            else
            {
                return; // 아직 플레이어가 없으면 이번 프레임은 대기
            }
        }

        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
    }
}