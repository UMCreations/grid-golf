using System;
using System.Collections.Generic;
using UnityEngine;

public static class LevelAuthoringConverter
{
    public static LevelData ToLevelData(LevelAuthoringDto dto, int levelIndex = 1)
    {
        LevelAuthoringValidationResult validation = LevelAuthoringValidator.Validate(dto);
        if (!validation.IsValid)
            throw new InvalidOperationException(string.Join("\n", validation.Errors));

        LevelAuthoringValidator.TryParseDifficulty(dto.difficulty, out Difficulty difficulty);
        LevelAuthoringValidator.TryParseMode(dto.mode, out GameMode mode);

        LevelData data = new LevelData
        {
            difficulty = difficulty,
            gameMode = mode,
            width = dto.width,
            height = dto.height,
            startPosition = dto.startPosition.ToVector2Int(),
            holePosition = dto.holePosition.ToVector2Int(),
            currentGridPosition = dto.startPosition.ToVector2Int(),
            currentStrokes = 0,
            levelPar = dto.levelPar,
            levelIndex = levelIndex,
            tilePowers = new int[dto.width, dto.height],
            tileTypes = new TileType[dto.width, dto.height]
        };

        for (int x = 0; x < dto.width; x++)
        {
            for (int y = 0; y < dto.height; y++)
            {
                data.tilePowers[x, y] = 1;
                data.tileTypes[x, y] = TileType.Standard;
            }
        }

        if (dto.tiles != null)
        {
            foreach (TileAuthoringDto tile in dto.tiles)
            {
                if (tile == null) continue;
                if (!Enum.TryParse(tile.type, true, out TileType tileType)) continue;

                data.tilePowers[tile.x, tile.y] = tile.power;
                data.tileTypes[tile.x, tile.y] = tileType;
            }
        }

        Vector2Int start = data.startPosition;
        Vector2Int hole = data.holePosition;
        data.tileTypes[start.x, start.y] = TileType.Start;
        data.tileTypes[hole.x, hole.y] = TileType.Hole;
        data.tilePowers[hole.x, hole.y] = 0;

        data.Flatten();
        return data;
    }

    public static void ApplyToHandcraftedLevel(HandcraftedLevelSO asset, LevelAuthoringDto dto)
    {
        if (asset == null)
            throw new ArgumentNullException(nameof(asset));

        LevelData data = ToLevelData(dto, 1);

        asset.levelName = string.IsNullOrWhiteSpace(dto.name) ? dto.id : dto.name;
        asset.width = data.width;
        asset.height = data.height;
        asset.difficulty = data.difficulty;
        asset.startPosition = data.startPosition;
        asset.holePosition = data.holePosition;
        asset.levelPar = data.levelPar;
        asset.ResetLayout();

        for (int x = 0; x < data.width; x++)
        {
            for (int y = 0; y < data.height; y++)
            {
                int index = y * data.width + x;
                asset.tilePowers[index] = data.tilePowers[x, y];
                asset.tileTypes[index] = data.tileTypes[x, y];
            }
        }
    }

    public static LevelAuthoringDto FromHandcraftedLevel(HandcraftedLevelSO asset, string levelId, string author = null)
    {
        if (asset == null)
            throw new ArgumentNullException(nameof(asset));

        int tileCount = asset.width * asset.height;
        var tiles = new List<TileAuthoringDto>(tileCount);

        for (int y = 0; y < asset.height; y++)
        {
            for (int x = 0; x < asset.width; x++)
            {
                int index = y * asset.width + x;
                TileType tileType = index < asset.tileTypes.Length ? asset.tileTypes[index] : TileType.Standard;
                int power = index < asset.tilePowers.Length ? asset.tilePowers[index] : 1;

                tiles.Add(new TileAuthoringDto
                {
                    x = x,
                    y = y,
                    type = tileType.ToString(),
                    power = power
                });
            }
        }

        return new LevelAuthoringDto
        {
            schemaVersion = 1,
            id = levelId,
            name = asset.levelName,
            mode = GameMode.Adventure.ToString(),
            difficulty = asset.difficulty.ToString(),
            width = asset.width,
            height = asset.height,
            startPosition = GridPositionDto.FromVector2Int(asset.startPosition),
            holePosition = GridPositionDto.FromVector2Int(asset.holePosition),
            levelPar = asset.levelPar,
            tiles = tiles.ToArray(),
            metadata = new LevelMetadataDto
            {
                author = author,
                updatedAt = DateTime.UtcNow.ToString("o")
            }
        };
    }
}
