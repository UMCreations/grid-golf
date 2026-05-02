using UnityEngine;
using System.Collections.Generic;

public class SmartLevelGeneratorStrategy : ILevelGeneratorStrategy
{
    private const int MAX_REFINEMENT_ATTEMPTS = 10;

    public LevelData GenerateLevel(Difficulty difficulty, int levelIndex, bool isTutorial = false)
    {
        if (isTutorial)
        {
            return GenerateTutorialLevel();
        }

        // Standard seeding to keep levels deterministic
        int seed = (int)difficulty * 1000 + levelIndex + 5000; // Offset to distinguish from Classic
        Random.InitState(seed);

        int width, height, targetPathLength, maxPower;

        switch (difficulty)
        {
            case Difficulty.Easy:
                width = 5; height = 5;
                targetPathLength = Random.Range(3, 5);
                maxPower = 2;
                break;
            case Difficulty.Medium:
                width = Random.Range(6, 8); height = Random.Range(6, 8);
                targetPathLength = Random.Range(5, 8);
                maxPower = 4;
                break;
            case Difficulty.Hard:
                width = Random.Range(8, 10); height = Random.Range(8, 10);
                targetPathLength = Random.Range(8, 13);
                maxPower = 5;
                break;
            default:
                width = 5; height = 5; targetPathLength = 3; maxPower = 2;
                break;
        }

        LevelData levelData = new LevelData
        {
            difficulty = difficulty,
            levelIndex = levelIndex,
            width = width,
            height = height,
            tilePowers = new int[width, height],
            gameMode = GameMode.Classic // Smart strategy optimizes classic puzzles too
        };

        // Phase 1: Generate a Winding Golden Path (guaranteed solution)
        GenerateWindingPath(levelData, targetPathLength, maxPower);

        // Phase 2: Strategic Deception
        FillNoise(levelData, maxPower);

        // Phase 3: Extreme Refinement - Ensure EXACTLY ONE solution
        // We prune any tile that creates a shortcut or an alternative route
        PruneAlternativeSolutions(levelData, targetPathLength);

        levelData.levelPar = targetPathLength; // Strict par for 'Exactly One Path' feel
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
            tilePowers = new int[3, 3],
            gameMode = GameMode.Classic
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

    private void GenerateWindingPath(LevelData level, int pathLength, int maxPower)
    {
        Vector2Int holePos = new Vector2Int(Random.Range(0, level.width), Random.Range(0, level.height));
        level.holePosition = holePos;
        level.tilePowers[holePos.x, holePos.y] = 0;
        level.goldenPath.Add(holePos);

        Vector2Int currentPos = holePos;
        Vector2Int lastMoveDir = Vector2Int.zero;

        Vector2Int[] directions = { 
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
            new Vector2Int(1,1), new Vector2Int(1,-1), new Vector2Int(-1,1), new Vector2Int(-1,-1)
        };
        
        for (int i = 0; i < pathLength; i++)
        {
            List<Vector2Int> candidates = new List<Vector2Int>();
            
            for (int d = 1; d <= maxPower; d++)
            {
                foreach (var dir in directions)
                {
                    Vector2Int retreat = currentPos - (dir * d);
                    if (IsWithinBounds(level, retreat) && level.tilePowers[retreat.x, retreat.y] == 0 && retreat != level.holePosition)
                    {
                        candidates.Add(retreat);
                    }
                }
            }

            if (candidates.Count > 0)
            {
                // Winding Heuristic: Prefer moves that:
                // 1. Aren't in the same direction as the previous move (prevents straight lines)
                // 2. Increase the Manhattan distance from the hole (forces going 'around')
                candidates.Sort((a, b) => {
                    int distA = Mathf.Abs(a.x - holePos.x) + Mathf.Abs(a.y - holePos.y);
                    int distB = Mathf.Abs(b.x - holePos.x) + Mathf.Abs(b.y - holePos.y);
                    
                    Vector2Int dirA = (currentPos - a);
                    Vector2Int dirB = (currentPos - b);
                    
                    bool isAStraight = dirA == lastMoveDir;
                    bool isBStraight = dirB == lastMoveDir;

                    if (isAStraight != isBStraight) return isAStraight ? 1 : -1;
                    return distB.CompareTo(distA); // Prefer further distance
                });

                // Pick from top 2 to keep it slightly organic but generally winding
                Vector2Int chosen = candidates[Random.Range(0, Mathf.Min(2, candidates.Count))];
                lastMoveDir = currentPos - chosen;
                
                int dist = Mathf.Max(Mathf.Abs(chosen.x - currentPos.x), Mathf.Abs(chosen.y - currentPos.y));
                level.tilePowers[chosen.x, chosen.y] = dist;
                currentPos = chosen;
                level.goldenPath.Add(currentPos);
            }
            else break;
        }

        level.startPosition = currentPos;
        level.currentGridPosition = currentPos;
    }

    private void PruneAlternativeSolutions(LevelData level, int targetStrokes)
    {
        // Use the solver to find the current shortest path
        for (int attempt = 0; attempt < 5; attempt++)
        {
            SolveResult result = PuzzleSolver.Solve(level);
            if (!result.IsSolvable) break;

            // If a path shorter than our Golden Path exists, we MUST break it.
            // If a path OF EQUAL LENGTH exists that isn't our Golden Path, we also break it (optional but keeps it unique).
            if (result.ShortestPathStrokes <= targetStrokes)
            {
                bool brokenAny = false;
                // Walk the solver's path and find a tile that isn't on our intended Golden Path
                foreach (var pos in result.Path)
                {
                    if (!level.goldenPath.Contains(pos) && pos != level.holePosition)
                    {
                        // Change its power to point it into a 'Dead Zone' or just randomize it again
                        level.tilePowers[pos.x, pos.y] = (level.tilePowers[pos.x, pos.y] % 5) + 1; 
                        brokenAny = true;
                        break; // Break one and re-solve
                    }
                }
                
                if (!brokenAny) break; // Entire path is the golden path
            }
            else break; // Shortest path is already long enough
        }
    }

    private void FillNoise(LevelData level, int maxPower)
    {
        // 1. Create False Branches from the Golden Path
        // This makes the player question where the 'real' path begins.
        CreateFalseBranches(level, maxPower);

        // 2. Create Near-Hole Traps
        // Tiles adjacent to the hole that look like the 'final step' but aren't reachable.
        CreateHoleTraps(level);

        // 3. Create Logic Loops (Vortexes)
        // Pairs of tiles that lead to each other to trap the player.
        CreateLogicLoops(level, maxPower);

        // 4. Final Polish: Fill remaining as standard random noise
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

    private void CreateFalseBranches(LevelData level, int maxPower)
    {
        int branchCount = level.difficulty == Difficulty.Easy ? 1 : (level.difficulty == Difficulty.Medium ? 2 : 3);
        
        for (int b = 0; b < branchCount; b++)
        {
            if (level.goldenPath.Count < 3) break;
            
            // Pick a random node on the golden path to branch from
            Vector2Int branchNode = level.goldenPath[Random.Range(1, level.goldenPath.Count - 1)];
            Vector2Int currentPos = branchNode;

            // Build a short 2-step false path
            for (int i = 0; i < 2; i++)
            {
                List<Vector2Int> validEmpty = GetAdjacentEmpty(level, currentPos);
                if (validEmpty.Count > 0)
                {
                    Vector2Int next = validEmpty[Random.Range(0, validEmpty.Count)];
                    int dist = Mathf.Max(Mathf.Abs(next.x - currentPos.x), Mathf.Abs(next.y - currentPos.y));
                    level.tilePowers[next.x, next.y] = dist; // Points back to the previous node
                    currentPos = next;
                }
            }
        }
    }

    private void CreateHoleTraps(LevelData level)
    {
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (var dir in dirs)
        {
            Vector2Int adj = level.holePosition + dir;
            if (IsWithinBounds(level, adj) && level.tilePowers[adj.x, adj.y] == 0)
            {
                // A tile right next to the hole with power 1 looks like the perfect final move.
                level.tilePowers[adj.x, adj.y] = 1;
            }
        }
    }

    private void CreateLogicLoops(LevelData level, int maxPower)
    {
        // Try to create at least one loop in medium/hard levels
        if (level.difficulty == Difficulty.Easy) return;

        for (int x = 0; x < level.width - 2; x++)
        {
            for (int y = 0; y < level.height; y++)
            {
                Vector2Int posA = new Vector2Int(x, y);
                Vector2Int posB = new Vector2Int(x + 2, y);

                if (IsWithinBounds(level, posA) && IsWithinBounds(level, posB) &&
                    level.tilePowers[posA.x, posA.y] == 0 && level.tilePowers[posB.x, posB.y] == 0)
                {
                    // A points to B, B points to A
                    level.tilePowers[posA.x, posA.y] = 2;
                    level.tilePowers[posB.x, posB.y] = 2;
                    return; // Just one loop is enough for deception
                }
            }
        }
    }

    private List<Vector2Int> GetAdjacentEmpty(LevelData level, Vector2Int center)
    {
        List<Vector2Int> empty = new List<Vector2Int>();
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        
        foreach (var dir in directions)
        {
            Vector2Int p = center + dir;
            if (IsWithinBounds(level, p) && level.tilePowers[p.x, p.y] == 0 && p != level.holePosition)
            {
                empty.Add(p);
            }
        }
        return empty;
    }

    private bool IsWithinBounds(LevelData level, Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < level.width && pos.y >= 0 && pos.y < level.height;
    }
}
