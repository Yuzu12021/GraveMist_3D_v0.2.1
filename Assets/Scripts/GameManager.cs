using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class EvolutionGaugeUI
{
    public Image gaugeImage;
}

[System.Serializable]
public class MPGaugeUI
{
    public RectTransform fillMask;
    public TMP_Text valueText;
}
public class GameManager : MonoBehaviour
{
    [Header("Current Player Panel")]
    public GameObject currentPlayerPanel;
    public CanvasGroup currentPlayerPanelCanvasGroup;
    public Image currentPlayerImage;
    public EvolutionGaugeUI currentPlayerEvolutionGauge;
    public Image currentPlayerBGImage;
    public Image currentPlayerNumberImage;

    [Header("Current Player BG Sprites")]
    public Sprite[] currentPlayerBGSprites; // 1P〜4P
    [Header("Current Player Number Sprites")]
    public Sprite[] currentPlayerNumberSprites; // 1P〜4P
    [Header("Current Player Panel Animation")]
    public float currentPlayerFadeDuration = 0.25f;

    [Header("Top Player Bar")]
    public GameObject[] topPlayerRoots;         // PlayerInfo_1 ～ 4
    public Image[] topPlayerNumberImages;       // PlayerNumber
    public Image[] topPlayerIcons;              // Icon
    public EvolutionGaugeUI[] topPlayerEvolutionGauges;

    [Header("Player Number Sprites")]
    public Sprite[] playerNumberSprites;        // 1P, 2P, 3P, 4P の画像

    [Header("Character Data")]
    public Sprite[] characterIcons;             // TopPlayerBar用
    public Sprite[] characterLargeSprites;      // CurrentPlayerPanel用
    public string[] characterNames;             // キャラ名（今は未使用でもOK）


    [Header("Shared Evolution Gauge Sprites (0-3)")]
    public Sprite[] evolutionGaugeSprites = new Sprite[4];

    [Header("UI")]
    public GameObject dragArea;
    public GameObject turnButtons;

    [Header("Grave")]
    public GameObject gravePrefab;
    public int graveCount = 4;

    [Header("Grave Throw")]
    public float spawnHeight = 5f;
    public float baseForce = 7.5f;
    public float downwardForce = 0.8f;
    public float forceJitter = 0.15f;
    public float torquePower = 0.4f;

    public float[] distanceRatios = { 0.25f, 0.45f, 0.7f, 1.0f };
    public float[] sideOffsets = { -0.3f, 0.2f, -0.15f, 0.3f };

    public float spawnSpreadMultiplier = 1.35f;
    public float sideForceMultiplier = 0.55f;
    public float randomSpawnSideRange = 0.08f;
    public float randomSideForceRange = 0.10f;
    public float forwardVarianceMin = 0.92f;
    public float forwardVarianceMax = 1.08f;
    public float forwardFlipTorque = 1.2f;

    [Header("Board / Player")]
    public BoardManager boardManager;
    public GameObject playerPrefab;
    [Header("Layout Control")]
    public Behaviour rightInfoLayoutGroup;

    private readonly List<GameObject> spawnedGraves = new List<GameObject>();
    private readonly List<GameObject> players = new List<GameObject>();
    private readonly List<int> playerPathIndices = new List<int>();
    private readonly List<int> playerStartPathIndices = new List<int>();
    private readonly List<List<MistColor>> playerMists = new List<List<MistColor>>();

    [Header("New Mist Effect Lottery")]
    [SerializeField]
    private List<MistEffectDefinition> mistEffectDefinitions =
    new List<MistEffectDefinition>();

    [SerializeField]
    private string evolutionEffectId = "Evolution";

    private readonly List<MistEffectDefinition> selectedMistEffects =
        new List<MistEffectDefinition>();

    // =========================================================
    // 旧Mist個別データ（移行中の互換用）
    // 新Mistの所持数は playerMistCounts が正本
    // =========================================================
    private readonly List<List<MistData>> playerMistData =
        new List<List<MistData>>();

    private readonly int[] playerEvolutionLevels = new int[4];

    // 新仕様：各色のMist所持上限
    private const int MAX_MIST_PER_COLOR = 10;

    // 新仕様：Mistの色数（赤・青・緑・黄）
    private const int MIST_COLOR_COUNT = 4;

    // 新仕様：プレイヤー別・色別のMist所持数
    // [プレイヤー番号, 色番号]
    private readonly int[,] playerMistCounts =
        new int[4, MIST_COLOR_COUNT];

    // 旧仕様との移行期間中だけ使用（新方式への移行後に削除予定）
    private const int MAX_MIST = 7;
    private const int START_MIST = 3;

    private int currentPlayerIndex = 0;
    private bool isClockwise = false; // false=左回り, true=右回り
    private Coroutine moveCoroutine;
    private Coroutine currentPlayerPanelFadeCoroutine;

    private int totalSteps = 0;
    // New engine: pending +3 movement for the current player's next throw.
    private readonly bool[] enginePending = new bool[4];
    // grave+1: only for the current turn
    private readonly bool[] gravePlusOnePending = new bool[4];
    private int activeGraveCount = 0;

    public bool ActivateGravePlusOne(int playerIndex)
    {
        if (currentState != GameState.Idle ||
            playerIndex != currentPlayerIndex ||
            playerIndex < 0 || playerIndex >= gravePlusOnePending.Length ||
            gravePlusOnePending[playerIndex])
            return false;

        gravePlusOnePending[playerIndex] = true;
        if (mistEffectManager != null)
            mistEffectManager.ShowNewMistStatusIcon("grave+1");
        Debug.Log($"[Grave+1] {playerIndex + 1}P: next throw +1 grave");
        return true;
    }


    public bool ActivateEngine(int playerIndex)
    {
        if (currentState != GameState.Idle ||
            playerIndex != currentPlayerIndex ||
            playerIndex < 0 || playerIndex >= enginePending.Length ||
            enginePending[playerIndex])
        {
            return false;
        }

        enginePending[playerIndex] = true;
        if (mistEffectManager != null)
            mistEffectManager.ShowNewMistStatusIcon("engine");
        Debug.Log("[Engine] Activated for player " + (playerIndex + 1));
        return true;
    }


    private readonly HashSet<GraveController> stoppedGraves =
        new HashSet<GraveController>();

    [Header("MP UI")]
    public MPGaugeUI currentPlayerMPGauge;
    private int[] playerMP = new int[4];
    private bool[] playerMistPlusBuff = new bool[4];
    private bool[] playerCakeBuff = new bool[4];
    private const int MAX_MP = 20;
    private const float MP_GAUGE_MAX_WIDTH = 350f;


    [Header("Used Mist UI")]
    public Image usedMistImage;              // Used_mist
    public Sprite[] usedMistEffectSprites;   // 0=Hole, 1=Plus1, 2=Cake, 3=Uturn

    public Transform currentPlayerMistHolder; // CurrentPlayer_mist
    public GameObject usedMistIconPrefab;     // ImageだけのPrefab

    [Header("Mist Effect")]
    [SerializeField]
    private MistEffectManager mistEffectManager;

    [Header("Evolution Settings")]

    [SerializeField]
    private int mpEvolutionCost = 20;

    public GameObject GetStatusIconPrefab()
    {
        return usedMistIconPrefab;
    }

    public int GetPlayerCount()
    {
        return players.Count;
    }

    public enum GameState
    {
        Idle,
        Shake
    }
    public enum MistColor
    {
        Red = 1,
        Blue = 2,
        Green = 3,
        Yellow = 4,
        Black = 5
    }

    [System.Serializable]
    public class MistData
    {
        public MistColor color;
        public MistEffectType effect;
        public bool isAnalyzed;

        public MistData(
            MistColor color,
            MistEffectType effect
        )
        {
            this.color = color;
            this.effect = effect;
            this.isAnalyzed = false;
        }
    }

    public GameState currentState = GameState.Idle;

    private GameObject CurrentPlayer => players[currentPlayerIndex];

