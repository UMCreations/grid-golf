using System.Collections.Generic;
using UnityEngine;

public class PatternDrivenClassicLevelGeneratorStrategy : ILevelGeneratorStrategy
{
    public LevelData GenerateLevel(Difficulty difficulty, int levelIndex, bool isTutorial = false)
    {
        if (isTutorial)
            return new SmartLevelGeneratorStrategy().GenerateLevel(Difficulty.Easy, 0, true);

        PatternGenerationProfile profile = GetProfile(difficulty);
        int baseSeed = 40000 + ((int)difficulty * 3000) + levelIndex;
        LevelArchetype archetype = PickArchetype(difficulty, levelIndex);

        LevelData bestLevel = null;
        ClassicLevelMetrics bestMetrics = null;
        float bestScore = float.NegativeInfinity;
        string bestPatternSummary = string.Empty;

        for (int attempt = 0; attempt < profile.candidateCount; attempt++)
        {
            Random.InitState(baseSeed + (attempt * 6151));
            LevelData candidate = CreateBaseLevel(difficulty, levelIndex, profile);
            if (candidate == null)
                continue;

            GenerateBasePath(candidate, profile);
            if (candidate.goldenPath.Count < profile.minimumGoldenPathNodes)
                continue;

            List<IClassicPatternModule> patterns = BuildPatternPack(archetype, difficulty);
            foreach (var pattern in patterns)
                pattern.Apply(candidate, profile);

            FillAmbientNoise(candidate, profile, archetype);
            PolishNearHole(candidate, profile);

            ClassicLevelMetrics metrics = ClassicLevelAnalyzer.Analyze(candidate);
            if (!metrics.isSolvable)
                continue;

            float score = ScoreCandidate(candidate, metrics, profile, patterns, archetype);
            if (score > bestScore)
            {
                bestScore = score;
                bestLevel = candidate;
                bestMetrics = metrics;
                bestPatternSummary = string.Join(", ", patterns.ConvertAll(p => p.Name));
            }
        }

        if (bestLevel != null)
        {
            bestLevel.levelPar = Mathf.Max(bestMetrics.shortestPathStrokes + profile.parBuffer, 1);
            Debug.Log($"[PatternGenerator] Selected {difficulty}-{levelIndex} archetype={archetype} score={bestScore:F1} patterns=[{bestPatternSummary}] shortest={bestMetrics.shortestPathStrokes} firstMoves={bestMetrics.validFirstMoves} shortestPaths={bestMetrics.shortestPathCount}");
            return bestLevel;
        }

        Debug.LogWarning($"[PatternGenerator] Fallback to evaluated classic generator for {difficulty}-{levelIndex}");
        return new EvaluatedClassicLevelGeneratorStrategy().GenerateLevel(difficulty, levelIndex, false);
    }

    private LevelData CreateBaseLevel(Difficulty difficulty, int levelIndex, PatternGenerationProfile profile)
    {
        int width = Random.Range(profile.minWidth, profile.maxWidth + 1);
        int height = Random.Range(profile.minHeight, profile.maxHeight + 1);

        return new LevelData
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
    }

    private void GenerateBasePath(LevelData level, PatternGenerationProfile profile)
    {
        Vector2Int hole = new Vector2Int(Random.Range(0, level.width), Random.Range(0, level.height));
        level.holePosition = hole;
        level.tilePowers[hole.x, hole.y] = 0;
        level.goldenPath.Add(hole);

        Vector2Int current = hole;
        Vector2Int previousDirection = Vector2Int.zero;
        int targetPathLength = Random.Range(profile.minTargetPathLength, profile.maxTargetPathLength + 1);

        for (int step = 0; step < targetPathLength; step++)
        {
            List<PatternPathCandidate> candidates = new List<PatternPathCandidate>();
            foreach (var direction in BoardRules.GetDirections())
            {
                for (int distance = 1; distance <= profile.maxPower; distance++)
                {
                    Vector2Int retreat = current - (direction * distance);
                    if (!IsWithinBounds(level, retreat))
                        continue;
                    if (level.tilePowers[retreat.x, retreat.y] != 0 || retreat == hole)
                        continue;

                    int score = 0;
                    score += Mathf.Abs(retreat.x - hole.x) * 4 + Mathf.Abs(retreat.y - hole.y) * 4;
                    if (direction != previousDirection) score += 6;
                    if (Mathf.Abs(direction.x) + Mathf.Abs(direction.y) == 2) score += 4;
                    if (retreat.x > 0 && retreat.x < level.width - 1) score += 2;
                    if (retreat.y > 0 && retreat.y < level.height - 1) score += 2;
                    score -= CountNearbyGolden(level, retreat) * 5;

                    candidates.Add(new PatternPathCandidate(retreat, distance, direction, score));
                }
            }

            if (candidates.Count == 0)
                break;

            candidates.Sort((a, b) => b.score.CompareTo(a.score));
            PatternPathCandidate chosen = candidates[Random.Range(0, Mathf.Min(4, candidates.Count))];
            level.tilePowers[chosen.position.x, chosen.position.y] = chosen.distance;
            current = chosen.position;
            previousDirection = chosen.direction;
            level.goldenPath.Add(current);
        }

        level.startPosition = current;
        level.currentGridPosition = current;
    }

