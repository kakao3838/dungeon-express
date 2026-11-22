using UnityEngine;

// 구역 하나(상세 지도 한 장)의 데이터
[CreateAssetMenu(menuName = "Dungeon Express/Map Area Data")]
public class MapAreaData : ScriptableObject
{
    public string areaName;   // 상세 지도 제목 ("기본 정글" 등)
    public Sprite detailMap;  // 상세 지도 이미지 (비워두면 전체 지도로 대신 보여줌)

    [Header("나중에 채움")]
    public Rect worldBounds;  // 이 구역의 월드 좌표 범위 (플레이어 위치 표시용)
}