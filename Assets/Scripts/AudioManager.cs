using System;
using System.Collections;
using UnityEngine;

using System.Collections.Generic;
using UnityEngine.SceneManagement;

[Serializable]
public class SoundEntry
{
    public string key;
    public AudioClip clip;
}

[Serializable]
public class BGMClipEntry
{
    public string key;
    public AudioClip clip;
}

// "BGMSettings.csv"
[Serializable]
public class BGMSetting
{
    // csvの項目
    public string key;
    public float loopStart;
    public float loopEnd;
    public string group;

    // 追加：時間指定ループを使うか
    public bool useLoop;
}

public class AudioManager : MonoBehaviour
{
    [Header("BGM")]
    public AudioSource bgmSource;

    public AudioClip bgmFanfare;
    public AudioClip bgmMainMenu;
    public AudioClip bgmBattleIntro;
    public AudioClip bgmBattleMain;

    [Header("BGM Settings")]
    public BGMClipEntry[] bgmClips;
    public float bgmFadeOutDuration = 1.5f;  //曲が変わったときのフェードアウト
    
    private readonly Dictionary<string, BGMSetting> bgmSettings
        = new Dictionary<string, BGMSetting>();
    
    private readonly Dictionary<string, AudioClip> bgmClipMap
        = new Dictionary<string, AudioClip>();
    
    private string currentBGMKey = "";
    private Coroutine bgmTransitionCoroutine;
    private Coroutine bgmLoopCoroutine;
    
    [Header("SE / Voice")]
    public AudioSource seSource;
    public AudioSource voiceSource;

    public static AudioManager Instance;

    public SoundEntry[] sounds;

    float bgmVolume = 0.3f;
    float seVolume = 0.5f;
    float voiceVolume = 0.5f;

