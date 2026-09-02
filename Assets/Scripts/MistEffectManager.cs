using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;


// =========================================================
// Mist効果一覧
// =========================================================
public enum MistEffectType
{
    // Red
    HoleTrap,
    Shot,
    ColorBall,
    Bind,

    // Blue
    Protector,
    Analyzer,
    Counter,
    MistPlus,

    // Green
    MovePlus2,
    FlashBadge,
    PowerCake,
    RespawnCoffin,

    // Yellow
    UTurn,
    TimeBomb,
    MirrorPortal,
    MagnaTornado,

    // Black
    RandomEffect,
    Jackpot,
    Bug
}


// =========================================================
// 付与状態の種類
// =========================================================
public enum StatusEffectType
{
    Bind,
    Protector,
    Counter,
    MistPlus
}


// =========================================================
// 攻撃処理結果
// =========================================================
public enum AttackResult
{
    NormalHit,
    BlockedByProtector,
    CounterHit,
    CounterBlocked
}


// =========================================================
// 付与状態1件
//
// List内の並び順が、そのまま付与順になる
// =========================================================
public class StatusEffectEntry
{
    public StatusEffectType type;
    public int value;

    public StatusEffectEntry(
        StatusEffectType type,
        int value
    )
    {
        this.type = type;
        this.value = value;
    }
}


// =========================================================
// Mist効果アイコン1件
// =========================================================
[System.Serializable]
public class MistEffectSpriteData
{
    public MistEffectType effect;
    public Sprite sprite;
}


public class MistEffectManager : MonoBehaviour
{
    // =========================================================
    // References
    // =========================================================

    [Header("References")]
    [SerializeField]
    private GameManager gameManager;

    [SerializeField]
    private BoardManager boardManager;


    // =========================================================
    // Mist Effect Icons
    // =========================================================

    [Header("Mist Effect Icons")]
    [SerializeField]
    private MistEffectSpriteData[] mistEffectSprites;


    // =========================================================
    // Hole Trap
    // =========================================================

    [Header("Hole Trap Visual")]
    [SerializeField]
    private GameObject holeMarkerPrefab;

    // Element 0 = 1P
    // Element 1 = 2P
    // Element 2 = 3P
    // Element 3 = 4P
    [SerializeField]
    private Sprite[] holeSprites;

    private readonly Dictionary<int, int>
        holeOwnerByPathIndex =
            new Dictionary<int, int>();

    private readonly Dictionary<int, GameObject>
        holeVisualsByPathIndex =
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
    // Status Sprites
    // =========================================================

    [Header("Bind")]
    [SerializeField]
    private Sprite bindSprite;

    [Header("Protector")]
    [SerializeField]
    private Sprite protectorSprite;

    [Header("Counter")]
    [SerializeField]
    private Sprite counterSprite;

    [Header("Mist Plus")]
    [SerializeField]
    private Sprite mistPlusSprite;


    // =========================================================
    // Status Data
    //
    // Listの並び順 =
    // 「付与されたタイミングが早い順」
    // =========================================================

    private readonly List<StatusEffectEntry>[]
        playerStatusEffects =
    {
        new List<StatusEffectEntry>(),
        new List<StatusEffectEntry>(),
        new List<StatusEffectEntry>(),
        new List<StatusEffectEntry>()
    };


    // =========================================================
    // Mist効果アイコン取得
    // Analyzer UIから使用
    // =========================================================

    public Sprite GetMistEffectSprite(
        MistEffectType effect
    )
    {
        if (mistEffectSprites == null)
            return null;

        for (int i = 0; i < mistEffectSprites.Length; i++)
        {
            MistEffectSpriteData data =
                mistEffectSprites[i];

            if (data == null)
                continue;

            if (data.effect != effect)
                continue;

            if (data.sprite == null)
            {
                Debug.LogWarning(
                    $"[MistEffect Icon] " +
                    $"{effect} のSpriteが設定されていません"
                );

                return null;
            }

            return data.sprite;
        }

        Debug.LogWarning(
            $"[MistEffect Icon] " +
            $"効果アイコン未登録 : {effect}"
        );

        return null;
    }


