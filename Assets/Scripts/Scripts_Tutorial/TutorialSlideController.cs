using System;
using UnityEngine;
using UnityEngine.UI;

public class TutorialSlideController : MonoBehaviour
{
    [Serializable]
    public class Slide
    {
        public GameObject pageRoot;
    }

    [SerializeField] Button previousButton;
    [SerializeField] Button nextButton;
    [SerializeField] Button closeButton;
    [SerializeField] Slide[] slides;

    int currentIndex;

    public bool IsOpen => gameObject.activeInHierarchy;

    void Awake()
    {
        if (previousButton == null) previousButton = FindChildButton("PreviousButton");
        if (nextButton == null) nextButton = FindChildButton("NextButton");
        if (closeButton == null) closeButton = FindChildButton("CloseButton");

        if (previousButton != null) previousButton.onClick.AddListener(ShowPrevious);
        if (nextButton != null) nextButton.onClick.AddListener(ShowNext);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
    }

    Button FindChildButton(string buttonName)
    {
        foreach (var button in GetComponentsInChildren<Button>(true))
            if (button.name == buttonName) return button;
        return null;
    }

    void OnEnable()
    {
        ShowPage(0);
    }

    public void Open()
    {
        gameObject.SetActive(true);
        ShowPage(0);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    void ShowPrevious()
    {
        if (currentIndex > 0)
            ShowPage(currentIndex - 1);
    }

    void ShowNext()
    {
        if (slides == null || slides.Length == 0) return;

        if (currentIndex < slides.Length - 1)
            ShowPage(currentIndex + 1);
    }

    void ShowPage(int index)
    {
        if (slides == null || slides.Length == 0) return;

        currentIndex = Mathf.Clamp(index, 0, slides.Length - 1);
        for (int i = 0; i < slides.Length; i++)
        {
            if (slides[i]?.pageRoot != null)
                slides[i].pageRoot.SetActive(i == currentIndex);
        }

        // Keep both arrows clickable on every page. At an edge, the handler
        // simply does nothing; this avoids a disabled Button state blocking
        // the previous-page click after navigating forward.
    }
}
