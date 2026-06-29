using System.Collections.Generic;
using UnityEngine;

public class EvaluatedClassicLevelGeneratorStrategy : ILevelGeneratorStrategy
{
    private const int MaxPathCountForScoring = 6;

    public LevelData GenerateLevel(Difficulty difficulty, int levelIndex, bool isTutorial = false)
    {
        if (isTutorial)
            return GenerateTutorialLevel();

        GenerationProfile profile = GetProfile(difficulty);
        int baseSeed = 20000 + ((int)difficulty * 1000) + levelIndex;

        LevelData bestLevel = null;
        ClassicLevelMetrics bestMetrics = null;
        float bestScore = float.NegativeInfinity;

        for (int attempt = 0; attempt < profile.candidateCount; attempt++)
        {
            Random.InitState(baseSeed + (attempt * 7919));
            LevelData candidate = BuildCandidate(difficulty, levelIndex, profile);
            if (candidate == null)
                continue;

            ClassicLevelMetrics metrics = ClassicLevelAnalyzer.Analyze(candidate);
            if (!metrics.isSolvable)
                continue;

            float score = ScoreCandidate(profile, candidate, metrics);
            if (score > bestScore)
            {
                bestScore = score;
                bestLevel = candidate;
                bestMetrics = metrics;
            }
        }

        if (bestLevel != null)
        {
            bestLevel.levelPar = CalculatePar(profile, bestMetrics);
            Debug.Log($"[EvaluatedGenerator] Selected level {difficulty}-{levelIndex} score={bestScore:F1} shortest={bestMetrics.shortestPathStrokes} shortestPaths={bestMetrics.shortestPathCount} reachable={bestMetrics.reachableStates}");
            return bestLevel;
        }

        Debug.LogWarning($"[EvaluatedGenerator] Fallback to smart generator for {difficulty}-{levelIndex}");
        return new SmartLevelGeneratorStrategy().GenerateLevel(difficulty, levelIndex, false);
    }

    private LevelData BuildCandidate(Difficulty difficulty, int levelIndex, GenerationProfile profile)
    {
        int width = Random.Range(profile.minWidth, profile.maxWidth + 1);
        int height = Random.Range(profile.minHeight, profile.maxHeight + 1);
        int targetPathLength = Random.Range(profile.minTargetPathLength, profile.maxTargetPathLength + 1);

        LevelData level = new LevelData
        {
            difficulty = difficulty,
            levelIndex = levelIndex,
            width = width,
            height = height,
            tilePowers = new int[width, height],
            gameMode = GameMode.Classic,
            currentStrokes = 0,
            currentPowerModifier = 0
        };

        if (!GenerateStructuredGoldenPath(level, profile.maxPower, targetPathLength))
            return null;

        FillNoise(level, profile.maxPower);
        PlantDecoys(level, profile.maxPower, profile.decoyCount);

        return level;
    }

    private bool GenerateStructuredGoldenPath(LevelData level, int maxPower, int targetPathLength)
    {
        Vector2Int holePos = new Vector2Int(Random.Range(0, level.width), Random.Range(0, level.height));
        level.holePosition = holePos;
        level.tilePowers[holePos.x, holePos.y] = 0;
        level.goldenPath.Add(holePos);

        Vector2Int currentPos = holePos;
        Vector2Int previousDirection = Vector2Int.zero;

        for (int i = 0; i < targetPathLength; i++)
        {
            List<PathCandidate> candidates = new List<PathCandidate>();
            foreach (var direction in BoardRules.GetDirections())
            {
                for (int distance = 1; distance <= maxPower; distance++)
                {
                    Vector2Int retreat = currentPos - (direction * distance);
                    if (!IsWithinBounds(level, retreat))
                        continue;
                    if (level.tilePowers[retreat.x, retreat.y] != 0 || retreat == level.holePosition)
                        continue;

                    int score = ScoreRetreatCandidate(level, retreat, currentPos, holePos, previousDirection, direction);
                    candidates.Add(new PathCandidate(retreat, distance, direction, score));
                }
            }

            if (candidates.Count == 0)
                break;

            candidates.Sort((a, b) => b.score.CompareTo(a.score));
            int pickIndex = Random.Range(0, Mathf.Min(3, candidates.Count));
            PathCandidate chosen = candidates[pickIndex];

            level.tilePowers[chosen.position.x, chosen.position.y] = chosen.distance;
            currentPos = chosen.position;
            previousDirection = chosen.direction;
            level.goldenPath.Add(currentPos);
        }

        if (level.goldenPath.Count < 3)
            return false;

        level.startPosition = currentPos;
        level.currentGridPosition = currentPos;
        return true;
    }