    public float BGMVolume => bgmVolume;
    public float SEVolume => seVolume;
    public float VoiceVolume => voiceVolume;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (bgmSource != null) bgmSource.volume = bgmVolume;
        if (seSource != null) seSource.volume = 1f;
        if (voiceSource != null) voiceSource.volume = 1f;
    }

    public void SetBGMVolume(float value)
    {
        bgmVolume = Mathf.Clamp01(value);

        if (bgmSource != null)
            bgmSource.volume = bgmVolume;
    }

    public void SetSEVolume(float value)
    {
        seVolume = Mathf.Clamp01(value);
    }

    public void SetVoiceVolume(float value)
    {
        voiceVolume = Mathf.Clamp01(value);
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

    // 時間指定ループ
    IEnumerator LoopBGM(BGMSetting setting)
    {
        while (bgmSource != null &&
               currentBGMKey == setting.key)
        {
            // 再生対象の音源がなくなった場合は終了
            if (bgmSource.clip == null)
                yield break;

            // ループ終了位置に到達した場合、
            // または曲の再生が終了してしまった場合
            if (bgmSource.time >= setting.loopEnd ||
                !bgmSource.isPlaying)
            {
                bgmSource.time = setting.loopStart;
                bgmSource.Play();
            }
        
            yield return null;
        }
    }

    // 曲切り替え時のフェードアウト
    IEnumerator FadeOutBGM()
    {
        if (bgmSource == null || !bgmSource.isPlaying)
            yield break;
    
        float startVolume = bgmSource.volume;
        float elapsed = 0f;
    
        while (elapsed < bgmFadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
    
            float t = Mathf.Clamp01(
                elapsed / bgmFadeOutDuration
            );
    
            bgmSource.volume = Mathf.Lerp(
                startVolume, 0f, t
            );
    
            yield return null;
        }
    
        bgmSource.volume = 0f;
        bgmSource.Stop();
    }

    // 現在のシーンに応じてBGMを選ぶ
    void HandleSceneBGM(string sceneName)
    {
        switch (sceneName)
        {
            case "StartScene":
                ChangeBGM("Title");
                break;
    
            case "ModeSelectScene":
            case "CharacterSelectScene":
                ChangeBGM("Menu");
                break;
    
            case "MainScene":
                PlayRandomMainBGM();
                break;
    
            case "WinScene":
                ChangeBGM("result");
                break;
        }
    }

    // 対戦曲ランダム化
    void PlayRandomMainBGM()
    {
        List<string> candidates = new List<string>();
    
        foreach (BGMSetting setting in bgmSettings.Values)
        {
            if (setting.group == "Battle" &&
                bgmClipMap.ContainsKey(setting.key))
            {
                candidates.Add(setting.key);
            }
        }
    
        if (candidates.Count == 0)
        {
            Debug.LogWarning(
                "対戦曲ランダム化エラー：group に Battle がありません"
            );
            return;
        }
    
        int index = UnityEngine.Random.Range(
            0, candidates.Count
        );
    
        ChangeBGM(candidates[index]);
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

    // 追加分：BGM切り替え処理
    public void ChangeBGM(string key)
    {
        if (bgmSource == null)
            return;
    
        if (!bgmSettings.TryGetValue(key, out BGMSetting setting))
        {
            Debug.LogWarning($"BGM設定がありません: {key}");
            return;
        }
    
        if (!bgmClipMap.TryGetValue(key, out AudioClip clip))
        {
            Debug.LogWarning($"BGM音源がありません: {key}");
            return;
        }
    
        if (clip == null ||
            (setting.useLoop &&
             (setting.loopEnd > clip.length ||
              setting.loopStart >= clip.length)))
            
        // 同じ曲が再生中なら、そのまま継続する
        if (currentBGMKey == key &&
            bgmSource.isPlaying &&
            bgmSource.clip == clip)
            return;
    
        if (bgmTransitionCoroutine != null)
            StopCoroutine(bgmTransitionCoroutine);
    
        bgmTransitionCoroutine = StartCoroutine(
            ChangeBGMCoroutine(key, clip, setting)
        );
    }
    
    IEnumerator ChangeBGMCoroutine(
        string key,
        AudioClip clip,
        BGMSetting setting)
    {
        // 前のループ処理を止める
        if (bgmLoopCoroutine != null)
        {
            StopCoroutine(bgmLoopCoroutine);
            bgmLoopCoroutine = null;
        }
    
        // 前の曲があればフェードアウトする
        if (bgmSource.isPlaying)
        {
            float startVolume = bgmSource.volume;
            float elapsed = 0f;
    
            while (elapsed < bgmFadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
    
                float t = Mathf.Clamp01(
                    elapsed / bgmFadeOutDuration
                );
    
                bgmSource.volume = Mathf.Lerp(
                    startVolume, 0f, t
                );
    
                yield return null;
            }
        }
    
        bgmSource.Stop();
        bgmSource.clip = clip;
        bgmSource.loop = false;
        bgmSource.time = 0f;
    
        currentBGMKey = key;
    
        // フェードインせず通常音量で開始
        bgmSource.volume = bgmVolume;
        bgmSource.Play();
    
        // ループ設定がある曲だけ、時間指定ループを開始
        if (setting.useLoop)
        {
            bgmLoopCoroutine = StartCoroutine(
                LoopBGM(setting)
            );
        }
        else
        {
            bgmLoopCoroutine = null;
        }
    
        bgmTransitionCoroutine = null;

        yield break; // 説明用仮コード
    }
    
    // csvをロード
    void LoadBGMSettings()
    {
        bgmSettings.Clear();
        bgmClipMap.Clear();
    
        TextAsset csv = Resources.Load<TextAsset>(
            "BGMSettings"
        );
    
        if (csv == null)
        {
            Debug.LogError("BGMSettings.csv が見つかりません");
            return;
        }
    
        string[] lines = csv.text.Split(
            new[] { '\r', '\n' },
            System.StringSplitOptions.RemoveEmptyEntries
        );
    
        for (int i = 1; i < lines.Length; i++)
        {
            string[] columns = lines[i].Split(',');
    
            if (columns.Length < 4)
                continue;
    
            string key = columns[0].Trim();
            string startText = columns[1].Trim();
            string endText = columns[2].Trim();
            string group = columns[3].Trim();
            
            bool startEmpty = string.IsNullOrEmpty(startText);
            bool endEmpty = string.IsNullOrEmpty(endText);
            
            // 片方だけ空欄なら設定ミス
            if (startEmpty != endEmpty)
            {
                Debug.LogWarning(
                    $"ループ時間は両方入力するか、両方空欄にしてください: {lines[i]}"
                );
                continue;
            }
            
            bool useLoop = !startEmpty;
            
            float start = 0f;
            float end = 0f;
            
            if (useLoop)
            {
                if (!float.TryParse(
                        startText,
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out start) ||
                    !float.TryParse(
                        endText,
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out end) ||
                    start < 0f ||
                    end <= start)
                {
                    Debug.LogWarning($"BGM設定が不正です: {lines[i]}");
                    continue;
                }
            }
            
            BGMSetting setting = new BGMSetting
            {
                key = key,
                loopStart = start,
                loopEnd = end,
                group = group,
                useLoop = useLoop
            };
            
            if (string.IsNullOrEmpty(setting.key))
                continue;
            
            bgmSettings[setting.key] = setting;
    
            if (string.IsNullOrEmpty(setting.key) ||
                start < 0 || end <= start)
            {
                Debug.LogWarning(
                    $"BGM設定が不正です: {lines[i]}"
                );
                continue;
            }
    
            bgmSettings[setting.key] = setting;
        }
    
        foreach (BGMClipEntry entry in bgmClips)
        {
            if (entry != null &&
                !string.IsNullOrWhiteSpace(entry.key) &&
                entry.clip != null)
            {
                bgmClipMap[entry.key] = entry.clip;
            }
        }
    }
    
    void Start()
    {
        LoadBGMSettings();
    
        SceneManager.sceneLoaded += OnSceneLoaded;
    
        HandleSceneBGM(
            SceneManager.GetActiveScene().name
        );
    }
    
    void OnSceneLoaded(
        UnityEngine.SceneManagement.Scene scene,
        LoadSceneMode mode)
    {
        HandleSceneBGM(scene.name);
    }
    
    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}