    private List<IClassicPatternModule> BuildPatternPack(LevelArchetype archetype, Difficulty difficulty)
    {
        List<IClassicPatternModule> patterns = new List<IClassicPatternModule>();

        switch (archetype)
        {
            case LevelArchetype.BaitAndSwitch:
                patterns.Add(new BaitHolePattern());
                patterns.Add(new FalseShortcutPattern());
                break;
            case LevelArchetype.TrickRoute:
                patterns.Add(new ForcedTurnPattern());
                patterns.Add(new DiagonalCommitPattern());
                break;
            case LevelArchetype.TrapGarden:
                patterns.Add(new LoopTrapPattern());
                patterns.Add(new FalseShortcutPattern());
                break;
            case LevelArchetype.RhythmRun:
                patterns.Add(new RhythmVariationPattern());
                patterns.Add(new ForcedTurnPattern());
                break;
            default:
                patterns.Add(new ForcedTurnPattern());
                patterns.Add(new BaitHolePattern());
                break;
        }

        if (difficulty != Difficulty.Easy && patterns.Find(p => p.Name == nameof(LoopTrapPattern)) == null)
            patterns.Add(new LoopTrapPattern());

        return patterns;
    }

    private void FillAmbientNoise(LevelData level, PatternGenerationProfile profile, LevelArchetype archetype)
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

