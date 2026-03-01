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

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public LevelData GenerateLevel(Difficulty difficulty, int levelIndex, bool isTutorial = false)
    {
        if (isTutorial)
        {
            return GenerateTutorialLevel();
        }

        // Set fixed seed so Level "N" is always the same for everyone
        int seed = (int)difficulty * 1000 + levelIndex;
        Random.InitState(seed);

        int width, height, pathLength, maxPower;

        switch (difficulty)
        {
            case Difficulty.Easy:
                width = 5; height = 5;
                pathLength = Random.Range(3, 5); // 3 to 4 strokes
                maxPower = 2;
                break;
            case Difficulty.Medium:
                width = Random.Range(6, 8); height = Random.Range(6, 8); // 6x6 to 7x7
                pathLength = Random.Range(5, 8); // 5 to 7 strokes
                maxPower = 4;
                break;
            case Difficulty.Hard:
                width = Random.Range(8, 10); height = Random.Range(8, 10); // 8x8 to 9x9
                pathLength = Random.Range(8, 13); // 8 to 12 strokes
                maxPower = 5;
                break;
            default:
                width = 5; height = 5; pathLength = 3; maxPower = 2;
                break;
        }

        LevelData levelData = new LevelData
        {
            difficulty = difficulty,
            levelIndex = levelIndex,
            width = width,
            height = height,
            tilePowers = new int[width, height]
        };

        GenerateGoldenPath(levelData, pathLength, maxPower, difficulty);

        return levelData;
    }

    private LevelData GenerateTutorialLevel()
    {
        LevelData tutorial = new LevelData
        {
            difficulty = Difficulty.Easy,
            levelIndex = 0, // Special index for tutorial
            width = 3,
            height = 3,
            tilePowers = new int[3, 3]
        };

        // Layout:
        // (0,2)[1] (1,2)[1] (2,2)[H]
        // (0,1)[1] (1,1)[1] (2,1)[1]
        // (0,0)[S,1] (1,0)[1] (2,0)[1]

        tutorial.startPosition = new Vector2Int(0, 0);
        tutorial.currentGridPosition = new Vector2Int(0, 0);
        tutorial.holePosition = new Vector2Int(2, 2);
        tutorial.levelPar = 4; // 4 steps to hole

        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                tutorial.tilePowers[x, y] = 1;
            }
        }
        tutorial.tilePowers[2, 2] = 0; // Hole

        return tutorial;
    }

    private void GenerateGoldenPath(LevelData level, int pathLength, int maxPower, Difficulty difficulty)
    {
        // 1. Place the hole randomly
        Vector2Int holePos = new Vector2Int(Random.Range(0, level.width), Random.Range(0, level.height));
        level.holePosition = holePos;
        level.tilePowers[holePos.x, holePos.y] = 0; // Hole has no movement power

        Vector2Int currentPos = holePos;
        int actualPathLength = 0;
        
        // 2. Reverse walk to create the guaranteed path
        for (int i = 0; i < pathLength; i++)
        {
            List<Vector2Int> validMoves = new List<Vector2Int>();
            
            // Try all 4 orthogonal directions
            Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            
            // Collect all possible backward jumps that land on an empty tile
            for (int d = 1; d <= maxPower; d++)
            {
                foreach (var dir in dirs)
                {
                    // If we move forward by dir * d, we retreat by subtracting
                    Vector2Int possibleRetreat = currentPos - (dir * d); 
                    
                    // Check bounds
                    if (possibleRetreat.x >= 0 && possibleRetreat.x < level.width &&
                        possibleRetreat.y >= 0 && possibleRetreat.y < level.height)
                    {
                        // Make sure the tile is empty and not the hole
                        if (level.tilePowers[possibleRetreat.x, possibleRetreat.y] == 0 && possibleRetreat != level.holePosition)
                        {
                            validMoves.Add(possibleRetreat);
                        }
                    }
                }
            }

            if (validMoves.Count > 0)
            {
                // Select a random valid retreat
                Vector2Int chosenRetreat = validMoves[Random.Range(0, validMoves.Count)];
                
                // The power on that chosen tile must be exactly the distance to currentPos
                int requiredPower = Mathf.Abs(chosenRetreat.x - currentPos.x) + Mathf.Abs(chosenRetreat.y - currentPos.y);
                
                level.tilePowers[chosenRetreat.x, chosenRetreat.y] = requiredPower;
                currentPos = chosenRetreat;
                actualPathLength++;
            }
            else
            {
                // Got stuck (blocked by our own path). Terminate path early.
                Debug.LogWarning($"LevelGen: Got stuck backing up path! Path length reached: {actualPathLength}");
                break;
            }
        }

        // 3. Mark the final landed position as Start
        level.startPosition = currentPos;
        level.currentGridPosition = currentPos;
        level.currentStrokes = 0;
        
        // Par is the length of the ideal path + 1 for breathing room (or strict par = length)
        level.levelPar = Mathf.Max(actualPathLength, 1) + 1; 

        // 4. Fill remaining tiles with noise
        FillNoise(level, maxPower, difficulty);
    }

    private void FillNoise(LevelData level, int maxPower, Difficulty difficulty)
    {
        for (int x = 0; x < level.width; x++)
        {
            for (int y = 0; y < level.height; y++)
            {
                // If tile is empty and isn't the hole
                if (level.tilePowers[x, y] == 0 && new Vector2Int(x, y) != level.holePosition)
                {
                    // Easy/MVP: purely random distractors
                    // A potential enhancement for Medium/Hard is to trace short false paths here.
                    level.tilePowers[x, y] = Random.Range(1, maxPower + 1);
                }
            }
        }
    }
}
