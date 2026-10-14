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

    private readonly List<Button> generatedButtons = new List<Button>();

    public void Show()
    {
        gameObject.SetActive(true);
        Refresh();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void Refresh()
    {
        ClearButtons();

        if (gameManager == null ||
            effectButtonHolder == null ||
            effectButtonPrefab == null)
        {
            Debug.LogWarning("[MistEffectPanelUI] Inspectorの参照が不足しています");
            return;
        }

        IReadOnlyList<GameManager.MistEffectDefinition> effects =
            gameManager.GetSelectedMistEffects();

        foreach (GameManager.MistEffectDefinition effect in effects)
        {
            if (effect == null)
                continue;

            Button button = Instantiate(effectButtonPrefab, effectButtonHolder);
            generatedButtons.Add(button);

            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.text = effect.effectName;

            button.onClick.AddListener(() => OnEffectClicked(effect));
        }
    }

    private void OnEffectClicked(GameManager.MistEffectDefinition effect)
    {
        if (gameManager == null || effect == null)
            return;

        if (effectNameText != null)
            effectNameText.text = effect.effectName;

        if (effectDescriptionText != null)
            effectDescriptionText.text = effect.description;

        int playerIndex = gameManager.GetCurrentPlayerIndex();

        if (!gameManager.CanAffordMistEffect(playerIndex, effect))
        {
            Debug.Log($"[Mist UI] Mist不足：{effect.effectName}");
            return;
        }

        bool activated = gameManager.TryActivateSelectedMistEffect(effect);

        if (activated)
            Debug.Log($"[Mist UI] 発動処理完了：{effect.effectName}");
        else
            Debug.LogWarning($"[Mist UI] 発動できませんでした：{effect.effectName}");
    }

    private void ClearButtons()
    {
        foreach (Button button in generatedButtons)
        {
            if (button != null)
                Destroy(button.gameObject);
        }

        generatedButtons.Clear();
    }

    public void ShowEffectDetails(int index)
    {
        if (gameManager == null)
            return;

        IReadOnlyList<GameManager.MistEffectDefinition> effects =
            gameManager.GetSelectedMistEffects();

        if (index < 0 || index >= effects.Count || effects[index] == null)
            return;

        GameManager.MistEffectDefinition effect = effects[index];

        if (effectNameText != null)
            effectNameText.text = effect.effectName;

        if (effectDescriptionText != null)
            effectDescriptionText.text = effect.description;
    }
}
