using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MistEffectPanelUI : MonoBehaviour
{
    [Serializable]
    private class EffectIconEntry
    {
        public string effectName;
        public Sprite icon;
    }

    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private Transform effectButtonHolder;
    [SerializeField] private Button effectButtonPrefab;

    [Header("Effect Icons")]
    [SerializeField] private List<EffectIconEntry> effectIcons = new List<EffectIconEntry>();
    [SerializeField] private bool hideLabelWhenIconExists = true;

    [Header("Effect Details")]
    [SerializeField] private TMP_Text effectNameText;
    [SerializeField] private TMP_Text effectDescriptionText;

    [Header("Scroll View (optional)")]
    [SerializeField] private ScrollRect effectScrollRect;
    [SerializeField] private bool resetScrollPositionOnOpen = true;

    private readonly List<Button> generatedButtons = new List<Button>();
    private readonly List<GameManager.MistEffectDefinition> buttonEffects =
        new List<GameManager.MistEffectDefinition>();

    private Coroutine layoutCoroutine;
    private GameManager.MistEffectDefinition currentDetailEffect;

    public void Show()
    {
        gameObject.SetActive(true);
        Refresh();

        if (resetScrollPositionOnOpen && effectScrollRect != null)
            effectScrollRect.verticalNormalizedPosition = 1f;

        if (layoutCoroutine != null)
            StopCoroutine(layoutCoroutine);
        layoutCoroutine = StartCoroutine(RefreshLayoutNextFrame());
    }

    public void Hide()
    {
        if (layoutCoroutine != null)
        {
            StopCoroutine(layoutCoroutine);
            layoutCoroutine = null;
        }

        gameObject.SetActive(false);
    }

    public void Refresh()
    {
        if (gameManager == null || effectButtonHolder == null || effectButtonPrefab == null)
        {
            Debug.LogWarning("[MistEffectPanelUI] Inspector references are missing.");
            return;
        }

        IReadOnlyList<GameManager.MistEffectDefinition> effects =
            gameManager.GetSelectedMistEffects();

        if (effects == null)
            return;

        // Preserve the exact effect object for each generated button.
        List<GameManager.MistEffectDefinition> validEffects =
            new List<GameManager.MistEffectDefinition>();

        foreach (GameManager.MistEffectDefinition effect in effects)
        {
            if (effect != null)
                validEffects.Add(effect);
        }

        bool rebuild = generatedButtons.Count != validEffects.Count;
        if (!rebuild)
        {
            for (int i = 0; i < validEffects.Count; i++)
            {
                if (generatedButtons[i] == null ||
                    !ReferenceEquals(buttonEffects[i], validEffects[i]))
                {
                    rebuild = true;
                    break;
                }
            }
        }

        if (rebuild)
        {
            ClearButtons();
            for (int i = 0; i < validEffects.Count; i++)
            {
                GameManager.MistEffectDefinition capturedEffect = validEffects[i];
                Button button = Instantiate(effectButtonPrefab, effectButtonHolder);
                button.gameObject.SetActive(true);

                // Remove persistent prefab listeners too, not only runtime listeners.
                // Runtime removal cannot remove inspector-persistent listeners,
                // so disable their execution via the cloned Button's event replacement.
                button.onClick = new Button.ButtonClickedEvent();
                button.onClick.AddListener(() => OnEffectClicked(capturedEffect));

                generatedButtons.Add(button);
                buttonEffects.Add(capturedEffect);
            }
        }

        for (int i = 0; i < generatedButtons.Count; i++)
            ApplyButtonVisual(generatedButtons[i], buttonEffects[i]);

        // Details follow the selected effect, not the ScrollRect index.
        if (currentDetailEffect != null && validEffects.Contains(currentDetailEffect))
            ShowDetails(currentDetailEffect);
        else if (validEffects.Count > 0)
            ShowDetails(validEffects[0]);
        else
            ClearDetails();
    }

    private void ApplyButtonVisual(Button button, GameManager.MistEffectDefinition effect)
    {
        if (button == null || effect == null)
            return;

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        Sprite icon = FindIcon(effect.effectName);
        Image image = button.GetComponent<Image>();

        if (image != null && icon != null)
        {
            image.sprite = icon;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.enabled = true;
            image.color = Color.white;
        }
        else if (icon == null)
        {
            Debug.LogWarning("[MistEffectPanelUI] Icon not registered: " + effect.effectName);
        }

        if (label != null)
        {
            label.text = effect.effectName;
            label.gameObject.SetActive(!hideLabelWhenIconExists || icon == null || image == null);
        }
    }

    private Sprite FindIcon(string effectName)
    {
        foreach (EffectIconEntry entry in effectIcons)
        {
            if (entry != null && entry.effectName == effectName)
                return entry.icon;
        }
        return null;
    }

    private void OnEffectClicked(GameManager.MistEffectDefinition effect)
    {
        if (gameManager == null || effect == null)
            return;

        ShowDetails(effect);

        int playerIndex = gameManager.GetCurrentPlayerIndex();
        if (!gameManager.CanAffordMistEffect(playerIndex, effect))
        {
            Debug.Log("[Mist UI] Cannot afford: " + effect.effectName);
            return;
        }

        bool activated = gameManager.TryActivateSelectedMistEffect(effect);
        if (activated)
            Debug.Log("[Mist UI] Activated: " + effect.effectName);
        else
            Debug.LogWarning("[Mist UI] Activation failed: " + effect.effectName);
    }

    private void ShowDetails(GameManager.MistEffectDefinition effect)
    {
        if (effect == null)
            return;

        currentDetailEffect = effect;

        if (effectNameText != null)
        {
            effectNameText.gameObject.SetActive(true);
            effectNameText.text = effect.effectName;
        }

        if (effectDescriptionText != null)
        {
            effectDescriptionText.gameObject.SetActive(true);
            effectDescriptionText.text = effect.description;
        }
    }

    private void ClearDetails()
    {
        currentDetailEffect = null;
        if (effectNameText != null)
            effectNameText.text = "";
        if (effectDescriptionText != null)
            effectDescriptionText.text = "";
    }

    private IEnumerator RefreshLayoutNextFrame()
    {
        yield return null;

        if (effectButtonHolder is RectTransform content)
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);

        Canvas.ForceUpdateCanvases();

        if (effectScrollRect != null)
        {
            if (resetScrollPositionOnOpen)
                effectScrollRect.verticalNormalizedPosition = 1f;
            effectScrollRect.StopMovement();
        }

        layoutCoroutine = null;
    }

    private void ClearButtons()
    {
        foreach (Button button in generatedButtons)
        {
            if (button == null)
                continue;

            // Immediately exclude the old object from layout and raycasts.
            button.gameObject.SetActive(false);
            button.transform.SetParent(null, false);
            Destroy(button.gameObject);
        }

        generatedButtons.Clear();
        buttonEffects.Clear();
    }

    private void OnDestroy()
    {
        ClearButtons();
    }

    public void ShowEffectDetails(int index)
    {
        if (index < 0 || index >= buttonEffects.Count)
            return;

        ShowDetails(buttonEffects[index]);
    }
}
