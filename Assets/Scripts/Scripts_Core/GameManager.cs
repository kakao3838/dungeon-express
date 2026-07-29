using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    void Awake()
    {
        // 이미 GameManager가 존재하면(중복 생성 방지) 나는 필요 없으니 자폭
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 안 사라지게
    }

    void Start()
    {
        // Boot 씬은 초기화만 하고 바로 Title 씬으로 넘어감
        SceneManager.LoadScene("TitleScene");
    }
}