    // =========================================================
    // Mist効果抽選
    //
    // Mist取得時にGameManagerから呼ばれる
    // 使用時には再抽選しない
    // =========================================================

    public MistEffectType GetRandomEffectForColor(
        GameManager.MistColor color
    )
    {
        return GetRandomEffect(color);
    }


    // =========================================================
    // 旧呼び出し互換
    // =========================================================

    public MistEffectType UseMist(
        GameManager.MistColor color,
        int playerIndex
    )
    {
        MistEffectType effect =
            GetRandomEffect(color);

        ExecuteEffect(
            effect,
            playerIndex
        );

        return effect;
    }


    // =========================================================
    // 色別Mist効果抽選
    // =========================================================

    MistEffectType GetRandomEffect(
        GameManager.MistColor color
    )
    {
        switch (color)
        {
            // =================================================
            // Red
            // =================================================

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


            // =================================================
            // Blue
            // =================================================

            case GameManager.MistColor.Blue:
                {
                    MistEffectType[] effects =
                    {
                    MistEffectType.Protector,
                    MistEffectType.Analyzer,
                    MistEffectType.Counter,
                    MistEffectType.MistPlus
                };

                    return effects[
                        Random.Range(
                            0,
                            effects.Length
                        )
                    ];
                }


            // =================================================
            // Green
            // =================================================

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


            // =================================================
            // Yellow
            // =================================================

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


            // =================================================
            // Black
            // =================================================

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
            $"未定義MistColor: {color}"
        );

        return MistEffectType.HoleTrap;
    }


    // =========================================================
    // 保存済みMist効果を実行
    //
    // Analyzer対応後はこちらが正式なMist使用ルート
    // =========================================================

    public void ExecuteStoredEffect(
        MistEffectType effect,
        int playerIndex
    )
    {
        ExecuteEffect(
            effect,
            playerIndex
        );
    }


    // =========================================================
    // Mist効果実行
    // =========================================================

    void ExecuteEffect(
        MistEffectType effect,
        int playerIndex
    )
    {
        switch (effect)
        {
            // =================================================
            // Red
            // =================================================

            case MistEffectType.HoleTrap:

                ActivateHole(
                    playerIndex
                );

                break;


            case MistEffectType.Shot:

                ActivateShot(
                    playerIndex
                );

                break;


            case MistEffectType.ColorBall:

                ActivateColorBall(
                    playerIndex
                );

                break;


            case MistEffectType.Bind:

                ActivateBind(
                    playerIndex
                );

                break;


            // =================================================
            // Blue
            // =================================================

            case MistEffectType.Protector:

                AddProtector(
                    playerIndex,
                    1
                );

                break;


            case MistEffectType.Analyzer:

                if (gameManager != null)
                {
                    gameManager.AnalyzeCurrentMists(
                        playerIndex
                    );
                }

                break;


            case MistEffectType.Counter:

                AddCounter(
                    playerIndex,
                    2
                );

                break;


            case MistEffectType.MistPlus:

                AddMistPlus(
                    playerIndex,
                    1
                );

                break;


            // =================================================
            // Green
            // =================================================

            case MistEffectType.MovePlus2:

                // TODO
                break;


            case MistEffectType.FlashBadge:

                // TODO
                break;


            case MistEffectType.PowerCake:

                // TODO
                break;


            case MistEffectType.RespawnCoffin:

                // TODO
                break;


            // =================================================
            // Yellow
            // =================================================

            case MistEffectType.UTurn:

                // TODO
                break;


            case MistEffectType.TimeBomb:

                // TODO
                break;


            case MistEffectType.MirrorPortal:

                // TODO
                break;


            case MistEffectType.MagnaTornado:

                // TODO
                break;


            // =================================================
            // Black
            // =================================================

            case MistEffectType.RandomEffect:

                // TODO
                break;


            case MistEffectType.Jackpot:

                // TODO
                break;


            case MistEffectType.Bug:

                // TODO
                break;
        }
    }


    // =========================================================
    // Status共通
    // =========================================================

