using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WinSceneController : MonoBehaviour
{
    [Header("Result Screen")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Sprite[] playerBackgrounds = new Sprite[4];
    [SerializeField] private Image winnerCharacterImage;
    [SerializeField] private Image winnerCharacterNameImage;
    [SerializeField] private Image winnerNumberImage;
    [SerializeField] private Sprite[] winnerNumberSprites = new Sprite[4];

    [Header("Character Sprites (same order as Character Select)")]
    [SerializeField] private Sprite[] characterPortraits;
    [SerializeField] private Sprite[] characterNameSprites;
    [SerializeField] private Sprite[] characterIcons;

    [Header("Bottom Player Slots (1P to 4P)")]
    [SerializeField] private GameObject[] playerSlotRoots = new GameObject[4];
    [SerializeField] private Image[] playerSlotIcons = new Image[4];

    [Header("Tap to Menu")]
    [SerializeField] private GameObject resultRoot;
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private bool tapAnywhereToOpenMenu = true;

    [Header("Player Slot Rows")]
    [SerializeField] private GameObject topRow;
    [SerializeField] private GameObject bottomRow;

    private bool menuOpened;

    private void Start()
    {
        int winner = GameSession.WinnerIndex;
        bool validWinner = winner >= 0 && winner < GameSession.PlayerCount &&
                           GameSession.PlayerCharacters != null &&
                           winner < GameSession.PlayerCharacters.Length;

        if (!validWinner)
            Debug.LogWarning("[WinScene] WinnerIndex or PlayerCharacters is invalid.");

        SetSprite(backgroundImage, playerBackgrounds, winner);
        SetSprite(winnerNumberImage, winnerNumberSprites, winner);

        if (validWinner)
        {
            int character = GameSession.PlayerCharacters[winner];
            SetSprite(winnerCharacterImage, characterPortraits, character);
            SetSprite(winnerCharacterNameImage, characterNameSprites, character);
        }

        for (int i = 0; i < playerSlotRoots.Length; i++)
        {
            bool active = i < GameSession.PlayerCount;
            if (playerSlotRoots[i] != null)
                playerSlotRoots[i].SetActive(active);
            if (active && GameSession.PlayerCharacters != null &&
                i < GameSession.PlayerCharacters.Length &&
                i < playerSlotIcons.Length)
                SetSprite(playerSlotIcons[i], characterIcons, GameSession.PlayerCharacters[i]);
        }

        ShowResult();
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayBGM(AudioManager.Instance.bgmFanfare, false);
    }

    private static void SetSprite(Image image, Sprite[] sprites, int index)
    {
        if (image == null) return;
        bool valid = sprites != null && index >= 0 &&
                     index < sprites.Length && sprites[index] != null;
        if (valid) image.sprite = sprites[index];
        image.enabled = valid;
    }

    private void Update()
    {
        if (tapAnywhereToOpenMenu && !menuOpened &&
            (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Return) ||
             Input.GetKeyDown(KeyCode.Space)))
            ShowMenu();
        UpdatePlayerSlotLayout();
    }

    public void ShowResult()
    {
        menuOpened = false;
        if (resultRoot != null) resultRoot.SetActive(true);
        if (menuRoot != null) menuRoot.SetActive(false);
    }

    public void ShowMenu()
    {
        menuOpened = true;
        // Keep the result visible behind the menu overlay.
        if (resultRoot != null) resultRoot.SetActive(true);
        if (menuRoot != null) menuRoot.SetActive(true);
    }

    // Same player count and character choices: start a new match.
    public void PlayAgain() => Load("MainScene");
    public void ChooseCharacters() => Load("CharacterSelectScene");
    public void ChooseMode() => Load("ModeSelectScene");
    public void BackToTitle() => Load("StartScene");

    private void Load(string sceneName)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySE("menu_enter");
        SceneManager.LoadScene(sceneName);
    }
    void UpdatePlayerSlotLayout()
    {
        int count = Mathf.Clamp(GameSession.PlayerCount, 2, 4);

        if (topRow != null)
            topRow.SetActive(true);

        if (bottomRow != null)
            bottomRow.SetActive(count >= 3);

        // 4Pの枠は4人プレイのときだけ表示
        if (playerSlotRoots != null && playerSlotRoots.Length >= 4)
        {
            if (playerSlotRoots[2] != null)
                playerSlotRoots[2].SetActive(count >= 3);

            if (playerSlotRoots[3] != null)
                playerSlotRoots[3].SetActive(count >= 4);
        }
    }
}
