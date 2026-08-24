using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public enum MistEffectType
{
    // =========================
    // Red
    // =========================
    HoleTrap,
    Shot,
    ColorBall,
    Bind,

    // =========================
    // Blue
    // =========================
    Protector,
    Analyzer,
    Counter,
    MistPlus,

    // =========================
    // Green
    // =========================
    MovePlus2,
    FlashBadge,
    PowerCake,
    RespawnCoffin,

    // =========================
    // Yellow
    // =========================
    UTurn,
    TimeBomb,
    MirrorPortal,
    MagnaTornado,

    // =========================
    // Black
    // =========================
    RandomEffect,
    Jackpot,
    Bug
}

public class MistEffectManager : MonoBehaviour
{
    // =========================================================
    // References
    // =========================================================

    [SerializeField]
    private GameManager gameManager;

    [SerializeField]
    private BoardManager boardManager;


    // =========================================================
    // Hole Trap
    // =========================================================

    [Header("Hole Trap Visual")]
    [SerializeField]
    private GameObject holeMarkerPrefab;

    // Element 0 = 1P用
    // Element 1 = 2P用
    // Element 2 = 3P用
    // Element 3 = 4P用
    [SerializeField]
    private Sprite[] holeSprites;

    private Dictionary<int, int> holeOwnerByPathIndex =
        new Dictionary<int, int>();

    private Dictionary<int, GameObject> holeVisualsByPathIndex =
        new Dictionary<int, GameObject>();


    // =========================================================
    // Status UI
    // =========================================================

    [Header("Status UI")]
    [SerializeField]
    private Transform currentPlayerMistHolder;

    // Element 0 = 01.png
    // Element 1 = 02.png
    // ...
    // Element 9 = 10.png
    [SerializeField]
    private Sprite[] statusCountSprites;


    // =========================================================
    // Bind
    // =========================================================

    [Header("Bind")]
    [SerializeField]
    private Sprite bindSprite;

    private int[] playerBindCount =
        new int[4];


    // =========================================================
    // Protector
    // =========================================================

    [Header("Protector")]
    [SerializeField]
    private Sprite protectorSprite;

    private int[] playerProtectorCount =
        new int[4];

    [Header("Counter")]
    [SerializeField]
    private Sprite counterSprite;

    // 残りターン数
    private int[] playerCounterTurns =
        new int[4];


    // =========================================================
    // Counter : 発動可能か確認
    // =========================================================
    public bool HasCounter(
        int playerIndex
    )
    {
        if (
            playerIndex < 0 ||
            playerIndex >= playerCounterTurns.Length
        )
        {
            return false;
        }

        return playerCounterTurns[playerIndex] > 0;
    }

    public enum CounterResult
    {
        None,           // Counterなし
        CounterSuccess, // Counter成功、反撃も通った
        CounterBlocked  // Counter発動、ただし反撃はProtectorで防がれた
    }
    // =========================================================
    // 外部から呼ぶ入口
    // =========================================================

    public MistEffectType UseMist(
        GameManager.MistColor color,
        int playerIndex
    )
    {
        MistEffectType effect =
            GetRandomEffect(color);

        Debug.Log(
            $"[MistEffect] " +
            $"Player {playerIndex + 1} / " +
            $"Color={color} / " +
            $"Effect={effect}"
        );

        ExecuteEffect(
            effect,
            playerIndex
        );

        return effect;
    }


    // =========================================================
    // 色ごとのランダム効果抽選
    // =========================================================

