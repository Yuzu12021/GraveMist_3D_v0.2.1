
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MistEffectSwipe : MonoBehaviour,
    IInitializePotentialDragHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [Header("References")]
    [SerializeField] private RectTransform content;

    [Header("Swipe Settings")]
    [SerializeField] private float swipeThreshold = 50f;
    [SerializeField] private float moveDuration = 0.2f;
    [SerializeField] private float slotSpacing = 120f;

    [Header("Selection Appearance")]
    [SerializeField] private float centerScale = 1.15f;
    [SerializeField] private float sideScale = 0.9f;

    [SerializeField] private MistEffectPanelUI panelUI;

    private int centerEffectIndex = 3;
    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        // ドラッグ開始の準備
    }

    public void OnDrag(PointerEventData eventData)
    {
        // ドラッグ中の処理はまだ不要
    }

    private const int SLOT_COUNT = 7;
    private const int CENTER_SLOT = 3;

    private RectTransform[] slots;
    private Vector2[] basePositions;

    private Vector2 dragStart;
    private bool isAnimating;

    private void Awake()
    {
        Debug.Log(
            $"[Mist Swipe] Awake実行！ " +
            $"Object: {gameObject.name}"
        );
    }

    private void Start()
    {
        // MistEffectPanelUIがボタンを生成した後に初期化
        StartCoroutine(Initialize());
    }

    private IEnumerator Initialize()
    {
        yield return null;

        if (content == null || content.childCount != SLOT_COUNT)
        {
            Debug.LogWarning(
                "[MistEffectSwipe] Contentには7個のボタンが必要です"
            );
            yield break;
        }

        slots = new RectTransform[SLOT_COUNT];
        basePositions = new Vector2[SLOT_COUNT];

        // Layout Groupに位置を変更されないようにする
        HorizontalLayoutGroup layout =
            content.GetComponent<HorizontalLayoutGroup>();

        if (layout != null)
            layout.enabled = false;

        ContentSizeFitter fitter =
            content.GetComponent<ContentSizeFitter>();

        if (fitter != null)
            fitter.enabled = false;

        float fixedY = 0f;

        for (int i = 0; i < SLOT_COUNT; i++)
        {
            slots[i] = content.GetChild(i) as RectTransform;

            slots[i].anchorMin = new Vector2(0.5f, 0.5f);
            slots[i].anchorMax = new Vector2(0.5f, 0.5f);
            slots[i].pivot = new Vector2(0.5f, 0.5f);

            float x = (i - CENTER_SLOT) * slotSpacing;

            basePositions[i] = new Vector2(x, fixedY);
            slots[i].anchoredPosition = basePositions[i];
        }
        centerEffectIndex = 3;
        panelUI?.ShowEffectDetails(centerEffectIndex);
        RefreshSlotScales();
        Debug.Log("[MistEffectSwipe] 初期化完了");
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log("[Mist Swipe] ドラッグ開始！");
        if (isAnimating || slots == null)
            return;

        dragStart = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log("[Mist Swipe] ドラッグ終了！");
        if (isAnimating || slots == null)
            return;

        float deltaX = eventData.position.x - dragStart.x;

        if (Mathf.Abs(deltaX) < swipeThreshold)
            return;

        int direction = deltaX < 0f ? 1 : -1;

        StartCoroutine(AnimateSwipe(direction));
    }

    private IEnumerator AnimateSwipe(int direction)
    {
        isAnimating = true;

        Vector2[] starts = new Vector2[SLOT_COUNT];

        for (int i = 0; i < SLOT_COUNT; i++)
            starts[i] = slots[i].anchoredPosition;

        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.SmoothStep(
                0f, 1f,
                Mathf.Clamp01(elapsed / moveDuration)
            );

            for (int i = 0; i < SLOT_COUNT; i++)
            {
                slots[i].anchoredPosition = Vector2.Lerp(
                    starts[i],
                    basePositions[i] +
                    Vector2.left * direction * slotSpacing,
                    t
                );
            }
            UpdateSlotScalesByPosition();
            yield return null;
        }

        // スロットを循環させる
        if (direction > 0)
        {
            RectTransform first = slots[0];

            for (int i = 0; i < SLOT_COUNT - 1; i++)
                slots[i] = slots[i + 1];

            slots[SLOT_COUNT - 1] = first;
        }
        else
        {
            RectTransform last = slots[SLOT_COUNT - 1];

            for (int i = SLOT_COUNT - 1; i > 0; i--)
                slots[i] = slots[i - 1];

            slots[0] = last;
        }

        // 新しいスロット位置に固定
        for (int i = 0; i < SLOT_COUNT; i++)
            slots[i].anchoredPosition = basePositions[i];
        centerEffectIndex =
    (centerEffectIndex + direction + 7) % 7;

        panelUI?.ShowEffectDetails(centerEffectIndex);
        RefreshSlotScales();
        isAnimating = false;
    }
    private void RefreshSlotScales()
    {
        if (slots == null)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
                continue;

            float scale = (i == CENTER_SLOT)
                ? centerScale
                : sideScale;

            slots[i].localScale = Vector3.one * scale;
        }
    }
    private void UpdateSlotScalesByPosition()
    {
        if (slots == null)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            float distance = Mathf.Abs(
                slots[i].anchoredPosition.x -
                basePositions[CENTER_SLOT].x
            );

            // 中央に近づくほど1、離れるほど0
            float t = 1f - Mathf.Clamp01(
                distance / slotSpacing
            );

            // 滑らかな拡大縮小
            t = Mathf.SmoothStep(0f, 1f, t);

            float scale = Mathf.Lerp(
                sideScale,
                centerScale,
                t
            );

            slots[i].localScale = Vector3.one * scale;
        }
    }
}
