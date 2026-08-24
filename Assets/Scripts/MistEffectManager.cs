using UnityEngine;
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
    [SerializeField]
    private GameManager gameManager;

    [SerializeField]
    private BoardManager boardManager;

    [Header("Hole Trap Visual")]
    [SerializeField]
    private GameObject holeMarkerPrefab;

    // Element 0 = 1P用（1.png）
    // Element 1 = 2P用（2.png）
    // Element 2 = 3P用（3.png）
    // Element 3 = 4P用（4.png）
    [SerializeField]
    private Sprite[] holeSprites;

    private Dictionary<int, int> holeOwnerByPathIndex =
    new Dictionary<int, int>();

    private Dictionary<int, GameObject> holeVisualsByPathIndex =
        new Dictionary<int, GameObject>();

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

    bool IsCorner(Vector2Int grid)
    {
        int max =
            boardManager.gridSize - 1;

        return
            (grid.x == 0 || grid.x == max) &&
            (grid.y == 0 || grid.y == max);
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
                    return MistEffectType.ColorBall;
                }


            // =========================
            // Blue
            // =========================
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

                // TODO:
                // 敵にBindを2個付与

                break;


            // =====================================================
            // Blue
            // =====================================================

            case MistEffectType.Protector:
                Debug.Log(
                    $"Player {playerIndex + 1} : Protector"
                );

                // TODO:
                // 自分にProtectorを1個付与

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

                // TODO:
                // 自分に2ターンCounter付与

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
                // 自分に1ターン移動+2付与

                break;


            case MistEffectType.FlashBadge:
                Debug.Log(
                    $"Player {playerIndex + 1} : FlashBadge"
                );

                // TODO:
                // 前後3マスへの任意ワープ

                break;


            case MistEffectType.PowerCake:
                Debug.Log(
                    $"Player {playerIndex + 1} : PowerCake"
                );

                // TODO:
                // 自分に1ターン移動2倍付与

                break;


            case MistEffectType.RespawnCoffin:
                Debug.Log(
                    $"Player {playerIndex + 1} : RespawnCoffin"
                );

                // TODO:
                // 自分または召喚ユニットを
                // 初期地点へ任意ワープ

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
                // 全ユニットに1ターン移動半減

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
                // 全員ランダムに前後3マスワープ

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
                // 自分を1段階進化

                break;


            case MistEffectType.Bug:
                Debug.Log(
                    $"Player {playerIndex + 1} : Bug"
                );

                // TODO:
                // 全ユニットのステータスを
                // ランダム化

                break;
        }
    }

    void ActivateHole(int playerIndex)
    {
        // 外周マスのうち、角を除いたマスだけ候補にする
        List<int> candidates = new List<int>();

        for (int i = 0; i < boardManager.outerPath.Count; i++)
        {
            Vector2Int grid = boardManager.outerPath[i];

            // 角は除外
            if (IsCorner(grid))
                continue;

            // すでに穴があるマスは除外
            if (holeOwnerByPathIndex.ContainsKey(i))
                continue;

            candidates.Add(i);
        }

        if (candidates.Count == 0)
        {
            Debug.Log("Holeを置けるマスがありません");
            return;
        }

        int randomIndex =
            Random.Range(0, candidates.Count);

        int holePathIndex =
            candidates[randomIndex];

        // ロジック上のHoleを登録
        holeOwnerByPathIndex[holePathIndex] =
            playerIndex;

        Vector2Int holeGrid =
            boardManager.outerPath[holePathIndex];

        Debug.Log(
            $"Player {playerIndex + 1} placed Hole at {holeGrid} " +
            $"(pathIndex={holePathIndex})"
        );

        CreateHoleVisual(
            holePathIndex,
            playerIndex
        );
    }

    // =========================================================
    // Red : Shot
    // 敵プレイヤーのMistをランダムで1つ破壊
    // =========================================================
    void ActivateShot(int attackerPlayerIndex)
    {
        if (gameManager == null)
        {
            Debug.LogWarning(
                "[Shot] GameManager が設定されていません"
            );
            return;
        }

        // =========================================
        // Mistを持っている敵プレイヤーを探す
        // =========================================
        List<int> candidates =
            new List<int>();

        for (int i = 0; i < 4; i++)
        {
            // 自分自身は対象外
            if (i == attackerPlayerIndex)
                continue;

            // Mistを1個以上持っているプレイヤーだけ対象
            if (gameManager.GetMistCount(i) <= 0)
                continue;

            candidates.Add(i);
        }

        // =========================================
        // 対象が誰もいない
        // =========================================
        if (candidates.Count == 0)
        {
            Debug.Log(
                $"[Shot] Player {attackerPlayerIndex + 1} / " +
                "破壊できる敵Mistがありません"
            );

            return;
        }

        // =========================================
        // 敵プレイヤーをランダムで1人選択
        // =========================================
        int targetPlayerIndex =
            candidates[
                Random.Range(
                    0,
                    candidates.Count
                )
            ];

        // =========================================
        // そのプレイヤーのMistからランダムで1個選択
        // =========================================
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

        // =========================================
        // Mist破壊
        // =========================================
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
    // 敵プレイヤー1人の所持Mistを
    // 同じランダム1色に染める
    // =========================================================
    void ActivateColorBall(int attackerPlayerIndex)
    {
        if (gameManager == null)
        {
            Debug.LogWarning(
                "[ColorBall] GameManager が設定されていません"
            );
            return;
        }

        // =========================================
        // Mistを持っている敵を候補にする
        // =========================================
        List<int> candidates =
            new List<int>();

        for (int i = 0; i < 4; i++)
        {
            if (i == attackerPlayerIndex)
                continue;

            if (gameManager.GetMistCount(i) <= 0)
                continue;

            candidates.Add(i);
        }

        if (candidates.Count == 0)
        {
            Debug.Log(
                $"[ColorBall] Player {attackerPlayerIndex + 1} / " +
                "対象にできる敵がいません"
            );

            return;
        }

        // =========================================
        // 対象プレイヤーをランダム選択
        // =========================================
        int targetPlayerIndex =
            candidates[
                Random.Range(
                    0,
                    candidates.Count
                )
            ];

        // =========================================
        // 新しい色をランダム選択
        // Blackは通常色変化では除外
        // =========================================
        GameManager.MistColor newColor =
            (GameManager.MistColor)Random.Range(
                1,
                5
            );

        int mistCount =
            gameManager.GetMistCount(
                targetPlayerIndex
            );

        // =========================================
        // 全Mistを同じ色に変更
        // =========================================
        for (int i = 0; i < mistCount; i++)
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
                "HoleTrap: Hole Sprites に1P～4P用の4枚を設定してください"
            );
            return;
        }

        if (
            playerIndex < 0 ||
            playerIndex >= holeSprites.Length
        )
        {
            Debug.LogWarning(
                $"HoleTrap: playerIndex が範囲外です: {playerIndex}"
            );
            return;
        }

        if (holeSprites[playerIndex] == null)
        {
            Debug.LogWarning(
                $"HoleTrap: {playerIndex + 1}P用Spriteが設定されていません"
            );
            return;
        }

        Vector2Int grid =
            boardManager.outerPath[holePathIndex];

        Vector3 pos =
            boardManager.GridToWorld(
                grid.x,
                grid.y
            );

        // 盤面より少し上
        pos.y = 5.05f;

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
            Debug.LogWarning(
                "HoleTrap: Hole Marker Prefab に SpriteRenderer がありません"
            );

            Destroy(marker);
            return;
        }

        // =========================================
        // 設置プレイヤーによって見た目変更
        // 0=1P, 1=2P, 2=3P, 3=4P
        // =========================================
        sr.sprite =
            holeSprites[playerIndex];

        holeVisualsByPathIndex[holePathIndex] =
            marker;

        Debug.Log(
            $"[HoleTrap Visual] " +
            $"{playerIndex + 1}P用アイコンを表示"
        );
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
                Destroy(marker);
            }

            holeVisualsByPathIndex.Remove(
                holePathIndex
            );
        }
    }
}