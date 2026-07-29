using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleMenu : MonoBehaviour
{
    [Tooltip("게임 시작 버튼을 누르면 이동할 씬 이름 (나중에 Town 씬이 완성되면 그걸로 바꾸세요)")]
    public string sceneToLoad = "DungeonScene";

    // Start 버튼의 OnClick()에 이 함수를 연결하세요
    public void OnStartGame()
    {
        SceneManager.LoadScene(sceneToLoad);
    }

    // 게임 종료 버튼의 OnClick()에 이 함수를 연결하세요
    public void OnQuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
