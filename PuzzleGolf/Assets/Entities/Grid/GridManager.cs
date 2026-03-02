using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Grid Generation Settings")]
    public float tileSize = 1.0f;
    public float spacing = 0.1f;
    public Tile tilePrefab;
    public Transform gridParent;

    [Header("Camera Settings")]
    public float screenPadding = 1.0f; // Extra space around the grid

    [Header("Tile Sprites")]
    public Sprite standardSprite;
    public Sprite startSprite;
    public Sprite holeSprite;

    [Header("Adventure Tile Sprites")]
    public Sprite iceSprite;
    public Sprite sandSprite;
    public Sprite boostSprite;

    [Header("Level Integration")]
    public Difficulty currentDifficulty = Difficulty.Easy;
    public LevelData CurrentLevelData { get; private set; }

    // Data populated by LevelGenerator
    public int width { get; private set; }
    public int height { get; private set; }
    public Vector2Int startPosition { get; private set; }
    public Vector2Int holePosition { get; private set; }
    public int levelPar { get; private set; }

    [Header("Player")]
    public BallController ballPrefab;
    private BallController currentBall;

    private Tile[,] gridArray;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Do not automatically generate level on scene load. Wait for InitializeGame to be called.
    }

    public void InitializeGame()
    {
        Difficulty targetDiff = currentDifficulty;
        int targetLevel = 1;

        if (LevelManager.Instance != null)
        {
            targetDiff = LevelManager.Instance.SelectedDifficulty;
            targetLevel = LevelManager.Instance.SelectedLevelIndex;

            // FTUE Logic: Force tutorial if not completed OR if manually requested
            if (!LevelManager.Instance.CurrentProfile.hasCompletedTutorial || LevelManager.Instance.IsTutorialMode)
            {
                Debug.Log($"FTUE Logic: Tutorial triggered (Mandatory: {!LevelManager.Instance.CurrentProfile.hasCompletedTutorial}, Manual: {LevelManager.Instance.IsTutorialMode}). Generating 3x3 board.");
                GenerateAndLoadNewLevel(Difficulty.Easy, 0, true);
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowTutorial();
                }
                return;
            }
        }

        LevelData savedLevel = SaveManager.LoadLevel();
        GameMode targetMode = LevelManager.Instance != null
            ? LevelManager.Instance.SelectedGameMode
            : GameMode.Classic;

        // Only load the save if it perfectly matches difficulty, level index AND game mode
        bool saveMatches = savedLevel != null &&
                           savedLevel.difficulty == targetDiff &&
                           savedLevel.levelIndex == targetLevel &&
                           savedLevel.gameMode == targetMode;

        if (saveMatches)
        {
            Debug.Log($"Loading saved {targetMode} level {targetDiff} - {targetLevel}...");
            LoadExistingLevel(savedLevel);
        }
        else
        {
            Debug.Log($"Generating new {targetMode} level {targetDiff} - {targetLevel}...");
            GenerateAndLoadNewLevel(targetDiff, targetLevel);
        }
    }

    public void LoadExistingLevel(LevelData levelData)
    {
        CurrentLevelData = levelData;
        currentDifficulty = levelData.difficulty;
        
        width = CurrentLevelData.width;
        height = CurrentLevelData.height;
        startPosition = CurrentLevelData.startPosition;
        holePosition = CurrentLevelData.holePosition;
        levelPar = CurrentLevelData.levelPar;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.InitializeLevel(levelPar, CurrentLevelData.currentStrokes);
        }

        GenerateGrid();
        SpawnBall();
        CenterAndFitCamera();
    }

    public void GenerateAndLoadNewLevel(Difficulty difficulty, int levelIndex, bool isTutorial = false)
    {
        if (LevelGenerator.Instance == null)
        {
            Debug.LogError("No LevelGenerator found in scene! Cannot generate level.");
            return;
        }

        // Make sure the generator is using the correct strategy for the current game mode
        if (LevelManager.Instance != null)
            LevelGenerator.Instance.SetGameMode(LevelManager.Instance.SelectedGameMode);

        LevelData newLevelData = LevelGenerator.Instance.GenerateLevel(difficulty, levelIndex, isTutorial);
        
        // Save it so that reloading the scene restarts this EXACT level pattern
        SaveManager.SaveLevel(newLevelData);

        LoadExistingLevel(newLevelData);
    }

    public void ResetLevelState()
    {
        if (CurrentLevelData != null)
        {
            CurrentLevelData.currentStrokes = 0;
            CurrentLevelData.currentGridPosition = CurrentLevelData.startPosition;
            SaveManager.SaveLevel(CurrentLevelData);
        }
    }

    public void SaveGameState(Vector2Int newBallPos, int strokes)
    {
        if (CurrentLevelData != null)
        {
            CurrentLevelData.currentGridPosition = newBallPos;
            CurrentLevelData.currentStrokes = strokes;
            SaveManager.SaveLevel(CurrentLevelData);
        }
    }

    private void CenterAndFitCamera()
    {
        if (Camera.main == null) return;
        
        // 1. Calculate the physical dimensions of the grid
        float physicalWidth = (width - 1) * (tileSize + spacing) + tileSize;
        float physicalHeight = (height - 1) * (tileSize + spacing) + tileSize;

        // 2. Find the exact center point of the grid
        float centerX = ((width - 1) * (tileSize + spacing)) / 2f;
        float centerY = ((height - 1) * (tileSize + spacing)) / 2f;
        
        // 3. Move the camera to the center
        Camera.main.transform.position = new Vector3(centerX, centerY, -10f);

        // 4. Calculate required orthographic sizes (half sizes) with padding
        float paddedWidth = physicalWidth + screenPadding;
        float paddedHeight = physicalHeight + screenPadding;

        // 5. aspect ratio = width / height
        float screenAspect = (float)Screen.width / (float)Screen.height;
        
        // Required size based on width vs height
        float requiredSizeHeight = paddedHeight / 2f;
        float requiredSizeWidth = (paddedWidth / screenAspect) / 2f;

        // The camera should take the larger requirement to ensure everything fits
        Camera.main.orthographicSize = Mathf.Max(requiredSizeHeight, requiredSizeWidth);
        
        Debug.Log("Camera perfectly fitted to the grid!");
    }

    public void SpawnBall()
    {
        if (currentBall != null)
        {
            Destroy(currentBall.gameObject);
        }

        if (ballPrefab != null)
        {
            Tile currentTile = GetTileAtPosition(CurrentLevelData.currentGridPosition);
            if (currentTile != null)
            {
                currentBall = Instantiate(ballPrefab, currentTile.transform.position, Quaternion.identity);
                currentBall.currentGridPosition = CurrentLevelData.currentGridPosition;
            }
        }
        else
        {
            Debug.LogWarning("Ball Prefab is not assigned in the GridManager!");
        }
    }

    public void ClearGrid()
    {
        if (gridParent != null)
        {
            foreach (Transform child in gridParent)
            {
                Destroy(child.gameObject);
            }
        }
        if (currentBall != null)
        {
            Destroy(currentBall.gameObject);
        }
    }

    public void GenerateGrid()
    {
        gridArray = new Tile[width, height];
        
        // Ensure we have a parent to organize tiles
        if (gridParent == null) 
        {
            GameObject parentObj = new GameObject("GridParent");
            parentObj.transform.parent = transform;
            gridParent = parentObj.transform;
        }
        else
        {
            // Clear existing grid if any
            foreach (Transform child in gridParent)
            {
                Destroy(child.gameObject);
            }
        }

        // Generate grid
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 worldPosition = new Vector3(x * (tileSize + spacing), y * (tileSize + spacing), 0);
                
                Tile spawnedTile = Instantiate(tilePrefab, worldPosition, Quaternion.identity, gridParent);
                spawnedTile.name = $"Tile {x},{y}";
                
                int power = CurrentLevelData.tilePowers[x, y];
                TileType currentType = TileType.Standard;
                Sprite tileSprite = standardSprite;

                // --- CLASSIC MODE: determine type purely from position ---
                if (x == startPosition.x && y == startPosition.y)
                {
                    currentType = TileType.Start;
                    tileSprite = startSprite;
                }
                else if (x == holePosition.x && y == holePosition.y)
                {
                    currentType = TileType.Hole;
                    power = 0;
                    tileSprite = holeSprite;
                }
                // --- ADVENTURE MODE: override type from tileTypes array ---
                else if (CurrentLevelData.gameMode == GameMode.Adventure &&
                         CurrentLevelData.tileTypes != null)
                {
                    currentType = CurrentLevelData.tileTypes[x, y];
                    tileSprite = GetSpriteForType(currentType);
                }

                spawnedTile.Init(new Vector2Int(x, y), power, currentType, tileSprite);
                gridArray[x, y] = spawnedTile;
            }
        }
    }

    private Sprite GetSpriteForType(TileType type)
    {
        switch (type)
        {
            case TileType.Start:  return startSprite;
            case TileType.Hole:   return holeSprite;
            case TileType.Ice:    return iceSprite   != null ? iceSprite   : standardSprite;
            case TileType.Sand:   return sandSprite  != null ? sandSprite  : standardSprite;
            case TileType.Boost:  return boostSprite != null ? boostSprite : standardSprite;
            default:              return standardSprite;
        }
    }

    public Tile GetTileAtPosition(Vector2Int gridPosition)
    {
        if (gridPosition.x >= 0 && gridPosition.x < width && 
            gridPosition.y >= 0 && gridPosition.y < height)
        {
            return gridArray[gridPosition.x, gridPosition.y];
        }
        
        // Out of bounds
        return null;
    }

    public void HighlightValidDestinations(Vector2Int sourcePos, int power)
    {
        ClearAllHighlights();

        if (power <= 0) return;

        Vector2Int[] directions = {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
            new Vector2Int(1, 1), new Vector2Int(-1, 1), new Vector2Int(1, -1), new Vector2Int(-1, -1)
        };

        foreach (var dir in directions)
        {
            Vector2Int targetPos = sourcePos + (dir * power);
            Tile targetTile = GetTileAtPosition(targetPos);
            if (targetTile != null)
            {
                targetTile.SetHighlight(true);
            }
        }
    }

    public void ClearAllHighlights()
    {
        if (gridArray == null) return;
        foreach (Tile tile in gridArray)
        {
            if (tile != null)
            {
                tile.SetHighlight(false);
            }
        }
    }

    public bool HasValidMoves(Vector2Int sourcePos, int power)
    {
        if (power <= 0) return false;

        Vector2Int[] directions = {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
            new Vector2Int(1, 1), new Vector2Int(-1, 1), new Vector2Int(1, -1), new Vector2Int(-1, -1)
        };

        foreach (var dir in directions)
        {
            Vector2Int targetPos = sourcePos + (dir * power);
            Tile targetTile = GetTileAtPosition(targetPos);
            if (targetTile != null)
            {
                return true;
            }
        }
        
        return false;
    }
}
