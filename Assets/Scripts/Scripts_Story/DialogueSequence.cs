using System;
using System.Collections.Generic;
using UnityEngine;

// 한 장면의 대사 목록입니다. 화자와 대사를 순서대로 적으면 DialogueRunner가 대사창으로 차례차례 보여줍니다.
// 화자가 비어 있으면 이름표 없이 설명 문장으로 보여줍니다. "(고개를 끄덕인다.)" 같은 행동 지문도 그냥 대사로 적습니다.
[CreateAssetMenu(fileName = "NewDialogue", menuName = "Dungeon Express/Dialogue Sequence")]
public class DialogueSequence : ScriptableObject
{
    [Serializable]
    public class Line
    {
        public string speaker;
        [TextArea(1, 4)] public string text;

        public Line() { }
        public Line(string speaker, string text)
        {
            this.speaker = speaker;
            this.text = text;
        }
    }

    public List<Line> lines = new List<Line>();

    // "화자: 대사" 형식의 줄들로 채움 (에디터 빌더에서 사용). 콜론이 없는 줄은 화자 없는 설명 문장
    public void SetFromText(string source)
    {
        lines.Clear();
        foreach (string raw in source.Split('\n'))
        {
            string row = raw.Trim();
            if (row.Length == 0) continue;

            int colon = row.IndexOf(':');
            if (colon > 0 && colon < 12) lines.Add(new Line(row.Substring(0, colon).Trim(), row.Substring(colon + 1).Trim()));
            else lines.Add(new Line("", row));
        }
    }
}
