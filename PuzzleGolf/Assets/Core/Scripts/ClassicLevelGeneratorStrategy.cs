using UnityEngine;
using System.Collections.Generic;

public class ClassicLevelGeneratorStrategy : ILevelGeneratorStrategy
{
    private const int MAX_GENERATION_ATTEMPTS = 50;

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

        LevelData bestLevel = null;
        LevelData lastLevel = null;
        int bestShortestPath = -1;
        bool bestHasUniqueShortestPath = false;
        for (int attempt = 0; attempt < MAX_GENERATION_ATTEMPTS; attempt++)
        {
            LevelData levelData = new LevelData
            {
                difficulty = difficulty,
                gameMode = GameMode.Classic,
                levelIndex = levelIndex,
                width = width,
                height = height,
                tilePowers = new int[width, height]
            };
            lastLevel = levelData;

            GenerateGoldenPath(levelData, pathLength, maxPower, difficulty);
            int actualPathLength = levelData.goldenPath.Count - 1;
            SolveResult result = PuzzleSolver.Solve(levelData);
            bool hasUniqueShortestPath = result.ShortestPathCount == 1;
            if (result.IsSolvable && (hasUniqueShortestPath && !bestHasUniqueShortestPath ||
                hasUniqueShortestPath == bestHasUniqueShortestPath && result.ShortestPathStrokes > bestShortestPath))
            {
                bestLevel = levelData;
                bestShortestPath = result.ShortestPathStrokes;
                bestHasUniqueShortestPath = hasUniqueShortestPath;
            }

            if (result.IsSolvable && actualPathLength == pathLength &&
                result.ShortestPathStrokes == actualPathLength && hasUniqueShortestPath)
            {
                levelData.levelPar = result.ShortestPathStrokes + 1;
                return levelData;
            }
        }

        if (bestLevel != null)
        {
            bestLevel.levelPar = bestShortestPath + 1;
            Debug.LogWarning($"[ClassicLevelGenerator] Could not generate the requested unique route length for level {levelIndex}; using the best solvable candidate ({bestShortestPath} strokes).");
            return bestLevel;
        }

        Debug.LogError($"[ClassicLevelGenerator] Could not generate a solvable level {levelIndex} after {MAX_GENERATION_ATTEMPTS} attempts.");
        lastLevel.levelPar = lastLevel.goldenPath.Count;
        return lastLevel;
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
        
        // Tutorial Golden Path
        tutorial.goldenPath.Add(new Vector2Int(0,0));
        tutorial.goldenPath.Add(new Vector2Int(1,0));
        tutorial.goldenPath.Add(new Vector2Int(2,0));
        tutorial.goldenPath.Add(new Vector2Int(2,1));
        tutorial.goldenPath.Add(new Vector2Int(2,2));

        return tutorial;
    }

    private void GenerateGoldenPath(LevelData level, int pathLength, int maxPower, Difficulty difficulty)
    {
        Vector2Int holePos = new Vector2Int(Random.Range(0, level.width), Random.Range(0, level.height));
        level.holePosition = holePos;
        level.tilePowers[holePos.x, holePos.y] = 0;
        level.goldenPath.Add(holePos);

        Vector2Int currentPos = holePos;
        int actualPathLength = 0;
        
        for (int i = 0; i < pathLength; i++)
        {
            List<Vector2Int> validMoves = new List<Vector2Int>();
            
            Vector2Int[] dirs = {
                Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
                new Vector2Int(1, 1), new Vector2Int(1, -1),
                new Vector2Int(-1, 1), new Vector2Int(-1, -1)
            };
            
            for (int d = 1; d <= maxPower; d++)
            {
                foreach (var dir in dirs)
                {
                    Vector2Int possibleRetreat = currentPos - (dir * d); 
                    
                    if (possibleRetreat.x >= 0 && possibleRetreat.x < level.width &&
                        possibleRetreat.y >= 0 && possibleRetreat.y < level.height)
                    {
                        if (level.tilePowers[possibleRetreat.x, possibleRetreat.y] == 0 && possibleRetreat != level.holePosition)
                        {
                            validMoves.Add(possibleRetreat);
                        }
                    }
                }
            }

            if (validMoves.Count > 0)
            {
                Vector2Int chosenRetreat = validMoves[Random.Range(0, validMoves.Count)];
                
                int requiredPower = Mathf.Max(Mathf.Abs(chosenRetreat.x - currentPos.x), Mathf.Abs(chosenRetreat.y - currentPos.y));
                
                level.tilePowers[chosenRetreat.x, chosenRetreat.y] = requiredPower;
                currentPos = chosenRetreat;
                level.goldenPath.Add(currentPos);
                actualPathLength++;
            }
            else
            {
                break;
            }
        }

        level.startPosition = currentPos;
        level.currentGridPosition = currentPos;
        level.currentStrokes = 0;
        level.levelPar = Mathf.Max(actualPathLength, 1) + 1; 

        FillNoise(level, maxPower, difficulty);
    }

    private void FillNoise(LevelData level, int maxPower, Difficulty difficulty)
    {
        for (int x = 0; x < level.width; x++)
        {
            for (int y = 0; y < level.height; y++)
            {
                if (level.tilePowers[x, y] == 0 && new Vector2Int(x, y) != level.holePosition)
                {
                    level.tilePowers[x, y] = Random.Range(1, maxPower + 1);
                }
            }
        }
    }
}
