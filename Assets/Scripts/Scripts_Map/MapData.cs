using UnityEngine;

// 지도 탭이 읽는 데이터. Resources 폴더에 "MapData"라는 이름으로 두면 자동으로 읽는다.
[CreateAssetMenu(menuName = "Dungeon Express/Map Data")]
public class MapData : ScriptableObject
{
    public Sprite overviewMap;   // 전체 지도 이미지
    public MapAreaData[] areas;  // 구역 목록 (지금은 첫 번째 구역만 사용)
}