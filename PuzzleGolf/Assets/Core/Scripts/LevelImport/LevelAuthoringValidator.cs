using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class LevelAuthoringValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<string> Errors { get; } = new List<string>();
    public List<string> Warnings { get; } = new List<string>();
}

public static class LevelAuthoringValidator
{
    public static LevelAuthoringValidationResult Validate(LevelAuthoringDto dto)
    {
        var result = new LevelAuthoringValidationResult();
        if (dto == null)
        {
            result.Errors.Add("Level payload is null.");
            return result;
        }

        if (dto.schemaVersion != 1)
            result.Errors.Add($"Unsupported schemaVersion: {dto.schemaVersion}. Expected 1.");

        if (string.IsNullOrWhiteSpace(dto.id))
            result.Errors.Add("Level id is required.");
        if (string.IsNullOrWhiteSpace(dto.name))
            result.Errors.Add("Level name is required.");
        if (dto.width <= 0 || dto.height <= 0)
            result.Errors.Add("Width and height must be greater than 0.");
        if (dto.levelPar < 1)
            result.Errors.Add("Level par must be at least 1.");

        if (!TryParseDifficulty(dto.difficulty, out _))
            result.Errors.Add($"Invalid difficulty '{dto.difficulty}'.");
        if (!TryParseMode(dto.mode, out _))
            result.Errors.Add($"Invalid mode '{dto.mode}'.");

        if (dto.startPosition == null)
            result.Errors.Add("startPosition is required.");
        if (dto.holePosition == null)
            result.Errors.Add("holePosition is required.");

        if (dto.startPosition != null && !IsWithinBounds(dto.startPosition.x, dto.startPosition.y, dto.width, dto.height))
            result.Errors.Add("startPosition is out of bounds.");
        if (dto.holePosition != null && !IsWithinBounds(dto.holePosition.x, dto.holePosition.y, dto.width, dto.height))
            result.Errors.Add("holePosition is out of bounds.");

        var seen = new HashSet<string>(StringComparer.Ordinal);
        if (dto.tiles == null || dto.tiles.Length == 0)
        {
            result.Warnings.Add("Level has no explicit tiles. Import will normalize missing cells to Standard power 1.");
            return result;
        }

        for (int i = 0; i < dto.tiles.Length; i++)
        {
            TileAuthoringDto tile = dto.tiles[i];
            if (tile == null)
            {
                result.Errors.Add($"Tile entry at index {i} is null.");
                continue;
            }

            if (!IsWithinBounds(tile.x, tile.y, dto.width, dto.height))
                result.Errors.Add($"Tile ({tile.x},{tile.y}) is out of bounds.");

            string key = $"{tile.x}:{tile.y}";
            if (!seen.Add(key))
                result.Errors.Add($"Duplicate tile coordinate at ({tile.x},{tile.y}).");

            if (tile.power < 0)
                result.Errors.Add($"Tile ({tile.x},{tile.y}) has negative power.");

            if (!Enum.TryParse(tile.type, true, out TileType tileType))
            {
                result.Errors.Add($"Tile ({tile.x},{tile.y}) has invalid type '{tile.type}'.");
                continue;
            }

            if (tileType == TileType.Hole && tile.power != 0)
                result.Errors.Add($"Hole tile at ({tile.x},{tile.y}) must have power 0.");

            if (dto.startPosition != null &&
                tileType == TileType.Start &&
                (tile.x != dto.startPosition.x || tile.y != dto.startPosition.y))
            {
                result.Errors.Add($"Start tile at ({tile.x},{tile.y}) does not match startPosition.");
            }

            if (dto.holePosition != null &&
                tileType == TileType.Hole &&
                (tile.x != dto.holePosition.x || tile.y != dto.holePosition.y))
            {
                result.Errors.Add($"Hole tile at ({tile.x},{tile.y}) does not match holePosition.");
            }
        }

        return result;
    }

    public static bool TryParseDifficulty(string value, out Difficulty difficulty)
    {
        return Enum.TryParse(value, true, out difficulty);
    }

    public static bool TryParseMode(string value, out GameMode mode)
    {
        return Enum.TryParse(value, true, out mode);
    }

    private static bool IsWithinBounds(int x, int y, int width, int height)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }
}