    MistEffectType GetRandomEffect(
        GameManager.MistColor color
    )
    {
        switch (color)
        {
            // =========================
            // Red
            // =========================
            case GameManager.MistColor.Red:
                {
                    MistEffectType[] effects =
                    {
                    MistEffectType.HoleTrap,
                    MistEffectType.Shot,
                    MistEffectType.ColorBall,
                    MistEffectType.Bind
                };

                    return effects[
                        Random.Range(
                            0,
                            effects.Length
                        )
                    ];
                }


            // =========================
            // Blue
            // =========================
            case GameManager.MistColor.Blue:
                {
                    // ★ Protector動作確認用
                    return MistEffectType.Protector;
                }


            // =========================
            // Green
            // =========================
            case GameManager.MistColor.Green:
                {
                    MistEffectType[] effects =
                    {
                    MistEffectType.MovePlus2,
                    MistEffectType.FlashBadge,
                    MistEffectType.PowerCake,
                    MistEffectType.RespawnCoffin
                };

                    return effects[
                        Random.Range(
                            0,
                            effects.Length
                        )
                    ];
                }


            // =========================
            // Yellow
            // =========================
            case GameManager.MistColor.Yellow:
                {
                    MistEffectType[] effects =
                    {
                    MistEffectType.UTurn,
                    MistEffectType.TimeBomb,
                    MistEffectType.MirrorPortal,
                    MistEffectType.MagnaTornado
                };

                    return effects[
                        Random.Range(
                            0,
                            effects.Length
                        )
                    ];
                }


            // =========================
            // Black
            // =========================
            case GameManager.MistColor.Black:
                {
                    MistEffectType[] effects =
                    {
                    MistEffectType.RandomEffect,
                    MistEffectType.Jackpot,
                    MistEffectType.Bug
                };

                    return effects[
                        Random.Range(
                            0,
                            effects.Length
                        )
                    ];
                }
        }

        Debug.LogWarning(
            $"未定義のMistColorです: {color}"
        );

        return MistEffectType.HoleTrap;
    }


    // =========================================================
    // 効果実行
    // =========================================================

    void ExecuteEffect(
        MistEffectType effect,
        int playerIndex
    )
    {
        switch (effect)
        {
            // =====================================================
            // Red
            // =====================================================

            case MistEffectType.HoleTrap:
                Debug.Log(
                    $"Player {playerIndex + 1} : HoleTrap"
                );

                ActivateHole(
                    playerIndex
                );

                break;


            case MistEffectType.Shot:
                Debug.Log(
                    $"Player {playerIndex + 1} : Shot"
                );

                ActivateShot(
                    playerIndex
                );

                break;


            case MistEffectType.ColorBall:
                Debug.Log(
                    $"Player {playerIndex + 1} : ColorBall"
                );

                ActivateColorBall(
                    playerIndex
                );

                break;


            case MistEffectType.Bind:
                Debug.Log(
                    $"Player {playerIndex + 1} : Bind"
                );

                ActivateBind(
                    playerIndex
                );

                break;


            // =====================================================
            // Blue
            // =====================================================

            case MistEffectType.Protector:
                Debug.Log(
                    $"Player {playerIndex + 1} : Protector"
                );

                AddProtector(
                    playerIndex,
                    1
                );

                break;


            case MistEffectType.Analyzer:
                Debug.Log(
                    $"Player {playerIndex + 1} : Analyzer"
                );

                // TODO:
                // 現在所持中のMist効果を表示

                break;


            case MistEffectType.Counter:

                Debug.Log(
                    $"Player {playerIndex + 1} : Counter"
                );

                AddCounter(
                    playerIndex,
                    2
                );

                break;


            case MistEffectType.MistPlus:
                Debug.Log(
                    $"Player {playerIndex + 1} : MistPlus"
                );

                // TODO:
                // 自分に1ターンMistPlus付与

                break;


            // =====================================================
            // Green
            // =====================================================

            case MistEffectType.MovePlus2:
                Debug.Log(
                    $"Player {playerIndex + 1} : MovePlus2"
                );

                // TODO:
                // 自分に1ターン移動+2

                break;


            case MistEffectType.FlashBadge:
                Debug.Log(
                    $"Player {playerIndex + 1} : FlashBadge"
                );

                // TODO:
                // 前後3マス任意ワープ

                break;


            case MistEffectType.PowerCake:
                Debug.Log(
                    $"Player {playerIndex + 1} : PowerCake"
                );

                // TODO:
                // 自分に1ターン移動2倍

                break;


            case MistEffectType.RespawnCoffin:
                Debug.Log(
                    $"Player {playerIndex + 1} : RespawnCoffin"
                );

                // TODO:
                // 初期地点への任意ワープ

                break;


            // =====================================================
            // Yellow
            // =====================================================

            case MistEffectType.UTurn:
                Debug.Log(
                    $"Player {playerIndex + 1} : UTurn"
                );

                // TODO:
                // 進行方向反転

                break;


            case MistEffectType.TimeBomb:
                Debug.Log(
                    $"Player {playerIndex + 1} : TimeBomb"
                );

                // TODO:
                // 全ユニットに移動半減

                break;


            case MistEffectType.MirrorPortal:
                Debug.Log(
                    $"Player {playerIndex + 1} : MirrorPortal"
                );

                // TODO:
                // ランダム2マスにPortal設置

                break;


            case MistEffectType.MagnaTornado:
                Debug.Log(
                    $"Player {playerIndex + 1} : MagnaTornado"
                );

                // TODO:
                // 全員ランダムワープ

                break;


            // =====================================================
            // Black
            // =====================================================

            case MistEffectType.RandomEffect:
                Debug.Log(
                    $"Player {playerIndex + 1} : RandomEffect"
                );

                // TODO:
                // 通常Mistからランダム1効果

                break;


            case MistEffectType.Jackpot:
                Debug.Log(
                    $"Player {playerIndex + 1} : Jackpot"
                );

                // TODO:
                // 1進化

                break;


            case MistEffectType.Bug:
                Debug.Log(
                    $"Player {playerIndex + 1} : Bug"
                );

                // TODO:
                // 全ユニットステータス混乱

                break;
        }
    }


