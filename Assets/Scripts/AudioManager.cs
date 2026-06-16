using System;
using System.Collections;
using UnityEngine;

[Serializable]
public class SoundEntry
{
    public string key;
    public AudioClip clip;
}

public class AudioManager : MonoBehaviour
{
    [Header("BGM")]
    public AudioSource bgmSource;

    public AudioClip bgmFanfare;
    public AudioClip bgmMainMenu;
    public AudioClip bgmBattleIntro;
    public AudioClip bgmBattleMain;

    [Header("SE / Voice")]
    public AudioSource seSource;
    public AudioSource voiceSource;

    public static AudioManager Instance;

    public SoundEntry[] sounds;

    float bgmVolume = 1f;
    float seVolume = 1f;
    float voiceVolume = 1f;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetBGMVolume(float value)
    {
        bgmVolume = value;

        if (bgmSource != null)
            bgmSource.volume = bgmVolume;
    }

    public void SetSEVolume(float value)
    {
        seVolume = value;
    }

    public void SetVoiceVolume(float value)
    {
        voiceVolume = value;
    }

    public void PlayBGM(AudioClip clip, bool loop = true)
    {
        if (bgmSource == null || clip == null) return;

        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.volume = bgmVolume;
        bgmSource.Play();
    }

    public void PlayBattleBGM()
    {
        StopAllCoroutines();
        StartCoroutine(PlayBattleSequence());
    }

    IEnumerator PlayBattleSequence()
    {
        if (bgmSource == null) yield break;
        if (bgmBattleIntro == null || bgmBattleMain == null) yield break;

        bgmSource.volume = bgmVolume;
        bgmSource.clip = bgmBattleIntro;
        bgmSource.loop = false;
        bgmSource.Play();

        yield return new WaitForSeconds(bgmBattleIntro.length);

        bgmSource.volume = bgmVolume;
        bgmSource.clip = bgmBattleMain;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void PlaySE(string key)
    {
        if (seSource == null) return;
        if (seVolume <= 0f) return;

        foreach (SoundEntry sound in sounds)
        {
            if (sound.key == key && sound.clip != null)
            {
                seSource.PlayOneShot(sound.clip, seVolume);
                return;
            }
        }

        Debug.LogWarning($"SE not found: {key}");
    }

    public void PlaySEClip(AudioClip clip)
    {
        if (seSource == null) return;
        if (clip == null) return;
        if (seVolume <= 0f) return;

        seSource.Stop();
        seSource.PlayOneShot(clip, seVolume);
    }

    public void PlayVoiceClip(AudioClip clip)
    {
        if (voiceSource == null) return;
        if (clip == null) return;
        if (voiceVolume <= 0f) return;

        voiceSource.Stop();
        voiceSource.PlayOneShot(clip, voiceVolume);
    }

}