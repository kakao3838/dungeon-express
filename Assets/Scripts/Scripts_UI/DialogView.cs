using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class DialogView : MonoBehaviour
{
    [Header("프리팹: 대화창 데이터 연결")]
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text bodyText;

    public static DialogView Install(GameObject panel, TMP_Text oldSpeaker, TMP_Text oldBody,
        UnityAction close, bool keepContent = false, bool advance = false)
    {
        if (panel == null) return null;
        DialogView view = panel.GetComponent<DialogView>();
        if (view == null) view = panel.GetComponentInChildren<DialogView>(true);
        if (view == null)
        {
            DialogStyle style = Resources.Load<DialogStyle>("DialogStyle");
            if (style == null || style.dialoguePrefab == null) return null;
            GameObject frame = Instantiate(style.dialoguePrefab, panel.transform, false);
            frame.name = "CommonDialogueFrame";
            view = frame.GetComponent<DialogView>();
            if (view == null) { Destroy(frame); return null; }
        }

        // Keep the controlling panel so scene references, pause and quest cards remain valid.
        if (view.gameObject != panel)
        {
            Image oldBackground = panel.GetComponent<Image>();
            if (oldBackground != null) oldBackground.enabled = false;
            if (!keepContent)
            {
                foreach (Transform child in panel.transform)
                    if (child != view.transform) child.gameObject.SetActive(false);
                RectTransform outer = panel.GetComponent<RectTransform>();
                RectTransform frame = view.GetComponent<RectTransform>();
                if (outer != null && frame != null)
                {
                    outer.anchorMin = new Vector2(0f, 0f);
                    outer.anchorMax = new Vector2(1f, 0f);
                    outer.pivot = new Vector2(0.5f, 0f);
                    outer.anchoredPosition = Vector2.zero;
                    outer.sizeDelta = new Vector2(0f, frame.sizeDelta.y);
                }
            }
        }
        if (oldSpeaker != null && oldSpeaker != view.speakerText && oldSpeaker.transform.IsChildOf(panel.transform))
            oldSpeaker.gameObject.SetActive(false);
        if (oldBody != null && oldBody != view.bodyText && oldBody.transform.IsChildOf(panel.transform))
            oldBody.gameObject.SetActive(false);
        if (keepContent)
        {
            // Old quest frames may retain a separate name box even after their text
            // reference was rebound to the common prefab's speaker text.
            foreach (Transform child in panel.GetComponentsInChildren<Transform>(true))
                if (child.name == "SpeakerBox" && !child.IsChildOf(view.transform) &&
                    !view.transform.IsChildOf(child))
                    child.gameObject.SetActive(false);
            if (oldSpeaker != null && oldSpeaker != view.speakerText && oldSpeaker.transform.IsChildOf(panel.transform))
            {
                Image oldNameBox = oldSpeaker.transform.parent.GetComponent<Image>();
                if (oldNameBox != null) oldNameBox.enabled = false;
            }
            foreach (Button oldButton in panel.GetComponentsInChildren<Button>(true))
                if (!oldButton.transform.IsChildOf(view.transform)) oldButton.gameObject.SetActive(false);
            view.transform.SetAsFirstSibling();
        }
        Transform closeAnchor = view.transform.Find("CloseButtonAnchor/CloseButton");
        Button button = closeAnchor != null ? closeAnchor.GetComponent<Button>() : null;
        if (button != null)
        {
            // Replace the prefab's scene-specific callback with this caller's action.
            button.onClick = new Button.ButtonClickedEvent();
            if (close != null) button.onClick.AddListener(close);
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.text = advance ? "다음" : "닫기";
        }
        view.gameObject.SetActive(true);
        return view;
    }

    // 새 프리팹은 내부 연결을 사용하고, 기존 창은 현재 텍스트 연결로 표시합니다.
    public static void Display(GameObject panel, string speaker, string body,
        TMP_Text existingSpeaker, TMP_Text existingBody)
    {
        if (panel == null) return;
        DialogView view = panel.GetComponent<DialogView>();
        if (view == null) view = panel.GetComponentInChildren<DialogView>(true);
        if (view == null)
        {
            view = panel.AddComponent<DialogView>();
            view.speakerText = existingSpeaker;
            view.bodyText = existingBody;
        }
        if (view.speakerText == null) view.speakerText = existingSpeaker;
        if (view.bodyText == null) view.bodyText = existingBody;
        view.SetContent(speaker, body);
    }

    public void SetSpeaker(string speaker)
    {
        if (speakerText != null)
            speakerText.text = speaker ?? string.Empty;
    }

    public void SetBody(string body)
    {
        if (bodyText != null)
            bodyText.text = body ?? string.Empty;
    }

    public void SetContent(string speaker, string body)
    {
        SetSpeaker(speaker);
        SetBody(body);
    }
}
