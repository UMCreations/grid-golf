using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Adventure Mode level generator strategy.
/// Builds on top of the classic reverse-path algorithm but also places
/// special tiles (Ice, Sand, Boost) on non-golden-path tiles to create
/// strategic variety and a distinct "adventure" feel.
/// 
/// Classic mode is NEVER affected by this strategy.
/// </summary>
public class AdventureLevelGeneratorStrategy : ILevelGeneratorStrategy
{
    // Chance that a noise tile becomes a special tile instead of a plain Standard
    private const float SpecialTileChance = 0.35f;

    public LevelData GenerateLevel(Difficulty difficulty, int levelIndex, bool isTutorial = false)
    {
        if (isTutorial)
        {
            return GenerateAdventureTutorialLevel();
        }

        int seed = (int)difficulty * 5000 + levelIndex + 999; // Separate seed from classic
        Random.InitState(seed);

        int width, height, pathLength, maxPower;

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
                pathLength = Random.Range(8, 12);
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
            tilePowers = new int[width, height],
            tileTypes = new TileType[width, height]
        };

        GenerateAdventureGoldenPath(levelData, pathLength, maxPower);
        FillAdventureNoise(levelData, maxPower, difficulty);

        return levelData;
    }

    private void GenerateAdventureGoldenPath(LevelData level, int pathLength, int maxPower)
    {
        // Initialize all types to Standard first
        for (int x = 0; x < level.width; x++)
            for (int y = 0; y < level.height; y++)
                level.tileTypes[x, y] = TileType.Standard;

        Vector2Int holePos = new Vector2Int(
            Random.Range(0, level.width),
            Random.Range(0, level.height)
        );
        level.holePosition = holePos;
        level.tilePowers[holePos.x, holePos.y] = 0;
        level.tileTypes[holePos.x, holePos.y] = TileType.Hole;
        level.goldenPath.Add(holePos);

        Vector2Int currentPos = holePos;
        int actualPathLength = 0;

        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        for (int i = 0; i < pathLength; i++)
        {
            List<Vector2Int> validMoves = new List<Vector2Int>();

            for (int d = 1; d <= maxPower; d++)
            {
                foreach (var dir in dirs)
                {
                    Vector2Int retreat = currentPos - (dir * d);
                    if (retreat.x >= 0 && retreat.x < level.width &&
                        retreat.y >= 0 && retreat.y < level.height &&
                        level.tilePowers[retreat.x, retreat.y] == 0 &&
                        retreat != level.holePosition)
                    {
                        validMoves.Add(retreat);
                    }
                }
            }

            if (validMoves.Count > 0)
            {
                Vector2Int chosen = validMoves[Random.Range(0, validMoves.Count)];
                int power = Mathf.Abs(chosen.x - currentPos.x) + Mathf.Abs(chosen.y - currentPos.y);

                level.tilePowers[chosen.x, chosen.y] = power;
                level.tileTypes[chosen.x, chosen.y] = TileType.Standard; // Golden path tiles stay Standard
                level.goldenPath.Add(chosen);
                currentPos = chosen;
                actualPathLength++;
            }
            else break;
        }

        level.startPosition = currentPos;
        level.currentGridPosition = currentPos;
        level.currentStrokes = 0;
        level.levelPar = Mathf.Max(actualPathLength, 1) + 2; // Slightly more generous par for adventure
        level.tileTypes[currentPos.x, currentPos.y] = TileType.Start;
    }

    private void FillAdventureNoise(LevelData level, int maxPower, Difficulty difficulty)
    {
        for (int x = 0; x < level.width; x++)
        {
            for (int y = 0; y < level.height; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);

                if (level.tilePowers[x, y] == 0 && pos != level.holePosition)
                {
                    level.tilePowers[x, y] = Random.Range(1, maxPower + 1);

                    // Roll for special tile (only non-golden-path tiles)
                    if (!level.goldenPath.Contains(pos) && Random.value < SpecialTileChance)
                    {
                        // Pick a random special tile type
                        // Introduce types gradually by difficulty
                        TileType specialType = PickSpecialTileType(difficulty);
                        level.tileTypes[x, y] = specialType;
                    }
                    else
                    {
                        level.tileTypes[x, y] = TileType.Standard;
                    }
                }
            }
        }
    }

    private TileType PickSpecialTileType(Difficulty difficulty)
    {
        // Easy: only Sand (forgiving)
        // Medium: Sand + Ice
        // Hard: Sand + Ice + Boost
        int maxIndex = difficulty == Difficulty.Easy ? 1 :
                       difficulty == Difficulty.Medium ? 2 : 3;

        int roll = Random.Range(0, maxIndex);
        switch (roll)
        {
            case 0: return TileType.Sand;
            case 1: return TileType.Ice;
            default: return TileType.Boost;
        }
    }

    private LevelData GenerateAdventureTutorialLevel()
    {
        // A hand-crafted 4x4 intro to adventure mode
        // Introduces one of each tile type so the player learns them
        LevelData tutorial = new LevelData
        {
            difficulty = Difficulty.Easy,
            levelIndex = 0,
            width = 4,
            height = 4,
            tilePowers = new int[4, 4],
            tileTypes = new TileType[4, 4]
        };

        // Fill everything as standard first
        for (int x = 0; x < 4; x++)
            for (int y = 0; y < 4; y++)
            {
                tutorial.tilePowers[x, y] = 1;
                tutorial.tileTypes[x, y] = TileType.Standard;
            }

        // Start
        tutorial.startPosition = new Vector2Int(0, 0);
        tutorial.currentGridPosition = new Vector2Int(0, 0);
        tutorial.tileTypes[0, 0] = TileType.Start;

        // Hole
        tutorial.holePosition = new Vector2Int(3, 3);
        tutorial.tilePowers[3, 3] = 0;
        tutorial.tileTypes[3, 3] = TileType.Hole;

        // Introduce special tiles for tutorial
        tutorial.tileTypes[1, 0] = TileType.Boost; // Boost on path
        tutorial.tileTypes[2, 1] = TileType.Ice;    // Ice tile
        tutorial.tileTypes[1, 2] = TileType.Sand;   // Sand tile
        tutorial.tileTypes[3, 2] = TileType.Standard;

        tutorial.levelPar = 6;

        // Golden path for feedback
        tutorial.goldenPath.Add(new Vector2Int(0, 0));
        tutorial.goldenPath.Add(new Vector2Int(1, 0));
        tutorial.goldenPath.Add(new Vector2Int(2, 1));
        tutorial.goldenPath.Add(new Vector2Int(3, 2));
        tutorial.goldenPath.Add(new Vector2Int(3, 3));

        return tutorial;
    }
}