    // =========================================================
    // Red : HoleTrap
    // =========================================================

    void ActivateHole(
        int playerIndex
    )
    {
        if (
            boardManager == null ||
            boardManager.outerPath == null
        )
        {
            return;
        }

        List<int> candidates =
            new List<int>();

        for (
            int i = 0;
            i < boardManager.outerPath.Count;
            i++
        )
        {
            Vector2Int grid =
                boardManager.outerPath[i];

            // 角は除外
            if (IsCorner(grid))
                continue;

            // すでにHoleがあるマスは除外
            if (
                holeOwnerByPathIndex.ContainsKey(i)
            )
            {
                continue;
            }

            candidates.Add(i);
        }

        if (candidates.Count == 0)
        {
            Debug.Log(
                "Holeを置けるマスがありません"
            );

            return;
        }

        int holePathIndex =
            candidates[
                Random.Range(
                    0,
                    candidates.Count
                )
            ];

        holeOwnerByPathIndex[
            holePathIndex
        ] = playerIndex;

        Vector2Int holeGrid =
            boardManager.outerPath[
                holePathIndex
            ];

        Debug.Log(
            $"Player {playerIndex + 1} placed Hole at " +
            $"{holeGrid} " +
            $"(pathIndex={holePathIndex})"
        );

        CreateHoleVisual(
            holePathIndex,
            playerIndex
        );
    }


    public bool TryGetHoleOwner(
        int pathIndex,
        out int ownerPlayerIndex
    )
    {
        return holeOwnerByPathIndex.TryGetValue(
            pathIndex,
            out ownerPlayerIndex
        );
    }


    public void RemoveHole(
        int pathIndex
    )
    {
        holeOwnerByPathIndex.Remove(
            pathIndex
        );

        RemoveHoleVisual(
            pathIndex
        );
    }


    void CreateHoleVisual(
        int holePathIndex,
        int playerIndex
    )
    {
        if (holeMarkerPrefab == null)
        {
            Debug.LogWarning(
                "HoleTrap: Hole Marker Prefab が設定されていません"
            );

            return;
        }

        if (
            holeSprites == null ||
            holeSprites.Length < 4
        )
        {
            Debug.LogWarning(
                "HoleTrap: Hole Spritesを4枚設定してください"
            );

            return;
        }

        if (
            playerIndex < 0 ||
            playerIndex >= holeSprites.Length
        )
        {
            return;
        }

        if (
            holeSprites[playerIndex] == null
        )
        {
            return;
        }

        Vector2Int grid =
            boardManager.outerPath[
                holePathIndex
            ];

        Vector3 pos =
            boardManager.GridToWorld(
                grid.x,
                grid.y
            );

        pos.y =
            5.05f;

        GameObject marker =
            Instantiate(
                holeMarkerPrefab,
                pos,
                Quaternion.Euler(
                    90f,
                    0f,
                    0f
                )
            );

        SpriteRenderer sr =
            marker.GetComponent<SpriteRenderer>();

        if (sr == null)
        {
            Destroy(
                marker
            );

            Debug.LogWarning(
                "HoleTrap: PrefabにSpriteRendererがありません"
            );

            return;
        }

        sr.sprite =
            holeSprites[playerIndex];

        holeVisualsByPathIndex[
            holePathIndex
        ] = marker;
    }


    void RemoveHoleVisual(
        int holePathIndex
    )
    {
        if (
            holeVisualsByPathIndex.TryGetValue(
                holePathIndex,
                out GameObject marker
            )
        )
        {
            if (marker != null)
            {
                Destroy(
                    marker
                );
            }

            holeVisualsByPathIndex.Remove(
                holePathIndex
            );
        }
    }


