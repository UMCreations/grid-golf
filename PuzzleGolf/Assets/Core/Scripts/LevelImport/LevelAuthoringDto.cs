using System;
using UnityEngine;

[Serializable]
public class LevelAuthoringDto
{
    public int schemaVersion = 1;
    public string id;
    public string name;
    public string mode;
    public string difficulty;
    public int width;
    public int height;
    public GridPositionDto startPosition;
    public GridPositionDto holePosition;
    public int levelPar;
    public TileAuthoringDto[] tiles;
    public LevelMetadataDto metadata;
}

[Serializable]
public class GridPositionDto
{
    public int x;
    public int y;

    public Vector2Int ToVector2Int()
    {
        return new Vector2Int(x, y);
    }

    public static GridPositionDto FromVector2Int(Vector2Int value)
    {
        return new GridPositionDto { x = value.x, y = value.y };
    }
}

[Serializable]
public class TileAuthoringDto
{
    public int x;
    public int y;
    public string type;
    public int power;

    public Vector2Int ToVector2Int()
    {
        return new Vector2Int(x, y);
    }
}

[Serializable]
public class LevelMetadataDto
{
    public string author;
    public string[] tags;
    public string notes;
    public string createdAt;
    public string updatedAt;
}
