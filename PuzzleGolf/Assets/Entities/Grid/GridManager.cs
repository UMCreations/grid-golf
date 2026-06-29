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
    public float maxAspect = 0.6f;     // Clamps aspect ratio on wider devices to maintain portrait safe area

    [Header("Theme Setup")]
    public GridTheme currentTheme;
    private SpriteRenderer bgRenderer;

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
        
        // Calculate max delay to know when to spawn the ball (based on increased stagger)
        float ballSpawnDelay = (width + height) * 0.1f + 0.8f;
        Invoke(nameof(SpawnBall), ballSpawnDelay);
        
        CenterAndFitCamera();
        UpdateBackground();
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
        
        // Clamp screenAspect to maxAspect to keep a portrait safe area on wider screens
        float effectiveAspect = Mathf.Min(screenAspect, maxAspect);
        
        // Required size based on width vs height
        float requiredSizeHeight = paddedHeight / 2f;
        float requiredSizeWidth = (paddedWidth / effectiveAspect) / 2f;

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
                // Spawn above the grid and animate it dropping in
                Vector3 targetPos = currentTile.transform.position;
                Vector3 spawnPos = targetPos + Vector3.up * 3f; 
                
                currentBall = Instantiate(ballPrefab, spawnPos, Quaternion.identity);
                currentBall.currentGridPosition = CurrentLevelData.currentGridPosition;
                
                // Trigger the characteristic jump animation to reach the start tile
                currentBall.StartCoroutine(currentBall.MoveAndAnimateBall(targetPos, currentTile, CurrentLevelData.currentGridPosition, Vector2Int.down));
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

        if (bgRenderer != null)
        {
            Destroy(bgRenderer.gameObject);
            bgRenderer = null;
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
                Sprite tileSprite = currentTheme != null ? currentTheme.GetSpriteForType(TileType.Standard, power) : null;

                // --- CLASSIC MODE: determine type purely from position ---
                if (x == startPosition.x && y == startPosition.y)
                {
                    currentType = TileType.Start;
                    tileSprite = currentTheme != null ? currentTheme.startSprite : null;
                }
                else if (x == holePosition.x && y == holePosition.y)
                {
                    currentType = TileType.Hole;
                    power = 0;
                    tileSprite = currentTheme != null ? currentTheme.holeSprite : null;
                }
                // --- ADVENTURE MODE: override type from tileTypes array ---
                else if (CurrentLevelData.gameMode == GameMode.Adventure &&
                         CurrentLevelData.tileTypes != null)
                {
                    currentType = CurrentLevelData.tileTypes[x, y];
                    tileSprite = currentTheme != null ? currentTheme.GetSpriteForType(currentType, power) : null;
                }

                spawnedTile.Init(new Vector2Int(x, y), power, currentType, tileSprite);
                
                // Calculate animation delay based on Manhattan distance from origin
                // Increased from 0.05f to 0.1f for a slower, more deliberate reveal
                float staggerDelay = (x + y) * 0.1f;
                
                // Start and Hole tiles appear a bit later for dramatic effect
                if (currentType == TileType.Start || currentType == TileType.Hole)
                {
                    staggerDelay += 0.8f;
                }
                
                spawnedTile.AnimateSpawn(staggerDelay);
                gridArray[x, y] = spawnedTile;
            }
        }
    }

    private Sprite GetSpriteForType(TileType type, int power = 0)
    {
        if (currentTheme != null) return currentTheme.GetSpriteForType(type, power);
        return null;
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

    public void ClearTutorialVisuals()
    {
        if (gridArray == null) return;
        foreach (Tile tile in gridArray)
        {
            if (tile != null)
            {
                tile.SetPulse(false);
                tile.SetPowerHighlight(false);
            }
        }
    }

    public void HighlightHoleForTutorial(bool shouldHighlight)
    {
        Tile hole = GetTileAtPosition(holePosition);
        if (hole != null)
        {
            hole.SetPulse(shouldHighlight);
        }
    }

    public void HighlightTileForTutorial(Vector2Int pos, bool shouldHighlight)
    {
        Tile tile = GetTileAtPosition(pos);
        if (tile != null)
        {
            tile.SetPulse(shouldHighlight);
        }
    }

    public void HighlightAllPowerNumbers(bool shouldHighlight)
    {
        if (gridArray == null) return;
        foreach (Tile tile in gridArray)
        {
            if (tile != null && tile.type != TileType.Hole)
            {
                tile.SetPowerHighlight(shouldHighlight);
            }
        }
    }

    private void UpdateBackground()
    {
        // Force clean up of the old background to prevent any alternating states
        if (bgRenderer != null)
        {
            Destroy(bgRenderer.gameObject);
            bgRenderer = null;
        }

        if (currentTheme == null || currentTheme.backgroundImage == null)
        {
            return;
        }

        GameObject bgObj = new GameObject("GridBackground");
        bgObj.transform.parent = transform;
        bgRenderer = bgObj.AddComponent<SpriteRenderer>();

        bgRenderer.gameObject.SetActive(true);
        bgRenderer.sprite = currentTheme.backgroundImage;
        bgRenderer.color = currentTheme.backgroundColor;
        bgRenderer.sortingOrder = -100; // Force it behind all standard tiles

        // Slicing support for nice borders
        if (currentTheme.backgroundImage.border != Vector4.zero)
        {
            bgRenderer.drawMode = SpriteDrawMode.Sliced;
        }
        else
        {
            bgRenderer.drawMode = SpriteDrawMode.Simple;
        }

        float physicalWidth = (width - 1) * (tileSize + spacing) + tileSize;
        float physicalHeight = (height - 1) * (tileSize + spacing) + tileSize;
        float centerX = ((width - 1) * (tileSize + spacing)) / 2f;
        float centerY = ((height - 1) * (tileSize + spacing)) / 2f;

        // Push Z position further back to ensure it doesn't clip tiles
        bgRenderer.transform.position = new Vector3(centerX, centerY, 5f);

        // Make the background cover the entire screen based on the Camera's orthographic size
        float targetHeight = Camera.main.orthographicSize * 2f;
        float targetWidth = targetHeight * Camera.main.aspect;
        
        // Add slight overscan padding to prevent edge bleeding
        targetWidth += 0.5f;
        targetHeight += 0.5f;

        if (bgRenderer.drawMode == SpriteDrawMode.Sliced)
        {
            bgRenderer.size = new Vector2(targetWidth, targetHeight);
            bgRenderer.transform.localScale = Vector3.one;
        }
        else
        {
            Vector2 spriteSize = currentTheme.backgroundImage.bounds.size;
            bgRenderer.transform.localScale = new Vector3(targetWidth / spriteSize.x, targetHeight / spriteSize.y, 1f);
        }
    }
}