                int bias = archetype == LevelArchetype.RhythmRun ? 0 : 1;
                int distanceToHole = Mathf.Abs(pos.x - level.holePosition.x) + Mathf.Abs(pos.y - level.holePosition.y);
                int power = Mathf.Clamp(Random.Range(1, profile.maxPower + 1) - bias + (distanceToHole / 5), 1, profile.maxPower);
                level.tilePowers[x, y] = power;
            }
        }
    }

    private void PolishNearHole(LevelData level, PatternGenerationProfile profile)
    {
        foreach (var direction in BoardRules.GetDirections())
        {
            Vector2Int nearHole = level.holePosition + direction;
            if (!IsWithinBounds(level, nearHole))
                continue;
            if (nearHole == level.startPosition || level.goldenPath.Contains(nearHole))
                continue;

            if (Random.value < 0.35f)
                level.tilePowers[nearHole.x, nearHole.y] = Mathf.Clamp(Random.Range(1, 3), 1, profile.maxPower);
        }
    }

    private float ScoreCandidate(LevelData level, ClassicLevelMetrics metrics, PatternGenerationProfile profile, List<IClassicPatternModule> patterns, LevelArchetype archetype)
    {
        float score = 1200f;

        score -= Mathf.Abs(metrics.shortestPathStrokes - profile.targetShortestPath) * 85f;
        score -= (Mathf.Min(metrics.shortestPathCount, 6) - 1) * 200f;

        if (metrics.validFirstMoves < profile.minInterestingFirstMoves)
            score -= (profile.minInterestingFirstMoves - metrics.validFirstMoves) * 90f;
        if (metrics.validFirstMoves > profile.maxInterestingFirstMoves)
            score -= (metrics.validFirstMoves - profile.maxInterestingFirstMoves) * 35f;

        float deadEndRatio = metrics.reachableStates > 0 ? (float)metrics.reachableDeadEnds / metrics.reachableStates : 0f;
        score += deadEndRatio * 180f;

        int rhythmScore = ScoreRhythm(level);
        score += rhythmScore * 18f;

        int holeDrama = CountNearHoleDecoys(level);
        score += holeDrama * 45f;

        score += patterns.Count * 40f;

        switch (archetype)
        {
            case LevelArchetype.BaitAndSwitch:
                score += holeDrama * 15f;
                break;
            case LevelArchetype.TrapGarden:
                score += metrics.reachableDeadEnds * 12f;
                break;
            case LevelArchetype.RhythmRun:
                score += rhythmScore * 10f;
                break;
        }

        return score;
    }

    private int ScoreRhythm(LevelData level)
    {
        if (level.goldenPath.Count < 3)
            return 0;

        int score = 0;
        int previousPower = -1;
        HashSet<int> seenPowers = new HashSet<int>();

        for (int i = level.goldenPath.Count - 1; i >= 1; i--)
        {
            Vector2Int pos = level.goldenPath[i];
            int power = level.tilePowers[pos.x, pos.y];
            seenPowers.Add(power);

            if (previousPower != -1 && previousPower != power)
                score++;

            previousPower = power;
        }

        score += seenPowers.Count;
        return score;
    }

    private int CountNearHoleDecoys(LevelData level)
    {
        int count = 0;
        foreach (var direction in BoardRules.GetDirections())
        {
            Vector2Int pos = level.holePosition + direction;
            if (!IsWithinBounds(level, pos))
                continue;
            if (level.goldenPath.Contains(pos))
                continue;
            if (level.tilePowers[pos.x, pos.y] > 0)
                count++;
        }
        return count;
    }

    private LevelArchetype PickArchetype(Difficulty difficulty, int levelIndex)
    {
        LevelArchetype[] easy =
        {
            LevelArchetype.BaitAndSwitch,
            LevelArchetype.TrickRoute,
            LevelArchetype.RhythmRun
        };

        LevelArchetype[] harder =
        {
            LevelArchetype.BaitAndSwitch,
            LevelArchetype.TrickRoute,
            LevelArchetype.TrapGarden,
            LevelArchetype.RhythmRun
        };

        LevelArchetype[] pool = difficulty == Difficulty.Easy ? easy : harder;
        return pool[levelIndex % pool.Length];
    }

    private PatternGenerationProfile GetProfile(Difficulty difficulty)
    {
        switch (difficulty)
        {
            case Difficulty.Easy:
                return new PatternGenerationProfile(5, 5, 5, 5, 4, 5, 2, 20, 3, 2, 4, 1, 4);
            case Difficulty.Medium:
                return new PatternGenerationProfile(6, 7, 6, 7, 6, 8, 4, 28, 5, 2, 5, 1, 5);
            case Difficulty.Hard:
                return new PatternGenerationProfile(8, 9, 8, 9, 8, 11, 5, 36, 8, 2, 6, 0, 5);
            default:
                return new PatternGenerationProfile(5, 5, 5, 5, 4, 5, 2, 20, 3, 2, 4, 1, 4);
        }
    }

    private int CountNearbyGolden(LevelData level, Vector2Int pos)
    {
        int count = 0;
        for (int i = 0; i < level.goldenPath.Count; i++)
        {
            Vector2Int g = level.goldenPath[i];
            if (Mathf.Abs(g.x - pos.x) <= 1 && Mathf.Abs(g.y - pos.y) <= 1)
                count++;
        }
        return count;
    }

    private bool IsWithinBounds(LevelData level, Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < level.width && pos.y >= 0 && pos.y < level.height;
    }

    private readonly struct PatternPathCandidate
    {
        public readonly Vector2Int position;
        public readonly int distance;
        public readonly Vector2Int direction;
        public readonly int score;

        public PatternPathCandidate(Vector2Int position, int distance, Vector2Int direction, int score)
        {
            this.position = position;
            this.distance = distance;
            this.direction = direction;
            this.score = score;
        }
    }
}

public enum LevelArchetype
{
    BaitAndSwitch,
    TrickRoute,
    TrapGarden,
    RhythmRun
}

public interface IClassicPatternModule
{
    string Name { get; }
    void Apply(LevelData level, PatternGenerationProfile profile);
}

public readonly struct PatternGenerationProfile
{
    public readonly int minWidth;
    public readonly int maxWidth;
    public readonly int minHeight;
    public readonly int maxHeight;
    public readonly int minTargetPathLength;
    public readonly int maxTargetPathLength;
    public readonly int maxPower;
    public readonly int candidateCount;
    public readonly int targetShortestPath;
    public readonly int minInterestingFirstMoves;
    public readonly int maxInterestingFirstMoves;
    public readonly int parBuffer;
    public readonly int minimumGoldenPathNodes;

    public PatternGenerationProfile(
        int minWidth,
        int maxWidth,
        int minHeight,
        int maxHeight,
        int minTargetPathLength,
        int maxTargetPathLength,
        int maxPower,
        int candidateCount,
        int targetShortestPath,
        int minInterestingFirstMoves,
        int maxInterestingFirstMoves,
        int parBuffer,
        int minimumGoldenPathNodes)
    {
        this.minWidth = minWidth;
        this.maxWidth = maxWidth;
        this.minHeight = minHeight;
        this.maxHeight = maxHeight;
        this.minTargetPathLength = minTargetPathLength;
        this.maxTargetPathLength = maxTargetPathLength;
        this.maxPower = maxPower;
        this.candidateCount = candidateCount;
        this.targetShortestPath = targetShortestPath;
        this.minInterestingFirstMoves = minInterestingFirstMoves;
        this.maxInterestingFirstMoves = maxInterestingFirstMoves;
        this.parBuffer = parBuffer;
        this.minimumGoldenPathNodes = minimumGoldenPathNodes;
    }
}

