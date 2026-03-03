using UnityEngine;
using System.Collections.Generic;

public static class AdventureSegmentResolver
{
    private static List<AdventureSegmentConfig> segments = new List<AdventureSegmentConfig>
    {
        // Segment 1: The Fairway (Levels 1 - 8)
        new AdventureSegmentConfig {
            startLevel = 1, endLevel = 8, themeName = "The Fairway",
            minGridSize = 5, maxGridSize = 5,
            minPathLength = 3, maxPathLength = 4, maxPower = 2,
            maxHazardsOnPath = 0, maxHazardsInNoise = 0,
            allowedHazards = new List<TileType>(),
            segmentColor = new Color(0.2f, 0.6f, 0.3f) // Lush Green
        },
        // Segment 2: Sandy Shores (Levels 9 - 15)
        new AdventureSegmentConfig {
            startLevel = 9, endLevel = 15, themeName = "Sandy Shores",
            minGridSize = 5, maxGridSize = 6,
            minPathLength = 4, maxPathLength = 5, maxPower = 3,
            maxHazardsOnPath = 1, maxHazardsInNoise = 0,
            allowedHazards = new List<TileType> { TileType.Sand },
            segmentColor = new Color(0.8f, 0.7f, 0.4f) // Sandy Yellow
        },
        // Segment 3: Deep Desert (Levels 16 - 25)
        new AdventureSegmentConfig {
            startLevel = 16, endLevel = 25, themeName = "Deep Desert",
            minGridSize = 6, maxGridSize = 6,
            minPathLength = 5, maxPathLength = 7, maxPower = 4,
            maxHazardsOnPath = 2, maxHazardsInNoise = 2,
            allowedHazards = new List<TileType> { TileType.Sand },
            segmentColor = new Color(0.9f, 0.5f, 0.2f) // Deep Orange
        },
        // Segment 4: Frozen Peaks (Levels 26 - 30)
        new AdventureSegmentConfig {
            startLevel = 26, endLevel = 30, themeName = "Frozen Peaks",
            minGridSize = 6, maxGridSize = 7,
            minPathLength = 5, maxPathLength = 7, maxPower = 4,
            maxHazardsOnPath = 2, maxHazardsInNoise = 2,
            allowedHazards = new List<TileType> { TileType.Sand, TileType.Ice },
            segmentColor = new Color(0.6f, 0.8f, 1.0f) // Light Blue
        },
        // Segment 5: Arctic Hazard (Levels 31 - 40)
        new AdventureSegmentConfig {
            startLevel = 31, endLevel = 40, themeName = "Arctic Hazard",
            minGridSize = 7, maxGridSize = 7,
            minPathLength = 7, maxPathLength = 9, maxPower = 4,
            maxHazardsOnPath = 3, maxHazardsInNoise = 3,
            allowedHazards = new List<TileType> { TileType.Sand, TileType.Ice },
            segmentColor = new Color(0.4f, 0.6f, 0.9f) // Deep Blue
        },
        // Segment 6: Launch Pad Valley (Levels 41 - 50)
        new AdventureSegmentConfig {
            startLevel = 41, endLevel = 50, themeName = "Launch Pad Valley",
            minGridSize = 7, maxGridSize = 8,
            minPathLength = 8, maxPathLength = 10, maxPower = 5,
            maxHazardsOnPath = 3, maxHazardsInNoise = 4,
            allowedHazards = new List<TileType> { TileType.Sand, TileType.Ice, TileType.Boost },
            segmentColor = new Color(0.5f, 0.3f, 0.7f) // Purple
        },
        // Segment 7: The Gauntlet (Levels 51 - 65)
        new AdventureSegmentConfig {
            startLevel = 51, endLevel = 65, themeName = "The Gauntlet",
            minGridSize = 8, maxGridSize = 8,
            minPathLength = 9, maxPathLength = 11, maxPower = 5,
            maxHazardsOnPath = 4, maxHazardsInNoise = 5,
            allowedHazards = new List<TileType> { TileType.Sand, TileType.Ice, TileType.Boost },
            segmentColor = new Color(0.7f, 0.2f, 0.2f) // Dark Red
        },
        // Segment 8: Master's Course (Levels 66 - 80)
        new AdventureSegmentConfig {
            startLevel = 66, endLevel = 80, themeName = "Master's Course",
            minGridSize = 8, maxGridSize = 9,
            minPathLength = 10, maxPathLength = 12, maxPower = 5,
            maxHazardsOnPath = 5, maxHazardsInNoise = 6,
            allowedHazards = new List<TileType> { TileType.Sand, TileType.Ice, TileType.Boost },
            segmentColor = new Color(0.3f, 0.1f, 0.4f) // Midnight Purple
        },
        // Segment 9: The Final Hole (Levels 81 - 100)
        new AdventureSegmentConfig {
            startLevel = 81, endLevel = 100, themeName = "The Final Hole",
            minGridSize = 9, maxGridSize = 9,
            minPathLength = 11, maxPathLength = 14, maxPower = 5,
            maxHazardsOnPath = 6, maxHazardsInNoise = 8,
            allowedHazards = new List<TileType> { TileType.Sand, TileType.Ice, TileType.Boost },
            segmentColor = new Color(0.1f, 0.1f, 0.1f) // Black/Void
        }
    };

    public static AdventureSegmentConfig GetConfigForLevel(int levelIndex)
    {
        // levelIndex is usually 1-indexed for players, but let's make sure
        // we clamp it to our defined range.
        int clampedLevel = Mathf.Clamp(levelIndex, 1, 100);

        foreach (var segment in segments)
        {
            if (clampedLevel >= segment.startLevel && clampedLevel <= segment.endLevel)
            {
                return segment;
            }
        }

        // Fallback (should never be reached if segments cover 1-100)
        return segments[segments.Count - 1];
    }
}