    private int CurrentPathIndex
    {
        get => playerPathIndices[currentPlayerIndex];
        set => playerPathIndices[currentPlayerIndex] = value;
    }


    bool IsStoppedOnOtherPlayer()
    {
        for (int i = 0; i < players.Count; i++)
        {
            if (i == currentPlayerIndex)
                continue;

            if (playerPathIndices[i] == CurrentPathIndex)
            {
                return true;
            }
        }

        return false;
    }
    int GetStoppedPlayerIndex()
    {
        for (int i = 0; i < players.Count; i++)
        {
            if (i == currentPlayerIndex)
                continue;

            if (
                playerPathIndices[i] ==
                CurrentPathIndex
            )
            {
                return i;
            }
        }

        return -1;
    }
    void Awake()
    {
        Physics.gravity = new Vector3(0f, -20f, 0f);
    }

    void Start()
    {

        ValidateGameSessionData();
        InitializePlayerUI();
        InitializeMistEffectLottery();
        CreatePlayers();
        EnterTurnStart();
        RefreshAllPlayerUI();
        ShowCurrentPlayerPanelImmediate();

        AudioManager.Instance.PlayBattleBGM();
    }

    void ValidateGameSessionData()
    {
        GameSession.PlayerCount = Mathf.Clamp(GameSession.PlayerCount, 2, 4);

        if (GameSession.PlayerCharacters == null || GameSession.PlayerCharacters.Length < 4)
        {
            GameSession.PlayerCharacters = new int[4];
        }
    }

    void InitializePlayerUI()
    {
        for (int i = 0; i < playerEvolutionLevels.Length; i++)
        {
            playerEvolutionLevels[i] = 0;
            playerMP[i] = 0;
            playerMistPlusBuff[i] = false;
            playerCakeBuff[i] = false;

            // 新仕様：色別Mist所持数を初期化
            for (int colorIndex = 0; colorIndex < MIST_COLOR_COUNT; colorIndex++)
            {
                playerMistCounts[i, colorIndex] = 0;
            }
        }
    }

    // Deadline build: fixed six effects + Evolution. Keep all definitions in Inspector.
    private static readonly string[] FixedMistEffectIds =
    {
        "trap", "engine", "grave+1",
        "Uturn", "shuffle", "ColorBall", "Evolution"
    };

    void InitializeMistEffectLottery()
    {
        selectedMistEffects.Clear();

        foreach (string id in FixedMistEffectIds)
        {
            MistEffectDefinition effect = mistEffectDefinitions.Find(
                item => item != null && item.effectId == id
            );

            if (effect == null)
            {
                Debug.LogError("[Mist Fixed] Missing effectId in Inspector: " + id);
                continue;
            }

            selectedMistEffects.Add(effect);
        }

        Debug.Log("[Mist Fixed] Loaded " + selectedMistEffects.Count + "/7 effects.");
    }
    public IReadOnlyList<MistEffectDefinition> GetSelectedMistEffects()
    {
        return selectedMistEffects.AsReadOnly();
    }
    void SelectRandomMistEffects(
        MistEffectRank rank,
        int count
    )
    {
        List<MistEffectDefinition> candidates =
            mistEffectDefinitions.FindAll(
                effect => effect != null &&
                          effect.rank == rank &&
                          effect.effectId != evolutionEffectId
            );

        // 同じ効果を重複抽選しない
        for (int i = 0; i < count && candidates.Count > 0; i++)
        {
            int randomIndex =
                Random.Range(0, candidates.Count);

            MistEffectDefinition selected =
                candidates[randomIndex];

            selectedMistEffects.Add(selected);
            candidates.RemoveAt(randomIndex);
        }

        if (candidates.Count == 0 &&
            selectedMistEffects.FindAll(
                effect => effect.rank == rank
            ).Count < count)
        {
            Debug.LogWarning(
                $"[Mist Lottery] {rank}ランクの登録数が不足しています"
            );
        }
    }

    // 指定プレイヤーの指定色Mist所持数を取得
    public int GetMistColorCount(
        int playerIndex,
        MistColor color
    )
    {
        int colorIndex = (int)color - 1;

        if (playerIndex < 0 || playerIndex >= 4)
            return 0;

        if (colorIndex < 0 || colorIndex >= MIST_COLOR_COUNT)
            return 0;

        return playerMistCounts[playerIndex, colorIndex];
    }

    // 新仕様：指定色のMistを獲得
    public void AddMistColor(
        int playerIndex,
        MistColor color,
        int amount
    )
    {
        if (playerIndex < 0 ||
            playerIndex >= playerMistCounts.GetLength(0))
            return;

        int colorIndex = (int)color - 1;

        if (colorIndex < 0 ||
            colorIndex >= MIST_COLOR_COUNT)
            return;

        if (amount <= 0)
            return;

        playerMistCounts[playerIndex, colorIndex] =
            Mathf.Clamp(
                playerMistCounts[playerIndex, colorIndex] + amount,
                0,
                MAX_MIST_PER_COLOR
            );
    }

    // 新仕様：指定色のMistを消費
    public bool TryConsumeMistColor(
        int playerIndex,
        MistColor color,
        int amount
    )
    {
        if (playerIndex < 0 ||
            playerIndex >= playerMistCounts.GetLength(0))
            return false;

        int colorIndex = (int)color - 1;

        if (colorIndex < 0 ||
            colorIndex >= MIST_COLOR_COUNT)
            return false;

        if (amount <= 0)
            return false;

        if (playerMistCounts[playerIndex, colorIndex] < amount)
            return false;

        playerMistCounts[playerIndex, colorIndex] -= amount;

        return true;
    }

    void RefreshAllPlayerUI()
    {
        RefreshTopPlayerBar();
        RefreshCurrentPlayerPanel();
    }

    void RefreshTopPlayerBar()
    {
        int playerCount = GameSession.PlayerCount;

        for (int i = 0; i < topPlayerRoots.Length; i++)
        {
            bool active = i < playerCount;

            if (topPlayerRoots[i] != null)
                topPlayerRoots[i].SetActive(active);

            if (!active) continue;

            int charIndex = GameSession.PlayerCharacters[i];

            if (topPlayerIcons != null &&
                i < topPlayerIcons.Length &&
                topPlayerIcons[i] != null &&
                charIndex >= 0 &&
                charIndex < characterIcons.Length)
            {
                topPlayerIcons[i].sprite = characterIcons[charIndex];
            }

            if (topPlayerNumberImages != null &&
                i < topPlayerNumberImages.Length &&
                topPlayerNumberImages[i] != null &&
                playerNumberSprites != null &&
                i < playerNumberSprites.Length)
            {
                topPlayerNumberImages[i].sprite = playerNumberSprites[i];
            }

            if (topPlayerEvolutionGauges != null &&
                i < topPlayerEvolutionGauges.Length)
            {
                RefreshEvolutionGauge(topPlayerEvolutionGauges[i], i, playerEvolutionLevels[i]);
            }
        }
    }

    void RefreshCurrentPlayerPanel()
    {
        if (players.Count == 0)
            return;

        int playerIndex = currentPlayerIndex;
        int charIndex = GameSession.PlayerCharacters[playerIndex];

        // キャラクター画像
        if (currentPlayerImage != null &&
            charIndex >= 0 &&
            charIndex < characterLargeSprites.Length)
        {
            currentPlayerImage.sprite =
                characterLargeSprites[charIndex];
        }

        // 背景画像
        if (currentPlayerBGImage != null &&
            currentPlayerBGSprites != null &&
            playerIndex >= 0 &&
            playerIndex < currentPlayerBGSprites.Length)
        {
            currentPlayerBGImage.sprite =
                currentPlayerBGSprites[playerIndex];
        }

        // 1P / 2P / 3P / 4P
        if (currentPlayerNumberImage != null &&
            currentPlayerNumberSprites != null &&
            playerIndex >= 0 &&
            playerIndex < currentPlayerNumberSprites.Length)
        {
            currentPlayerNumberImage.sprite =
                currentPlayerNumberSprites[playerIndex];
        }

        // 進化ゲージ
        RefreshEvolutionGauge(
            currentPlayerEvolutionGauge,
            playerIndex,
            playerEvolutionLevels[playerIndex]
        );


        // MPゲージ
        RefreshMPGauge(
            currentPlayerMPGauge,
            playerMP[playerIndex]
        );

        MistInventoryUI[] inventoryUIs =
    FindObjectsByType<MistInventoryUI>(
        FindObjectsInactive.Include,
        FindObjectsSortMode.None
    );

        foreach (MistInventoryUI inventoryUI in inventoryUIs)
        {
            if (inventoryUI != null)
            {
                inventoryUI.Refresh();
            }
        }
    }

