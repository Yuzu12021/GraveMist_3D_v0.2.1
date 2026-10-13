using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MistEffectPanelUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private Transform effectButtonHolder;
    [SerializeField] private Button effectButtonPrefab;

    [Header("Effect Details")]
    [SerializeField] private TMP_Text effectNameText;
    [SerializeField] private TMP_Text effectDescriptionText;

    private readonly List<Button> generatedButtons =
        new List<Button>();

    // パネルを開いたときに呼び出す
    public void Show()
    {
        gameObject.SetActive(true);
        Refresh();
    }

    // パネルを閉じる
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    // 抽選された効果をボタンとして表示
    public void Refresh()
    {
        ClearButtons();

        if (gameManager == null ||
            effectButtonHolder == null ||
            effectButtonPrefab == null)
        {
            Debug.LogWarning(
                "[MistEffectPanelUI] Inspectorの参照が不足しています"
            );
            return;
        }

        IReadOnlyList<GameManager.MistEffectDefinition> effects =
            gameManager.GetSelectedMistEffects();

        foreach (GameManager.MistEffectDefinition effect in effects)
        {
            if (effect == null)
                continue;

            Button button = Instantiate(
                effectButtonPrefab,
                effectButtonHolder
            );

            generatedButtons.Add(button);

            TMP_Text label =
                button.GetComponentInChildren<TMP_Text>();

            if (label != null)
            {
                label.text = effect.effectName;
            }

            button.onClick.AddListener(() =>
            {
                // 効果名を表示
                if (effectNameText != null)
                {
                    effectNameText.text = effect.effectName;
                }

                // 効果説明を表示
                if (effectDescriptionText != null)
                {
                    effectDescriptionText.text = effect.description;
                }

                // 現在のプレイヤー
                int playerIndex =
                    gameManager.GetCurrentPlayerIndex();

                // Mistが足りているか判定
                bool canAfford =
                    gameManager.CanAffordMistEffect(
                        playerIndex,
                        effect
                    );

                if (canAfford)
                {
                    Debug.Log(
                        $"[Mist UI] 発動可能：{effect.effectName}"
                    );
                }
                else
                {
                    Debug.Log(
                        $"[Mist UI] Mist不足：{effect.effectName}"
                    );
                }
            });
        }
    }

    private void ClearButtons()
    {
        foreach (Button button in generatedButtons)
        {
            if (button != null)
            {
                Destroy(button.gameObject);
            }
        }

        generatedButtons.Clear();
    }
    public void ShowEffectDetails(int index)
    {
        var effects = gameManager.GetSelectedMistEffects();

        if (index < 0 || index >= effects.Count)
            return;

        var effect = effects[index];

        if (effectNameText != null)
            effectNameText.text = effect.effectName;

        if (effectDescriptionText != null)
            effectDescriptionText.text = effect.description;
    }
}