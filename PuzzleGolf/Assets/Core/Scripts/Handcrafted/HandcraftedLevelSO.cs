using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewHandcraftedLevel", menuName = "Puzzle Golf/Handcrafted Level")]
public class HandcraftedLevelSO : ScriptableObject
{
    public string levelName = "New Level";
    public Difficulty difficulty = Difficulty.Easy;
    public int width = 5;
    public int height = 5;
    
    private void OnValidate()
    {
        // Auto-init if arrays are empty upon creation
        if (tilePowers == null || tilePowers.Length == 0)
        {
            ResetLayout();
        }
    }
    
    [Header("Layout Data (Flat)")]
    public int[] tilePowers;
    public TileType[] tileTypes;
    
    public Vector2Int startPosition;
    public Vector2Int holePosition;
    public int levelPar;

    public LevelData ToLevelData(int index)
    {
        LevelData data = new LevelData();
        data.difficulty = difficulty;
        data.gameMode = GameMode.Adventure;
        data.width = width;
        data.height = height;
        data.startPosition = startPosition;
        data.holePosition = holePosition;
        data.levelPar = levelPar;
        data.levelIndex = index;
        data.currentGridPosition = startPosition;
        
        data.tilePowers = new int[width, height];
        data.tileTypes = new TileType[width, height];
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int flatIndex = y * width + x;
                if (flatIndex < tilePowers.Length)
                    data.tilePowers[x, y] = tilePowers[flatIndex];
                
                if (flatIndex < tileTypes.Length)
                    data.tileTypes[x, y] = tileTypes[flatIndex];
            }
        }
        
        data.Flatten();
        return data;
    }

    // Called when changing dimensions in editor
    public void ResetLayout()
    {
        tilePowers = new int[width * height];
        tileTypes = new TileType[width * height];
        for (int i = 0; i < tileTypes.Length; i++) tileTypes[i] = TileType.Standard;
    }
}