    void RefreshEvolutionGauge(EvolutionGaugeUI gaugeUI, int playerIndex, int level)
    {
        if (gaugeUI == null || gaugeUI.gaugeImage == null)
            return;

        int index = Mathf.Clamp(level, 0, 3);
        if (evolutionGaugeSprites == null ||
            index >= evolutionGaugeSprites.Length ||
            evolutionGaugeSprites[index] == null)
        {
            Debug.LogWarning("[EvolutionUI] shinka_" + index + " sprite is not assigned.");
            return;
        }

        gaugeUI.gaugeImage.sprite = evolutionGaugeSprites[index];
        gaugeUI.gaugeImage.enabled = true;
        gaugeUI.gaugeImage.color = Color.white;
    }


    void GoToWinScene(int winnerIndex)
    {
        GameSession.WinnerIndex = winnerIndex;
        SceneManager.LoadScene("WinScene");
    }

    // =========================================================
    // Player生成
    // =========================================================
    void CreatePlayers()
    {
        players.Clear();
        playerPathIndices.Clear();
        playerStartPathIndices.Clear();

        playerMists.Clear();
        playerMistData.Clear();

        Vector2Int[] allStartGrids = new Vector2Int[]
        {
            new Vector2Int(8, 0), // 1P
            new Vector2Int(8, 8), // 2P
            new Vector2Int(0, 8), // 3P
            new Vector2Int(0, 0)  // 4P
        };

        int playerCount = Mathf.Clamp(GameSession.PlayerCount, 2, 4);
        List<Vector2Int> startGrids = new List<Vector2Int>();

        if (playerCount == 2)
        {
            startGrids.Add(allStartGrids[0]);
            startGrids.Add(allStartGrids[2]);
        }
        else
        {
            for (int i = 0; i < playerCount; i++)
                startGrids.Add(allStartGrids[i]);
        }

        for (int i = 0; i < startGrids.Count; i++)
        {
            Vector2Int grid = startGrids[i];

            Vector3 pos = boardManager.GridToWorld(grid.x, grid.y);
            pos.y = 5f;

            float yRot = 0f;

            if (grid == new Vector2Int(8, 0)) yRot = 0f;
            else if (grid == new Vector2Int(8, 8)) yRot = -90f;
            else if (grid == new Vector2Int(0, 8)) yRot = 180f;
            else if (grid == new Vector2Int(0, 0)) yRot = 90f;

            Quaternion rot = Quaternion.Euler(90f, yRot, 0f);

            GameObject p = Instantiate(playerPrefab, pos, rot);

            PlayerController pc = p.GetComponent<PlayerController>();
            players.Add(p);

            int playerIndex = players.Count - 1;

            int characterIndex =
                GameSession.PlayerCharacters[playerIndex];

            pc.SetPlayerData(
                playerIndex,
                characterIndex
            );

            int pathIndex = boardManager.outerPath.IndexOf(grid);
            playerPathIndices.Add(pathIndex);
            playerStartPathIndices.Add(pathIndex);
            playerMists.Add(new List<MistColor>());

            playerMistData.Add(
                new List<MistData>()
            );
        }

        currentPlayerIndex = 0;
        GiveInitialMists();
    }

    void GiveInitialMists()
    {
        for (int playerIndex = 0;
             playerIndex < players.Count;
             playerIndex++)
        {
            for (int i = 0; i < START_MIST; i++)
            {
                // 赤・青・緑・黄からランダム抽選
                MistColor color =
                    (MistColor)Random.Range(1, 5);

                // 新仕様：色別所持数
                AddMistColor(
                    playerIndex,
                    color,
                    1
                );

                // 旧UIとの暫定互換
                playerMists[playerIndex].Add(color);

                playerMistData[playerIndex].Add(
                    new MistData(
                        color,
                        MistEffectType.HoleTrap
                    )
                );
            }
        }
    }

    void GiveMist(int playerIndex, int amount)
    {
        if (playerIndex < 0 ||
            playerIndex >= playerMistCounts.GetLength(0) ||
            playerIndex >= playerMists.Count ||
            playerIndex >= playerMistData.Count)
            return;

        if (amount <= 0)
            return;

        for (int i = 0; i < amount; i++)
        {
            // 赤・青・緑・黄からランダム抽選
            MistColor color = (MistColor)Random.Range(1, 5);

            // 指定色が上限10個なら獲得しない
            if (GetMistColorCount(playerIndex, color) >= MAX_MIST_PER_COLOR)
                continue;

            // 新仕様の所持数を増やす
            AddMistColor(playerIndex, color, 1);
            Debug.Log(
    $"[Mist獲得確認] {playerIndex + 1}P / " +
    $"{color} / " +
    $"現在数={GetMistColorCount(playerIndex, color)}"
);

            // 旧UI用データも一時的に更新
            // ※旧UIの7枠を超えないようにする
            if (playerMists[playerIndex].Count < MAX_MIST)
            {
                playerMists[playerIndex].Add(color);

                playerMistData[playerIndex].Add(
                    new MistData(
                        color,
                        MistEffectType.HoleTrap
                    )
                );
            }
        }
        RefreshAllPlayerUI();
    }


    // =========================================================
    // MistEffectManagerなど外部から使うMist所持データ操作
    // =========================================================

    // 指定プレイヤーが持っているMistの数を取得
    public int GetMistCount(int playerIndex)
    {
        if (
            playerIndex < 0 ||
            playerIndex >= playerMists.Count
        )
        {
            return 0;
        }

        return playerMists[playerIndex].Count;
    }


    // 指定プレイヤーの指定位置にあるMistを取得
    public MistColor GetMist(
        int playerIndex,
        int mistIndex
    )
    {
        return playerMists[playerIndex][mistIndex];
    }

    public void AnalyzeCurrentMists(
    int playerIndex
)
    {
        if (
            playerIndex < 0 ||
            playerIndex >= playerMistData.Count
        )
        {
            return;
        }

        List<MistData> mists =
            playerMistData[playerIndex];

        for (int i = 0; i < mists.Count; i++)
        {
            mists[i].isAnalyzed =
                true;

        }

    }
    // 指定プレイヤーのMistを1個削除
    public void RemoveMist(
    int playerIndex,
    int mistIndex
)
    {
        if (
            playerIndex < 0 ||
            playerIndex >= playerMists.Count ||
            playerIndex >= playerMistData.Count
        )
        {
            return;
        }

        if (
            mistIndex < 0 ||
            mistIndex >= playerMists[playerIndex].Count ||
            mistIndex >= playerMistData[playerIndex].Count
        )
        {
            return;
        }

        // 旧データ
        playerMists[playerIndex].RemoveAt(
            mistIndex
        );

        // 新データ
        playerMistData[playerIndex].RemoveAt(
            mistIndex
        );
    }

    public void SetMistColor(
    int playerIndex,
    int mistIndex,
    MistColor newColor
)
    {
        if (
            playerIndex < 0 ||
            playerIndex >= playerMists.Count ||
            playerIndex >= playerMistData.Count
        )
        {
            return;
        }

        if (
            mistIndex < 0 ||
            mistIndex >= playerMists[playerIndex].Count ||
            mistIndex >= playerMistData[playerIndex].Count
        )
        {
            return;
        }

        // =========================================
        // 旧データの色変更
        // =========================================
        playerMists[playerIndex][mistIndex] =
            newColor;


        // =========================================
        // 新データの色変更
        // =========================================
        playerMistData[playerIndex][mistIndex].color =
            newColor;


        // =========================================
        // Analyzer公開状態は解除
        //
        // ColorBallによって新しいMist内容になったため
        // 未解析状態へ戻す
        // =========================================
        playerMistData[playerIndex][mistIndex].isAnalyzed =
            false;


        RefreshAllPlayerUI();
    }