    private int ScoreRetreatCandidate(LevelData level, Vector2Int retreat, Vector2Int currentPos, Vector2Int holePos, Vector2Int previousDirection, Vector2Int direction)
    {
        int score = 0;

        int holeDistance = Mathf.Abs(retreat.x - holePos.x) + Mathf.Abs(retreat.y - holePos.y);
        score += holeDistance * 5;

        if (direction != previousDirection)
            score += 8;

        if (Mathf.Abs(direction.x) + Mathf.Abs(direction.y) == 2)
            score += 4;

        int nearbyGoldenTiles = CountNearbyGoldenTiles(level, retreat);
        score -= nearbyGoldenTiles * 6;

        int edgePenalty = 0;
        if (retreat.x == 0 || retreat.x == level.width - 1) edgePenalty++;
        if (retreat.y == 0 || retreat.y == level.height - 1) edgePenalty++;
        score -= edgePenalty * 3;

        int turnPotential = Mathf.Abs(retreat.x - currentPos.x) + Mathf.Abs(retreat.y - currentPos.y);
        score += turnPotential;

        return score;
    }

    private void FillNoise(LevelData level, int maxPower)
    {
        for (int x = 0; x < level.width; x++)
        {
            for (int y = 0; y < level.height; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (pos == level.holePosition)
                    continue;
                if (level.tilePowers[x, y] != 0)
                    continue;

                int distanceToHole = Mathf.Abs(pos.x - level.holePosition.x) + Mathf.Abs(pos.y - level.holePosition.y);
                int biasedMax = Mathf.Clamp(1 + (distanceToHole / 2), 2, maxPower);
                level.tilePowers[x, y] = Random.Range(1, biasedMax + 1);
            }
        }
    }

    private void PlantDecoys(LevelData level, int maxPower, int desiredCount)
    {
        int placed = 0;
        for (int i = 1; i < level.goldenPath.Count - 1 && placed < desiredCount; i++)
        {
            Vector2Int anchor = level.goldenPath[i];
            foreach (var direction in BoardRules.GetDirections())
            {
                if (placed >= desiredCount)
                    break;

                Vector2Int decoyPos = anchor + direction;
                if (!IsWithinBounds(level, decoyPos))
                    continue;
                if (level.goldenPath.Contains(decoyPos) || decoyPos == level.holePosition)
                    continue;

                int anchorPower = level.tilePowers[anchor.x, anchor.y];
                int decoyPower = Mathf.Clamp(anchorPower + Random.Range(-1, 2), 1, maxPower);
                level.tilePowers[decoyPos.x, decoyPos.y] = decoyPower;
                placed++;
            }
        }
    }

    private int CalculatePar(GenerationProfile profile, ClassicLevelMetrics metrics)
    {
        if (metrics == null)
            return profile.minTargetPathLength;

        return Mathf.Max(metrics.shortestPathStrokes + profile.parBuffer, 1);
    }

    private float ScoreCandidate(GenerationProfile profile, LevelData level, ClassicLevelMetrics metrics)
    {
        float score = 1000f;

        score -= Mathf.Abs(metrics.shortestPathStrokes - profile.targetShortestPath) * 90f;

        if (metrics.shortestPathStrokes < profile.minTargetPathLength)
            score -= 250f;
        if (metrics.shortestPathStrokes > profile.maxTargetPathLength + 1)
            score -= 150f;

        score -= (Mathf.Min(metrics.shortestPathCount, MaxPathCountForScoring) - 1) * 180f;

        if (metrics.validFirstMoves < profile.minPreferredFirstMoves)
            score -= (profile.minPreferredFirstMoves - metrics.validFirstMoves) * 80f;
        if (metrics.validFirstMoves > profile.maxPreferredFirstMoves)
            score -= (metrics.validFirstMoves - profile.maxPreferredFirstMoves) * 45f;

        if (metrics.reachableStates < metrics.shortestPathStrokes + 2)
            score -= 120f;
        if (metrics.reachableStates < profile.reachableFloor)
            score -= 100f;

        float deadEndRatio = metrics.reachableStates > 0 ? (float)metrics.reachableDeadEnds / metrics.reachableStates : 0f;
        score += deadEndRatio * 140f;

        score += Mathf.Min(level.goldenPath.Count, 20) * 6f;

        return score;
    }

    private int CountNearbyGoldenTiles(LevelData level, Vector2Int position)
    {
        int count = 0;
        for (int i = 0; i < level.goldenPath.Count; i++)
        {
            Vector2Int golden = level.goldenPath[i];
            if (Mathf.Abs(golden.x - position.x) <= 1 && Mathf.Abs(golden.y - position.y) <= 1)
                count++;
        }
        return count;
    }

    private bool IsWithinBounds(LevelData level, Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < level.width && pos.y >= 0 && pos.y < level.height;
    }

    private LevelData GenerateTutorialLevel()
    {
        return new SmartLevelGeneratorStrategy().GenerateLevel(Difficulty.Easy, 0, true);
    }

