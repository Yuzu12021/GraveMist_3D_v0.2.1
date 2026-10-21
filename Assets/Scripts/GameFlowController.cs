using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class GameFlowController : MonoBehaviour
{
    public TextMeshProUGUI winnerText;

    [Header("Character Select UI")]
    public GameObject settingsPanel;
    public GameObject settingsButton;
    public GameObject[] playerSlots;

    public Image characterPreviewImage;
    public Image characterNameImage; // ← 変更（Text → Image）
    public TextMeshProUGUI selectingPlayerText;

    public Image[] playerSlotIcons;

    [Header("Character Data")]
    public Sprite[] characterSprites;      // 立ち絵（中央）
    public Sprite[] characterIcons;        // 上部スロット用
    public Sprite[] characterNameSprites;

    [Header("Preview Animation")]
    public float previewAnimDuration = 0.18f;
    public float previewMinScale = 0.85f;
    public float previewMaxScale = 1.08f;
    [Header("Character Voice")]
    public AudioClip[] characterSelectVoices;
    public int currentCharacter = 0;

    [Header("Settings Panels")]
    public GameObject settingsMenuPanel;
    public GameObject playerCountPanel;
    public GameObject soundSettingsPanel;

    [Header("Volume Sliders (0 - 1)")]
    [SerializeField] private Slider bgmVolumeSlider;
    [SerializeField] private Slider seVolumeSlider;
    [SerializeField] private Slider voiceVolumeSlider;

    [Header("Volume Preview")]
    [SerializeField] private AudioClip seVolumePreviewClip;
    [SerializeField] private AudioClip voiceVolumePreviewClip;

    [Header("Cancel Button")]
    [SerializeField] private GameObject cancelButton;

    void UpdateCancelButton()
    {
        if (cancelButton != null)
            cancelButton.SetActive(selectingPlayer > 0);
    }

    // スライダーを離したときだけプレビューする。OnValueChangedでは再生しない。
    public void OnSEVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetSEVolume(value);
    }

    public void OnVoiceVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetVoiceVolume(value);
    }

    private void InitializeVolumeSliders()
    {
        // Sliderの表示とAudioManagerの音量を揃える。初期化時の試聴はしない。
        if (AudioManager.Instance == null) return;

        if (bgmVolumeSlider != null)
        {
            bgmVolumeSlider.SetValueWithoutNotify(AudioManager.Instance.BGMVolume);
            bgmVolumeSlider.onValueChanged.AddListener(AudioManager.Instance.SetBGMVolume);
        }
        if (seVolumeSlider != null)
        {
            seVolumeSlider.SetValueWithoutNotify(AudioManager.Instance.SEVolume);
            seVolumeSlider.onValueChanged.AddListener(OnSEVolumeChanged);
            AttachVolumeReleaseHandler(seVolumeSlider, true);
        }
        if (voiceVolumeSlider != null)
        {
            voiceVolumeSlider.SetValueWithoutNotify(AudioManager.Instance.VoiceVolume);
            voiceVolumeSlider.onValueChanged.AddListener(OnVoiceVolumeChanged);
            AttachVolumeReleaseHandler(voiceVolumeSlider, false);
        }
    }

    private void AttachVolumeReleaseHandler(Slider slider, bool isSE)
    {
        VolumeSliderReleaseHandler handler = slider.GetComponent<VolumeSliderReleaseHandler>();
        if (handler == null)
            handler = slider.gameObject.AddComponent<VolumeSliderReleaseHandler>();

        handler.Configure(slider, () =>
        {
            if (AudioManager.Instance == null) return;
            if (isSE)
                AudioManager.Instance.PlaySEClip(seVolumePreviewClip);
            else
            {
                AudioClip preview = voiceVolumePreviewClip;
                if (preview == null && characterSelectVoices != null &&
                    currentCharacter >= 0 && currentCharacter < characterSelectVoices.Length)
                    preview = characterSelectVoices[currentCharacter];
                AudioManager.Instance.PlayVoiceClip(preview);
            }
        });
    }

    int selectingPlayer = 0;
    int[] selectedCharacters = new int[4];

    Coroutine previewAnimCoroutine;
    bool initializedPreview = false;

    void Start()
    {
        // キャラクター選択画面に入ったときの初期人数
        if (SceneManager.GetActiveScene().name == "CharacterSelectScene")
            GameSession.PlayerCount = 2;

        if (winnerText != null)
        {
            int w = GameSession.WinnerIndex;
            winnerText.text = (w < 0) ? "Winner: ?" : $"{w + 1}P の勝利！";

            AudioManager.Instance.PlayBGM(AudioManager.Instance.bgmFanfare, false);
        }
        else
        {
            AudioManager.Instance.PlayBGM(AudioManager.Instance.bgmMainMenu, true);
        }

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        // SettingButtonは表示
        if (settingsButton != null)
            settingsButton.SetActive(true);

        // 念のため子Panelも全部OFF
        if (settingsMenuPanel != null)
            settingsMenuPanel.SetActive(false);

        if (playerCountPanel != null)
            playerCountPanel.SetActive(false);

        if (soundSettingsPanel != null)
            soundSettingsPanel.SetActive(false);

        InitializeVolumeSliders();
        UpdatePlayerSlots();
        UpdateSelectingPlayerUI();
        ResetPlayerSlotIcons();
        UpdateCharacterPreviewImmediate();
        UpdateCancelButton();
    }

    void ResetPlayerSlotIcons()
    {
        if (playerSlotIcons == null) return;

        for (int i = 0; i < playerSlotIcons.Length; i++)
        {
            if (playerSlotIcons[i] != null)
            {
                playerSlotIcons[i].sprite = null;
                playerSlotIcons[i].enabled = false;
            }
        }
    }

    public void SetCurrentCharacter(int index)
    {
        if (index < 0 || index >= characterSprites.Length) return;
        if (index == currentCharacter && initializedPreview) return;

        currentCharacter = index;
        UpdateCharacterPreviewAnimated();
    }

    void UpdateCharacterPreviewImmediate()
    {
        if (characterPreviewImage != null &&
            characterSprites != null &&
            currentCharacter >= 0 &&
            currentCharacter < characterSprites.Length)
        {
            characterPreviewImage.sprite = characterSprites[currentCharacter];
            characterPreviewImage.rectTransform.localScale = Vector3.one;
        }

        // 名前画像更新
        if (characterNameImage != null &&
            characterNameSprites != null &&
            currentCharacter >= 0 &&
            currentCharacter < characterNameSprites.Length)
        {
            characterNameImage.sprite = characterNameSprites[currentCharacter];
        }

        initializedPreview = true;
        UpdateCharacterNameImage();
    }
    void UpdateCharacterNameImage()
    {
        if (characterNameImage == null) return;
        if (characterNameSprites == null) return;
        if (currentCharacter < 0 || currentCharacter >= characterNameSprites.Length) return;

        characterNameImage.sprite = characterNameSprites[currentCharacter];
        characterNameImage.enabled = true;
    }
    void UpdateCharacterPreviewAnimated()
    {
        // 名前画像更新
        if (characterNameImage != null &&
            characterNameSprites != null &&
            currentCharacter >= 0 &&
            currentCharacter < characterNameSprites.Length)
        {
            characterNameImage.sprite = characterNameSprites[currentCharacter];
        }

        if (characterPreviewImage == null ||
            characterSprites == null ||
            currentCharacter < 0 ||
            currentCharacter >= characterSprites.Length)
        {
            return;
        }

        if (!initializedPreview || characterPreviewImage.sprite == null)
        {
            UpdateCharacterPreviewImmediate();
            return;
        }

        if (previewAnimCoroutine != null)
            StopCoroutine(previewAnimCoroutine);

        previewAnimCoroutine = StartCoroutine(
            PlayPreviewChangeAnimation(characterSprites[currentCharacter])
        );
        UpdateCharacterNameImage();
    }

    IEnumerator PlayPreviewChangeAnimation(Sprite nextSprite)
    {
        RectTransform rt = characterPreviewImage.rectTransform;

        Vector3 startScale = rt.localScale;
        Vector3 shrinkScale = Vector3.one * previewMinScale;
        Vector3 overshootScale = Vector3.one * previewMaxScale;
        Vector3 normalScale = Vector3.one;

        float halfDuration = previewAnimDuration * 0.5f;
        float elapsed = 0f;

        // 1) 縮小
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            rt.localScale = Vector3.Lerp(startScale, shrinkScale, t);
            yield return null;
        }

        rt.localScale = shrinkScale;

        // 2) 切り替え
        characterPreviewImage.sprite = nextSprite;

        // 3) 拡大→戻す
        elapsed = 0f;
        rt.localScale = overshootScale;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            rt.localScale = Vector3.Lerp(overshootScale, normalScale, t);
            yield return null;
        }

        rt.localScale = normalScale;
        previewAnimCoroutine = null;
    }

    void UpdateSelectingPlayerUI()
    {
        if (selectingPlayerText != null)
            selectingPlayerText.text = $"{selectingPlayer + 1}P キャラクター選択";
    }

    void UpdatePlayerSlots()
    {
        if (playerSlots == null) return;

        for (int i = 0; i < playerSlots.Length; i++)
        {
            if (playerSlots[i] != null)
                playerSlots[i].SetActive(i < GameSession.PlayerCount);
        }
    }

    private void ResetAllCharacterSelections()
    {
        // 選択プレイヤーを1Pに戻す
        selectingPlayer = 0;

        // 全プレイヤーの選択データを解除
        System.Array.Clear(
            selectedCharacters,
            0,
            selectedCharacters.Length
        );

        // 全プレイヤーの確定アイコンを消す
        ResetPlayerSlotIcons();

        // 「1P キャラクター選択」に表示を戻す
        UpdateSelectingPlayerUI();

        // 1P選択中なのでキャンセルボタンを隠す
        UpdateCancelButton();
    }
    private void ChangePlayerCount(int newCount)
    {
        // 人数が同じなら何もしない
        if (GameSession.PlayerCount == newCount)
            return;

        // 新しい人数を設定
        GameSession.PlayerCount = newCount;

        // 全員のキャラ選択を解除
        ResetAllCharacterSelections();

        // プレイヤー人数に合わせてスロット表示を更新
        UpdatePlayerSlots();
    }

    public void CancelCharacterSelection()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySE("menu_cancel");

        // 1P選択中ならモード選択画面へ戻る
        if (selectingPlayer <= 0)
        {
            SceneManager.LoadScene("ModeSelectScene");
            return;
        }

        // 1つ前のプレイヤーへ戻る
        selectingPlayer--;

        // 前のプレイヤーの確定アイコンを消す
        if (playerSlotIcons != null &&
            selectingPlayer < playerSlotIcons.Length &&
            playerSlotIcons[selectingPlayer] != null)
        {
            playerSlotIcons[selectingPlayer].sprite = null;
            playerSlotIcons[selectingPlayer].enabled = false;
        }

        UpdateSelectingPlayerUI();
        UpdateCancelButton();
    }
    public void ConfirmCharacter()
    {
        AudioManager.Instance.PlaySE("menu_enter");

        if (selectingPlayer >= GameSession.PlayerCount) return;
        if (currentCharacter < 0 || currentCharacter >= characterIcons.Length) return;

        selectedCharacters[selectingPlayer] = currentCharacter;

        if (playerSlotIcons != null &&
            selectingPlayer < playerSlotIcons.Length &&
            playerSlotIcons[selectingPlayer] != null)
        {
            playerSlotIcons[selectingPlayer].sprite = characterIcons[currentCharacter];
            playerSlotIcons[selectingPlayer].enabled = true;
        }

        if (characterSelectVoices != null &&
    currentCharacter >= 0 &&
    currentCharacter < characterSelectVoices.Length)
        {
            AudioManager.Instance.PlayVoiceClip(characterSelectVoices[currentCharacter]);
        }

        selectingPlayer++;

        if (selectingPlayer >= GameSession.PlayerCount)
        {
            StartBattle();
        }
        else
        {
            UpdateSelectingPlayerUI();
            UpdateCancelButton();
        }
    }
    void StartBattle()
    {
        for (int i = 0; i < GameSession.PlayerCount; i++)
        {
            Debug.Log($"[CharacterSelect] {i + 1}P selectedCharacters = {selectedCharacters[i]}");
        }

        GameSession.PlayerCharacters = (int[])selectedCharacters.Clone();
        SceneManager.LoadScene("MainScene");
    }

    public void StartGame()
    {
        AudioManager.Instance.PlaySE("menu_enter");
        SceneManager.LoadScene("ModeSelectScene");
    }

    public void SelectClassic()
    {
        AudioManager.Instance.PlaySE("menu_enter");
        SceneManager.LoadScene("CharacterSelectScene");
    }

    public void SelectBasic()
    {
        Debug.Log("Basicモードは未実装");
    }

    public void BackToTitle()
    {
        AudioManager.Instance.PlaySE("menu_cancel");
        SceneManager.LoadScene("StartScene");
    }

    public void OpenSettings()
    {
        AudioManager.Instance.PlaySE("menu_pause");

        // Settings全体を表示
        if (settingsPanel != null)
            settingsPanel.SetActive(true);

        // SettingButtonを隠す
        if (settingsButton != null)
            settingsButton.SetActive(false);

        // 最初はSettingMenuPanelだけ表示
        ShowSettingsMenu();
    }

    public void CloseSettings()
    {
        AudioManager.Instance.PlaySE("menu_cancel");

        // Settings全体を閉じる
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        // SettingButtonを再表示
        if (settingsButton != null)
            settingsButton.SetActive(true);

        // 念のため内部Panelもリセット
        if (settingsMenuPanel != null)
            settingsMenuPanel.SetActive(false);

        if (playerCountPanel != null)
            playerCountPanel.SetActive(false);

        if (soundSettingsPanel != null)
            soundSettingsPanel.SetActive(false);
    }

    public void Set2Players()
    {
        ChangePlayerCount(2);
        ShowSettingsMenu();
    }

    public void Set3Players()
    {
        ChangePlayerCount(3);
        ShowSettingsMenu();
    }

    public void Set4Players()
    {
        ChangePlayerCount(4);
        ShowSettingsMenu();
    }

    void ShowSettingsMenu()
    {
        if (settingsMenuPanel != null)
            settingsMenuPanel.SetActive(true);

        if (playerCountPanel != null)
            playerCountPanel.SetActive(false);

        if (soundSettingsPanel != null)
            soundSettingsPanel.SetActive(false);
    }

    public void OpenPlayerCountSettings()
    {
        AudioManager.Instance.PlaySE("menu_enter");

        if (settingsMenuPanel != null)
            settingsMenuPanel.SetActive(false);

        if (playerCountPanel != null)
            playerCountPanel.SetActive(true);

        if (soundSettingsPanel != null)
            soundSettingsPanel.SetActive(false);
    }

    public void OpenSoundSettings()
    {
        AudioManager.Instance.PlaySE("menu_enter");

        if (settingsMenuPanel != null)
            settingsMenuPanel.SetActive(false);

        if (playerCountPanel != null)
            playerCountPanel.SetActive(false);

        if (soundSettingsPanel != null)
            soundSettingsPanel.SetActive(true);
    }

    public void BackToSettingsMenu()
    {
        AudioManager.Instance.PlaySE("menu_cancel");

        ShowSettingsMenu();
    }
}