    // =========================================================
    // UI制御
    // =========================================================
    public void OnTossPressed()
    {
        if (turnButtons != null) turnButtons.SetActive(false);

        FadeCurrentPlayerPanel(false);
        EnterShake();
    }

    public void OnMistPressed()
    {
        MistEffectPanelUI panel =
            FindFirstObjectByType<MistEffectPanelUI>(
                FindObjectsInactive.Include
            );

        if (panel == null)
        {
            Debug.LogWarning("MistEffectPanelUI が見つかりません");
            return;
        }

        panel.Show();
    }

    public void OnMistZoomBackButtonPressed()
    {
        MistEffectPanelUI panel =
            FindFirstObjectByType<MistEffectPanelUI>(
                FindObjectsInactive.Include
            );

        if (panel != null)
            panel.Hide();

    }

    void EnterTurnStart()
    {
        if (turnButtons != null) turnButtons.SetActive(true);
        if (dragArea != null) dragArea.SetActive(false);

        DragAreaController dragController = dragArea != null
            ? dragArea.GetComponent<DragAreaController>()
            : null;

        if (dragController != null)
            dragController.SetDraggable(false);

        currentState = GameState.Idle;
    }

    void EnterShake()
    {
        currentState = GameState.Shake;

        if (turnButtons != null) turnButtons.SetActive(false);
        if (dragArea != null) dragArea.SetActive(true);

        DragAreaController dragController = dragArea != null
            ? dragArea.GetComponent<DragAreaController>()
            : null;

        if (dragController != null)
            dragController.SetDraggable(true);

    }

    string GetMistColorName(
    MistColor color
)
    {
        switch (color)
        {
            case MistColor.Red:
                return "赤";

            case MistColor.Blue:
                return "青";

            case MistColor.Green:
                return "緑";

            case MistColor.Yellow:
                return "黄";

            case MistColor.Black:
                return "黒";

            default:
                return "不明";
        }
    }

    public enum MistEffectRank
    {
        A,
        B,
        C,
        Fixed
    }

    [System.Serializable]
    public class MistEffectDefinition
    {
        public string effectId;
        public string effectName;

        [TextArea(2, 4)]
        public string description;

        public MistEffectRank rank;

        [Header("Mist Cost")]
        [Min(0)] public int redCost;
        [Min(0)] public int blueCost;
        [Min(0)] public int greenCost;
        [Min(0)] public int yellowCost;
    }
    // 新Mist効果のコストを検証（旧Mistリストは参照しない）
    public bool CanAffordMistEffect(
        int playerIndex,
        MistEffectDefinition effect
    )
    {
        if (effect == null ||
            playerIndex < 0 ||
            playerIndex >= playerMistCounts.GetLength(0))
            return false;

        if (effect.redCost < 0 ||
            effect.blueCost < 0 ||
            effect.greenCost < 0 ||
            effect.yellowCost < 0)
            return false;

        return
            GetMistColorCount(playerIndex, MistColor.Red) >= effect.redCost &&
            GetMistColorCount(playerIndex, MistColor.Blue) >= effect.blueCost &&
            GetMistColorCount(playerIndex, MistColor.Green) >= effect.greenCost &&
            GetMistColorCount(playerIndex, MistColor.Yellow) >= effect.yellowCost;
    }

    // 4色分のコストを一括で消費
    public bool TryConsumeMistEffectCost(
        int playerIndex,
        MistEffectDefinition effect
    )
    {
        if (!CanAffordMistEffect(playerIndex, effect))
            return false;

        playerMistCounts[playerIndex, 0] -= effect.redCost;
        playerMistCounts[playerIndex, 1] -= effect.blueCost;
        playerMistCounts[playerIndex, 2] -= effect.greenCost;
        playerMistCounts[playerIndex, 3] -= effect.yellowCost;

        RefreshAllPlayerUI();
        return true;
    }

    // 新Mistの発動窓口。
    // MistEffectManager側が実行不可なら消費しない。
    // 注意：TryExecuteEffectByIdのtrueは効果の完了を保証しないため、
    // 実効果の成否が分かるAPIへ今後移行する。
    public bool TryActivateSelectedMistEffect(MistEffectDefinition effect)
    {
        if (effect == null || !selectedMistEffects.Contains(effect))
            return false;

        if (mistEffectManager == null)
        {
            Debug.LogWarning("[Mist] MistEffectManagerが未設定です");
            return false;
        }

        int playerIndex = currentPlayerIndex;
        if (!CanAffordMistEffect(playerIndex, effect))
            return false;

        // Evolutionのみ、勝利シーンへの遷移前にコストを消費する
        if (effect.effectId == "Evolution")
        {
            if (GetPlayerEvolutionLevel(playerIndex) >= 3)
                return false;

            if (players[playerIndex] == null ||
                players[playerIndex].GetComponent<PlayerController>() == null)
                return false;

            if (!TryConsumeMistEffectCost(playerIndex, effect))
                return false;

            return mistEffectManager.TryExecuteEffectById(
                effect.effectId,
                playerIndex
            );
        }

        // その他のMistは従来どおり
        if (!mistEffectManager.TryExecuteEffectById(effect.effectId, playerIndex))
            return false;

        return TryConsumeMistEffectCost(playerIndex, effect);
    }

    void ShowUsedMistUI(MistColor mist)
    {
        if (usedMistImage == null) return;

        int spriteIndex = (int)mist - 1;

        if (usedMistEffectSprites != null &&
            spriteIndex >= 0 &&
            spriteIndex < usedMistEffectSprites.Length &&
            usedMistEffectSprites[spriteIndex] != null)
        {
            usedMistImage.sprite = usedMistEffectSprites[spriteIndex];
            usedMistImage.enabled = true;
            usedMistImage.color = Color.white;
            usedMistImage.gameObject.SetActive(true);
        }
    }

    void AddCurrentPlayerUsedMistIcon(MistColor mist)
    {
        if (currentPlayerMistHolder == null || usedMistIconPrefab == null) return;

        int spriteIndex = (int)mist - 1;

        if (usedMistEffectSprites == null ||
            spriteIndex < 0 ||
            spriteIndex >= usedMistEffectSprites.Length ||
            usedMistEffectSprites[spriteIndex] == null)
        {
            return;
        }

        GameObject icon = Instantiate(usedMistIconPrefab, currentPlayerMistHolder);
        Image img = icon.GetComponent<Image>();

        if (img != null)
        {
            img.sprite = usedMistEffectSprites[spriteIndex];
            img.enabled = true;
            img.color = Color.white;
        }
    }


    void ActivatePlus1(int playerIndex)
    {
        if (playerMistPlusBuff[playerIndex])
        {
            return;
        }

        playerMistPlusBuff[playerIndex] = true;
    }

    void ActivateCake(int playerIndex)
    {
        playerCakeBuff[playerIndex] = true;
    }

    public void ActivateUturn(int playerIndex)
    {
        isClockwise = !isClockwise;


        for (int i = 0; i < players.Count; i++)
        {
            GameObject p = players[i];

            if (p == null)
                continue;

            if (i < 0 || i >= playerPathIndices.Count)
                continue;

            Vector2Int grid =
                boardManager.outerPath[
                    playerPathIndices[i]
                ];

            Vector3 euler =
                p.transform.eulerAngles;

            if (IsCorner(grid))
            {
                float turnAngle =
                    isClockwise
                        ? -90f
                        : 90f;

                euler.y =
                    RoundTo90(
                        euler.y + turnAngle
                    );
            }
            else
            {
                // 角以外にいる駒は180度回転
                euler.y =
                    RoundTo90(
                        euler.y + 180f
                    );
            }

            p.transform.rotation =
                Quaternion.Euler(
                    90f,
                    euler.y,
                    0f
                );
        }
    }

