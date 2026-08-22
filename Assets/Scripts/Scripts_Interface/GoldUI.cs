using UnityEngine;
using UnityEngine.UI;

// 좌상단 HUD의 골드(재화) 표시. 아직 재화를 적립/차감하는 시스템이 따로 없어서
// 지금은 0에서 시작하는 표시만 담당합니다. 나중에 재화 시스템이 생기면 AddGold/SetGold를 연결하세요.
public class GoldUI : MonoBehaviour
{
    [Header("연결")]
    public Text goldText;

    private int currentGold = 0;

    void Start()
    {
        UpdateDisplay();
    }

    public void SetGold(int amount)
    {
        currentGold = amount;
        UpdateDisplay();
    }

    public void AddGold(int amount)
    {
        currentGold += amount;
        UpdateDisplay();
    }

    void UpdateDisplay()
    {
        if (goldText != null) goldText.text = currentGold.ToString();
    }
}
