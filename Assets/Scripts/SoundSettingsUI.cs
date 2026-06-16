using UnityEngine;
using UnityEngine.UI;

public class SoundSettingsUI : MonoBehaviour
{
    [Header("Sliders")]
    public Slider bgmSlider;
    public Slider seSlider;
    public Slider voiceSlider;

    [Header("Test Sounds")]
    public AudioClip seTestClip;
    public AudioClip voiceTestClip;

    void Start()
    {
        if (bgmSlider == null || seSlider == null || voiceSlider == null)
        {
            Debug.LogError("SoundSettingsUI : SliderÇ™ñ¢ê›íËÇ≈Ç∑");
            return;
        }

        LoadSettings();
        ApplyAll();

        bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        seSlider.onValueChanged.AddListener(SetSEVolumeAndPlayTest);
        voiceSlider.onValueChanged.AddListener(SetVoiceVolumeAndPlayTest);
    }

    void LoadSettings()
    {
        bgmSlider.value = PlayerPrefs.GetFloat("BGM_VOLUME", 1f);
        seSlider.value = PlayerPrefs.GetFloat("SE_VOLUME", 1f);
        voiceSlider.value = PlayerPrefs.GetFloat("VOICE_VOLUME", 1f);
    }

    void ApplyAll()
    {
        SetBGMVolume(bgmSlider.value);
        AudioManager.Instance.SetSEVolume(seSlider.value);
        AudioManager.Instance.SetVoiceVolume(voiceSlider.value);
    }

    public void SetBGMVolume(float value)
    {
        PlayerPrefs.SetFloat("BGM_VOLUME", value);
        PlayerPrefs.Save();

        AudioManager.Instance.SetBGMVolume(value);
    }

    public void SetSEVolumeAndPlayTest(float value)
    {
        PlayerPrefs.SetFloat("SE_VOLUME", value);
        PlayerPrefs.Save();

        AudioManager.Instance.SetSEVolume(value);

        if (seTestClip != null)
            AudioManager.Instance.PlaySEClip(seTestClip);
    }

    public void SetVoiceVolumeAndPlayTest(float value)
    {
        PlayerPrefs.SetFloat("VOICE_VOLUME", value);
        PlayerPrefs.Save();

        AudioManager.Instance.SetVoiceVolume(value);

        if (voiceTestClip != null)
            AudioManager.Instance.PlayVoiceClip(voiceTestClip);
    }
}