    bool IsValidPlayerIndex(
        int playerIndex
    )
    {
        return
            playerIndex >= 0 &&
            playerIndex <
            playerStatusEffects.Length;
    }


    void AddStatus(
        int playerIndex,
        StatusEffectType type,
        int value
    )
    {
        if (!IsValidPlayerIndex(playerIndex))
            return;

        if (value <= 0)
            return;

        // 末尾へ追加
        // Listの並び順 = 付与順
        playerStatusEffects[
            playerIndex
        ].Add(
            new StatusEffectEntry(
                type,
                value
            )
        );

        RefreshAllStatusUI();
    }


    int GetStatusTotal(
        int playerIndex,
        StatusEffectType type
    )
    {
        if (!IsValidPlayerIndex(playerIndex))
            return 0;

        int total = 0;

        foreach (
            StatusEffectEntry status
            in playerStatusEffects[playerIndex]
        )
        {
            if (status.type != type)
                continue;

            total +=
                status.value;
        }

        return total;
    }


    // =========================================================
    // Bind
    // =========================================================

    public void AddBind(
        int playerIndex,
        int amount
    )
    {
        AddStatus(
            playerIndex,
            StatusEffectType.Bind,
            amount
        );
    }


    public int GetBindCount(
        int playerIndex
    )
    {
        return GetStatusTotal(
            playerIndex,
            StatusEffectType.Bind
        );
    }


    // =========================================================
    // Bind：移動トリガー
    // =========================================================

    public int ApplyBindToMovement(
        int playerIndex,
        int moveAmount
    )
    {
        if (!IsValidPlayerIndex(playerIndex))
            return moveAmount;

        if (moveAmount <= 0)
            return moveAmount;

        List<StatusEffectEntry> list =
            playerStatusEffects[
                playerIndex
            ];

        int remainingMove =
            moveAmount;

        int i = 0;

        while (
            i < list.Count &&
            remainingMove > 0
        )
        {
            StatusEffectEntry status =
                list[i];

            if (
                status.type !=
                StatusEffectType.Bind
            )
            {
                i++;
                continue;
            }

            int consumed =
                Mathf.Min(
                    status.value,
                    remainingMove
                );

            status.value -=
                consumed;

            remainingMove -=
                consumed;

            if (status.value <= 0)
            {
                list.RemoveAt(i);
            }
            else
            {
                i++;
            }
        }

        RefreshAllStatusUI();

        return remainingMove;
    }


    // =========================================================
    // Protector
    // =========================================================

    public void AddProtector(
        int playerIndex,
        int amount
    )
    {
        AddStatus(
            playerIndex,
            StatusEffectType.Protector,
            amount
        );
    }


    public int GetProtectorCount(
        int playerIndex
    )
    {
        return GetStatusTotal(
            playerIndex,
            StatusEffectType.Protector
        );
    }


    // =========================================================
    // Protector：妨害トリガー
    // =========================================================

    bool TryBlockInterference(
        int targetPlayerIndex,
        string effectName
    )
    {
        if (!IsValidPlayerIndex(targetPlayerIndex))
            return false;

        List<StatusEffectEntry> list =
            playerStatusEffects[
                targetPlayerIndex
            ];

        for (int i = 0; i < list.Count; i++)
        {
            StatusEffectEntry status =
                list[i];

            if (
                status.type !=
                StatusEffectType.Protector
            )
            {
                continue;
            }

            ConsumeOneFromStatus(
                targetPlayerIndex,
                i
            );

            RefreshAllStatusUI();

            return true;
        }

        return false;
    }


    void ConsumeOneFromStatus(
        int playerIndex,
        int listIndex
    )
    {
        List<StatusEffectEntry> list =
            playerStatusEffects[
                playerIndex
            ];

        if (
            listIndex < 0 ||
            listIndex >= list.Count
        )
        {
            return;
        }

        list[listIndex].value--;

        if (
            list[listIndex].value <= 0
        )
        {
            list.RemoveAt(
                listIndex
            );
        }
    }


    // =========================================================
    // Counter
    // =========================================================

