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
    public LevelData GenerateLevel(Difficulty difficulty, int levelIndex, bool isTutorial = false)
    {
        if (isTutorial)
        {
            return GenerateAdventureTutorialLevel();
        }

        // Fixed seed based on level index to guarantee the same level generates every time
        int seed = 9000 + levelIndex;
        Random.InitState(seed);

        AdventureSegmentConfig config = AdventureSegmentResolver.GetConfigForLevel(levelIndex);

        int width = Random.Range(config.minGridSize, config.maxGridSize + 1);
        int height = Random.Range(config.minGridSize, config.maxGridSize + 1);
        int pathLength = Random.Range(config.minPathLength, config.maxPathLength + 1);
        int maxPower = config.maxPower;

        LevelData levelData = new LevelData
        {
            difficulty = difficulty, // Preserved for compatibility, though largely unused in Adventure now
            levelIndex = levelIndex,
            width = width,
            height = height,
            tilePowers = new int[width, height],
            tileTypes = new TileType[width, height]
        };

        GenerateAdventureGoldenPath(levelData, pathLength, config);

        // Phase 2: Smart Refinement Loop
        // Solve the level to ensure noise tiles didn't create an accidental shortcut
        for (int i = 0; i < 5; i++)
        {
            FillAdventureNoise(levelData, config);
            SolveResult result = PuzzleSolver.Solve(levelData);

            if (result.IsSolvable)
            {
                if (result.ShortestPathStrokes < pathLength)
                {
                    // Shortcut detected, let the next iteration of FillAdventureNoise try again
                    // (Optionally we could specifically break the shortcut path here)
                    continue;
                }
                else
                {
                    // Level is valid: intended path is the shortest or equal to it
                    levelData.levelPar = result.ShortestPathStrokes + 2; 
                    break;
                }
            }
        }

        return levelData;
    }

    private void GenerateAdventureGoldenPath(LevelData level, int pathLength, AdventureSegmentConfig config)
    {
        int maxPower = config.maxPower;
        int maxHazardsOnPath = config.maxHazardsOnPath;
        List<TileType> allowedHazards = config.allowedHazards;
        int pathHazardsPlaced = 0;

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
                        // To place Ice, we need the tile 1 step from currentPos to be empty too
                        if (d >= 2)
                        {
                            Vector2Int iceNode = currentPos - dir;
                            if (level.tilePowers[iceNode.x, iceNode.y] != 0 && iceNode != level.holePosition)
                            {
                                continue; // Ice path blocked
                            }
                        }

                        validMoves.Add(retreat);
                    }
                }
            }

            if (validMoves.Count > 0)
            {
                Vector2Int chosen = validMoves[Random.Range(0, validMoves.Count)];
                
                // Determine direction from chosen to currentPos
                int dx = currentPos.x - chosen.x;
                int dy = currentPos.y - chosen.y;
                int dist = Mathf.Abs(dx) + Mathf.Abs(dy);
                Vector2Int forwardDir = new Vector2Int(
                    dx == 0 ? 0 : dx / Mathf.Abs(dx),
                    dy == 0 ? 0 : dy / Mathf.Abs(dy)
                );

                int assignedPower = dist;
                TileType assignedType = TileType.Standard;

                // Attempt to place a hazard
                if (pathHazardsPlaced < maxHazardsOnPath && allowedHazards.Count > 0 && Random.value < 0.5f)
                {
                    // Shuffle valid hazards for this jump
                    List<TileType> possibleHazards = new List<TileType>();
                    if (allowedHazards.Contains(TileType.Sand)) possibleHazards.Add(TileType.Sand);
                    if (dist > 1 && allowedHazards.Contains(TileType.Boost)) possibleHazards.Add(TileType.Boost);
                    if (dist >= 2 && allowedHazards.Contains(TileType.Ice)) possibleHazards.Add(TileType.Ice);

                    if (possibleHazards.Count > 0)
                    {
                        TileType chosenHazard = possibleHazards[Random.Range(0, possibleHazards.Count)];

                        if (chosenHazard == TileType.Sand)
                        {
                            assignedType = TileType.Sand;
                            assignedPower = dist + 1; // Compensate for -1 effect
                            pathHazardsPlaced++;
                        }
                        else if (chosenHazard == TileType.Boost)
                        {
                            assignedType = TileType.Boost;
                            assignedPower = dist - 1; // Compensate for +1 effect
                            pathHazardsPlaced++;
                        }
                        else if (chosenHazard == TileType.Ice)
                        {
                            // Place the ice exactly 1 step before the destination
                            Vector2Int icePos = currentPos - forwardDir;
                            level.tileTypes[icePos.x, icePos.y] = TileType.Ice;
                            level.tilePowers[icePos.x, icePos.y] = Random.Range(1, maxPower + 1); // Fake power to protect from noise fill
                            
                            // Chosen tile just shoots to the Ice tile
                            assignedType = TileType.Standard;
                            assignedPower = dist - 1; 
                            pathHazardsPlaced++;
                        }
                    }
                }

                level.tilePowers[chosen.x, chosen.y] = assignedPower;
                level.tileTypes[chosen.x, chosen.y] = assignedType; 
                level.goldenPath.Add(chosen);
                currentPos = chosen;
                actualPathLength++;
            }
            else break;
        }

        level.startPosition = currentPos;
        level.currentGridPosition = currentPos;
        level.currentStrokes = 0;
        level.levelPar = Mathf.Max(actualPathLength, 1) + 2; 
        level.tileTypes[currentPos.x, currentPos.y] = TileType.Start;
    }

    private void FillAdventureNoise(LevelData level, AdventureSegmentConfig config)
    {
        int maxPower = config.maxPower;
        int noiseHazardsPlaced = 0;
        int maxNoiseHazards = config.maxHazardsInNoise;
        List<TileType> allowedHazards = config.allowedHazards;

        // Step 1: Assign powers to all non-path tiles first to establish the board
        for (int x = 0; x < level.width; x++)
        {
            for (int y = 0; y < level.height; y++)
            {
                if (level.tilePowers[x, y] == 0 && new Vector2Int(x, y) != level.holePosition)
                {
                    level.tilePowers[x, y] = Random.Range(1, maxPower + 1);
                    level.tileTypes[x, y] = TileType.Standard; // Default initialization
                }
            }
        }

        if (maxNoiseHazards == 0 || allowedHazards.Count == 0) return; // World 1 exit early

        // Helper directions
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        // Step 2: Build False Trails (Highest Priority Noise)
        // Iterate over the golden path (excluding Hole and Start if possible)
        for (int i = 1; i < level.goldenPath.Count - 1; i++)
        {
            if (noiseHazardsPlaced >= maxNoiseHazards) break;

            Vector2Int pathNode = level.goldenPath[i];
            int nodePower = level.tilePowers[pathNode.x, pathNode.y];

            // 30% chance to try building a false trail from this node
            if (Random.value < 0.3f)
            {
                // Look for an adjacent tile that is NOT on the golden path
                List<Vector2Int> possibleFalseStarts = new List<Vector2Int>();
                foreach (var dir in dirs)
                {
                    Vector2Int adj = pathNode + dir;
                    if (IsNoiseTile(level, adj))
                    {
                        possibleFalseStarts.Add(adj);
                    }
                }

                if (possibleFalseStarts.Count > 0)
                {
                    Vector2Int falseStart = possibleFalseStarts[Random.Range(0, possibleFalseStarts.Count)];
                    
                    // Assign a similar power to make the false trail confusingly believable
                    int fakePower = Mathf.Clamp(nodePower + Random.Range(-1, 2), 1, maxPower);
                    level.tilePowers[falseStart.x, falseStart.y] = fakePower;

                    // Place a hazard here to trap the player
                    TileType hazard = allowedHazards[Random.Range(0, allowedHazards.Count)];
                    level.tileTypes[falseStart.x, falseStart.y] = hazard;
                    noiseHazardsPlaced++;
                }
            }
        }

        // Step 3: Path-Adjacent Hazards (Second Priority Noise)
        if (noiseHazardsPlaced < maxNoiseHazards)
        {
            List<Vector2Int> adjacentNoiseTiles = new List<Vector2Int>();

            for (int i = 0; i < level.goldenPath.Count; i++)
            {
                Vector2Int pathNode = level.goldenPath[i];
                foreach (var dir in dirs)
                {
                    Vector2Int adj = pathNode + dir;
                    // Only collect tiles that are noise and haven't already been given a hazard
                    if (IsNoiseTile(level, adj) && level.tileTypes[adj.x, adj.y] == TileType.Standard)
                    {
                        if (!adjacentNoiseTiles.Contains(adj))
                        {
                            adjacentNoiseTiles.Add(adj);
                        }
                    }
                }
            }

            // Shuffle the adjacent list
            for (int i = 0; i < adjacentNoiseTiles.Count; i++)
            {
                Vector2Int temp = adjacentNoiseTiles[i];
                int randomIndex = Random.Range(i, adjacentNoiseTiles.Count);
                adjacentNoiseTiles[i] = adjacentNoiseTiles[randomIndex];
                adjacentNoiseTiles[randomIndex] = temp;
            }

            // Distribute remaining budget securely around the path
            foreach (Vector2Int trapTile in adjacentNoiseTiles)
            {
                if (noiseHazardsPlaced >= maxNoiseHazards) break;

                TileType hazard = allowedHazards[Random.Range(0, allowedHazards.Count)];
                level.tileTypes[trapTile.x, trapTile.y] = hazard;
                noiseHazardsPlaced++;
            }
        }
    }

    private bool IsNoiseTile(LevelData level, Vector2Int pos)
    {
        if (pos.x < 0 || pos.x >= level.width || pos.y < 0 || pos.y >= level.height) return false;
        if (pos == level.holePosition) return false;
        if (level.goldenPath.Contains(pos)) return false;
        return true;
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
