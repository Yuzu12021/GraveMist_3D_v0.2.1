using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSwipe : MonoBehaviour
{
    [Header("References")]
    public RectTransform[] displaySlots;   // 7枠（左右バッファ込み）
    public Image[] displaySlotImages;      // 7枠分
    public GameFlowController flow;

    [Header("Selection")]
    public RectTransform displaySlot3;     // 中央スロット
    public float upwardSelectThreshold = 100f;

    [Header("Character Data")]
    public Sprite[] characterIcons;

    [Header("Swipe Settings")]
    public float horizontalThreshold = 80f;

    [Header("Animation")]
    public float moveDuration = 0.22f;
    public float centerScale = 1.2f;
    public float sideScale = 0.9f;

    [Header("Select Animation")]
    public float selectMoveY = 140f;
    public float selectDuration = 0.2f;
    public float selectScale = 1.35f;
    public float selectFadeAlpha = 0.55f;

    int selectedCharacterIndex = 0;

    Vector2 swipeStart;

    bool touching = false;
    bool isAnimating = false;
    bool isSelecting = false;
    bool startedOnCenterSlot = false;

    const int CENTER_SLOT = 3;

    Vector2[] basePositions;

    [Header("Layout")]
    public float slotSpacing = 180f;


    void Start()
    {
        if (flow == null)
            flow = FindObjectOfType<GameFlowController>();

        if (!ValidateSetup())
            return;

        basePositions = new Vector2[displaySlots.Length];

        // CharacterBar全体の基準Y
        float fixedY = displaySlots[CENTER_SLOT].anchoredPosition.y;

        for (int i = 0; i < displaySlots.Length; i++)
        {
            float x = (i - CENTER_SLOT) * slotSpacing;

            basePositions[i] = new Vector2(x, fixedY);

            // 初期位置を完全固定
            displaySlots[i].anchoredPosition = basePositions[i];
        }

        RefreshDisplayImmediate();
        NotifyCenterCharacter();
    }


    bool ValidateSetup()
    {
        if (displaySlots == null || displaySlots.Length != 7)
        {
            Debug.LogError(
                "displaySlots は 7 個必要です。（左右バッファ込み）"
            );

            return false;
        }

        if (displaySlotImages == null ||
            displaySlotImages.Length != 7)
        {
            Debug.LogError(
                "displaySlotImages は 7 個必要です。（左右バッファ込み）"
            );

            return false;
        }

        if (characterIcons == null ||
            characterIcons.Length == 0)
        {
            Debug.LogError(
                "characterIcons が未設定です。"
            );

            return false;
        }

        if (displaySlot3 == null)
        {
            Debug.LogError(
                "displaySlot3 が未設定です。"
            );

            return false;
        }

        return true;
    }


    void Update()
    {
        HandleInput();
    }


    void HandleInput()
    {
#if UNITY_EDITOR
        HandleMouse();
#else
        HandleTouch();
#endif
    }


    // =========================================================
    // Mouse
    // =========================================================

    void HandleMouse()
    {
        if (isAnimating || isSelecting)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            BeginSwipe(Input.mousePosition);
        }

        if (Input.GetMouseButtonUp(0) && touching)
        {
            EndSwipe(Input.mousePosition);
        }
    }


    // =========================================================
    // Touch
    // =========================================================

    void HandleTouch()
    {
        if (isAnimating || isSelecting)
            return;

        if (Input.touchCount == 0)
            return;

        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began)
        {
            BeginSwipe(touch.position);
        }
        else if (
            (touch.phase == TouchPhase.Ended ||
             touch.phase == TouchPhase.Canceled) &&
            touching
        )
        {
            EndSwipe(touch.position);
        }
    }


    // =========================================================
    // Swipe Start
    // =========================================================

    void BeginSwipe(Vector2 startPos)
    {
        swipeStart = startPos;
        touching = true;

        // スワイプ開始時点で、
        // 全DisplaySlotのY座標を基準位置へ戻しておく
        LockDisplaySlotsToHorizontalLine();

        // 上スワイプ決定可能なのは
        // 中央Slotから開始した場合のみ
        startedOnCenterSlot =
            RectTransformUtility.RectangleContainsScreenPoint(
                displaySlot3,
                startPos,
                null
            );
    }


    // =========================================================
    // Swipe End
    // =========================================================

    void EndSwipe(Vector2 endPos)
    {
        touching = false;

        // 入力値そのものはX/Y両方取得する
        Vector2 swipe = endPos - swipeStart;


        // -----------------------------------------------------
        // 上スワイプ
        // -----------------------------------------------------
        //
        // 中央スロット上から開始
        // ＋
        // Y方向が一定以上
        // ＋
        // 横より縦方向の入力が強い
        //
        // この条件だけキャラ決定
        //
        if (
            startedOnCenterSlot &&
            swipe.y > upwardSelectThreshold &&
            Mathf.Abs(swipe.y) > Mathf.Abs(swipe.x)
        )
        {
            StartCoroutine(
                PlaySelectAnimation()
            );

            return;
        }


        // -----------------------------------------------------
        // 横スワイプ
        // -----------------------------------------------------

        if (swipe.x < -horizontalThreshold)
        {
            // 左スワイプ
            StartCoroutine(
                AnimateSwipe(+1)
            );
        }
        else if (swipe.x > horizontalThreshold)
        {
            // 右スワイプ
            StartCoroutine(
                AnimateSwipe(-1)
            );
        }
        else
        {
            // スワイプとして成立しなかった場合も
            // Y位置を確実に固定
            LockDisplaySlotsToHorizontalLine();
        }
    }


    // =========================================================
    // Character Select Animation
    // =========================================================

    IEnumerator PlaySelectAnimation()
    {
        if (isSelecting || isAnimating)
            yield break;

        isSelecting = true;

        RectTransform centerSlot =
            displaySlots[CENTER_SLOT];

        int originalIndex =
            centerSlot.GetSiblingIndex();

        // 中央キャラだけ最前面へ
        centerSlot.SetAsLastSibling();

        // 決定アニメーション開始時だけ
        // 縦方向への移動を許可する
        Vector2 startPos =
            basePositions[CENTER_SLOT];

        centerSlot.anchoredPosition =
            startPos;

        Vector2 endPos =
            startPos +
            new Vector2(
                0f,
                selectMoveY
            );

        Vector3 startScale =
            centerSlot.localScale;

        Vector3 endScale =
            Vector3.one *
            selectScale;

        float elapsed = 0f;

        while (elapsed < selectDuration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed /
                    selectDuration
                );

            t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

            centerSlot.anchoredPosition =
                Vector2.Lerp(
                    startPos,
                    endPos,
                    t
                );

            centerSlot.localScale =
                Vector3.Lerp(
                    startScale,
                    endScale,
                    t
                );

            yield return null;
        }

        yield return new WaitForSeconds(
            0.05f
        );

        if (flow != null)
        {
            flow.ConfirmCharacter();
        }

        centerSlot.SetSiblingIndex(
            originalIndex
        );

        isSelecting = false;
    }


    // =========================================================
    // Horizontal Swipe Animation
    // =========================================================

    IEnumerator AnimateSwipe(
        int characterDelta
    )
    {
        if (
            isAnimating ||
            isSelecting ||
            characterIcons.Length == 0
        )
        {
            yield break;
        }

        isAnimating = true;


        // 横移動開始前にも
        // Y位置を強制的に基準へ戻す
        LockDisplaySlotsToHorizontalLine();


        Vector2[] startPositions =
            new Vector2[
                displaySlots.Length
            ];

        Vector2[] endPositions =
            new Vector2[
                displaySlots.Length
            ];


        for (
            int i = 0;
            i < displaySlots.Length;
            i++
        )
        {
            startPositions[i] =
                new Vector2(
                    displaySlots[i]
                        .anchoredPosition.x,

                    basePositions[i].y
                );
        }


        if (characterDelta > 0)
        {
            // 左スワイプ

            endPositions[0] =
                basePositions[0];

            endPositions[1] =
                basePositions[0];

            endPositions[2] =
                basePositions[1];

            endPositions[3] =
                basePositions[2];

            endPositions[4] =
                basePositions[3];

            endPositions[5] =
                basePositions[4];

            endPositions[6] =
                basePositions[5];
        }
        else
        {
            // 右スワイプ

            endPositions[0] =
                basePositions[1];

            endPositions[1] =
                basePositions[2];

            endPositions[2] =
                basePositions[3];

            endPositions[3] =
                basePositions[4];

            endPositions[4] =
                basePositions[5];

            endPositions[5] =
                basePositions[6];

            endPositions[6] =
                basePositions[6];
        }


        yield return AnimateSlots(
            startPositions,
            endPositions
        );


        selectedCharacterIndex =
            (
                selectedCharacterIndex +
                characterDelta +
                characterIcons.Length
            )
            %
            characterIcons.Length;


        // Sprite差し替え後は
        // 全Slotを元の固定位置へ戻す
        for (
            int i = 0;
            i < displaySlots.Length;
            i++
        )
        {
            displaySlots[i]
                .anchoredPosition =
                basePositions[i];
        }


        RefreshDisplayImages();
        RefreshScalesImmediate();
        NotifyCenterCharacter();


        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySE(
                "menu_select"
            );
        }


        isAnimating = false;
    }


    // =========================================================
    // Slot Animation
    // =========================================================

    IEnumerator AnimateSlots(
        Vector2[] startPositions,
        Vector2[] endPositions
    )
    {
        Vector3[] startScales =
            new Vector3[
                displaySlots.Length
            ];

        Vector3[] endScales =
            new Vector3[
                displaySlots.Length
            ];


        for (
            int i = 0;
            i < displaySlots.Length;
            i++
        )
        {
            startScales[i] =
                displaySlots[i]
                    .localScale;


            int nearestSlot =
                FindNearestBaseSlotIndex(
                    endPositions[i]
                );


            float scale =
                (
                    nearestSlot ==
                    CENTER_SLOT
                )
                ?
                centerScale
                :
                sideScale;


            endScales[i] =
                Vector3.one *
                scale;
        }


        float elapsed = 0f;


        while (
            elapsed <
            moveDuration
        )
        {
            elapsed +=
                Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    elapsed /
                    moveDuration
                );


            t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );


            for (
                int i = 0;
                i <
                displaySlots.Length;
                i++
            )
            {
                // Xだけ補間
                float x =
                    Mathf.Lerp(
                        startPositions[i].x,
                        endPositions[i].x,
                        t
                    );


                // Yは絶対に固定
                float y =
                    basePositions[i].y;


                displaySlots[i]
                    .anchoredPosition =
                    new Vector2(
                        x,
                        y
                    );


                displaySlots[i]
                    .localScale =
                    Vector3.Lerp(
                        startScales[i],
                        endScales[i],
                        t
                    );
            }

            yield return null;
        }


        for (
            int i = 0;
            i < displaySlots.Length;
            i++
        )
        {
            displaySlots[i]
                .anchoredPosition =
                new Vector2(
                    endPositions[i].x,
                    basePositions[i].y
                );


            displaySlots[i]
                .localScale =
                endScales[i];
        }
    }


    // =========================================================
    // Horizontal Position Lock
    // =========================================================

    void LockDisplaySlotsToHorizontalLine()
    {
        if (basePositions == null)
            return;

        for (
            int i = 0;
            i < displaySlots.Length;
            i++
        )
        {
            Vector2 pos =
                displaySlots[i]
                    .anchoredPosition;

            displaySlots[i]
                .anchoredPosition =
                new Vector2(
                    pos.x,
                    basePositions[i].y
                );
        }
    }


    // =========================================================
    // Nearest Slot
    // =========================================================

    int FindNearestBaseSlotIndex(
        Vector2 pos
    )
    {
        float closest =
            Mathf.Infinity;

        int index = 0;


        for (
            int i = 0;
            i < basePositions.Length;
            i++
        )
        {
            float d =
                Vector2.Distance(
                    pos,
                    basePositions[i]
                );


            if (d < closest)
            {
                closest = d;
                index = i;
            }
        }

        return index;
    }


    // =========================================================
    // Refresh
    // =========================================================

    void RefreshDisplayImmediate()
    {
        RefreshDisplayImages();
        RefreshScalesImmediate();
    }


    void RefreshDisplayImages()
    {
        for (
            int i = 0;
            i < displaySlotImages.Length;
            i++
        )
        {
            int offset =
                i -
                CENTER_SLOT;


            int charIndex =
                (
                    selectedCharacterIndex +
                    offset +
                    characterIcons.Length
                )
                %
                characterIcons.Length;


            if (
                displaySlotImages[i] ==
                null
            )
            {
                continue;
            }


            displaySlotImages[i].sprite =
                characterIcons[
                    charIndex
                ];


            Color c =
                displaySlotImages[i]
                    .color;

            c.a = 1f;

            displaySlotImages[i]
                .color = c;
        }
    }


    void RefreshScalesImmediate()
    {
        for (
            int i = 0;
            i < displaySlots.Length;
            i++
        )
        {
            float scale =
                (
                    i ==
                    CENTER_SLOT
                )
                ?
                centerScale
                :
                sideScale;


            displaySlots[i]
                .localScale =
                Vector3.one *
                scale;
        }
    }


    // =========================================================
    // Notify
    // =========================================================

    void NotifyCenterCharacter()
    {
        if (flow == null)
            return;


        flow.SetCurrentCharacter(
            selectedCharacterIndex
        );
    }
}