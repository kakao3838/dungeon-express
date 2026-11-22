using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 화면 아래 대사창으로 DialogueSequence를 재생합니다. (튜토리얼 대사창과 같은 모양)
// 재생하는 동안 GamePause로 게임이 멈추고 플레이어 조작이 막힙니다.
// 클릭, F, Space, Enter로 다음 줄로 넘깁니다. StoryDirector 오브젝트(씬 간 유지) 아래에 둡니다.
public class DialogueRunner : MonoBehaviour
{
    public static DialogueRunner Instance { get; private set; }

    [Header("연결")]
    public GameObject panel;
    public GameObject speakerBox;
    public TMP_Text speakerText;
    public TMP_Text lineText;
    public GameObject nextHint;
    public Button panelButton;

    private bool holdingPause;
    private bool advanceClicked;
    private int shownFrame = -1;

    public bool IsOpen => panel != null && panel.activeSelf;

    void Awake()
    {
        if (Instance != null && Instance != this) return;
        Instance = this;
        if (panel != null) panel.SetActive(false);
        if (panelButton != null) panelButton.onClick.AddListener(() => advanceClicked = true);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        ReleasePause(); // 대사 중에 씬이 바뀌어도 시간이 멈춰 있지 않게
    }

    // 대사를 처음부터 끝까지 재생. closeAtEnd가 false면 끝나도 대사창과 정지를 유지함 (이어서 다른 연출을 할 때)
    public IEnumerator Play(DialogueSequence sequence, bool closeAtEnd = true)
    {
        if (sequence != null)
        {
            foreach (var line in sequence.lines) yield return Say(line.speaker, line.text);
        }
        if (closeAtEnd) Close();
    }

    // 한 줄 재생: 다음으로 넘기는 입력이 올 때까지 기다림
    public IEnumerator Say(string speaker, string text)
    {
        if (panel == null) yield break;

        bool hasSpeaker = !string.IsNullOrEmpty(speaker);
        if (speakerBox != null) speakerBox.SetActive(hasSpeaker);
        if (speakerText != null) speakerText.text = speaker;
        if (lineText != null) lineText.text = text;

        panel.SetActive(true);
        HoldPause();

        advanceClicked = false;
        shownFrame = Time.frameCount;
        while (true)
        {
            yield return null;
            // 이 줄을 띄운 프레임의 입력(앞 줄을 넘긴 F 등)은 무시
            if (Time.frameCount == shownFrame) continue;
            if (advanceClicked || AdvanceKeyPressed()) break;
        }
        advanceClicked = false;
    }

    // 다른 창(배송 완료 창 등)을 띄우는 동안 대사창만 가림 (정지는 유지)
    public void SetVisible(bool visible)
    {
        if (panel != null) panel.SetActive(visible);
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        ReleasePause();
    }

    static bool AdvanceKeyPressed()
    {
        if (SaveSlotWindow.BlocksGameplayInput) return false;
        var kb = Keyboard.current;
        return kb != null && (kb.fKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame
            || kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame);
    }

    void HoldPause()
    {
        if (holdingPause) return;
        holdingPause = true;
        GamePause.Push();
    }

    void ReleasePause()
    {
        if (!holdingPause) return;
        holdingPause = false;
        GamePause.Pop();
    }
}