    public void AddCounter(
        int playerIndex,
        int turns
    )
    {
        AddStatus(
            playerIndex,
            StatusEffectType.Counter,
            turns
        );
    }


    public int GetCounterTurns(
        int playerIndex
    )
    {
        return GetStatusTotal(
            playerIndex,
            StatusEffectType.Counter
        );
    }


    public bool HasCounter(
        int playerIndex
    )
    {
        return
            GetCounterTurns(
                playerIndex
            ) > 0;
    }


    // =========================================================
    // 攻撃処理
    //
    // 防御側の状態を付与順で参照し、
    // 最初にトリガーへ該当した状態を発動
    // =========================================================

    public AttackResult ResolveAttack(
        int attackerPlayerIndex,
        int defenderPlayerIndex
    )
    {
        if (
            !IsValidPlayerIndex(
                attackerPlayerIndex
            ) ||
            !IsValidPlayerIndex(
                defenderPlayerIndex
            )
        )
        {
            return AttackResult.NormalHit;
        }

        List<StatusEffectEntry> defenderList =
            playerStatusEffects[
                defenderPlayerIndex
            ];

        for (
            int i = 0;
            i < defenderList.Count;
            i++
        )
        {
            StatusEffectEntry status =
                defenderList[i];

            switch (status.type)
            {
                // =============================================
                // Protector
                // =============================================

                case StatusEffectType.Protector:

                    ConsumeOneFromStatus(
                        defenderPlayerIndex,
                        i
                    );

                    RefreshAllStatusUI();

                    return
                        AttackResult.BlockedByProtector;


                // =============================================
                // Counter
                // =============================================

                case StatusEffectType.Counter:

                    bool counterBlocked =
                        TryBlockCounterAttack(
                            attackerPlayerIndex
                        );

                    RefreshAllStatusUI();

                    if (counterBlocked)
                    {
                        return
                            AttackResult.CounterBlocked;
                    }

                    return
                        AttackResult.CounterHit;


                // Bind / MistPlusなどは
                // 被攻撃トリガーでは発動しない
                default:
                    continue;
            }
        }

        return AttackResult.NormalHit;
    }


    // =========================================================
    // Counter反撃に対するProtector
    // =========================================================

    bool TryBlockCounterAttack(
        int targetPlayerIndex
    )
    {
        if (!IsValidPlayerIndex(targetPlayerIndex))
            return false;

        List<StatusEffectEntry> list =
            playerStatusEffects[
                targetPlayerIndex
            ];

        for (
            int i = 0;
            i < list.Count;
            i++
        )
        {
            StatusEffectEntry status =
                list[i];

            if (
                status.type !=
                StatusEffectType.Protector
            )
            {
                continue;
            }

            ConsumeOneFromStatus(
                targetPlayerIndex,
                i
            );

            return true;
        }

        return false;
    }


    // =========================================================
    // プレイヤーのターン終了
    // =========================================================

    public void OnPlayerTurnEnded(
        int playerIndex
    )
    {
        if (!IsValidPlayerIndex(playerIndex))
            return;

        TickTurnStatus(
            playerIndex,
            StatusEffectType.Counter
        );

        TickTurnStatus(
            playerIndex,
            StatusEffectType.MistPlus
        );

        RefreshAllStatusUI();
    }


    // =========================================================
    // ターン型状態を1ターン減らす
    //
    // 同種状態が複数ある場合は、
    // 最も古いもの1件だけ減らす
    // =========================================================