public sealed class BaitHolePattern : IClassicPatternModule
{
    public string Name => "BaitHole";

    public void Apply(LevelData level, PatternGenerationProfile profile)
    {
        foreach (var direction in BoardRules.GetDirections())
        {
            Vector2Int pos = level.holePosition + direction;
            if (!Within(level, pos) || level.goldenPath.Contains(pos) || pos == level.startPosition)
                continue;

            level.tilePowers[pos.x, pos.y] = Mathf.Clamp(Random.Range(1, 3), 1, profile.maxPower);
            break;
        }
    }

    private bool Within(LevelData level, Vector2Int pos) => pos.x >= 0 && pos.x < level.width && pos.y >= 0 && pos.y < level.height;
}

public sealed class FalseShortcutPattern : IClassicPatternModule
{
    public string Name => "FalseShortcut";

    public void Apply(LevelData level, PatternGenerationProfile profile)
    {
        if (level.goldenPath.Count < 4)
            return;

        Vector2Int anchor = level.goldenPath[Random.Range(1, level.goldenPath.Count - 2)];
        Vector2Int hole = level.holePosition;
        Vector2Int step = new Vector2Int(
            hole.x == anchor.x ? 0 : (hole.x > anchor.x ? 1 : -1),
            hole.y == anchor.y ? 0 : (hole.y > anchor.y ? 1 : -1)
        );

        Vector2Int falsePos = anchor + step;
        if (Within(level, falsePos) && !level.goldenPath.Contains(falsePos) && falsePos != level.holePosition)
        {
            int power = Mathf.Clamp(Mathf.Max(Mathf.Abs(hole.x - falsePos.x), Mathf.Abs(hole.y - falsePos.y)) - 1, 1, profile.maxPower);
            level.tilePowers[falsePos.x, falsePos.y] = power;
        }
    }

    private bool Within(LevelData level, Vector2Int pos) => pos.x >= 0 && pos.x < level.width && pos.y >= 0 && pos.y < level.height;
}

public sealed class ForcedTurnPattern : IClassicPatternModule
{
    public string Name => "ForcedTurn";

    public void Apply(LevelData level, PatternGenerationProfile profile)
    {
        if (level.goldenPath.Count < 4)
            return;

        for (int i = level.goldenPath.Count - 2; i >= 1; i--)
        {
            Vector2Int pos = level.goldenPath[i];
            int currentPower = level.tilePowers[pos.x, pos.y];
            if (currentPower < profile.maxPower)
            {
                level.tilePowers[pos.x, pos.y] = Mathf.Clamp(currentPower + 1, 1, profile.maxPower);
                return;
            }
        }
    }
}

public sealed class DiagonalCommitPattern : IClassicPatternModule
{
    public string Name => "DiagonalCommit";

    public void Apply(LevelData level, PatternGenerationProfile profile)
    {
        if (level.goldenPath.Count < 5)
            return;

        for (int i = 1; i < level.goldenPath.Count - 1; i++)
        {
            Vector2Int current = level.goldenPath[i];
            Vector2Int next = level.goldenPath[i - 1];
            Vector2Int delta = next - current;

            if (delta.x != 0 && delta.y != 0)
            {
                int power = Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y));
                level.tilePowers[current.x, current.y] = Mathf.Clamp(power, 1, profile.maxPower);
                return;
            }
        }
    }
}

public sealed class LoopTrapPattern : IClassicPatternModule
{
    public string Name => "LoopTrap";

    public void Apply(LevelData level, PatternGenerationProfile profile)
    {
        for (int x = 0; x < level.width - 2; x++)
        {
            for (int y = 0; y < level.height; y++)
            {
                Vector2Int a = new Vector2Int(x, y);
                Vector2Int b = new Vector2Int(x + 2, y);

                if (level.goldenPath.Contains(a) || level.goldenPath.Contains(b))
                    continue;
                if (a == level.holePosition || b == level.holePosition)
                    continue;

                level.tilePowers[a.x, a.y] = Mathf.Clamp(2, 1, profile.maxPower);
                level.tilePowers[b.x, b.y] = Mathf.Clamp(2, 1, profile.maxPower);
                return;
            }
        }
    }
}

public sealed class RhythmVariationPattern : IClassicPatternModule
{
    public string Name => "RhythmVariation";

    public void Apply(LevelData level, PatternGenerationProfile profile)
    {
        bool high = false;
        for (int i = level.goldenPath.Count - 1; i >= 1; i--)
        {
            Vector2Int pos = level.goldenPath[i];
            level.tilePowers[pos.x, pos.y] = high ? profile.maxPower : 1;
            high = !high;
        }
    }
}
