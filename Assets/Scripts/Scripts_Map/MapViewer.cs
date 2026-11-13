using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 지도 이미지를 보여주고 확대/이동하는 뷰어. (Viewport 오브젝트에 붙임)
//  Viewport(이 스크립트 + RectMask2D) > Content(확대/이동되는 부분) > 지도 이미지
// 조작: 마우스 휠 = 확대/축소, 드래그 또는 WASD/방향키 = 이동
public class MapViewer : MonoBehaviour, IDragHandler, IScrollHandler
{
    public RectTransform viewport;
    public RectTransform content;
    public Image mapImage;

    [Header("조작")]
    public float maxZoom = 6f;        // 처음 크기 대비 최대 배율
    public float wheelStep = 1.15f;   // 휠 한 칸당 배율
    public float keyPanSpeed = 600f;  // WASD/방향키 이동 속도
    public bool keyPanEnabled = true; // 핀 위치를 고를 때는 끌 예정

    private float zoom = 1f; // 1 = 처음 크기
    private Canvas rootCanvas;

    private float ScaleFactor
    {
        get
        {
            if (rootCanvas == null) rootCanvas = GetComponentInParent<Canvas>().rootCanvas;
            return rootCanvas != null ? rootCanvas.scaleFactor : 1f;
        }
    }

    // fitHeight가 false면 전체가 한 화면에 보이게, true면 높이를 맞추고 좌우로 이동하게 (긴 지도용)
    public void SetMap(Sprite sprite, bool fitHeight)
    {
        mapImage.sprite = sprite;

        Vector2 view = viewport.rect.size;
        Vector2 src = sprite.rect.size;
        float scale = fitHeight ? view.y / src.y : Mathf.Min(view.x / src.x, view.y / src.y);

        content.sizeDelta = src * scale;
        zoom = 1f;
        // 긴 지도는 왼쪽 끝(시작 위치)부터 보이게. 아래 ApplyView가 범위 안으로 맞춰 줌
        content.anchoredPosition = new Vector2(fitHeight ? -1e6f : 0f, 0f);
        ApplyView();
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || !keyPanEnabled) return;

        Vector2 dir = Vector2.zero;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) dir.x -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) dir.x += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) dir.y -= 1f;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) dir.y += 1f;
        if (dir == Vector2.zero) return;

        // 보는 쪽으로 가려면 지도는 반대로 움직여야 함.
        // 태블릿이 열려 있으면 Time.timeScale이 0이라서 unscaledDeltaTime을 써야 움직임
        content.anchoredPosition -= dir * keyPanSpeed * Time.unscaledDeltaTime;
        ApplyView();
    }

    public void OnDrag(PointerEventData e)
    {
        if (e.button != PointerEventData.InputButton.Left) return;
        content.anchoredPosition += e.delta / ScaleFactor;
        ApplyView();
    }

    public void OnScroll(PointerEventData e)
    {
        if (e.scrollDelta.y == 0f) return;
        ZoomAt(e.scrollDelta.y > 0f ? wheelStep : 1f / wheelStep, e.position);
    }

    // 마우스 아래의 지도 위 점이 그대로 마우스 아래에 있도록 확대/축소 (Canvas가 Screen Space - Overlay라 카메라는 null)
    void ZoomAt(float factor, Vector2 screenPos)
    {
        float newZoom = Mathf.Clamp(zoom * factor, 1f, maxZoom);
        if (Mathf.Approximately(newZoom, zoom)) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, screenPos, null, out Vector2 local);
        Vector2 fromCenter = local - viewport.rect.center;
        Vector2 pointOnMap = (fromCenter - content.anchoredPosition) / zoom;

        zoom = newZoom;
        content.anchoredPosition = fromCenter - pointOnMap * zoom;
        ApplyView();
    }

    // 배율을 적용하고, 지도가 창 밖으로 너무 벗어나지 않게 위치를 맞춤
    void ApplyView()
    {
        zoom = Mathf.Clamp(zoom, 1f, maxZoom);
        content.localScale = new Vector3(zoom, zoom, 1f);

        Vector2 over = (content.sizeDelta * zoom - viewport.rect.size) * 0.5f;
        float maxX = Mathf.Max(0f, over.x);
        float maxY = Mathf.Max(0f, over.y);

        Vector2 p = content.anchoredPosition;
        p.x = Mathf.Clamp(p.x, -maxX, maxX);
        p.y = Mathf.Clamp(p.y, -maxY, maxY);
        content.anchoredPosition = p;
    }
}