// Sliderの操作終了時に1回だけ試聴。ドラッグと通常クリックに対応。
public class VolumeSliderReleaseHandler : MonoBehaviour,
    UnityEngine.EventSystems.IPointerDownHandler,
    UnityEngine.EventSystems.IPointerUpHandler,
    UnityEngine.EventSystems.IBeginDragHandler,
    UnityEngine.EventSystems.IEndDragHandler
{
    private Slider slider;
    private System.Action onReleased;
    private bool pressed;
    private float startingValue;

    public void Configure(Slider targetSlider, System.Action callback)
    {
        slider = targetSlider;
        onReleased = callback;
    }

    public void OnPointerDown(UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (slider == null || !slider.interactable) return;
        pressed = true;
        startingValue = slider.value;
    }

    public void OnBeginDrag(UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (slider == null || !slider.interactable) return;
        if (!pressed)
        {
            pressed = true;
            startingValue = slider.value;
        }
    }

    public void OnEndDrag(UnityEngine.EventSystems.PointerEventData eventData)
    {
        FinishInteraction();
    }

    public void OnPointerUp(UnityEngine.EventSystems.PointerEventData eventData)
    {
        FinishInteraction();
    }

    private void Update()
    {
        // PointerUpがUIに届かないケース（UI外でマウスを離す等）への保険。
        if (pressed && Input.GetMouseButtonUp(0))
            FinishInteraction();
    }

    private void FinishInteraction()
    {
        if (!pressed) return;
        pressed = false;
        if (slider != null && slider.interactable &&
            !Mathf.Approximately(startingValue, slider.value))
            onReleased?.Invoke();
    }

    private void OnDisable() { pressed = false; }
}
