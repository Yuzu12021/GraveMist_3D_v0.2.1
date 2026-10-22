using UnityEngine;
using UnityEngine.UI;

public class MistInventoryUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;

    [Header("Mist Count Images")]
    [SerializeField] private Image redCountImage;
    [SerializeField] private Image blueCountImage;
    [SerializeField] private Image greenCountImage;
    [SerializeField] private Image yellowCountImage;

    [Header("Shared Count Sprites (0.png - 10.png)")]
    [SerializeField] private Sprite[] countSprites = new Sprite[11];

    public void Refresh()
    {
        if (gameManager == null)
            return;

        int playerIndex = gameManager.GetCurrentPlayerIndex();

        SetCountImage(redCountImage, gameManager.GetMistColorCount(
            playerIndex, GameManager.MistColor.Red));
        SetCountImage(blueCountImage, gameManager.GetMistColorCount(
            playerIndex, GameManager.MistColor.Blue));
        SetCountImage(greenCountImage, gameManager.GetMistColorCount(
            playerIndex, GameManager.MistColor.Green));
        SetCountImage(yellowCountImage, gameManager.GetMistColorCount(
            playerIndex, GameManager.MistColor.Yellow));
    }

    private void SetCountImage(Image target, int count)
    {
        if (target == null || countSprites == null || countSprites.Length == 0)
            return;

        int index = Mathf.Clamp(count, 0, 10);
        if (index >= countSprites.Length || countSprites[index] == null)
        {
            Debug.LogWarning("[MistInventoryUI] Missing count sprite: " + index + ".png");
            return;
        }

        target.sprite = countSprites[index];
        target.enabled = true;
        target.color = Color.white;
    }
}
