using UnityEngine;
using System.Collections.Generic;

public enum Difficulty
{
    Easy,
    Medium,
    Hard
}

[System.Serializable]
public class LevelData
{
    public Difficulty difficulty;
    public int width;
    public int height;
    public Vector2Int startPosition;
    public Vector2Int holePosition;
    public int levelPar;
    public int currentStrokes;
    public int levelIndex;
    public Vector2Int currentGridPosition;
    public List<Vector2Int> goldenPath = new List<Vector2Int>();

    // For JSON serialization
    public int[] tilePowersFlat; 

    public void Flatten()
    {
        tilePowersFlat = new int[width * height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                tilePowersFlat[y * width + x] = tilePowers[x, y];
            }
        }
    }

    public void Unflatten()
    {
        tilePowers = new int[width, height];
        if (tilePowersFlat != null && tilePowersFlat.Length == width * height)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    tilePowers[x, y] = tilePowersFlat[y * width + x];
                }
            }
        }
    }

    [System.NonSerialized]
    public int[,] tilePowers; // 0 means empty, other numbers mean specific movement distance
}

public class LevelGenerator : MonoBehaviour
{
    public static LevelGenerator Instance { get; private set; }

    [Header("Generation Settings")]
    [Tooltip("Enable to use the new Sawtooth pacing and False Paths.")]
    public bool useAdvancedMechanics = true;

    private ILevelGeneratorStrategy currentStrategy;

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

    public LevelData GenerateLevel(Difficulty difficulty, int levelIndex, bool isTutorial = false)
    {
        if (currentStrategy == null)
        {
            InitializeStrategy();
        }
        return currentStrategy.GenerateLevel(difficulty, levelIndex, isTutorial);
    }
}
