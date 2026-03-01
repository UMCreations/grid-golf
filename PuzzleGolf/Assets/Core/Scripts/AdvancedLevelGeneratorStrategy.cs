using UnityEngine;
using System.Collections.Generic;

public class AdvancedLevelGeneratorStrategy : ILevelGeneratorStrategy
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

        // Base values
        switch (difficulty)
        {
            case Difficulty.Easy:
                width = 5; height = 5;
                pathLength = Random.Range(3, 5);
                maxPower = 2;
                break;
            case Difficulty.Medium:
                width = Random.Range(6, 8); height = Random.Range(6, 8);
                pathLength = Random.Range(5, 8);
                maxPower = 4;
                break;
            case Difficulty.Hard:
                width = Random.Range(8, 10); height = Random.Range(8, 10);
                pathLength = Random.Range(8, 13);
                maxPower = 5;
                break;
            default:
                width = 5; height = 5; pathLength = 3; maxPower = 2;
                break;
        }

        // Sawtooth pacing
        // Level 5, 10, 15... is a breather
        // Level 4, 9, 14... is the peak
        int sequenceInCycle = levelIndex % 5;
        
        if (sequenceInCycle == 0 && levelIndex > 0) // The Breather (e.g. 5)
        {
            width = Mathf.Max(3, width - 2);
            height = Mathf.Max(3, height - 2);
            pathLength = Mathf.Max(2, pathLength - 2);
        }
        else if (sequenceInCycle == 4) // The Peak (e.g. 4)
        {
            width = Mathf.Min(10, width + 1);
            height = Mathf.Min(10, height + 1);
            pathLength += 2;
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
            levelIndex = 0,
            width = 3,
            height = 3,
            tilePowers = new int[3, 3]
        };

        tutorial.startPosition = new Vector2Int(0, 0);
        tutorial.currentGridPosition = new Vector2Int(0, 0);
        tutorial.holePosition = new Vector2Int(2, 2);
        tutorial.levelPar = 4;

        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                tutorial.tilePowers[x, y] = 1;
            }
        }
        tutorial.tilePowers[2, 2] = 0;

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
        
        int sequenceInCycle = level.levelIndex % 5;
        if (sequenceInCycle == 0 && level.levelIndex > 0)
        {
            // Give extra generous par for breathers to give a dopamine hit
            level.levelPar = Mathf.Max(actualPathLength, 1) + 2; 
        }
        else 
        {
            level.levelPar = Mathf.Max(actualPathLength, 1) + 1; 
        }

        GenerateFalsePath(level, maxPower, currentPos, actualPathLength);
        FillNoise(level, maxPower, difficulty);
    }

    private void GenerateFalsePath(LevelData level, int maxPower, Vector2Int startPos, int pathLengthTarget)
    {
        // Find a tile near the hole that isn't on the golden path
        List<Vector2Int> nearHole = new List<Vector2Int>();
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        
        foreach (var dir in dirs)
        {
            Vector2Int pos = level.holePosition + dir;
            if (pos.x >= 0 && pos.x < level.width && pos.y >= 0 && pos.y < level.height)
            {
                if (level.tilePowers[pos.x, pos.y] == 0) // Empty tile
                {
                    nearHole.Add(pos);
                }
            }
        }

        if (nearHole.Count == 0) return;

        Vector2Int trapPos = nearHole[Random.Range(0, nearHole.Count)];
        
        // Let's create a path that leads away from the trap point back toward startPos area
        Vector2Int currentPos = trapPos;
        int falsePathStrokes = Random.Range(2, Mathf.Max(3, pathLengthTarget));

        for (int i = 0; i < falsePathStrokes; i++)
        {
            List<Vector2Int> validMoves = new List<Vector2Int>();
            
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
            }
            else
            {
                break;
            }
        }

        // Set the final trap tile to a value that would send it off to a wall, or just random
        level.tilePowers[trapPos.x, trapPos.y] = Random.Range(1, maxPower + 1);
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