    bool IsCorner(
        Vector2Int grid
    )
    {
        int max =
            boardManager.gridSize - 1;

        return
            (grid.x == 0 || grid.x == max) &&
            (grid.y == 0 || grid.y == max);
    }


    // =========================================================
    // Red : Shot
    // =========================================================

    void ActivateShot(
        int attackerPlayerIndex
    )
    {
        if (gameManager == null)
            return;

        List<int> candidates =
            new List<int>();

        int playerCount =
            gameManager.GetPlayerCount();

        for (
            int i = 0;
            i < playerCount;
            i++
        )
        {
            if (
                i == attackerPlayerIndex
            )
            {
                continue;
            }

            if (
                gameManager.GetMistCount(i) <= 0
            )
            {
                continue;
            }

            candidates.Add(i);
        }

        if (candidates.Count == 0)
        {
            Debug.Log(
                "[Shot] 破壊できる敵Mistがありません"
            );

            return;
        }

        int targetPlayerIndex =
            candidates[
                Random.Range(
                    0,
                    candidates.Count
                )
            ];

        if (
            TryBlockWithProtector(
                targetPlayerIndex,
                "Shot"
            )
        )
        {
            return;
        }

        int mistCount =
            gameManager.GetMistCount(
                targetPlayerIndex
            );

        int targetMistIndex =
            Random.Range(
                0,
                mistCount
            );

        GameManager.MistColor destroyedMist =
            gameManager.GetMist(
                targetPlayerIndex,
                targetMistIndex
            );

        gameManager.RemoveMist(
            targetPlayerIndex,
            targetMistIndex
        );

        Debug.Log(
            $"[Shot] Player {attackerPlayerIndex + 1} → " +
            $"Player {targetPlayerIndex + 1} / " +
            $"{destroyedMist} Mistを破壊"
        );
    }


    // =========================================================
    // Red : ColorBall
    // =========================================================

    void ActivateColorBall(
        int attackerPlayerIndex
    )
    {
        if (gameManager == null)
            return;

        List<int> candidates =
            new List<int>();

        int playerCount =
            gameManager.GetPlayerCount();

        for (
            int i = 0;
            i < playerCount;
            i++
        )
        {
            if (
                i == attackerPlayerIndex
            )
            {
                continue;
            }

            if (
                gameManager.GetMistCount(i) <= 0
            )
            {
                continue;
            }

            candidates.Add(i);
        }

        if (candidates.Count == 0)
        {
            Debug.Log(
                "[ColorBall] 対象にできる敵がいません"
            );

            return;
        }

        int targetPlayerIndex =
            candidates[
                Random.Range(
                    0,
                    candidates.Count
                )
            ];

        if (
            TryBlockWithProtector(
                targetPlayerIndex,
                "ColorBall"
            )
        )
        {
            return;
        }

        GameManager.MistColor newColor =
            (GameManager.MistColor)
            Random.Range(
                1,
                5
            );

        int mistCount =
            gameManager.GetMistCount(
                targetPlayerIndex
            );

        for (
            int i = 0;
            i < mistCount;
            i++
        )
        {
            gameManager.SetMistColor(
                targetPlayerIndex,
                i,
                newColor
            );
        }

        Debug.Log(
            $"[ColorBall] Player {attackerPlayerIndex + 1} → " +
            $"Player {targetPlayerIndex + 1} のMistを全て " +
            $"{newColor} に変更"
        );
    }


    // =========================================================
    // Red : Bind
    // =========================================================

    void ActivateBind(
        int attackerPlayerIndex
    )
    {
        if (gameManager == null)
            return;

        List<int> candidates =
            new List<int>();

        int playerCount =
            gameManager.GetPlayerCount();

        for (
            int i = 0;
            i < playerCount;
            i++
        )
        {
            if (
                i == attackerPlayerIndex
            )
            {
                continue;
            }

            candidates.Add(i);
        }

        if (candidates.Count == 0)
            return;

        int targetPlayerIndex =
            candidates[
                Random.Range(
                    0,
                    candidates.Count
                )
            ];

        if (
            TryBlockWithProtector(
                targetPlayerIndex,
                "Bind"
            )
        )
        {
            return;
        }

        AddBind(
            targetPlayerIndex,
            2
        );

        Debug.Log(
            $"[Bind] Player {attackerPlayerIndex + 1} → " +
            $"Player {targetPlayerIndex + 1} にBind +2"
        );
    }


