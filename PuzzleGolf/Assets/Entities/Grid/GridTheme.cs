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
    public Sprite[] standardSprites;
    public Sprite startSprite;
    public Sprite holeSprite;

    [Header("Numbered Sprites (Optional)")]
    [Tooltip("Map power count to sprite index (0 = power 1, 1 = power 2, etc.)")]
    public Sprite[] numberedSprites;

    [Header("Adventure Tile Sprites (Optional)")]
    public Sprite[] iceSprites;
    public Sprite[] sandSprites;
    public Sprite[] boostSprites;

    [Header("Visual Tuning")]
    public Color standardTileColor = Color.white;
    public Color powerTextColor = Color.white;

    public Sprite GetSpriteForType(TileType type, int power = 0)
    {
        switch (type)
        {
            case TileType.Start:    return startSprite;
            case TileType.Hole:     return holeSprite;
            case TileType.Ice:      return GetRandomFromList(iceSprites);
            case TileType.Sand:     return GetRandomFromList(sandSprites);
            case TileType.Boost:    return GetRandomFromList(boostSprites);
            case TileType.Standard:
                if (power > 0 && numberedSprites != null && power - 1 < numberedSprites.Length && numberedSprites[power - 1] != null)
                {
                    return numberedSprites[power - 1];
                }
                return GetRandomFromList(standardSprites);
            default:                return GetRandomFromList(standardSprites);
        }
    }

    private Sprite GetRandomFromList(Sprite[] list)
    {
        if (list != null && list.Length > 0)
        {
            return list[Random.Range(0, list.Length)];
        }
        
        // Return standard if no sprites assigned to specific list
        if (standardSprites != null && standardSprites.Length > 0)
            return standardSprites[0];
            
        return null;
    }
}
