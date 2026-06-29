using System.Collections.Generic;
using UnityEngine;

public enum MoveInvalidReason
{
    None,
    MissingLevelData,
    ZeroDirection,
    SourceOutOfBounds,
    SourceIsHole,
    NonPositivePower,
    TargetOutOfBounds
}

public sealed class BoardMoveResult
{
    public bool IsValid;
    public MoveInvalidReason InvalidReason;
    public Vector2Int StartPosition;
    public Vector2Int Direction;
    public int EffectivePower;
    public int ConsumedPowerModifier;
    public int NextPowerModifier;
    public Vector2Int TargetPosition;
    public TileType FinalTileType;
    public bool ReachedHole;
    public bool TriggersAutoSlide;
    public bool HasAnyValidMovesFromFinal;
}

public static class BoardRules
{
    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
        new Vector2Int(1, 1), new Vector2Int(-1, 1), new Vector2Int(1, -1), new Vector2Int(-1, -1)
    };

    public static IReadOnlyList<Vector2Int> GetDirections()
    {
        return Directions;
    }

    public static IEnumerable<Vector2Int> GetValidDestinations(LevelData level, Vector2Int sourcePos, int powerModifier = 0)
    {
        if (level == null || !IsWithinBounds(level, sourcePos))
            yield break;

        int power = GetEffectivePower(level, sourcePos, powerModifier);
        if (power <= 0)
            yield break;

        foreach (var direction in Directions)
        {
            Vector2Int targetPos = sourcePos + (direction * power);
            if (IsWithinBounds(level, targetPos))
                yield return targetPos;
        }
    }

    public static bool HasValidMoves(LevelData level, Vector2Int sourcePos, int powerModifier = 0)
    {
        foreach (var _ in GetValidDestinations(level, sourcePos, powerModifier))
            return true;

        return false;
    }

    public static BoardMoveResult ResolveMove(LevelData level, Vector2Int sourcePos, Vector2Int direction, int powerModifier = 0)
    {
        var result = new BoardMoveResult
        {
            IsValid = false,
            InvalidReason = MoveInvalidReason.None,
            StartPosition = sourcePos,
            Direction = direction,
            ConsumedPowerModifier = powerModifier
        };

        if (level == null)
        {
            result.InvalidReason = MoveInvalidReason.MissingLevelData;
            return result;
        }

        if (direction == Vector2Int.zero)
        {
            result.InvalidReason = MoveInvalidReason.ZeroDirection;
            return result;
        }

        if (!IsWithinBounds(level, sourcePos))
        {
            result.InvalidReason = MoveInvalidReason.SourceOutOfBounds;
            return result;
        }

        if (sourcePos == level.holePosition)
        {
            result.InvalidReason = MoveInvalidReason.SourceIsHole;
            return result;
        }

        int power = GetEffectivePower(level, sourcePos, powerModifier);
        result.EffectivePower = power;
        if (power <= 0)
        {
            result.InvalidReason = MoveInvalidReason.NonPositivePower;
            return result;
        }

        Vector2Int targetPos = sourcePos + (direction * power);
        if (!IsWithinBounds(level, targetPos))
        {
            result.InvalidReason = MoveInvalidReason.TargetOutOfBounds;
            return result;
        }

        result.IsValid = true;
        result.TargetPosition = targetPos;
        result.FinalTileType = GetTileType(level, targetPos);
        result.ReachedHole = targetPos == level.holePosition;

        if (level.gameMode == GameMode.Adventure && !result.ReachedHole)
        {
            ITileEffect effect = TileEffectResolver.GetEffect(result.FinalTileType);
            if (effect != null)
            {
                int landingPower = GetTilePower(level, targetPos);
                int modifiedPower = effect.ApplyEffect(landingPower, direction);
                result.NextPowerModifier = modifiedPower - landingPower;
                result.TriggersAutoSlide = effect.CausesAutoSlide;
            }
        }

        result.HasAnyValidMovesFromFinal = result.ReachedHole ||
            HasValidMoves(level, targetPos, result.NextPowerModifier);

        return result;
    }

    public static int GetEffectivePower(LevelData level, Vector2Int position, int powerModifier = 0)
    {
        if (level == null || !IsWithinBounds(level, position))
            return 0;

        int basePower = GetTilePower(level, position);
        if (basePower <= 0)
            return 0;

        return Mathf.Max(1, basePower + powerModifier);
    }

    public static int GetTilePower(LevelData level, Vector2Int position)
    {
        if (level == null || level.tilePowers == null || !IsWithinBounds(level, position))
            return 0;

        return level.tilePowers[position.x, position.y];
    }

    public static TileType GetTileType(LevelData level, Vector2Int position)
    {
        if (level == null || !IsWithinBounds(level, position))
            return TileType.Standard;

        if (position == level.holePosition)
            return TileType.Hole;

        if (position == level.startPosition)
            return TileType.Start;

        if (level.tileTypes != null)
            return level.tileTypes[position.x, position.y];

        return TileType.Standard;
    }

    private static bool IsWithinBounds(LevelData level, Vector2Int position)
    {
        return position.x >= 0 && position.x < level.width &&
               position.y >= 0 && position.y < level.height;
    }
}