    // =========================================================
    // Bind
    // =========================================================

    public void AddBind(
        int playerIndex,
        int amount
    )
    {
        if (
            playerIndex < 0 ||
            playerIndex >= playerBindCount.Length
        )
        {
            return;
        }

        if (amount <= 0)
            return;

        playerBindCount[playerIndex] +=
            amount;

        Debug.Log(
            $"[Bind] Player {playerIndex + 1} " +
            $"+{amount} → 合計 {playerBindCount[playerIndex]}"
        );

        RefreshBindUI();
    }


    public int ApplyBindToMovement(
        int playerIndex,
        int moveAmount
    )
    {
        if (
            playerIndex < 0 ||
            playerIndex >= playerBindCount.Length
        )
        {
            return moveAmount;
        }

        if (moveAmount <= 0)
            return moveAmount;

        int consumed =
            Mathf.Min(
                playerBindCount[playerIndex],
                moveAmount
            );

        playerBindCount[playerIndex] -=
            consumed;

        int remainingMove =
            moveAmount - consumed;

        Debug.Log(
            $"[Bind] Player {playerIndex + 1} / " +
            $"移動 {moveAmount} - Bind {consumed} " +
            $"= 実移動 {remainingMove} / " +
            $"残Bind {playerBindCount[playerIndex]}"
        );

        RefreshBindUI();

        return remainingMove;
    }


    public void RefreshBindUI()
    {
        if (gameManager == null)
            return;

        int playerIndex =
            gameManager.GetCurrentPlayerIndex();

        if (
            playerIndex < 0 ||
            playerIndex >= playerBindCount.Length
        )
        {
            return;
        }

        RefreshStatusIcon(
            "BindStatus",
            bindSprite,
            playerBindCount[playerIndex]
        );
    }


    // =========================================================
    // Blue : Protector
    // =========================================================

    public void AddProtector(
        int playerIndex,
        int amount
    )
    {
        if (
            playerIndex < 0 ||
            playerIndex >= playerProtectorCount.Length
        )
        {
            return;
        }

        if (amount <= 0)
            return;

        playerProtectorCount[playerIndex] +=
            amount;

        Debug.Log(
            $"[Protector] Player {playerIndex + 1} " +
            $"+{amount} → 合計 {playerProtectorCount[playerIndex]}"
        );

        RefreshProtectorUI();
    }


    public int GetProtectorCount(
        int playerIndex
    )
    {
        if (
            playerIndex < 0 ||
            playerIndex >= playerProtectorCount.Length
        )
        {
            return 0;
        }

        return playerProtectorCount[
            playerIndex
        ];
    }


    public void RefreshProtectorUI()
    {
        if (gameManager == null)
            return;

        int playerIndex =
            gameManager.GetCurrentPlayerIndex();

        if (
            playerIndex < 0 ||
            playerIndex >= playerProtectorCount.Length
        )
        {
            return;
        }

        RefreshStatusIcon(
            "ProtectorStatus",
            protectorSprite,
            playerProtectorCount[playerIndex]
        );
    }


    bool TryBlockWithProtector(
        int targetPlayerIndex,
        string effectName
    )
    {
        if (
            targetPlayerIndex < 0 ||
            targetPlayerIndex >= playerProtectorCount.Length
        )
        {
            return false;
        }

        if (
            playerProtectorCount[
                targetPlayerIndex
            ] <= 0
        )
        {
            return false;
        }

        playerProtectorCount[
            targetPlayerIndex
        ]--;

        Debug.Log(
            $"[Protector] Player {targetPlayerIndex + 1} が " +
            $"{effectName} を無効化 / " +
            $"残り {playerProtectorCount[targetPlayerIndex]}"
        );

        RefreshProtectorUI();

        return true;
    }


    // =========================================================
    // Status UI 共通表示
    // =========================================================

