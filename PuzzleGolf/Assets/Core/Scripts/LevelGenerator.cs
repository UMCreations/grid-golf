using UnityEngine;
using System.Collections.Generic;

public enum Difficulty
{
    Easy,
    Medium,
    Hard
}

public enum GameMode
{
    Classic,  // Standard mode — no special tiles
    Adventure // New mode — Ice, Sand, Boost tiles
}

[System.Serializable]
public class LevelData
{
    public int dataVersion = 1;
    public Difficulty difficulty;
    public GameMode gameMode;   // Classic or Adventure
    public int width;
    public int height;
    public Vector2Int startPosition;
    public Vector2Int holePosition;
    public int levelPar;
    public int currentStrokes;
    public int currentPowerModifier;
    public int levelIndex;
    public Vector2Int currentGridPosition;
    public List<Vector2Int> goldenPath = new List<Vector2Int>();

    // For JSON serialization
    public int[] tilePowersFlat;
    public int[] tileTypesFlat; // Serialized TileType enum as int

    public void Flatten()
    {
        tilePowersFlat = new int[width * height];
        tileTypesFlat  = new int[width * height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                tilePowersFlat[y * width + x] = tilePowers[x, y];
                if (tileTypes != null)
                    tileTypesFlat[y * width + x] = (int)tileTypes[x, y];
            }
        }
    }

    public void Unflatten()
    {
        tilePowers = new int[width, height];
        tileTypes  = new TileType[width, height];
        if (tilePowersFlat != null && tilePowersFlat.Length == width * height)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    tilePowers[x, y] = tilePowersFlat[y * width + x];
                    if (tileTypesFlat != null && tileTypesFlat.Length == width * height)
                        tileTypes[x, y] = (TileType)tileTypesFlat[y * width + x];
                    else
                        tileTypes[x, y] = TileType.Standard;
                }
            }
        }
    }

    [System.NonSerialized]
    public int[,] tilePowers;

    [System.NonSerialized]
    public TileType[,] tileTypes; // Adventure tile types per cell
}

public class LevelGenerator : MonoBehaviour
{
    public static LevelGenerator Instance { get; private set; }

    [Header("Generation Settings")]
    [Tooltip("Enable to use the new Sawtooth pacing and False Paths.")]
    public bool useAdvancedMechanics = true;

    private ILevelGeneratorStrategy currentStrategy;
    private GameMode currentGameMode = GameMode.Classic;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeStrategy();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeStrategy()
    {
        currentStrategy = LevelGeneratorFactory.GetStrategy(useAdvancedMechanics);
    }

    public void SetGameMode(GameMode mode)
    {
        currentGameMode = mode;
        if (mode == GameMode.Adventure)
            currentStrategy = new AdventureLevelGeneratorStrategy();
        else
            currentStrategy = LevelGeneratorFactory.GetStrategy(useAdvancedMechanics);
    }

    public LevelData GenerateLevel(Difficulty difficulty, int levelIndex, bool isTutorial = false)
    {
        // Check for Handcrafted Level first (Adventure Mode)
        if (currentGameMode == GameMode.Adventure && LevelManager.Instance != null)
        {
            HandcraftedLevelSO handcrafted = LevelManager.Instance.GetHandcraftedLevel(levelIndex);
            if (handcrafted != null)
            {
                Debug.Log($"[LevelGenerator] Loading handcrafted level: {handcrafted.levelName}");
                return handcrafted.ToLevelData(levelIndex);
            }
        }

        if (currentStrategy == null) InitializeStrategy();
        LevelData data = currentStrategy.GenerateLevel(difficulty, levelIndex, isTutorial);
        data.gameMode = currentGameMode;
        return data;
    }
}
