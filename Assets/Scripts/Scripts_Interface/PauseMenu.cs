using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// ESC를 누르면 뜨는 일시정지 창입니다. (계속하기 / 설정 / 메인 메뉴)
// 프리팹(PauseMenu)을 게임 씬(Town, Dungeon 등)에 하나씩 놓으면 동작합니다.
// 대화창이나 결과창처럼 다른 창이 게임을 멈추고 있을 때는 ESC를 무시합니다.
public class PauseMenu : MonoBehaviour
{
    [Header("연결")]
    public GameObject panel;
    [Tooltip("설정 버튼을 누르면 여는 설정 창 (타이틀 화면의 Settings와 같은 창)")]
    public SettingsWindow settingsWindow;
    public TMP_Text noticeText;

    [Header("메인 메뉴로 나갈 때 이동할 씬")]
    public string mainMenuScene = "TitleScene";

    public bool IsOpen => panel != null && panel.activeSelf;

    private bool holdingPause;
    private Coroutine noticeRoutine;

    void Awake()
    {
        if (panel != null) panel.SetActive(false);
        if (noticeText != null) noticeText.gameObject.SetActive(false);
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return;

        if (settingsWindow != null && settingsWindow.IsOpen) settingsWindow.Close(); // 설정 창에서는 ESC로 일시정지 창으로 돌아감
        else if (IsOpen) Resume();
        else if (!GamePause.IsPaused) Open(); // 다른 창(대화창, 결과창)이 열려 있으면 무시
    }

    void OnDestroy()
    {
        ReleasePause(); // 일시정지 창이 열린 채로 씬이 바뀌어도 시간이 멈춰 있지 않게
    }

    public void Open()
    {
        if (panel == null || IsOpen) return;
        panel.SetActive(true);
        if (!holdingPause)
        {
            holdingPause = true;
            GamePause.Push();
        }
    }

    // "계속하기" 버튼의 OnClick에 연결됨
    public void Resume()
    {
        if (settingsWindow != null) settingsWindow.Close();
        if (panel != null) panel.SetActive(false);
        ReleasePause();
    }

    // "설정" 버튼의 OnClick에 연결됨
    public void OnClickSettings()
    {
        if (settingsWindow != null) settingsWindow.Open();
        else ShowNotice("설정 창이 연결되지 않았어요.");
    }

    // "메인 메뉴" 버튼의 OnClick에 연결됨
    public void OnClickMainMenu()
    {
        Resume(); // 시간과 조작을 먼저 원래대로

        // 씬을 넘어 유지되던 플레이어/퀘스트 상태를 지워서, 새로 시작할 때 이전 판이 섞이지 않게 함
        if (PersistentPlayer.Instance != null) Destroy(PersistentPlayer.Instance.gameObject);
        if (QuestManager.Instance != null) Destroy(QuestManager.Instance.gameObject);

        SceneManager.LoadScene(mainMenuScene);
    }

    void ReleasePause()
    {
        if (!holdingPause) return;
        holdingPause = false;
        GamePause.Pop();
    }

    void ShowNotice(string message)
    {
        if (noticeText == null) return;
        noticeText.text = message;
        noticeText.gameObject.SetActive(true);
        if (noticeRoutine != null) StopCoroutine(noticeRoutine);
        noticeRoutine = StartCoroutine(HideNoticeLater());
    }

    IEnumerator HideNoticeLater()
    {
        yield return new WaitForSecondsRealtime(1.5f); // 시간이 멈춰 있어도 흐르는 시간
        if (noticeText != null) noticeText.gameObject.SetActive(false);
        noticeRoutine = null;
    }
}
