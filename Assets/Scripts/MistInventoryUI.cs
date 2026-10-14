using UnityEngine;
using TMPro;

public class MistInventoryUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;

    [Header("Mist Count Texts")]
    [SerializeField] private TMP_Text redCountText;
    [SerializeField] private TMP_Text blueCountText;
    [SerializeField] private TMP_Text greenCountText;
    [SerializeField] private TMP_Text yellowCountText;

    public void Refresh()
    {
        if (gameManager == null)
            return;

        int playerIndex = gameManager.GetCurrentPlayerIndex();
        Debug.Log(
    $"[MistUIämîF] {gameObject.name} / " +
    $"ï\é¶ëŒè€={playerIndex + 1}P / " +
    $"ê‘={gameManager.GetMistColorCount(playerIndex, GameManager.MistColor.Red)} / " +
    $"ê¬={gameManager.GetMistColorCount(playerIndex, GameManager.MistColor.Blue)} / " +
    $"óŒ={gameManager.GetMistColorCount(playerIndex, GameManager.MistColor.Green)} / " +
    $"â©={gameManager.GetMistColorCount(playerIndex, GameManager.MistColor.Yellow)}"
);
        if (redCountText != null)
            redCountText.text = gameManager.GetMistColorCount(
                playerIndex, GameManager.MistColor.Red).ToString();

        if (blueCountText != null)
            blueCountText.text = gameManager.GetMistColorCount(
                playerIndex, GameManager.MistColor.Blue).ToString();

        if (greenCountText != null)
            greenCountText.text = gameManager.GetMistColorCount(
                playerIndex, GameManager.MistColor.Green).ToString();

        if (yellowCountText != null)
            yellowCountText.text = gameManager.GetMistColorCount(
                playerIndex, GameManager.MistColor.Yellow).ToString();
    }
}