using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using System.Collections.Generic;

// 旧GameManager / MistDataの移行が終わるまで型を残す。
// 新Mist効果の識別には使用しない。
public enum MistEffectType
{
    HoleTrap, Shot, ColorBall, Bind,
    Protector, Analyzer, Counter, MistPlus,
    MovePlus2, FlashBadge, PowerCake, RespawnCoffin,
    UTurn, TimeBomb, MirrorPortal, MagnaTornado,
    RandomEffect, Jackpot, Bug
}

public enum StatusEffectType
{
    Bind, Protector, Counter, MistPlus, MovePlus2, PowerCake
}

public enum AttackResult
{
    NormalHit, BlockedByProtector, CounterHit, CounterBlocked
}

public class StatusEffectEntry
{
    public StatusEffectType type;
    public int value;

    public StatusEffectEntry(StatusEffectType type, int value)
    {
        this.type = type;
        this.value = value;
    }
}

[System.Serializable]
public class MistEffectSpriteData
{
    public MistEffectType effect;
    public Sprite sprite;
}

public class MistEffectManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private BoardManager boardManager;

    // 旧Statusアイコンとの互換。新効果アイコンへの移行後に整理。
    [Header("Legacy Status Effect Icons")]
    [SerializeField] private MistEffectSpriteData[] mistEffectSprites;

    [Header("Hole Visual (temporary compatibility)")]
    [SerializeField] private GameObject holeMarkerPrefab;
    [SerializeField] private Sprite[] holeSprites;

    private readonly Dictionary<int, int> holeOwnerByPathIndex =
        new Dictionary<int, int>();
    private readonly Dictionary<int, GameObject> holeVisualsByPathIndex =
        new Dictionary<int, GameObject>();

    [Header("Status UI")]
    [SerializeField]
    [FormerlySerializedAs("currentPlayerMistHolder")]
    private Transform statusIconsHolder;

    [SerializeField] private Sprite[] statusCountSprites;

    [System.Serializable]
    public class NewMistStatusIcon
    {
        public string effectId;
        public Sprite sprite;
    }

    [Header("New Mist Status Icons")]
    [SerializeField] private GameObject newMistStatusIconPrefab;
    [SerializeField] private NewMistStatusIcon[] newMistStatusIcons;

    private readonly Dictionary<string, GameObject> activeNewMistIcons =
        new Dictionary<string, GameObject>();

    public void ShowNewMistStatusIcon(string effectId)
    {
        if (statusIconsHolder == null || newMistStatusIconPrefab == null)
            return;

        if (activeNewMistIcons.TryGetValue(effectId, out GameObject existing)
            && existing != null)
            return;

        Sprite sprite = null;

        foreach (NewMistStatusIcon entry in newMistStatusIcons)
        {
            if (entry != null && entry.effectId == effectId)
            {
                sprite = entry.sprite;
                break;
            }
        }

        if (sprite == null)
        {
            Debug.LogWarning($"[Mist Status] Sprite未設定: {effectId}");
            return;
        }

        GameObject icon = Instantiate(
            newMistStatusIconPrefab,
            statusIconsHolder
        );

        icon.name = "NewMist_" + effectId;

        Image image = icon.GetComponent<Image>();

        if (image == null)
            image = icon.GetComponentInChildren<Image>();

        if (image == null)
        {
            Destroy(icon);
            Debug.LogWarning("[Mist Status] PrefabにImageがありません");
            return;
        }

        image.sprite = sprite;
        image.color = Color.white;
        image.enabled = true;

        activeNewMistIcons[effectId] = icon;
    }

    public void HideNewMistStatusIcon(string effectId)
    {
        if (!activeNewMistIcons.TryGetValue(effectId, out GameObject icon))
            return;

        activeNewMistIcons.Remove(effectId);

        if (icon != null)
        {
            icon.transform.SetParent(null, false);
            Destroy(icon);
        }
    }

    private readonly List<StatusEffectEntry>[] playerStatusEffects =
    {
        new List<StatusEffectEntry>(),
        new List<StatusEffectEntry>(),
        new List<StatusEffectEntry>(),
        new List<StatusEffectEntry>()
    };

    // InspectorのeffectIdと完全一致（大文字小文字も一致）させる。
    private static readonly HashSet<string> NewEffectIds =
        new HashSet<string>(System.StringComparer.Ordinal)
        {
            "trap", "engine", "grave+1", "grave-1",
            "metal", "mistPlus", "light", "Garter guard",
            "Uturn", "shuffle", "Aerocoat",
            "ColorBall", "Trap Coat", "Graveshower", "Evolution"
        };

    public bool IsKnownNewEffectId(string effectId)
    {
        return !string.IsNullOrEmpty(effectId) &&
               NewEffectIds.Contains(effectId);
    }

    // 新Mist効果の唯一の入口。
    // 現段階では15種類のID認識のみ実装済み。
    // 効果本体が未実装なのでfalseを返し、消費処理を進めない。
    public bool TryExecuteEffectById(string effectId, int playerIndex)
    {
        if (!IsValidPlayerIndex(playerIndex))
        {
            Debug.LogWarning($"[NewMist] 無効なプレイヤー: {playerIndex}");
            return false;
        }

        if (!IsKnownNewEffectId(effectId))
        {
            Debug.LogWarning($"[NewMist] 未登録のeffectId: {effectId}");
            return false;
        }

        switch (effectId)
        {
            case "trap":
                return ActivateTrap(playerIndex);

            case "engine":
                return gameManager != null && gameManager.ActivateEngine(playerIndex);
            case "grave+1":
                return gameManager != null && gameManager.ActivateGravePlusOne(playerIndex);
            case "Uturn":
                if (gameManager == null)
                    return false;

                gameManager.ActivateUturn(playerIndex);

                Debug.Log("[NewMist] Uターン発動！");
                return true;

            case "shuffle":
                if (gameManager == null)
                    return false;

                bool shuffled = gameManager.ActivateShuffle();

                if (shuffled)
                    Debug.Log("[NewMist] シャッフル発動！");

                return shuffled;
            case "ColorBall":
                if (gameManager == null)
                    return false;

                return gameManager.ActivateColorBall(playerIndex);
        }

        Debug.LogWarning(
            $"[NewMist] {effectId} は登録済みですが、効果本体は未実装です。Mistは消費しないでください。");
        return false;
    }

    // 旧GameManagerが参照するAPIのコンパイル互換。
    // 新Mistでは使わない。旧GameManagerの移行完了後に削除。
    [System.Obsolete("旧Mist専用。新Mistでは使用しないでください。")]
    public MistEffectType GetRandomEffectForColor(GameManager.MistColor color)
    {
        Debug.LogWarning("[LegacyMist] 旧効果抽選が呼ばれました。GameManagerの移行が必要です。");
        return MistEffectType.HoleTrap;
    }

    [System.Obsolete("旧Mist専用。新Mistでは使用しないでください。")]
    public void ExecuteStoredEffect(MistEffectType effect, int playerIndex)
    {
        Debug.LogWarning("[LegacyMist] 旧効果の実行は無効化されています。");
    }

    public Sprite GetMistEffectSprite(MistEffectType effect)
    {
        if (mistEffectSprites == null) return null;
        foreach (MistEffectSpriteData data in mistEffectSprites)
        {
            if (data != null && data.effect == effect)
                return data.sprite;
        }
        return null;
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

        TickTurnStatus(
            playerIndex,
            StatusEffectType.MovePlus2
        );

        TickTurnStatus(
            playerIndex,
            StatusEffectType.PowerCake
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


    // 旧盤面のHole参照互換。新trapの実装まで残す。
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


    // 新Mist trap：旧HoleTrapと同じ盤面トラップを設置する。
    // 設置可能マスがなければfalseを返し、GameManager側でコストを消費しない。
    public bool ActivateTrap(int playerIndex)
    {
        if (!IsValidPlayerIndex(playerIndex) ||
            gameManager == null ||
            boardManager == null ||
            boardManager.outerPath == null ||
            boardManager.outerPath.Count == 0)
        {
            Debug.LogWarning("[Trap] 参照またはプレイヤー番号が不正です");
            return false;
        }

        // 見えないトラップが発生しないよう、表示設定も確認する。
        if (holeMarkerPrefab == null ||
            holeMarkerPrefab.GetComponent<SpriteRenderer>() == null ||
            holeSprites == null ||
            playerIndex >= holeSprites.Length ||
            holeSprites[playerIndex] == null)
        {
            Debug.LogWarning("[Trap] Hole Marker Prefab / Hole Sprites の設定を確認してください");
            return false;
        }

        List<int> candidates = new List<int>();
        for (int i = 0; i < boardManager.outerPath.Count; i++)
        {
            if (IsCorner(boardManager.outerPath[i]))
                continue;

            if (holeOwnerByPathIndex.ContainsKey(i))
                continue;

            if (gameManager.IsAnyPlayerAtPathIndex(i))
                continue;

            candidates.Add(i);
        }

        if (candidates.Count == 0)
        {
            Debug.LogWarning("[Trap] 設置可能なマスがありません");
            return false;
        }

        int pathIndex = candidates[Random.Range(0, candidates.Count)];
        holeOwnerByPathIndex.Add(pathIndex, playerIndex);
        CreateHoleVisual(pathIndex, playerIndex);

        Debug.Log($"[Trap] {playerIndex + 1}P がパス {pathIndex} に設置");
        return true;
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
            5.8f;

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
            GetMistEffectSprite(MistEffectType.Bind),
            GetBindCount(playerIndex)
        );

        RefreshStatusIcon(
            "ProtectorStatus",
            GetMistEffectSprite(MistEffectType.Protector),
            GetProtectorCount(playerIndex)
        );

        RefreshStatusIcon(
            "CounterStatus",
            GetMistEffectSprite(MistEffectType.Counter),
            GetCounterTurns(playerIndex)
        );

        RefreshStatusIcon(
            "MistPlusStatus",
            GetMistEffectSprite(MistEffectType.MistPlus),
            GetStatusTotal(
                playerIndex,
                StatusEffectType.MistPlus
            )
        );

        RefreshStatusIcon(
            "MovePlus2Status",
            GetMistEffectSprite(MistEffectType.MovePlus2),
            GetStatusTotal(
                playerIndex,
                StatusEffectType.MovePlus2
            )
        );

        RefreshStatusIcon(
            "PowerCakeStatus",
            GetMistEffectSprite(MistEffectType.PowerCake),
            GetPowerCakeTurns(playerIndex)
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
            statusIconsHolder == null
        )
        {
            return;
        }

        Transform oldStatus =
    statusIconsHolder.Find(statusName);

        if (oldStatus != null)
        {
            // 先に親から外して、次のFindで見つからないようにする
            oldStatus.SetParent(null, false);
            Destroy(oldStatus.gameObject);
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
                statusIconsHolder
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

    // =========================================================
    // Green : MovePlus2（ミニエンジン）
    // =========================================================

    public void AddMovePlus2(
        int playerIndex,
        int turns
    )
    {
        AddStatus(
            playerIndex,
            StatusEffectType.MovePlus2,
            turns
        );
    }


    public bool HasMovePlus2(
        int playerIndex
    )
    {
        return
            GetStatusTotal(
                playerIndex,
                StatusEffectType.MovePlus2
            ) > 0;
    }


    // =========================================================
    // MovePlus2：移動量 +2
    // =========================================================

    public int ApplyMovePlus2(
        int playerIndex,
        int moveAmount
    )
    {
        if (!IsValidPlayerIndex(playerIndex))
            return moveAmount;

        if (moveAmount <= 0)
            return moveAmount;

        if (!HasMovePlus2(playerIndex))
            return moveAmount;

        return moveAmount + 2;
    }
    // =========================================================
    // Green : PowerCake（パワーケーキ）
    // =========================================================

    public void AddPowerCake(
        int playerIndex,
        int turns
    )
    {
        AddStatus(
            playerIndex,
            StatusEffectType.PowerCake,
            turns
        );
    }


    public int GetPowerCakeTurns(
        int playerIndex
    )
    {
        return GetStatusTotal(
            playerIndex,
            StatusEffectType.PowerCake
        );
    }
    // =========================================================
    // 移動系Statusを付与順に処理
    //
    // Bind      = 1個につき -1
    // MovePlus2 = +2
    // PowerCake = ×2
    //
    // playerStatusEffects の先頭から処理するため
    // そのまま「付与された順」になる
    // =========================================================

    public int ApplyMovementStatuses(
        int playerIndex,
        int baseMoveAmount
    )
    {
        if (!IsValidPlayerIndex(playerIndex))
            return baseMoveAmount;

        if (baseMoveAmount <= 0)
            return baseMoveAmount;

        List<StatusEffectEntry> list =
            playerStatusEffects[playerIndex];

        int moveAmount =
            baseMoveAmount;

        for (int i = 0; i < list.Count; i++)
        {
            StatusEffectEntry status =
                list[i];

            switch (status.type)
            {
                // =====================================
                // Bind
                // =====================================
                case StatusEffectType.Bind:

                    if (moveAmount > 0)
                    {
                        int consumed =
                            Mathf.Min(
                                status.value,
                                moveAmount
                            );

                        moveAmount -= consumed;
                        status.value -= consumed;

                        if (status.value <= 0)
                        {
                            list.RemoveAt(i);
                            i--;
                        }
                    }

                    break;


                // =====================================
                // MovePlus2
                // =====================================
                case StatusEffectType.MovePlus2:

                    moveAmount += 2;

                    break;


                // =====================================
                // PowerCake
                // =====================================
                case StatusEffectType.PowerCake:

                    moveAmount *= 2;

                    break;
            }
        }

        RefreshAllStatusUI();

        return moveAmount;
    }
}

