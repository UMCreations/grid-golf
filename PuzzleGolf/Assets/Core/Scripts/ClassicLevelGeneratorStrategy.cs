using UnityEngine;
using System.Collections.Generic;

public class ClassicLevelGeneratorStrategy : ILevelGeneratorStrategy
{
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
        Vector2Int holePos = new Vector2Int(Random.Range(0, level.width), Random.Range(0, level.height));
        level.holePosition = holePos;
        level.tilePowers[holePos.x, holePos.y] = 0;

        Vector2Int currentPos = holePos;
        int actualPathLength = 0;
        
        for (int i = 0; i < pathLength; i++)
        {
            List<Vector2Int> validMoves = new List<Vector2Int>();
            
            Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            
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
                
                int requiredPower = Mathf.Abs(chosenRetreat.x - currentPos.x) + Mathf.Abs(chosenRetreat.y - currentPos.y);
                
                level.tilePowers[chosenRetreat.x, chosenRetreat.y] = requiredPower;
                currentPos = chosenRetreat;
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
