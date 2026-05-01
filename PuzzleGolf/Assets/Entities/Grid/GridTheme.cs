using UnityEngine;

[CreateAssetMenu(fileName = "NewGridTheme", menuName = "Puzzle Golf/Grid Theme")]
public class GridTheme : ScriptableObject
{
    [Header("Theme Info")]
    public string themeName;

    [Header("Background Settings")]
    public Sprite backgroundImage;
    public Color backgroundColor = Color.white;
    public float backgroundPadding = 1.5f;

    [Header("Tile Sprites (Required)")]
    public Sprite standardSprite;
    public Sprite startSprite;
    public Sprite holeSprite;

    [Header("Numbered Sprites (Optional)")]
    [Tooltip("Map power count to sprite index (0 = power 1, 1 = power 2, etc.)")]
    public Sprite[] numberedSprites;

    [Header("Adventure Tile Sprites (Optional)")]
    public Sprite iceSprite;
    public Sprite sandSprite;
    public Sprite boostSprite;

    [Header("Visual Tuning")]
    public Color standardTileColor = Color.white;
    public Color powerTextColor = Color.white;

    public Sprite GetSpriteForType(TileType type, int power = 0)
    {
        switch (type)
        {
            case TileType.Start:    return startSprite;
            case TileType.Hole:     return holeSprite;
            case TileType.Ice:      return iceSprite != null ? iceSprite : standardSprite;
            case TileType.Sand:     return sandSprite != null ? sandSprite : standardSprite;
            case TileType.Boost:    return boostSprite != null ? boostSprite : standardSprite;
            case TileType.Standard:
                if (power > 0 && numberedSprites != null && power - 1 < numberedSprites.Length && numberedSprites[power - 1] != null)
                {
                    return numberedSprites[power - 1];
                }
                return standardSprite;
            default:                return standardSprite;
        }
    }
}
