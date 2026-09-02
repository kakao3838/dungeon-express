using UnityEngine;
using UnityEngine.SceneManagement;

// Player 오브젝트에 붙이세요. Town 씬에서 Dungeon 씬으로 넘어갈 때
// 인벤토리, 체력 등 상태가 그대로 유지되게 해줍니다.
public class PersistentPlayer : MonoBehaviour
{
    public static PersistentPlayer Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // 씬이 다시 로드되면서 새 Player가 또 생기면, 기존 것만 남기고 새 걸 제거
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        GameObject spawnPointObject = GameObject.Find("SpawnPoint");
        if (spawnPointObject == null) return;

        transform.SetPositionAndRotation(spawnPointObject.transform.position, spawnPointObject.transform.rotation);

        Rigidbody2D playerRigidbody = GetComponent<Rigidbody2D>();
        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector2.zero;
            playerRigidbody.angularVelocity = 0f;
        }
    }
}
