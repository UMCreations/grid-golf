using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Grid Generation Settings")]
    public int width = 5;
    public int height = 5;
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

    [Header("Level Data (Temporary MVP)")]
    public Vector2Int startPosition = new Vector2Int(0, 0);
    public Vector2Int holePosition = new Vector2Int(4, 4);
    public int levelPar = 5; // Max allowed strokes

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
        if (GameManager.Instance != null)
        {
            GameManager.Instance.InitializeLevel(levelPar);
        }

        GenerateGrid();
        SpawnBall();
        CenterAndFitCamera();
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
        if (ballPrefab != null)
        {
            Tile startTile = GetTileAtPosition(startPosition);
            if (startTile != null)
            {
                currentBall = Instantiate(ballPrefab, startTile.transform.position, Quaternion.identity);
            }
        }
        else
        {
            Debug.LogWarning("Ball Prefab is not assigned in the GridManager!");
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
                // Calculate position based on tile size and spacing
                Vector3 worldPosition = new Vector3(x * (tileSize + spacing), y * (tileSize + spacing), 0);
                
                // Spawn tile
                Tile spawnedTile = Instantiate(tilePrefab, worldPosition, Quaternion.identity, gridParent);
                spawnedTile.name = $"Tile {x},{y}";
                
                // Determine type and power
                TileType currentType = TileType.Standard;
                int power = Random.Range(1, 4);
                Sprite tileSprite = standardSprite;

                if (x == startPosition.x && y == startPosition.y)
                {
                    currentType = TileType.Start;
                    power = 2; // For now, give the start tile a default power so the ball can move off it
                    tileSprite = startSprite;
                }
                else if (x == holePosition.x && y == holePosition.y)
                {
                    currentType = TileType.Hole;
                    power = 0; // The hole doesn't need to push the ball anywhere
                    tileSprite = holeSprite;
                }

                spawnedTile.Init(new Vector2Int(x, y), power, currentType, tileSprite);

                gridArray[x, y] = spawnedTile;
            }
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
}
