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
        
        // Dynamic Difficulty Adjustment (DDA)
        int ddaPathModifier = 0;
        if (LevelManager.Instance != null && LevelManager.Instance.CurrentProfile != null)
        {
            var profile = LevelManager.Instance.CurrentProfile;
            if (profile.consecutiveFailures >= 3) ddaPathModifier = -1;
            if (profile.consecutivePerfects >= 3) ddaPathModifier = 1;
        }

        pathLength += ddaPathModifier;

        if (sequenceInCycle == 0 && levelIndex > 0) // The Breather (e.g. 5)
        {
            width = Mathf.Max(3, width - 2);
            height = Mathf.Max(3, height - 2);
            pathLength = Mathf.Max(2, pathLength - 1); // Breathers are already short, don't over-reduce
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

        GenerateGoldenPath(levelData, pathLength, maxPower, difficulty, ddaPathModifier);

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

    private void GenerateGoldenPath(LevelData level, int pathLength, int maxPower, Difficulty difficulty, int ddaModifier)
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
        
        // Base Par calculation
        level.levelPar = Mathf.Max(actualPathLength, 1) + 1;

        // Apply DDA and Breather modifiers to PAR
        if (sequenceInCycle == 0 && level.levelIndex > 0)
        {
            // Give extra generous par for breathers
            level.levelPar += 1; 
        }
        
        if (ddaModifier < 0) // Player is failing
        {
            level.levelPar += 1; // Give them an extra stroke on top of the shorter path
        }

        GenerateFalsePath(level, maxPower, currentPos, actualPathLength);
        FillNoise(level, maxPower, difficulty);
    }

    private void GenerateFalsePath(LevelData level, int maxPower, Vector2Int startPos, int pathLengthTarget)
    {
        // 1. Identify "Trap Zones" near the hole (1 tile away)
        List<Vector2Int> trapZones = new List<Vector2Int>();
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        
        foreach (var dir in dirs)
        {
            Vector2Int pos = level.holePosition + dir;
            if (pos.x >= 0 && pos.x < level.width && pos.y >= 0 && pos.y < level.height)
            {
                // Must be an empty tile (not part of the Golden Path)
                if (level.tilePowers[pos.x, pos.y] == 0) 
                {
                    trapZones.Add(pos);
                }
            }
        }

        if (trapZones.Count == 0) return;

        // 2. Select a trap tile and build a path BACKWARDS from it
        // This makes it look like a valid path that just "nearly" makes it.
        Vector2Int trapTile = trapZones[Random.Range(0, trapZones.Count)];
        Vector2Int currentPos = trapTile;

        // The false path should be roughly as long as the real one to be a convincing distractor
        int falsePathLength = Mathf.Clamp(pathLengthTarget + Random.Range(-1, 2), 2, 12);

        for (int i = 0; i < falsePathLength; i++)
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
                        // Don't overwrite the Golden Path or the Hole
                        if (level.tilePowers[possibleRetreat.x, possibleRetreat.y] == 0 && possibleRetreat != level.holePosition)
                        {
                            validMoves.Add(possibleRetreat);
                        }
                    }
                }
            }

            if (validMoves.Count > 0)
            {
                // Prioritize retreating towards the Start Position to make it look like an alternative starting branch
                validMoves.Sort((a, b) => Vector2Int.Distance(a, startPos).CompareTo(Vector2Int.Distance(b, startPos)));
                
                // Pick one of the best 2 moves to keep it slightly organic but generally moving toward start
                int index = Random.Range(0, Mathf.Min(2, validMoves.Count));
                Vector2Int chosenRetreat = validMoves[index];
                
                int requiredPower = Mathf.Abs(chosenRetreat.x - currentPos.x) + Mathf.Abs(chosenRetreat.y - currentPos.y);
                
                level.tilePowers[chosenRetreat.x, chosenRetreat.y] = requiredPower;
                currentPos = chosenRetreat;
            }
            else
            {
                break;
            }
        }

        // 3. Final Step: The Trap!
        // The tile adjacent to the hole (trapTile) must have a power that makes it 
        // ALMOST reach the hole but overshoot or undershoot if the player isn't careful.
        // Or, we just give it a legitimate distance to the hole, but because it's a "False Path,"
        // the player will run out of strokes (Par) before they can finish it correctly.
        level.tilePowers[trapTile.x, trapTile.y] = 1; // Requires exactly 1 more stroke than the player likely has.
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
