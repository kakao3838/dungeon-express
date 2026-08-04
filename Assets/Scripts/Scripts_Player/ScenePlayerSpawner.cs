using UnityEngine;

// 씬에 빈 오브젝트를 하나 만들어서 이 스크립트를 붙이세요.
// Town에서 정상적으로 넘어온 경우(PersistentPlayer가 이미 존재) 아무것도 안 하고,
// 이 씬만 단독으로 Play해서 테스트하는 경우(PersistentPlayer가 없음) 플레이어를 자동 생성합니다.
public class ScenePlayerSpawner : MonoBehaviour
{
    public GameObject playerPrefab;
    public Transform spawnPoint;

    void Awake()
    {
        if (PersistentPlayer.Instance == null)
        {
            Vector3 pos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
            Instantiate(playerPrefab, pos, Quaternion.identity);
            Debug.Log("PersistentPlayer가 없어서 테스트용 플레이어를 자동 생성했습니다.");
        }
    }
}
