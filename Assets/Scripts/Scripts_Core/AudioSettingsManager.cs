using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;

public class AudioSettingsManager : MonoBehaviour
{
    private const string MasterVolumeParameter = "MasterVolume";
    private const string BgmVolumeParameter = "BGMVolume";
    private const string SfxVolumeParameter = "SFXVolume";

    private const string MasterVolumeKey = "MasterVolume";
    private const string BgmVolumeKey = "BGMVolume";
    private const string SfxVolumeKey = "SFXVolume";

    private const float DefaultMasterVolume = 100f;
    private const float DefaultBgmVolume = 80f;
    private const float DefaultSfxVolume = 80f;
    private const float MinimumDecibels = -80f;

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Volume Sliders (0-100)")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider bgmVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    [Header("Volume Value Texts")]
    [SerializeField] private TMP_Text masterVolumeText;
    [SerializeField] private TMP_Text bgmVolumeText;
    [SerializeField] private TMP_Text sfxVolumeText;

    private void Start()
    {
        FindVolumeTexts();
        LoadSettings();
    }

    private void OnDisable()
    {
        PlayerPrefs.Save();
    }

    public void SetMasterVolume(float value)
    {
        SetMixerVolume(MasterVolumeParameter, value);
        UpdateVolumeText(masterVolumeText, value);
        SaveVolume(MasterVolumeKey, value);
    }

    public void SetBgmVolume(float value)
    {
        SetMixerVolume(BgmVolumeParameter, value);
        UpdateVolumeText(bgmVolumeText, value);
        SaveVolume(BgmVolumeKey, value);
    }

    public void SetSfxVolume(float value)
    {
        SetMixerVolume(SfxVolumeParameter, value);
        UpdateVolumeText(sfxVolumeText, value);
        SaveVolume(SfxVolumeKey, value);
    }

    public void ResetToDefaults()
    {
        ApplySettings(
            DefaultMasterVolume,
            DefaultBgmVolume,
            DefaultSfxVolume);

        PlayerPrefs.Save();
    }

    private void LoadSettings()
    {
        float masterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, DefaultMasterVolume);
        float bgmVolume = PlayerPrefs.GetFloat(BgmVolumeKey, DefaultBgmVolume);
        float sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, DefaultSfxVolume);

        ApplySettings(masterVolume, bgmVolume, sfxVolume);
    }

    private void ApplySettings(float masterVolume, float bgmVolume, float sfxVolume)
    {
        masterVolumeSlider.SetValueWithoutNotify(masterVolume);
        bgmVolumeSlider.SetValueWithoutNotify(bgmVolume);
        sfxVolumeSlider.SetValueWithoutNotify(sfxVolume);

        SetMasterVolume(masterVolume);
        SetBgmVolume(bgmVolume);
        SetSfxVolume(sfxVolume);
    }

    private void SetMixerVolume(string parameterName, float sliderValue)
    {
        float normalizedVolume = Mathf.Clamp01(sliderValue / 100f);
        float decibels = normalizedVolume <= 0f
            ? MinimumDecibels
            : Mathf.Log10(normalizedVolume) * 20f;

        audioMixer.SetFloat(parameterName, decibels);
    }

    private static void SaveVolume(string key, float value)
    {
        PlayerPrefs.SetFloat(key, value);
        PlayerPrefs.Save();
    }

    private static void UpdateVolumeText(TMP_Text volumeText, float value)
    {
        if (volumeText != null)
        {
            volumeText.text = Mathf.RoundToInt(value).ToString();
        }
    }

    private void FindVolumeTexts()
    {
        masterVolumeText ??= FindValueText(masterVolumeSlider);
        bgmVolumeText ??= FindValueText(bgmVolumeSlider);
        sfxVolumeText ??= FindValueText(sfxVolumeSlider);
    }

    private static TMP_Text FindValueText(Slider slider)
    {
        Transform valueTextTransform = slider.transform.parent.Find("ValueText");
        return valueTextTransform != null
            ? valueTextTransform.GetComponent<TMP_Text>()
            : null;
    }
}
