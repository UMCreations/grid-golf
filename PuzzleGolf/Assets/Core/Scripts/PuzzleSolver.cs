using UnityEngine;
using System.Collections.Generic;

public class SolveResult
{
    public bool IsSolvable;
    public int ShortestPathStrokes;
    public List<Vector2Int> Path;

    public SolveResult()
    {
        IsSolvable = false;
        ShortestPathStrokes = -1;
        Path = new List<Vector2Int>();
    }
}

public static class PuzzleSolver
{
    private struct SolverState
    {
        public Vector2Int Position;
        public int PowerModifier;
        public int Strokes;
        public List<Vector2Int> History;

        public SolverState(Vector2Int pos, int mod, int strokes, List<Vector2Int> history)
        {
            Position = pos;
            PowerModifier = mod;
            Strokes = strokes;
            History = new List<Vector2Int>(history) { pos };
        }
    }

    public static SolveResult Solve(LevelData level)
    {
        if (level == null) return new SolveResult();

        Queue<SolverState> queue = new Queue<SolverState>();
        // State is position + current power modifier (from last tile)
        HashSet<(Vector2Int, int)> visited = new HashSet<(Vector2Int, int)>();

        // Initial state
        queue.Enqueue(new SolverState(level.startPosition, 0, 0, new List<Vector2Int>()));
        visited.Add((level.startPosition, 0));

        Vector2Int[] directions = {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
            new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
        };

        while (queue.Count > 0)
        {
            SolverState current = queue.Dequeue();

            // If we reached the hole, this is the shortest path (due to BFS)
            if (current.Position == level.holePosition)
            {
                return new SolveResult
                {
                    IsSolvable = true,
                    ShortestPathStrokes = current.Strokes,
                    Path = current.History
                };
            }

            // Boundary safety check for tile access
            if (!IsWithinBounds(level, current.Position)) continue;

            int basePower = level.tilePowers[current.Position.x, current.Position.y];
            if (basePower <= 0 && current.Position != level.holePosition) continue; 

            // Apply adventure modifier (Sand/Boost)
            int effectivePower = Mathf.Max(1, basePower + current.PowerModifier);

            foreach (var dir in directions)
            {
                Vector2Int nextPos = current.Position + (dir * effectivePower);
                
                // Bounds check
                if (!IsWithinBounds(level, nextPos))
                    continue;

                int nextModifier = 0;
                Vector2Int finalPos = nextPos;
                bool hitHole = (nextPos == level.holePosition);

                // Handle Adventure Mode hazards
                if (level.gameMode == GameMode.Adventure && !hitHole)
                {
                    TileType type = level.tileTypes[nextPos.x, nextPos.y];
                    
                    if (type == TileType.Ice)
                    {
                        // Auto-slide logic: Move again using the Ice tile's power in the same direction
                        int icePower = level.tilePowers[nextPos.x, nextPos.y];
                        finalPos = nextPos + (dir * icePower);
                        
                        // Recursive Ice check
                        int safetyMax = 100;
                        while (IsWithinBounds(level, finalPos) && level.tileTypes[finalPos.x, finalPos.y] == TileType.Ice && finalPos != level.holePosition && safetyMax-- > 0)
                        {
                             icePower = level.tilePowers[finalPos.x, finalPos.y];
                             finalPos = finalPos + (dir * icePower);
                        }

                        // Final check after sliding
                        if (!IsWithinBounds(level, finalPos)) continue;
                        
                        if (finalPos == level.holePosition) hitHole = true;
                        else
                        {
                            TileType landingType = level.tileTypes[finalPos.x, finalPos.y];
                            if (landingType == TileType.Sand) nextModifier = -1;
                            else if (landingType == TileType.Boost) nextModifier = 1;
                        }
                    }
                    else if (type == TileType.Sand)
                    {
                        nextModifier = -1;
                    }
                    else if (type == TileType.Boost)
                    {
                        nextModifier = 1;
                    }
                }

                if (!visited.Contains((finalPos, nextModifier)))
                {
                    visited.Add((finalPos, nextModifier));
                    queue.Enqueue(new SolverState(finalPos, nextModifier, current.Strokes + 1, current.History));
                }
            }
        }

        return new SolveResult(); // No solution found
    }

    private static bool IsWithinBounds(LevelData level, Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < level.width && pos.y >= 0 && pos.y < level.height;
    }
}
