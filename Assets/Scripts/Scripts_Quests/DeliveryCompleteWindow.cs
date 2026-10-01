using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 퀘스트를 완료하면 뜨는 "배송 완료" 결과창입니다.
// 의뢰 물품별 품질/등급/보상과 기본 보상, 등급 보너스, 최종 보상을 보여주고 최종 보상을 지갑에 지급합니다.
// 확인 버튼(또는 F키)으로 닫습니다.
public class DeliveryCompleteWindow : MonoBehaviour
{
    [Header("연결")]
    public GameObject panel;
    public RectTransform rowsParent;
    public GameObject rowTemplate;        // 꺼둔 상태의 물품 한 줄 견본
    public TMP_Text baseRewardText;
    public TMP_Text gradeBonusText;
    public TMP_Text finalRewardText;

    [Header("물품 줄 배치 (견본 첫 줄의 위치에서 이 간격으로 아래로 쌓음)")]
    public float rowSpacing = 62f;

    [Header("품질 / 등급 규칙")]
    [Tooltip("품질 변동이 없는 물품의 품질. 품질 시스템이 생기면 실제 값으로 교체")]
    [Range(0, 100)] public int unchangedQuality = 100;
    public int gradeAMinQuality = 90;
    public int gradeBMinQuality = 70;
    [Tooltip("A등급이면 물품 기본 보상에 더해지는 비율 (0.2 = 20%)")]
    public float gradeABonusRate = 0.2f;

    public bool IsOpen => panel != null && panel.activeSelf;

    private readonly List<GameObject> spawnedRows = new List<GameObject>();
    private bool holdingPause;
    private int openedFrame = -1;

    void Awake()
    {
        if (panel != null) panel.SetActive(false);
        if (rowTemplate != null) rowTemplate.SetActive(false);
    }

    void Update()
    {
        // 열린 뒤 F를 누르면 확인 (열린 그 프레임의 F는 무시)
        if (IsOpen && Time.frameCount != openedFrame)
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.fKey.wasPressedThisFrame) Close();
        }
    }

    void OnDestroy()
    {
        ReleasePause(); // 창이 열린 채로 씬이 바뀌어도 시간이 멈춰 있지 않게
    }

    public void Show(QuestData quest)
    {
        if (panel == null || quest == null) return;

        BuildRows(quest, out int baseTotal, out int bonusTotal);

        int finalReward = baseTotal + bonusTotal;
        if (baseRewardText != null) baseRewardText.text = baseTotal.ToString();
        if (gradeBonusText != null) gradeBonusText.text = bonusTotal.ToString();
        if (finalRewardText != null) finalRewardText.text = finalReward.ToString();

        // 최종 보상을 지갑에 지급 (HUD 돈 표시가 자동 갱신됨)
        var wallet = FindFirstObjectByType<PlayerWallet>();
        if (wallet != null && finalReward > 0) wallet.AddGold(finalReward);

        panel.SetActive(true);
        openedFrame = Time.frameCount;
        if (!holdingPause)
        {
            holdingPause = true;
            GamePause.Push();
        }
    }

    // 확인 버튼의 OnClick에 연결됨
    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        ReleasePause();
    }

    void ReleasePause()
    {
        if (!holdingPause) return;
        holdingPause = false;
        GamePause.Pop();
    }

    void BuildRows(QuestData quest, out int baseTotal, out int bonusTotal)
    {
        foreach (var r in spawnedRows) if (r != null) Destroy(r);
        spawnedRows.Clear();

        baseTotal = 0;
        bonusTotal = 0;
        if (rowTemplate == null || rowsParent == null) return;

        // 퀘스트 보수(reward)를 의뢰 물품 개수에 나눠서 물품별 기본 보상으로 씀
        int totalUnits = 0;
        foreach (var req in quest.requiredItems) totalUnits += Mathf.Max(1, req.quantity);
        if (totalUnits == 0) totalUnits = 1;

        Vector2 firstPos = ((RectTransform)rowTemplate.transform).anchoredPosition;
        int index = 0;

        foreach (var req in quest.requiredItems)
        {
            if (req.item == null) continue;

            int units = Mathf.Max(1, req.quantity);
            int itemBase = Mathf.RoundToInt(quest.reward * (units / (float)totalUnits));

            int quality = unchangedQuality;
            bool failed = quality <= 0;
            string grade = quality >= gradeAMinQuality ? "A" : quality >= gradeBMinQuality ? "B" : "C";
            int itemBonus = (!failed && grade == "A") ? Mathf.RoundToInt(itemBase * gradeABonusRate) : 0;
            if (failed) itemBase = 0;

            baseTotal += itemBase;
            bonusTotal += itemBonus;

            var row = Instantiate(rowTemplate, rowsParent);
            row.SetActive(true);
            ((RectTransform)row.transform).anchoredPosition = firstPos + new Vector2(0f, -rowSpacing * index);
            spawnedRows.Add(row);
            index++;

            FillRow(row.transform, req.item.itemName + (units > 1 ? " x" + units : ""), quality, failed, grade, itemBase, itemBonus);
        }
    }

    static void FillRow(Transform row, string itemName, int quality, bool failed, string grade, int reward, int bonus)
    {
        SetText(row, "NameText", itemName);

        var barBg = row.Find("BarBg");
        var fail = row.Find("FailText");
        if (barBg != null) barBg.gameObject.SetActive(!failed);
        if (fail != null) fail.gameObject.SetActive(failed);

        // 품질 바: 칸 전체를 채운 막대의 오른쪽 끝(anchorMax.x)을 품질 비율로 줄여서 표현
        var fill = row.Find("BarBg/BarFill") as RectTransform;
        if (fill != null)
        {
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(Mathf.Clamp01(quality / 100f), 1f);
            fill.offsetMin = fill.offsetMax = Vector2.zero;
        }

        var percent = row.Find("PercentText");
        if (percent != null)
        {
            percent.gameObject.SetActive(!failed);
            SetText(row, "PercentText", quality + "%");
        }

        SetText(row, "GradeText", failed ? "" : grade);
        SetText(row, "RewardText", bonus > 0 ? reward + " <size=70%>+ " + bonus + "</size>" : reward.ToString());
    }

    static void SetText(Transform parent, string childName, string value)
    {
        var t = parent.Find(childName);
        if (t == null) return;
        var tmp = t.GetComponent<TMP_Text>();
        if (tmp != null) tmp.text = value;
    }
}