    // =========================================================
    // Yellow : MagnaTornado（マグナトルネード）
    //
    // 全プレイヤーを
    // 現在位置から前後3マス以内へランダムワープ
    //
    // ・進行方向 isClockwise は変更しない
    // ・ワープ後、見た目だけ現在の進行方向へ合わせる
    // =========================================================
    public void ActivateMagnaTornado()
    {
        if (
            boardManager == null ||
            boardManager.outerPath == null ||
            boardManager.outerPath.Count == 0
        )
        {
            return;
        }

        int pathCount =
            boardManager.outerPath.Count;


        for (int i = 0; i < players.Count; i++)
        {
            if (
                players[i] == null ||
                i >= playerPathIndices.Count
            )
            {
                continue;
            }


            // =========================================
            // 現在地から前後3マス以内
            // =========================================

            int offset =
                Random.Range(-3, 4);


            int currentIndex =
                playerPathIndices[i];

            int targetIndex =
                currentIndex + offset;


            // outerPathをループ
            targetIndex =
                (targetIndex % pathCount + pathCount)
                % pathCount;


            // =========================================
            // 現在地をワープ先へ更新
            // =========================================

            playerPathIndices[i] =
                targetIndex;


            // =========================================
            // 実際の駒をワープ
            // =========================================

            Vector2Int targetGrid =
                boardManager.outerPath[targetIndex];

            Vector3 targetPosition =
                boardManager.GridToWorld(
                    targetGrid.x,
                    targetGrid.y
                );

            targetPosition.y =
                players[i].transform.position.y;

            players[i].transform.position =
                targetPosition;
        }


        // =========================================
        // 全員のワープ完了後、
        // 現在の進行方向に見た目を合わせる
        //
        // ※ isClockwise は変更しない
        // =========================================

        UpdateAllPlayerFacing();
    }