    void TickTurnStatus(
        int playerIndex,
        StatusEffectType type
    )
    {
        if (!IsValidPlayerIndex(playerIndex))
            return;

        List<StatusEffectEntry> list =
            playerStatusEffects[
                playerIndex
            ];

        for (int i = 0; i < list.Count; i++)
        {
            StatusEffectEntry status =
                list[i];

            if (status.type != type)
                continue;

            status.value--;

            if (status.value <= 0)
            {
                list.RemoveAt(i);
            }

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

            // 角は設置対象外
            if (IsCorner(grid))
                continue;

            // 既存Holeは設置対象外
            if (
                holeOwnerByPathIndex.ContainsKey(i)
            )
            {
                continue;
            }

            candidates.Add(i);
        }

        if (candidates.Count == 0)
            return;

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
        return
            holeOwnerByPathIndex.TryGetValue(
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
        if (
            holeMarkerPrefab == null ||
            boardManager == null
        )
        {
            return;
        }

        if (
            holeSprites == null ||
            playerIndex < 0 ||
            playerIndex >= holeSprites.Length ||
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
            Destroy(marker);

            Debug.LogWarning(
                "[HoleTrap] PrefabにSpriteRendererがありません"
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
            !holeVisualsByPathIndex.TryGetValue(
                holePathIndex,
                out GameObject marker
            )
        )
        {
            return;
        }

        if (marker != null)
        {
            Destroy(marker);
        }

        holeVisualsByPathIndex.Remove(
            holePathIndex
        );
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
            if (i == attackerPlayerIndex)
                continue;

            if (
                gameManager.GetMistCount(i) <= 0
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
            TryBlockInterference(
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

        gameManager.RemoveMist(
            targetPlayerIndex,
            targetMistIndex
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
            if (i == attackerPlayerIndex)
                continue;

            if (
                gameManager.GetMistCount(i) <= 0
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
            TryBlockInterference(
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
            if (i == attackerPlayerIndex)
                continue;

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
            TryBlockInterference(
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
    }


    // =========================================================
    // Status UI
    // =========================================================

    public void RefreshAllStatusUI()
    {
        if (gameManager == null)
            return;

        int playerIndex =
            gameManager.GetCurrentPlayerIndex();

        if (!IsValidPlayerIndex(playerIndex))
            return;

        RefreshStatusIcon(
            "BindStatus",
            bindSprite,
            GetBindCount(playerIndex)
        );

        RefreshStatusIcon(
            "ProtectorStatus",
            protectorSprite,
            GetProtectorCount(playerIndex)
        );

        RefreshStatusIcon(
            "CounterStatus",
            counterSprite,
            GetCounterTurns(playerIndex)
        );

        RefreshStatusIcon(
            "MistPlusStatus",
            mistPlusSprite,
            GetStatusTotal(
                playerIndex,
                StatusEffectType.MistPlus
            )
        );
    }


    // =========================================================
    // 互換ラッパー
    // =========================================================

    public void RefreshBindUI()
    {
        RefreshAllStatusUI();
    }


    public void RefreshProtectorUI()
    {
        RefreshAllStatusUI();
    }


    public void RefreshCounterUI()
    {
        RefreshAllStatusUI();
    }


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
                "[Status UI] Icon Prefabが設定されていません"
            );

            return;
        }

        if (mainSprite == null)
        {
            Debug.LogWarning(
                $"[Status UI] {statusName} Spriteが設定されていません"
            );

            return;
        }

        if (
            statusCountSprites == null ||
            statusCountSprites.Length == 0
        )
        {
            Debug.LogWarning(
                "[Status UI] Count Spriteが設定されていません"
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
                $"[Status UI] " +
                $"{statusName} Prefab内にCountがありません"
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
    // Blue : MistPlus（アトラリング）
    // =========================================================

    public void AddMistPlus(
        int playerIndex,
        int turns
    )
    {
        AddStatus(
            playerIndex,
            StatusEffectType.MistPlus,
            turns
        );
    }


    public bool HasMistPlus(
        int playerIndex
    )
    {
        return
            GetStatusTotal(
                playerIndex,
                StatusEffectType.MistPlus
            ) > 0;
    }


    // =========================================================
    // Mist増加量へMistPlusを適用
    //
    // Mist増加イベント1回につき +1
    // 例：
    // 1個獲得 → 2個
    // 2個獲得 → 3個
    // =========================================================

    public int ApplyMistPlus(
        int playerIndex,
        int baseAmount
    )
    {
        if (baseAmount <= 0)
            return baseAmount;

        if (!HasMistPlus(playerIndex))
            return baseAmount;

        return
            baseAmount + 1;
    }
}