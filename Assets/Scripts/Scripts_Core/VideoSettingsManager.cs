using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VideoSettingsManager : MonoBehaviour
{
    // Inspector에서 연결할 비디오 설정 UI
    [Header("UI")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown screenModeDropdown;
    [SerializeField] private Toggle vSyncToggle;
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private TMP_Text brightnessValueText;
    [SerializeField] private TMP_Dropdown frameRateDropdown;
    [SerializeField] private TMP_Dropdown graphicsQualityDropdown;

    // 각 Dropdown 옵션의 표시 순서와 동일한 실제 설정값
    private static readonly int[] Widths = { 1920, 1600, 1280 };
    private static readonly int[] Heights = { 1080, 900, 720 };
    private static readonly int[] FrameRates = { 144, 120, 60, 30 };
    private static readonly string[] QualityLevelNames =
    {
        "Ultra", "High", "Medium", "Low"
    };

    // PlayerPrefs에서 사용할 저장 키
    private const string ResolutionKey = "VideoResolution";
    private const string ScreenModeKey = "VideoScreenMode";
    private const string VSyncKey = "VideoVSync";
    private const string BrightnessKey = "VideoBrightness";
    private const string FrameRateKey = "VideoFrameRate";
    private const string GraphicsQualityKey = "VideoGraphicsQuality";

    // 저장된 값이 없을 때 사용할 기본 Dropdown 인덱스
    private const int DefaultResolution = 0;       // 1920 x 1080
    private const int DefaultScreenMode = 0;       // 전체 화면
    private const int DefaultVSync = 1;            // VSync 켜기
    private const float DefaultBrightness = 45f;   // 밝기 표시 45%
    private const int DefaultFrameRate = 0;        // 144 FPS
    private const int DefaultGraphicsQuality = 0;  // 울트라

    private void Start()
    {
        // Inspector 연결이 빠진 경우 씬에서 밝기 UI를 자동으로 찾음
        FindBrightnessUI();

        // 저장된 값을 UI에 복원한 후 실제 게임 설정에도 적용
        LoadSettings();
        ApplyCurrentSettings();
    }

    // Dropdown 또는 Toggle 값이 바뀔 때 호출하여 즉시 적용하고 저장
    public void OnSettingsChanged()
    {
        // 전체 화면 전환 전에 선택값을 먼저 저장
        SaveSettings();
        ApplyCurrentSettings();
    }

    // 밝기 슬라이더는 실제 밝기를 변경하지 않고 숫자 표시와 설정값만 저장
    public void OnBrightnessChanged(float value)
    {
        UpdateBrightnessValueText(value);
        PlayerPrefs.SetFloat(BrightnessKey, value);
        PlayerPrefs.Save();
    }

    // 기본값 버튼에서 호출하여 UI, 실제 설정, 저장값을 모두 초기화
    public void ResetToDefault()
    {
        // 밝기는 다른 UI 참조에서 오류가 나더라도 반드시 먼저 복원
        if (brightnessSlider != null)
        {
            brightnessSlider.SetValueWithoutNotify(DefaultBrightness);
        }
        UpdateBrightnessValueText(DefaultBrightness);
        PlayerPrefs.SetFloat(BrightnessKey, DefaultBrightness);

        // 값을 바꾸는 동안 On Value Changed가 중복 실행되지 않게 함
        resolutionDropdown?.SetValueWithoutNotify(DefaultResolution);
        screenModeDropdown?.SetValueWithoutNotify(DefaultScreenMode);
        vSyncToggle?.SetIsOnWithoutNotify(DefaultVSync == 1);
        frameRateDropdown?.SetValueWithoutNotify(DefaultFrameRate);
        graphicsQualityDropdown?.SetValueWithoutNotify(DefaultGraphicsQuality);

        // 변경된 기본값을 Dropdown 화면에 즉시 표시
        resolutionDropdown?.RefreshShownValue();
        screenModeDropdown?.RefreshShownValue();
        frameRateDropdown?.RefreshShownValue();
        graphicsQualityDropdown?.RefreshShownValue();
        UpdateBrightnessValueText(DefaultBrightness);
        Canvas.ForceUpdateCanvases();

        // 밝기 기본값은 다른 설정 저장 전에 명시적으로 확정
        PlayerPrefs.SetFloat(BrightnessKey, DefaultBrightness);

        // 전체 화면 전환 전에 기본값을 먼저 저장해 전환 중에도 값이 유지되게 함
        SaveSettings();
        ApplyCurrentSettings();

    }

    // 현재 UI에서 선택된 모든 비디오 설정을 실제 게임에 적용
    private void ApplyCurrentSettings()
    {
        ApplyResolutionAndScreenMode();
        ApplyVSync();
        ApplyFrameRate();
        ApplyGraphicsQuality();
    }

    // 선택된 해상도와 화면 모드를 적용
    private void ApplyResolutionAndScreenMode()
    {
        // 잘못된 인덱스가 들어와도 배열 범위를 벗어나지 않도록 제한
        int resolutionIndex = Mathf.Clamp(
            resolutionDropdown.value,
            0,
            Widths.Length - 1
        );

        Screen.SetResolution(
            Widths[resolutionIndex],
            Heights[resolutionIndex],
            GetSelectedScreenMode()
        );
    }

    // 화면 모드 Dropdown 인덱스를 Unity의 FullScreenMode 값으로 변환
    private FullScreenMode GetSelectedScreenMode()
    {
        switch (screenModeDropdown.value)
        {
            case 0:
                return FullScreenMode.ExclusiveFullScreen;
            case 1:
                return FullScreenMode.FullScreenWindow;
            default:
                return FullScreenMode.Windowed;
        }
    }

    // Toggle이 켜져 있으면 VSync를 켜고, 꺼져 있으면 해제
    private void ApplyVSync()
    {
        QualitySettings.vSyncCount = vSyncToggle.isOn ? 1 : 0;
    }

    // 선택된 Dropdown 항목에 대응하는 최대 FPS를 적용
    private void ApplyFrameRate()
    {
        int frameRateIndex = Mathf.Clamp(
            frameRateDropdown.value,
            0,
            FrameRates.Length - 1
        );

        Application.targetFrameRate = FrameRates[frameRateIndex];
    }

    // Dropdown 항목에 대응하는 Unity 품질 단계를 이름으로 찾아 적용
    private void ApplyGraphicsQuality()
    {
        int dropdownIndex = Mathf.Clamp(
            graphicsQualityDropdown.value,
            0,
            QualityLevelNames.Length - 1
        );

        int qualityIndex = System.Array.IndexOf(
            QualitySettings.names,
            QualityLevelNames[dropdownIndex]
        );

        // 해당 이름을 찾지 못하면 현재 품질 단계를 유지
        if (qualityIndex < 0)
        {
            return;
        }

        QualitySettings.SetQualityLevel(qualityIndex, true);
    }

    // 현재 UI의 선택 인덱스를 PlayerPrefs에 저장
    private void SaveSettings()
    {
        if (resolutionDropdown != null)
            PlayerPrefs.SetInt(ResolutionKey, resolutionDropdown.value);
        if (screenModeDropdown != null)
            PlayerPrefs.SetInt(ScreenModeKey, screenModeDropdown.value);
        if (vSyncToggle != null)
            PlayerPrefs.SetInt(VSyncKey, vSyncToggle.isOn ? 1 : 0);
        if (brightnessSlider != null)
        {
            PlayerPrefs.SetFloat(BrightnessKey, brightnessSlider.value);
        }
        if (frameRateDropdown != null)
            PlayerPrefs.SetInt(FrameRateKey, frameRateDropdown.value);
        if (graphicsQualityDropdown != null)
            PlayerPrefs.SetInt(GraphicsQualityKey, graphicsQualityDropdown.value);
        PlayerPrefs.Save();
    }

    // 저장값을 불러와 UI에 표시하며, 저장값이 없으면 기본값 사용
    private void LoadSettings()
    {
        // 이벤트를 호출하지 않고 UI 값만 안전하게 복원
        resolutionDropdown.SetValueWithoutNotify(
            PlayerPrefs.GetInt(ResolutionKey, DefaultResolution)
        );
        screenModeDropdown.SetValueWithoutNotify(
            PlayerPrefs.GetInt(ScreenModeKey, DefaultScreenMode)
        );
        vSyncToggle.SetIsOnWithoutNotify(
            PlayerPrefs.GetInt(VSyncKey, DefaultVSync) == 1
        );
        float brightness = PlayerPrefs.GetFloat(
            BrightnessKey,
            DefaultBrightness
        );
        if (brightnessSlider != null)
        {
            brightnessSlider.SetValueWithoutNotify(brightness);
        }
        frameRateDropdown.SetValueWithoutNotify(
            PlayerPrefs.GetInt(FrameRateKey, DefaultFrameRate)
        );
        graphicsQualityDropdown.SetValueWithoutNotify(
            PlayerPrefs.GetInt(GraphicsQualityKey, DefaultGraphicsQuality)
        );

        // 불러온 값을 Dropdown 화면에 즉시 표시
        resolutionDropdown.RefreshShownValue();
        screenModeDropdown.RefreshShownValue();
        frameRateDropdown.RefreshShownValue();
        graphicsQualityDropdown.RefreshShownValue();
        UpdateBrightnessValueText(brightness);
    }

    // 밝기 값을 반올림하여 퍼센트 형식으로 표시
    private void UpdateBrightnessValueText(float value)
    {
        if (brightnessValueText != null)
        {
            brightnessValueText.text = $"{Mathf.RoundToInt(value)}%";
        }
    }

    // 밝기는 실제 화면 효과 없이 슬라이더 값과 퍼센트 표시만 관리
    private void FindBrightnessUI()
    {
        if (brightnessSlider == null)
        {
            Slider[] sliders = FindObjectsByType<Slider>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

            foreach (Slider slider in sliders)
            {
                if (slider.name == "BrightnessSlider")
                {
                    brightnessSlider = slider;
                    break;
                }
            }
        }

        if (brightnessSlider == null)
        {
            return;
        }

        if (brightnessValueText == null)
        {
            Transform valueTextTransform =
                brightnessSlider.transform.parent.Find("ValueText");

            if (valueTextTransform != null)
            {
                brightnessValueText = valueTextTransform.GetComponent<TMP_Text>();
            }
        }

        // Inspector 이벤트 연결 없이도 숫자가 실시간으로 변경되도록 등록
        brightnessSlider.onValueChanged.RemoveListener(OnBrightnessChanged);
        brightnessSlider.onValueChanged.AddListener(OnBrightnessChanged);
    }
}
