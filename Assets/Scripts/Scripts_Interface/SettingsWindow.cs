using UnityEngine;

// 타이틀 화면의 설정 창(Settings: KeyBoard / Audio / Video)을 게임 중에도 쓰기 위한 프리팹용 스크립트입니다.
// TitleMenu에서 설정 창을 열고 닫는 부분(OpenSettings, OpenXxxSettings)만 가져왔고, 버튼 연결도 같은 이름으로 맞춰뒀습니다.
// 타이틀의 "Back"(OpenMainMenu)은 여기서는 창 닫기(Close)가 됩니다.
public class SettingsWindow : MonoBehaviour
{
    [Header("연결")]
    public GameObject panel;                // 설정 창 전체 (SettingsPanel)
    public GameObject categoryGroup;        // Keyboard / Audio / Video 선택 버튼들
    public GameObject homeBottomButtons;    // 설정 홈 화면 아래의 Default / Back 버튼
    public GameObject keyboardPanel;
    public GameObject audioPanel;
    public GameObject videoPanel;

    public bool IsOpen => panel != null && panel.activeSelf;

    void Awake()
    {
        if (panel != null) panel.SetActive(false);
    }

    public void Open()
    {
        if (panel == null) return;
        panel.SetActive(true);
        OpenSettingsHome();
    }

    // 타이틀의 Back 버튼(OpenMainMenu)에 해당
    public void Close()
    {
        if (panel != null) panel.SetActive(false);
    }

    public void OpenSettingsHome()
    {
        categoryGroup.SetActive(true);
        homeBottomButtons.SetActive(true);

        keyboardPanel.SetActive(false);
        audioPanel.SetActive(false);
        videoPanel.SetActive(false);
    }

    public void OpenKeyboardSettings()
    {
        categoryGroup.SetActive(false);
        homeBottomButtons.SetActive(false);

        keyboardPanel.SetActive(true);
        audioPanel.SetActive(false);
        videoPanel.SetActive(false);
    }

    public void OpenAudioSettings()
    {
        categoryGroup.SetActive(false);
        homeBottomButtons.SetActive(false);

        keyboardPanel.SetActive(false);
        audioPanel.SetActive(true);
        videoPanel.SetActive(false);
    }

    public void OpenVideoSettings()
    {
        categoryGroup.SetActive(false);
        homeBottomButtons.SetActive(false);

        keyboardPanel.SetActive(false);
        audioPanel.SetActive(false);
        videoPanel.SetActive(true);
    }
}
