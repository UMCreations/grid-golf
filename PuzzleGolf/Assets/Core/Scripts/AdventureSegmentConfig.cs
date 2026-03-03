using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class AdventureSegmentConfig
{
    public int startLevel;
    public int endLevel;
    public string themeName;
    
    public int minGridSize;
    public int maxGridSize;
    
    public int minPathLength;
    public int maxPathLength;
    
    public int maxPower;
    
    public int maxHazardsOnPath;
    public int maxHazardsInNoise;
    
    public List<TileType> allowedHazards = new List<TileType>();
    
    [Header("UI Aesthetics")]
    public Color segmentColor = Color.gray;
}
