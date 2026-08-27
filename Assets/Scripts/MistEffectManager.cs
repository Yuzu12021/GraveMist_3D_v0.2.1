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

    // 防御側のProtectorで通常攻撃を無効化
    BlockedByProtector,

    // 防御側Counter発動 → 反撃成功
    CounterHit,

    // Counter発動したが
    // 攻撃者側Protectorで反撃を防御
    CounterBlocked
}


// =========================================================
// 付与状態1件
//
// Listへの追加順 = 付与された順番
// value:
// Bind      → 個数
// Protector → 個数
// Counter   → 残りターン
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
    // ★ Listの並び順そのものが
    //   「付与されたタイミングの早い順」
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
    // Mist使用入口
    // =========================================================

    public MistEffectType GetRandomEffectForColor(
    GameManager.MistColor color
)
    {
        return GetRandomEffect(color);
    }

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
    // 色別抽選
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

                    // Counterテスト時は↑をコメントアウトして
                    // return MistEffectType.Counter;
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
    // 効果実行
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

                Debug.Log(
                    $"Player {playerIndex + 1} : Analyzer"
                );

                // TODO
                break;


            case MistEffectType.Counter:

                AddCounter(
                    playerIndex,
                    2
                );

                break;


            case MistEffectType.MistPlus:

                Debug.Log(
                    $"Player {playerIndex + 1} : MistPlus"
                );

                AddMistPlus(
                    playerIndex,
                    1
                );

                break;


            // =================================================
            // Green
            // =================================================

            case MistEffectType.MovePlus2:

                Debug.Log(
                    $"Player {playerIndex + 1} : MovePlus2"
                );

                // TODO
                break;


            case MistEffectType.FlashBadge:

                Debug.Log(
                    $"Player {playerIndex + 1} : FlashBadge"
                );

                // TODO
                break;


            case MistEffectType.PowerCake:

                Debug.Log(
                    $"Player {playerIndex + 1} : PowerCake"
                );

                // TODO
                break;


            case MistEffectType.RespawnCoffin:

                Debug.Log(
                    $"Player {playerIndex + 1} : RespawnCoffin"
                );

                // TODO
                break;


            // =================================================
            // Yellow
            // =================================================

            case MistEffectType.UTurn:

                Debug.Log(
                    $"Player {playerIndex + 1} : UTurn"
                );

                // TODO
                break;


            case MistEffectType.TimeBomb:

                Debug.Log(
                    $"Player {playerIndex + 1} : TimeBomb"
                );

                // TODO
                break;


            case MistEffectType.MirrorPortal:

                Debug.Log(
                    $"Player {playerIndex + 1} : MirrorPortal"
                );

                // TODO
                break;


            case MistEffectType.MagnaTornado:

                Debug.Log(
                    $"Player {playerIndex + 1} : MagnaTornado"
                );

                // TODO
                break;


            // =================================================
            // Black
            // =================================================

            case MistEffectType.RandomEffect:

                Debug.Log(
                    $"Player {playerIndex + 1} : RandomEffect"
                );

                // TODO
                break;


            case MistEffectType.Jackpot:

                Debug.Log(
                    $"Player {playerIndex + 1} : Jackpot"
                );

                // TODO
                break;


            case MistEffectType.Bug:

                Debug.Log(
                    $"Player {playerIndex + 1} : Bug"
                );

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

        // ★ 必ず末尾へ追加
        // これが付与順になる
        playerStatusEffects[
            playerIndex
        ].Add(
            new StatusEffectEntry(
                type,
                value
            )
        );

        Debug.Log(
            $"[Status Add] " +
            $"Player {playerIndex + 1} / " +
            $"{type} / value={value}"
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

        Debug.Log(
            $"[Bind] Player {playerIndex + 1} " +
            $"+{amount} / " +
            $"合計 {GetBindCount(playerIndex)}"
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
    //
    // Bindだけが「移動」に反応するため
    // 古いBindから順に消費
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

        int consumedTotal =
            0;

        int i = 0;

        while (
            i < list.Count &&
            remainingMove > 0
        )
        {
            StatusEffectEntry status =
                list[i];

            // 移動トリガーに
            // Bind以外は反応しない
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

            consumedTotal +=
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

        Debug.Log(
            $"[Bind] Player {playerIndex + 1} / " +
            $"移動 {moveAmount} - Bind {consumedTotal} " +
            $"= {remainingMove} / " +
            $"残Bind {GetBindCount(playerIndex)}"
        );

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

        Debug.Log(
            $"[Protector] Player {playerIndex + 1} " +
            $"+{amount} / " +
            $"合計 {GetProtectorCount(playerIndex)}"
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
    //
    // Shot / ColorBall / Bindなど。
    //
    // 付与順に確認するが、
    // このトリガーに引っかかるのはProtectorだけ。
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

        for (
            int i = 0;
            i < list.Count;
            i++
        )
        {
            StatusEffectEntry status =
                list[i];

            // この妨害トリガーには
            // Protectorだけが反応
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

            Debug.Log(
                $"[Protector] " +
                $"Player {targetPlayerIndex + 1} が " +
                $"{effectName} を無効化"
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

        Debug.Log(
            $"[Counter] Player {playerIndex + 1} " +
            $"+{turns}ターン / " +
            $"合計残り {GetCounterTurns(playerIndex)}"
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
    // ★ defenderの付与状態を
    //   古い順から見る
    //
    // Protector → Counter の順で付与されていれば
    // Protector発動
    //
    // Counter → Protector の順なら
    // Counter発動
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

        // =========================================
        // 防御側の状態を付与順で確認
        // =========================================

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
                // =================================
                // Protector
                // 被攻撃トリガーに一致
                // =================================

                case StatusEffectType.Protector:

                    ConsumeOneFromStatus(
                        defenderPlayerIndex,
                        i
                    );

                    Debug.Log(
                        $"[Attack] " +
                        $"Player {defenderPlayerIndex + 1} の " +
                        $"Protectorが先に発動 → 攻撃無効"
                    );

                    RefreshAllStatusUI();

                    return
                        AttackResult.BlockedByProtector;


                // =================================
                // Counter
                // 被攻撃トリガーに一致
                // =================================

                case StatusEffectType.Counter:

                    Debug.Log(
                        $"[Counter] " +
                        $"Player {defenderPlayerIndex + 1} が " +
                        $"攻撃を無効化して反撃"
                    );

                    // =================================
                    // Counterの反撃先
                    // =================================
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


                // Bindは攻撃に反応しない
                default:
                    continue;
            }
        }

        // ProtectorもCounterもなし
        return AttackResult.NormalHit;
    }


    // =========================================================
    // Counter反撃に対する防御
    //
    // 現時点ではCounter→Counter→Counter...の
    // 無限連鎖を避けるため、
    // 「Counterによる反撃」にはProtectorだけ反応させる。
    //
    // ここは将来ルール確定時に変更可能。
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

            Debug.Log(
                $"[Counter] " +
                $"Player {targetPlayerIndex + 1} の " +
                $"Protectorが反撃を無効化"
            );

            return true;
        }

        return false;
    }


    // =========================================================
    // プレイヤーのターン終了
    //
    // ターンタイプの付与効果を1ターン進める
    // =========================================================
    public void OnPlayerTurnEnded(
        int playerIndex
    )
    {
        if (!IsValidPlayerIndex(playerIndex))
            return;

        // Counter
        TickTurnStatus(
            playerIndex,
            StatusEffectType.Counter
        );

        // MistPlus
        TickTurnStatus(
            playerIndex,
            StatusEffectType.MistPlus
        );

        RefreshAllStatusUI();
    }


    // =========================================================
    // ターンタイプ状態を1ターン減らす
    //
    // 同じ状態が複数回付与されている場合、
    // 最も古く付与されたものから消化する
    // =========================================================
    void TickTurnStatus(
        int playerIndex,
        StatusEffectType type
    )
    {
        if (!IsValidPlayerIndex(playerIndex))
            return;

        List<StatusEffectEntry> list =
            playerStatusEffects[playerIndex];

        for (int i = 0; i < list.Count; i++)
        {
            StatusEffectEntry status =
                list[i];

            if (status.type != type)
                continue;

            // =========================================
            // 一番古い同種効果を1ターン減らす
            // =========================================
            status.value--;

            Debug.Log(
                $"[Turn Status] " +
                $"Player {playerIndex + 1} / " +
                $"{type} -1 → " +
                $"{GetStatusTotal(playerIndex, type)}"
            );

            // 0になったら消滅
            if (status.value <= 0)
            {
                list.RemoveAt(i);

                Debug.Log(
                    $"[Turn Status] " +
                    $"Player {playerIndex + 1} / " +
                    $"{type} が終了"
                );
            }

            // ★ 1ターンで減らすのは
            // 最も古い同種効果1件だけ
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

            // 現仕様：角は除外
            if (IsCorner(grid))
                continue;

            // 既存Holeマスは除外
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
                "[HoleTrap] 設置可能マスなし"
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

        CreateHoleVisual(
            holePathIndex,
            playerIndex
        );

        Debug.Log(
            $"[HoleTrap] Player {playerIndex + 1} " +
            $"→ pathIndex={holePathIndex}"
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
                "[HoleTrap] PrefabにSpriteRendererなし"
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
        {
            Debug.Log(
                "[Shot] 破壊できるMistなし"
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

        // Protectorは妨害トリガー
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
            $"[Shot] " +
            $"Player {attackerPlayerIndex + 1} → " +
            $"Player {targetPlayerIndex + 1} / " +
            $"{destroyedMist}破壊"
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

        Debug.Log(
            $"[ColorBall] " +
            $"Player {targetPlayerIndex + 1} → " +
            $"{newColor} に統一"
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

        Debug.Log(
            $"[Bind] " +
            $"Player {attackerPlayerIndex + 1} → " +
            $"Player {targetPlayerIndex + 1} / +2"
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
            GetStatusTotal(playerIndex,StatusEffectType.MistPlus)
        );
    }


    // 旧GameManager側から呼ばれても壊れないよう
    // ラッパーは残しておく
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
                "[Status UI] Icon Prefabなし"
            );

            return;
        }

        if (mainSprite == null)
        {
            Debug.LogWarning(
                $"[Status UI] {statusName} Spriteなし"
            );

            return;
        }

        if (
            statusCountSprites == null ||
            statusCountSprites.Length == 0
        )
        {
            Debug.LogWarning(
                "[Status UI] Count Spriteなし"
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
                $"{statusName} Prefab内にCountなし"
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
    // Blue : MistPlus
    // 自分に1ターンのMistPlusを付与
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

        Debug.Log(
            $"[MistPlus] Player {playerIndex + 1} " +
            $"+{turns}ターン"
        );
    }


    // =========================================================
    // MistPlusが有効か
    // =========================================================
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
    // ミスト増加量へMistPlusを適用
    //
    // 元が1 → 2
    // 元が2 → 3
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

        int result =
            baseAmount + 1;

        Debug.Log(
            $"[MistPlus] Player {playerIndex + 1} / " +
            $"Mist増加 {baseAmount} → {result}"
        );

        return result;
    }
}