    // =========================================================
    // New Mist : shuffle
    // 全プレイヤーを外周のランダムなマスへワープ
    // ワープ先のマス効果は発動しない
    // 進行方向は変更しない
    // =========================================================
    public bool ActivateShuffle()
    {
        if (boardManager == null ||
    boardManager.outerPath == null ||
    boardManager.outerPath.Count == 0 ||
    players.Count == 0 ||
    currentState != GameState.Idle)
        {
            Debug.LogWarning(
                "[Shuffle] 実行できない状態です"
            );
            return false;
        }

        int pathCount = boardManager.outerPath.Count;

        // 全員分のワープ先を先に決定
        int[] destinations = new int[players.Count];

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i] == null ||
                i >= playerPathIndices.Count)
            {
                return false;
            }

            destinations[i] = Random.Range(0, pathCount);
        }

        // 全プレイヤーをワープ
        for (int i = 0; i < players.Count; i++)
        {
            int targetIndex = destinations[i];

            playerPathIndices[i] = targetIndex;

            Vector2Int grid =
                boardManager.outerPath[targetIndex];

            Vector3 position =
                boardManager.GridToWorld(grid.x, grid.y);

            // 既存の駒の高さを維持
            position.y = players[i].transform.position.y;

            players[i].transform.position = position;
        }

        // 現在の進行方向に合わせて見た目を更新
        UpdateAllPlayerFacing();

        Debug.Log("[Shuffle] 全プレイヤーのワープ完了");

        return true;
    }

    // =========================================================
    // New Mist : ColorBall
    // 発動者以外のMistを、合計数を維持してランダム配分
    // 各色の上限は10
    // =========================================================
    public bool ActivateColorBall(int activatingPlayerIndex)
    {
        if (activatingPlayerIndex < 0 ||
            activatingPlayerIndex >= playerMistCounts.GetLength(0))
            return false;

        const int colorCount = 4;
        const int maxPerColor = 10;

        for (int p = 0; p < playerMistCounts.GetLength(0); p++)
        {
            // 発動者自身は変更しない
            if (p == activatingPlayerIndex)
                continue;

            int total = 0;

            for (int c = 0; c < colorCount; c++)
                total += playerMistCounts[p, c];

            // 全Mistを一度空にしてからランダムに振り分け
            int[] newCounts = new int[colorCount];

            for (int n = 0; n < total; n++)
            {
                int[] available = new int[colorCount];
                int availableCount = 0;

                for (int c = 0; c < colorCount; c++)
                {
                    if (newCounts[c] < maxPerColor)
                        available[availableCount++] = c;
                }

                if (availableCount == 0)
                    break;

                int chosen = available[
                    Random.Range(0, availableCount)
                ];

                newCounts[chosen]++;
            }

            for (int c = 0; c < colorCount; c++)
                playerMistCounts[p, c] = newCounts[c];
        }

        RefreshAllPlayerUI();

        Debug.Log("[ColorBall] 相手全員のMist配分を変更！");
        return true;
    }

    void AddMP(int playerIndex, int amount)
    {
        if (playerIndex < 0 || playerIndex >= playerMP.Length)
            return;

        if (amount <= 0)
            return;

        playerMP[playerIndex] += amount;


        // =========================================
        // MP20ごとに即進化
        // =========================================
        while (playerMP[playerIndex] >= mpEvolutionCost)
        {
            // すでにLevel3ならこれ以上進化しない
            if (GetPlayerEvolutionLevel(playerIndex) >= 3)
                break;

            // MP消費
            playerMP[playerIndex] -= mpEvolutionCost;


            // 即進化
            bool won =
                TryAdvanceEvolution(playerIndex);

            // Level3になったら勝利済みなので終了
            if (won)
                return;
        }

        RefreshAllPlayerUI();
    }
    void RefreshMPGauge(
    MPGaugeUI gaugeUI,
    int mpValue
)
    {
        if (gaugeUI == null)
            return;

        mpValue =
            Mathf.Clamp(
                mpValue,
                0,
                MAX_MP
            );

        // ================================
        // ゲージ幅
        // ================================

        if (gaugeUI.fillMask != null)
        {
            float ratio =
                (float)mpValue / MAX_MP;

            gaugeUI.fillMask.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal,
                MP_GAUGE_MAX_WIDTH * ratio
            );
        }

        // ================================
        // 現在MP表示
        // ================================

        if (gaugeUI.valueText != null)
        {
            gaugeUI.valueText.text =
                mpValue.ToString();
        }
    }
    // =========================================================
    // Drag → 発射
    // =========================================================

    public void OnShakeRelease(Vector3 launchPos, Vector2 dir2D, float dragDistance)
    {
        if (currentState != GameState.Shake) return;
        if (dir2D.sqrMagnitude < 0.0001f) return;

        Vector3 dir3D = new Vector3(dir2D.x, 0f, dir2D.y).normalized;

        SpawnAndLaunchGraves(launchPos, dir3D, dragDistance);
    }

    void SpawnAndLaunchGraves(
    Vector3 launchPos,
    Vector3 dir,
    float dragDistance)
    {
        // =========================================================
        // 投擲SE
        // =========================================================

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySE("grave_toss");
        }
        else
        {
            Debug.LogWarning("AudioManager.Instance が null です");
        }


        // =========================================================
        // 前回のGrave / 投擲状態をリセット
        // =========================================================

        ClearSpawnedGraves();

        totalSteps = 0;
        stoppedGraves.Clear();
        activeGraveCount = graveCount + (gravePlusOnePending[currentPlayerIndex] ? 1 : 0);



        // =========================================================
        // Prefab確認
        // =========================================================

        if (gravePrefab == null)
        {
            Debug.LogError(
                "gravePrefab が GameManager に設定されていません！"
            );

            return;
        }


        // =========================================================
        // 投擲方向
        // =========================================================

        Vector3 forward = dir;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.forward;
        }

        forward.Normalize();

        Vector3 side =
            Vector3.Cross(
                Vector3.up,
                forward
            ).normalized;


        // =========================================================
        // ドラッグ距離 → 基本投擲パワー
        // =========================================================

        float power =
            Mathf.Clamp(
                dragDistance,
                0.5f,
                10f
            );


        // =========================================================
        // Grave生成
        // =========================================================

        for (int i = 0; i < activeGraveCount; i++)
        {
            // -----------------------------------------------------
            // Graveごとの基本設定
            // -----------------------------------------------------

            float ratio =
                GetArrayValueOrDefault(
                    distanceRatios,
                    i,
                    1f
                );

            float sideOffset =
                GetArrayValueOrDefault(
                    sideOffsets,
                    i,
                    0f
                );


            // -----------------------------------------------------
            // 生成位置
            // -----------------------------------------------------

            float randomSpawnSide =
                Random.Range(
                    -randomSpawnSideRange,
                    randomSpawnSideRange
                );

            Vector3 spawnPos =
                launchPos
                + side * (
                    sideOffset * spawnSpreadMultiplier
                    + randomSpawnSide
                )
                + Vector3.up * spawnHeight;


            // -----------------------------------------------------
            // Grave生成
            // -----------------------------------------------------

            GameObject grave =
                Instantiate(
                    gravePrefab,
                    spawnPos,
                    Quaternion.identity
                );


            // -----------------------------------------------------
            // 必須Component取得
            // -----------------------------------------------------

            Rigidbody rb =
                grave.GetComponent<Rigidbody>();

            if (rb == null)
            {
                Debug.LogError(
                    "生成された grave に Rigidbody がありません！"
                );

                return;
            }

            GraveController gc =
                grave.GetComponent<GraveController>();

            if (gc == null)
            {
                Debug.LogError(
                    "生成された grave に GraveController がありません！"
                );

                return;
            }


            // -----------------------------------------------------
            // Rigidbody初期化
            // -----------------------------------------------------

            rb.linearVelocity =
                Vector3.zero;

            rb.angularVelocity =
                Vector3.zero;

            rb.collisionDetectionMode =
                CollisionDetectionMode.ContinuousDynamic;

            rb.interpolation =
                RigidbodyInterpolation.Interpolate;

            rb.linearDamping =
                1.0f;

            rb.angularDamping =
                3.5f;


            // -----------------------------------------------------
            // 初期姿勢
            // -----------------------------------------------------

            rb.rotation =
                Quaternion.LookRotation(
                    forward,
                    Vector3.up
                )
                *
                Quaternion.Euler(
                    -25f,
                    Random.Range(-8f, 8f),
                    Random.Range(-8f, 8f)
                );


            // -----------------------------------------------------
            // 停止イベント登録
            // -----------------------------------------------------

            gc.OnStopped -=
                OnGraveStopped;

            gc.OnStopped +=
                OnGraveStopped;


            // -----------------------------------------------------
            // 発射力
            // -----------------------------------------------------

            float forwardVariance =
                Random.Range(
                    forwardVarianceMin,
                    forwardVarianceMax
                );

            float force =
                baseForce
                * ratio
                * power
                * forwardVariance;

            float randomSide =
                Random.Range(
                    -randomSideForceRange,
                    randomSideForceRange
                );

            Vector3 finalForce =
                forward * (
                    force
                    + Random.Range(
                        -forceJitter,
                        forceJitter
                    )
                )
                + side * (
                    sideOffset * sideForceMultiplier
                    + randomSide
                )
                + Vector3.down * downwardForce;

            rb.AddForce(
                finalForce,
                ForceMode.Impulse
            );


            // -----------------------------------------------------
            // 回転力
            // -----------------------------------------------------

            Vector3 torque =
                side * forwardFlipTorque
                +
                new Vector3(
                    Random.Range(-1f, 1f),
                    Random.Range(-0.3f, 0.3f),
                    Random.Range(-1f, 1f)
                )
                * torquePower;

            rb.AddTorque(
                torque,
                ForceMode.Impulse
            );


            // -----------------------------------------------------
            // 管理対象へ追加
            // -----------------------------------------------------

            spawnedGraves.Add(grave);
        }
    }

    // =========================================================
    // 墓停止処理
    // =========================================================
    void OnGraveStopped(GraveController grave)
    {
        // 同じGraveからの停止通知は1回だけ受け付ける
        if (!stoppedGraves.Add(grave))
            return;

        // 全Graveが停止するまで待つ
        if (stoppedGraves.Count < activeGraveCount)
            return;

        // 投擲が無効なら次のターンへ
        if (IsInvalidGraveThrow())
        {
            HandleInvalidGraveThrow();
            return;
        }

        // 出目を集計
        List<GraveFaceResult> graveResults =
            CalculateGraveResults();

        // 出目確定SE
        PlayFaceDecidedSE();

        // Mistによる移動補正
        if (mistEffectManager != null)
        {
            totalSteps =
                mistEffectManager.ApplyMovementStatuses(
                    currentPlayerIndex,
                    totalSteps
                );
        }

        if (enginePending[currentPlayerIndex])
        {
            totalSteps += 3;
            enginePending[currentPlayerIndex] = false;
        }

        // デバッグ
        Debug.Log(
            $"[GraveResult] " +
            $"出目=[{string.Join(", ", graveResults)}] / " +
            $"最終移動={totalSteps}マス"
        );

        // プレイヤー移動
        StartPlayerMovement();
    }
    bool IsInvalidGraveThrow()
    {
        // 1個でも盤外なら無効
        foreach (GameObject g in spawnedGraves)
        {
            if (g == null)
                continue;

            GraveController gc =
                g.GetComponent<GraveController>();

            if (gc != null && gc.IsOutOfBoard())
            {
                return true;
            }
        }

        // 1組でもGrave同士が重なっていたら無効
        return HasAnyGraveOverlap();
    }
    void HandleInvalidGraveThrow()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySE(
                "grave_miss"
            );
        }

        // 出目・Mistは取得せず次のターンへ
        NextTurn();
    }
    List<GraveFaceResult> CalculateGraveResults()
    {
        totalSteps = 0;
        int backCount = 0;

        List<GraveFaceResult> graveResults =
            new List<GraveFaceResult>();

        foreach (GameObject g in spawnedGraves)
        {
            if (g == null)
                continue;

            GraveController gc =
                g.GetComponent<GraveController>();

            if (gc == null)
                continue;

            GraveFaceResult result =
                gc.GetResult();

            graveResults.Add(result);

            switch (result)
            {
                case GraveFaceResult.Front:

                    totalSteps += 1;
                    break;


                case GraveFaceResult.Back:

                    backCount++;
                    break;


                case GraveFaceResult.Side:

                    totalSteps += 5;

                    GiveMist(
                        currentPlayerIndex,
                        1
                    );

                    AddMP(
                        currentPlayerIndex,
                        1
                    );

                    break;


                case GraveFaceResult.Vertical:

                    totalSteps += 10;

                    GiveMist(
                        currentPlayerIndex,
                        2
                    );

                    AddMP(
                        currentPlayerIndex,
                        2
                    );

                    break;


                case GraveFaceResult.Reverse:

                    totalSteps += 32;

                    AddMP(
                        currentPlayerIndex,
                        20
                    );

                    break;
            }
        }

        // 全て裏なら特殊ルールで8マス
        if (graveResults.Count > 0 &&
    backCount == graveResults.Count)
        {
            totalSteps = 8;
        }

        return graveResults;
    }
    void StartPlayerMovement()
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(
                moveCoroutine
            );
        }

        moveCoroutine =
            StartCoroutine(
                MovePlayerCoroutine(totalSteps)
            );
    }
    bool HasAnyGraveOverlap()
    {
        for (int i = 0; i < spawnedGraves.Count; i++)
        {
            GameObject graveA =
                spawnedGraves[i];

            if (graveA == null)
                continue;

            Collider colliderA =
                graveA.GetComponent<Collider>();

            if (colliderA == null)
                continue;


            for (int j = i + 1; j < spawnedGraves.Count; j++)
            {
                GameObject graveB =
                    spawnedGraves[j];

                if (graveB == null)
                    continue;

                Collider colliderB =
                    graveB.GetComponent<Collider>();

                if (colliderB == null)
                    continue;


                Vector3 direction;
                float distance;


                bool isOverlapping =
                    Physics.ComputePenetration(
                        colliderA,
                        colliderA.transform.position,
                        colliderA.transform.rotation,

                        colliderB,
                        colliderB.transform.position,
                        colliderB.transform.rotation,

                        out direction,
                        out distance
                    );


                if (isOverlapping && distance > 0.001f)
                {

                    return true;
                }
            }
        }

        return false;
    }
    void PlayFaceDecidedSE()
    {
        if (totalSteps >= 11)
        {
            AudioManager.Instance.PlaySE("grave_faceDecided3");
        }
        else if (totalSteps >= 5)
        {
            AudioManager.Instance.PlaySE("grave_faceDecided2");
        }
        else
        {
            AudioManager.Instance.PlaySE("grave_faceDecided1");
        }
    }
    // =========================================================
    // 移動処理
    // =========================================================
    IEnumerator MovePlayerCoroutine(int steps)
    {
        int dir = isClockwise ? -1 : 1;

        if (steps < 0)
            dir *= -1;

        int count = Mathf.Abs(steps);

        // =========================================
        // 1マスずつ移動
        // =========================================
        for (int i = 0; i < count; i++)
        {
            CurrentPathIndex =
                (CurrentPathIndex + dir + boardManager.outerPath.Count)
                % boardManager.outerPath.Count;

            Vector2Int grid =
                boardManager.outerPath[CurrentPathIndex];

            Vector3 pos =
                boardManager.GridToWorld(grid.x, grid.y);

            pos.y = 5f;

            yield return MoveToPosition(
                CurrentPlayer.transform,
                pos,
                0.15f
            );

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySE(
                    "player_step"
                );
            }

            yield return new WaitForSeconds(0.05f);


            // =========================================
            // このマスで強制停止するか確認
            // =========================================
            bool forcedStop = false;

            if (
                mistEffectManager != null &&
                mistEffectManager.TryGetHoleOwner(
                CurrentPathIndex,
                out int holeOwner
                )
                )
            {
                // 自分が置いた穴なら無視
                if (holeOwner != currentPlayerIndex)
                {

                    mistEffectManager.RemoveHole(
                        CurrentPathIndex
                    );

                    forcedStop = true;
                }
            }


            // =========================================
            // このマスが最終マスか
            // =========================================
            bool isFinalStep =
                i == count - 1;


            // =========================================
            // 本当に「通過」する場合だけ
            // 相手プレイヤー追い越し判定
            // =========================================
            if (!forcedStop && !isFinalStep)
            {
                CheckPlayerTread();
            }


            // =========================================
            // 強制停止なら移動終了
            // =========================================
            if (forcedStop)
            {
                break;
            }


            // =========================================
            // 角を通過
            // =========================================
            if (IsCorner(grid))
            {
                float turnAngle =
                    isClockwise ? 90f : -90f;

                CurrentPlayer.transform.rotation =
                    Quaternion.Euler(
                        90f,
                        RoundTo90(
                            CurrentPlayer.transform.eulerAngles.y
                            + turnAngle
                        ),
                        0f
                    );
            }
        }


        // =========================================
        // 移動終了後
        // =========================================

        int stoppedPlayerIndex =
    GetStoppedPlayerIndex();

        bool stoppedOnOtherPlayer =
            stoppedPlayerIndex >= 0;

        Vector2Int stopGrid =
            boardManager.outerPath[CurrentPathIndex];

        bool isCorner =
            IsCorner(stopGrid);

        // 0マスでは進化させない
        bool movedThisTurn =
            totalSteps > 0;


        // =========================================
        // 四隅進化
        // =========================================
        if (movedThisTurn && isCorner)
        {
            bool won =
                TryAdvanceEvolution(
                    currentPlayerIndex
                );

            // Level3になった場合は
            // TryAdvanceEvolution内で勝利処理済み
            if (won)
            {
                yield break;
            }
        }


        // =========================================
        // 相手プレイヤーのマスに停止
        // =========================================
        if (stoppedOnOtherPlayer)
        {

            AttackResult attackResult =
                AttackResult.NormalHit;

            if (mistEffectManager != null)
            {
                attackResult =
                    mistEffectManager.ResolveAttack(
                        currentPlayerIndex,
                        stoppedPlayerIndex
                    );
            }

            // =========================================
            // 通常攻撃成功
            // =========================================
            if (
                attackResult ==
                AttackResult.NormalHit
            )
            {
                GiveMist(
                    currentPlayerIndex,
                    1
                );

            }

            // =========================================
            // Protectorで通常攻撃を防御
            // =========================================
            else if (
                attackResult ==
                AttackResult.BlockedByProtector
            )
            {
            }

            // =========================================
            // Counter反撃成功
            // =========================================
            else if (
                attackResult ==
                AttackResult.CounterHit
            )
            {
                GiveMist(
                    stoppedPlayerIndex,
                    1
                );

            }

            // =========================================
            // Counterは発動したが
            // 攻撃者Protectorで反撃防御
            // =========================================
            else if (
                attackResult ==
                AttackResult.CounterBlocked
            )
            {
            }

            // =========================================
            // ここで今の手番は終了
            // ただし停止攻撃なので同じ人が再手番
            // =========================================
            ProcessEndOfTurn(
                currentPlayerIndex
            );

            EnterTurnStart();

            RefreshAllPlayerUI();

            if (mistEffectManager != null)
            {
                mistEffectManager.RefreshAllStatusUI();
            }

            FadeCurrentPlayerPanel(true);

            yield break;
        }


        // =========================================
        // 通常ターン終了
        // =========================================
        NextTurn();
    }
    void CheckPlayerTread()
    {
        for (int i = 0; i < players.Count; i++)
        {
            if (i == currentPlayerIndex)
                continue;

            if (playerPathIndices[i] == CurrentPathIndex)
            {
                AudioManager.Instance.PlaySE("player_tread");

                // 相手を抜かしたのでMist +1
                GiveMist(
                    currentPlayerIndex,
                    1
                );

            }
        }
    }
    float GetArrayValueOrDefault(float[] array, int index, float defaultValue)
    {
        if (array == null || index < 0 || index >= array.Length)
            return defaultValue;

        return array[index];
    }
    float RoundTo90(float y)
    {
        y = Mathf.Repeat(y, 360f);
        return Mathf.Round(y / 90f) * 90f;
    }
    void UpdatePlayerFacing(int playerIndex)
    {
        if (
            playerIndex < 0 ||
            playerIndex >= players.Count ||
            playerIndex >= playerPathIndices.Count ||
            players[playerIndex] == null ||
            boardManager == null ||
            boardManager.outerPath == null ||
            boardManager.outerPath.Count == 0
        )
        {
            return;
        }


        int currentIndex =
            playerPathIndices[playerIndex];

        // 通常移動と完全に同じ進行方向
        int dir =
            isClockwise ? -1 : 1;

        int nextIndex =
            (currentIndex + dir + boardManager.outerPath.Count)
            % boardManager.outerPath.Count;


        Vector2Int currentGrid =
            boardManager.outerPath[currentIndex];

        Vector2Int nextGrid =
            boardManager.outerPath[nextIndex];

        Vector2Int delta =
            nextGrid - currentGrid;


        // =========================================
        // Playerモデルの正面方向に合わせた回転
        //
        // CreatePlayers() の初期回転と
        // 通常移動時の回転規則に合わせる
        // =========================================

        float yRot = 0f;

        // 右へ進む
        if (delta.x > 0)
        {
            yRot = 90f;
        }

        // 上へ進む
        else if (delta.y > 0)
        {
            yRot = 0f;
        }

        // 左へ進む
        else if (delta.x < 0)
        {
            yRot = -90f;
        }

        // 下へ進む
        else if (delta.y < 0)
        {
            yRot = 180f;
        }


        players[playerIndex].transform.rotation =
            Quaternion.Euler(
                90f,
                yRot,
                0f
            );
    }
    void UpdateAllPlayerFacing()
    {
        for (int i = 0; i < players.Count; i++)
        {
            UpdatePlayerFacing(i);
        }
    }
    void NextTurn()
    {
        int endingPlayerIndex =
            currentPlayerIndex;

        enginePending[endingPlayerIndex] = false;
        gravePlusOnePending[endingPlayerIndex] = false;
        if (mistEffectManager != null)
        {
            mistEffectManager.HideNewMistStatusIcon("engine");
            mistEffectManager.HideNewMistStatusIcon("grave+1");
        }
        activeGraveCount = 0;
        ClearCurrentPlayerUsedMistIcons();

        if (usedMistImage != null)
        {
            usedMistImage.gameObject.SetActive(false);
        }

        ClearSpawnedGraves();
        ProcessEndOfTurn(
            endingPlayerIndex
        );

        // =========================================
        // 次プレイヤーへ
        // =========================================

        currentPlayerIndex =
            (currentPlayerIndex + 1)
            % players.Count;

        EnterTurnStart();

        RefreshAllPlayerUI();

        if (mistEffectManager != null)
        {
            mistEffectManager.RefreshAllStatusUI();
        }

        FadeCurrentPlayerPanel(true);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySE(
                "game_turnSwitch"
            );
        }
    }
    void ClearCurrentPlayerUsedMistIcons()
    {
        if (currentPlayerMistHolder == null) return;

        foreach (Transform child in currentPlayerMistHolder)
        {
            Destroy(child.gameObject);
        }
    }
    void ProcessEndOfTurn(
    int playerIndex
)
    {
        if (
            playerIndex < 0 ||
            playerIndex >= playerMistPlusBuff.Length
        )
        {
            return;
        }

        // =========================================
        // 旧MistPlus
        // =========================================
        if (playerMistPlusBuff[playerIndex])
        {

            playerMistPlusBuff[playerIndex] =
                false;
        }



        // =========================================
        // MistEffectManager側の
        // ターンタイプ状態を1ターン進める
        // =========================================
        if (mistEffectManager != null)
        {
            mistEffectManager.OnPlayerTurnEnded(
                playerIndex
            );
        }
    }

    IEnumerator MoveToPosition(Transform obj, Vector3 target, float time)
    {
        Vector3 start = obj.position;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / time;
            obj.position = Vector3.Lerp(start, target, t);
            yield return null;
        }

        obj.position = target;
    }

    void ClearSpawnedGraves()
    {
        foreach (GameObject grave in spawnedGraves)
        {
            if (grave != null)
                Destroy(grave);
        }

        spawnedGraves.Clear();
    }

    bool IsCorner(Vector2Int grid)
    {
        int max = boardManager.gridSize - 1;
        return (grid.x == 0 || grid.x == max) &&
               (grid.y == 0 || grid.y == max);
    }

    public void SetEvolutionLevel(int playerIndex, int level)
    {
        if (playerIndex < 0 || playerIndex >= playerEvolutionLevels.Length) return;

        playerEvolutionLevels[playerIndex] = Mathf.Clamp(level, 0, 3);
        RefreshAllPlayerUI();
    }

    public void AddEvolutionLevel(int playerIndex, int amount)
    {
        if (playerIndex < 0 || playerIndex >= playerEvolutionLevels.Length) return;

        playerEvolutionLevels[playerIndex] += amount;
        playerEvolutionLevels[playerIndex] = Mathf.Clamp(playerEvolutionLevels[playerIndex], 0, 3);

        AudioManager.Instance.PlaySE("player_evolution");

        RefreshAllPlayerUI();
    }
    bool TryAdvanceEvolution(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= players.Count)
            return false;

        int currentLevel =
            GetPlayerEvolutionLevel(playerIndex);

        // すでにLevel3なら何もしない
        if (currentLevel >= 3)
            return false;

        PlayerController pc =
            players[playerIndex].GetComponent<PlayerController>();

        if (pc == null)
            return false;

        // 進化
        pc.AdvanceEvolution();

        AddEvolutionLevel(
            playerIndex,
            1
        );

        int newLevel =
            GetPlayerEvolutionLevel(playerIndex);


        // Level3になった瞬間に勝利
        if (newLevel >= 3)
        {

            GoToWinScene(playerIndex);

            return true;
        }

        return false;
    }

    // =========================================================
    // New Mist : Evolution
    // 進化レベルを1上げる。レベル3で勝利。
    // =========================================================
    public bool ActivateMistEvolution(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= players.Count)
            return false;

        if (players[playerIndex] == null)
            return false;

        // レベル3のプレイヤーは進化できない
        if (GetPlayerEvolutionLevel(playerIndex) >= 3)
            return false;

        PlayerController pc =
            players[playerIndex].GetComponent<PlayerController>();

        if (pc == null)
            return false;

        // 既存の進化演出・レベル更新・勝利処理を利用
        TryAdvanceEvolution(playerIndex);

        Debug.Log("[Evolution] Mist進化発動！");

        // レベル1・2でも発動成功として扱う
        return true;
    }

    // 新Mist trap：既にプレイヤーがいるマスを設置候補から除外する。
    public bool IsAnyPlayerAtPathIndex(int pathIndex)
    {
        for (int i = 0; i < playerPathIndices.Count; i++)
        {
            if (playerPathIndices[i] == pathIndex)
                return true;
        }

        return false;
    }

    public int GetCurrentPlayerIndex()
    {
        return currentPlayerIndex;
    }

    public int GetPlayerEvolutionLevel(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= playerEvolutionLevels.Length) return 0;
        return playerEvolutionLevels[playerIndex];
    }

    public int GetPlayerMistCount(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= playerMists.Count) return 0;
        return playerMists[playerIndex].Count;
    }

    void ShowCurrentPlayerPanelImmediate()
    {
        if (currentPlayerPanel != null)
            currentPlayerPanel.SetActive(true);

        if (currentPlayerPanelCanvasGroup != null)
        {
            currentPlayerPanelCanvasGroup.alpha = 1f;
            currentPlayerPanelCanvasGroup.interactable = true;
            currentPlayerPanelCanvasGroup.blocksRaycasts = true;
        }
    }

    void HideCurrentPlayerPanelImmediate()
    {
        if (currentPlayerPanelCanvasGroup != null)
        {
            currentPlayerPanelCanvasGroup.alpha = 0f;
            currentPlayerPanelCanvasGroup.interactable = false;
            currentPlayerPanelCanvasGroup.blocksRaycasts = false;
        }

        if (currentPlayerPanel != null)
            currentPlayerPanel.SetActive(false);
    }

    void FadeCurrentPlayerPanel(bool fadeIn)
    {
        if (currentPlayerPanelCanvasGroup == null)
        {
            if (currentPlayerPanel != null)
                currentPlayerPanel.SetActive(fadeIn);
            return;
        }

        if (currentPlayerPanelFadeCoroutine != null)
            StopCoroutine(currentPlayerPanelFadeCoroutine);

        currentPlayerPanelFadeCoroutine = StartCoroutine(FadeCurrentPlayerPanelCoroutine(fadeIn));
    }

    IEnumerator FadeCurrentPlayerPanelCoroutine(bool fadeIn)
    {
        if (currentPlayerPanel != null && fadeIn)
            currentPlayerPanel.SetActive(true);

        float startAlpha = currentPlayerPanelCanvasGroup.alpha;
        float targetAlpha = fadeIn ? 1f : 0f;

        float elapsed = 0f;

        while (elapsed < currentPlayerFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / currentPlayerFadeDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            currentPlayerPanelCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        currentPlayerPanelCanvasGroup.alpha = targetAlpha;
        currentPlayerPanelCanvasGroup.interactable = fadeIn;
        currentPlayerPanelCanvasGroup.blocksRaycasts = fadeIn;

        if (!fadeIn && currentPlayerPanel != null)
            currentPlayerPanel.SetActive(false);

        currentPlayerPanelFadeCoroutine = null;
    }
}
