using UnityEngine;

public enum EvolutionStage
{
    White,
    Gray,
    Black
}
public enum CharacterWorld
{
    Cyber,
    Hydro,
    Marchen,
    Ethnic
}
public class PlayerController : MonoBehaviour


{
    [System.Serializable]
    public class WorldPlayerSprites
    {
        public Sprite player1;
        public Sprite player2;
        public Sprite player3;
        public Sprite player4;
    }
    [Header("World Player Sprites")]
    public WorldPlayerSprites cyberSprites;
    public WorldPlayerSprites hydroSprites;
    public WorldPlayerSprites marchenSprites;
    public WorldPlayerSprites ethnicSprites;

    [SerializeField] int playerIndex = -1;
    [SerializeField] int characterIndex = -1;

    [Header("Player Sprite Renderer")]
    public SpriteRenderer playerSpriteRenderer;

    EvolutionStage currentStage = EvolutionStage.White;

    CharacterWorld GetCharacterWorld(int characterIndex)
    {
        switch (characterIndex)
        {
            case 0: // マインガール
            case 1: // キラーウルフ
            case 2: // ポッピンキティ
                return CharacterWorld.Cyber;

            case 3: // セーラーボーイ
            case 4: // ヤッピー
            case 6: // クールビズ
                return CharacterWorld.Hydro;

            case 5: // ケンセル
                return CharacterWorld.Marchen;

            case 7: // ホロウクラウン
                return CharacterWorld.Ethnic;

            default:
                Debug.LogWarning(
                    $"未定義のキャラクター番号です: {characterIndex}"
                );

                return CharacterWorld.Cyber;
        }
    }
    Sprite GetPlayerSprite(CharacterWorld world, int index)
    {
        WorldPlayerSprites targetSprites = null;

        switch (world)
        {
            case CharacterWorld.Cyber:
                targetSprites = cyberSprites;
                break;

            case CharacterWorld.Hydro:
                targetSprites = hydroSprites;
                break;

            case CharacterWorld.Marchen:
                targetSprites = marchenSprites;
                break;

            case CharacterWorld.Ethnic:
                targetSprites = ethnicSprites;
                break;
        }

        if (targetSprites == null)
        {
            Debug.LogWarning(
                $"WorldPlayerSprites が未設定です: {world}"
            );

            return null;
        }

        switch (index)
        {
            case 0:
                return targetSprites.player1;

            case 1:
                return targetSprites.player2;

            case 2:
                return targetSprites.player3;

            case 3:
                return targetSprites.player4;

            default:
                Debug.LogWarning(
                    $"プレイヤー番号が範囲外です: {index}"
                );

                return null;
        }
    }
    void Awake()
    {
        if (playerSpriteRenderer == null)
            playerSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public void SetPlayerData(int playerIndex, int characterIndex)
    {
        this.playerIndex = playerIndex;
        this.characterIndex = characterIndex;

        ApplySprite();
    }

    void ApplySprite()
    {
        if (playerSpriteRenderer == null)
        {
            Debug.LogWarning("PlayerのSpriteRendererが見つかりません");
            return;
        }

        if (playerIndex < 0 || playerIndex > 3)
        {
            Debug.LogWarning($"プレイヤー番号が範囲外です: {playerIndex}");
            return;
        }

        if (characterIndex < 0)
        {
            Debug.LogWarning("キャラクター番号が未設定です");
            return;
        }

        CharacterWorld world =
            GetCharacterWorld(characterIndex);

        Sprite sprite =
            GetPlayerSprite(world, playerIndex);

        if (sprite == null)
        {
            Debug.LogWarning(
                $"{playerIndex + 1}P / {world} のSpriteが未設定です"
            );

            return;
        }

        playerSpriteRenderer.sprite = sprite;

        Debug.Log(
            $"{playerIndex + 1}P 駒設定: " +
            $"Character={characterIndex}, World={world}"
        );
    }

    public void AdvanceEvolution()
    {
        if (currentStage == EvolutionStage.Black) return;

        currentStage++;

        int stageNumber = (int)currentStage + 1;
        Debug.Log($"{playerIndex + 1}P が第{stageNumber}段階に進化しました！");
    }

    public bool IsFinalStage()
    {
        return currentStage == EvolutionStage.Black;
    }
}