    private GenerationProfile GetProfile(Difficulty difficulty)
    {
        switch (difficulty)
        {
            case Difficulty.Easy:
                return new GenerationProfile(5, 5, 5, 5, 3, 4, 3, 2, 18, 1, 2, 1, 4, 1);
            case Difficulty.Medium:
                return new GenerationProfile(6, 7, 6, 7, 5, 7, 6, 4, 24, 2, 2, 1, 4, 1);
            case Difficulty.Hard:
                return new GenerationProfile(8, 9, 8, 9, 8, 11, 9, 5, 32, 3, 2, 2, 5, 0);
            default:
                return new GenerationProfile(5, 5, 5, 5, 3, 4, 3, 2, 18, 1, 2, 1, 4, 1);
        }
    }

    private readonly struct PathCandidate
    {
        public readonly Vector2Int position;
        public readonly int distance;
        public readonly Vector2Int direction;
        public readonly int score;

        public PathCandidate(Vector2Int position, int distance, Vector2Int direction, int score)
        {
            this.position = position;
            this.distance = distance;
            this.direction = direction;
            this.score = score;
        }
    }

    private readonly struct GenerationProfile
    {
        public readonly int minWidth;
        public readonly int maxWidth;
        public readonly int minHeight;
        public readonly int maxHeight;
        public readonly int minTargetPathLength;
        public readonly int maxTargetPathLength;
        public readonly int targetShortestPath;
        public readonly int maxPower;
        public readonly int candidateCount;
        public readonly int decoyCount;
        public readonly int minPreferredFirstMoves;
        public readonly int maxPreferredFirstMoves;
        public readonly int reachableFloor;
        public readonly int parBuffer;

        public GenerationProfile(
            int minWidth,
            int maxWidth,
            int minHeight,
            int maxHeight,
            int minTargetPathLength,
            int maxTargetPathLength,
            int targetShortestPath,
            int maxPower,
            int candidateCount,
            int decoyCount,
            int minPreferredFirstMoves,
            int maxPreferredFirstMoves,
            int reachableFloor,
            int parBuffer)
        {
            this.minWidth = minWidth;
            this.maxWidth = maxWidth;
            this.minHeight = minHeight;
            this.maxHeight = maxHeight;
            this.minTargetPathLength = minTargetPathLength;
            this.maxTargetPathLength = maxTargetPathLength;
            this.targetShortestPath = targetShortestPath;
            this.maxPower = maxPower;
            this.candidateCount = candidateCount;
            this.decoyCount = decoyCount;
            this.minPreferredFirstMoves = minPreferredFirstMoves;
            this.maxPreferredFirstMoves = maxPreferredFirstMoves;
            this.reachableFloor = reachableFloor;
            this.parBuffer = parBuffer;
        }
    }
}

public sealed class ClassicLevelMetrics
{
    public bool isSolvable;
    public int shortestPathStrokes;
    public int shortestPathCount;
    public int reachableStates;
    public int reachableDeadEnds;
    public int validFirstMoves;
}

public static class ClassicLevelAnalyzer
{
    public static ClassicLevelMetrics Analyze(LevelData level)
    {
        var metrics = new ClassicLevelMetrics();
        if (level == null)
            return metrics;

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        Dictionary<Vector2Int, int> distances = new Dictionary<Vector2Int, int>();
        Dictionary<Vector2Int, int> shortestPathCounts = new Dictionary<Vector2Int, int>();

        queue.Enqueue(level.startPosition);
        distances[level.startPosition] = 0;
        shortestPathCounts[level.startPosition] = 1;

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            int nextDistance = distances[current] + 1;

            foreach (var next in BoardRules.GetValidDestinations(level, current))
            {
                if (!distances.ContainsKey(next))
                {
                    distances[next] = nextDistance;
                    shortestPathCounts[next] = shortestPathCounts[current];
                    queue.Enqueue(next);
                }
                else if (distances[next] == nextDistance)
                {
                    shortestPathCounts[next] = Mathf.Min(shortestPathCounts[next] + shortestPathCounts[current], MaxPathCountForTracking);
                }
            }
        }

        metrics.validFirstMoves = CountFirstMoves(level);
        metrics.reachableStates = distances.Count;
        metrics.reachableDeadEnds = CountReachableDeadEnds(level, distances);

        if (distances.ContainsKey(level.holePosition))
        {
            metrics.isSolvable = true;
            metrics.shortestPathStrokes = distances[level.holePosition];
            metrics.shortestPathCount = shortestPathCounts[level.holePosition];
        }

        return metrics;
    }

    private const int MaxPathCountForTracking = 32;

    private static int CountFirstMoves(LevelData level)
    {
        int count = 0;
        foreach (var _ in BoardRules.GetValidDestinations(level, level.startPosition))
            count++;
        return count;
    }

    private static int CountReachableDeadEnds(LevelData level, Dictionary<Vector2Int, int> distances)
    {
        int count = 0;
        foreach (var kvp in distances)
        {
            Vector2Int pos = kvp.Key;
            if (pos == level.holePosition)
                continue;
            if (!BoardRules.HasValidMoves(level, pos))
                count++;
        }
        return count;
    }
}
