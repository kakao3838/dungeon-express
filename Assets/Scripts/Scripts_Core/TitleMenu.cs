using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleMenu : MonoBehaviour
{
    [Tooltip("게임 시작 버튼을 누르면 이동할 씬 이름 (나중에 Town 씬이 완성되면 그걸로 바꾸세요)")]
    public string sceneToLoad = "DungeonScene";

    [Header("메뉴 패널")]
    public GameObject mainMenuPanel;
    public GameObject continuePanel;
    public GameObject settingsPanel;

    [Header("설정 탭")]
    public GameObject keyboardPanel;
    public GameObject audioPanel;
    public GameObject videoPanel;

    void Start()
    {
        OpenMainMenu();
    }

    public void OpenMainMenu()
    {
        mainMenuPanel.SetActive(true);
        continuePanel.SetActive(false);
        settingsPanel.SetActive(false);
    }

    public void OpenContinueMenu()
    {
        mainMenuPanel.SetActive(false);
        continuePanel.SetActive(true);
        settingsPanel.SetActive(false);
    }

    public void OpenSettings()
    {
        mainMenuPanel.SetActive(false);
        continuePanel.SetActive(false);
        settingsPanel.SetActive(true);

        OpenKeyboardSettings();
    }

    public void OpenKeyboardSettings()
    {
        keyboardPanel.SetActive(true);
        audioPanel.SetActive(false);
        videoPanel.SetActive(false);
    }

    public void OpenAudioSettings()
    {
        keyboardPanel.SetActive(false);
        audioPanel.SetActive(true);
        videoPanel.SetActive(false);
    }

    public void OpenVideoSettings()
    {
        keyboardPanel.SetActive(false);
        audioPanel.SetActive(false);
        videoPanel.SetActive(true);
    }

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