    void RefreshStatusIcon(
        string statusName,
        Sprite mainSprite,
        int count
    )
    {
        if (
            gameManager == null ||
            currentPlayerMistHolder == null
        )
        {
            return;
        }

        Transform oldStatus =
            currentPlayerMistHolder.Find(
                statusName
            );

        if (oldStatus != null)
        {
            Destroy(
                oldStatus.gameObject
            );
        }

        if (count <= 0)
            return;

        GameObject iconPrefab =
            gameManager.GetStatusIconPrefab();

        if (iconPrefab == null)
        {
            Debug.LogWarning(
                "[Status UI] Icon Prefab が設定されていません"
            );

            return;
        }

        if (mainSprite == null)
        {
            Debug.LogWarning(
                $"[Status UI] {statusName} のSpriteがありません"
            );

            return;
        }

        if (
            statusCountSprites == null ||
            statusCountSprites.Length == 0
        )
        {
            Debug.LogWarning(
                "[Status UI] 個数Spriteが設定されていません"
            );

            return;
        }

        int countSpriteIndex =
            Mathf.Clamp(
                count - 1,
                0,
                statusCountSprites.Length - 1
            );

        Sprite countSprite =
            statusCountSprites[
                countSpriteIndex
            ];

        GameObject icon =
            Instantiate(
                iconPrefab,
                currentPlayerMistHolder
            );

        icon.name =
            statusName;

        Image mainImage =
            icon.GetComponent<Image>();

        if (mainImage != null)
        {
            mainImage.sprite =
                mainSprite;

            mainImage.enabled =
                true;

            mainImage.color =
                Color.white;
        }

        Transform countTransform =
            icon.transform.Find(
                "Count"
            );

        if (countTransform == null)
        {
            Debug.LogWarning(
                $"[Status UI] {statusName} のPrefab内にCountがありません"
            );

            return;
        }

        Image countImage =
            countTransform.GetComponent<Image>();

        if (countImage != null)
        {
            countImage.sprite =
                countSprite;

            countImage.enabled =
                true;

            countImage.color =
                Color.white;
        }
    }

    // =========================================================
    // Blue : Counter
    // 自分にCounterを指定ターン付与
    // =========================================================
    public void AddCounter(
        int playerIndex,
        int turns
    )
    {
        if (
            playerIndex < 0 ||
            playerIndex >= playerCounterTurns.Length
        )
        {
            return;
        }

        if (turns <= 0)
            return;

        playerCounterTurns[playerIndex] +=
            turns;

        Debug.Log(
            $"[Counter] Player {playerIndex + 1} " +
            $"+{turns}ターン → 残り {playerCounterTurns[playerIndex]}ターン"
        );

        RefreshCounterUI();
    }


    // =========================================================
    // Counter残りターン取得
    // =========================================================
    public int GetCounterTurns(
        int playerIndex
    )
    {
        if (
            playerIndex < 0 ||
            playerIndex >= playerCounterTurns.Length
        )
        {
            return 0;
        }

        return playerCounterTurns[
            playerIndex
        ];
    }


    // =========================================================
    // Counter UI更新
    // =========================================================
    public void RefreshCounterUI()
    {
        if (gameManager == null)
            return;

        int playerIndex =
            gameManager.GetCurrentPlayerIndex();

        if (
            playerIndex < 0 ||
            playerIndex >= playerCounterTurns.Length
        )
        {
            return;
        }

        RefreshStatusIcon(
            "CounterStatus",
            counterSprite,
            playerCounterTurns[playerIndex]
        );
    }
    // =========================================================
    // Counter : 攻撃を反射
    // defender が攻撃を無効化し、attackerへ反撃
    // =========================================================
    // =========================================================
    // Counter : 攻撃を無効化して反撃
    // =========================================================
    public CounterResult TryCounterAttack(
        int attackerPlayerIndex,
        int defenderPlayerIndex
    )
    {
        if (
            defenderPlayerIndex < 0 ||
            defenderPlayerIndex >= playerCounterTurns.Length
        )
        {
            return CounterResult.None;
        }

        // Counterなし
        if (
            playerCounterTurns[defenderPlayerIndex] <= 0
        )
        {
            return CounterResult.None;
        }

        Debug.Log(
            $"[Counter] Player {defenderPlayerIndex + 1} が " +
            $"Player {attackerPlayerIndex + 1} の攻撃を無効化"
        );

        // =========================================
        // 反撃先のProtector判定
        // =========================================
        if (
            TryBlockWithProtector(
                attackerPlayerIndex,
                "Counter Attack"
            )
        )
        {
            Debug.Log(
                $"[Counter] Player {attackerPlayerIndex + 1} の " +
                $"Protectorによって反撃は無効化"
            );

            return CounterResult.CounterBlocked;
        }

        Debug.Log(
            $"[Counter] Player {defenderPlayerIndex + 1} の反撃成功"
        );

        return CounterResult.CounterSuccess;
    }
}