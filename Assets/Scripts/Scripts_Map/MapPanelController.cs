using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// 태블릿 메뉴 '지도' 탭. 전체 지도와 상세 지도(구역)를 오가며 보여줍니다.
public class MapPanelController : MonoBehaviour
{
    public MapViewer viewer;
    public TMP_Text areaTitle;
    public GameObject enlargeButton; // 전체 지도에서 보이는 [확대]
    public GameObject shrinkButton;  // 상세 지도에서 보이는 [축소]
    public GameObject noDataText;

    private const string DataResourcePath = "MapData";

    private MapData data;
    private bool inDetail;
    private int areaIndex; // 지금은 항상 첫 번째 구역 (구역 선택은 나중에)

    void OnEnable()
    {
        data = Resources.Load<MapData>(DataResourcePath);
        areaIndex = 0;
        ShowOverview();
    }

    // 상세 지도에서 Esc: 태블릿을 닫지 않고 전체 지도로 한 단계 돌아감.
    // MenuManager가 Esc를 받을 때 직접 물어봄 (Update 실행 순서와 상관없이 같은 결과가 나오게)
    public bool TryHandleEscape()
    {
        if (!isActiveAndEnabled || !inDetail) return false;
        ShowOverview();
        return true;
    }

    // [축소] 버튼, 탭을 열 때
    public void ShowOverview()
    {
        inDetail = false;
        bool hasImage = data != null && data.overviewMap != null;
        bool hasArea = data != null && data.areas != null && data.areas.Length > 0;

        noDataText.SetActive(!hasImage);
        viewer.gameObject.SetActive(hasImage);
        areaTitle.gameObject.SetActive(false);
        enlargeButton.SetActive(hasImage && hasArea);
        shrinkButton.SetActive(false);

        if (hasImage) viewer.SetMap(data.overviewMap, false);
    }

    // [확대] 버튼
    public void ShowDetail()
    {
        if (data == null || data.areas == null || areaIndex >= data.areas.Length) return;
        MapAreaData area = data.areas[areaIndex];
        if (area == null) return;

        // 상세 지도 이미지가 아직 없으면 전체 지도 이미지로 대신 (임시)
        Sprite sprite = area.detailMap != null ? area.detailMap : data.overviewMap;
        if (sprite == null) return;

        inDetail = true;
        areaTitle.text = area.areaName;
        areaTitle.gameObject.SetActive(true);
        enlargeButton.SetActive(false);
        shrinkButton.SetActive(true);
        viewer.SetMap(sprite, true);
    }
}