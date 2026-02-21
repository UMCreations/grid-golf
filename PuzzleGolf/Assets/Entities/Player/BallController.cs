using UnityEngine;

public class BallController : MonoBehaviour
{
    [Header("State")]
    public Vector2Int currentGridPosition;
    public bool isMoving;

    private void Start()
    {
        // Now handled by GridManager spawning the ball at the correct start position.
        if (GridManager.Instance != null)
        {
            currentGridPosition = GridManager.Instance.startPosition;
        }
    }

    private void Update()
    {
        // Don't accept input if ball is already animating a move
        if (isMoving) return;

        Vector2Int inputDirection = GetInputDirection();
        if (inputDirection != Vector2Int.zero)
        {
            AttemptMove(inputDirection);
        }
    }

    private Vector2Int GetInputDirection()
    {
        // 4-way Standard Input
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) return Vector2Int.up;
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) return Vector2Int.down;
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) return Vector2Int.left;
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) return Vector2Int.right;

        // 8-way Diagonal Input via Numpad
        if (Input.GetKeyDown(KeyCode.Keypad8)) return Vector2Int.up;
        if (Input.GetKeyDown(KeyCode.Keypad2)) return Vector2Int.down;
        if (Input.GetKeyDown(KeyCode.Keypad4)) return Vector2Int.left;
        if (Input.GetKeyDown(KeyCode.Keypad6)) return Vector2Int.right;
        
        if (Input.GetKeyDown(KeyCode.Keypad7)) return new Vector2Int(-1, 1);
        if (Input.GetKeyDown(KeyCode.Keypad9)) return new Vector2Int(1, 1);
        if (Input.GetKeyDown(KeyCode.Keypad1)) return new Vector2Int(-1, -1);
        if (Input.GetKeyDown(KeyCode.Keypad3)) return new Vector2Int(1, -1);

        return Vector2Int.zero;
    }

    private void AttemptMove(Vector2Int direction)
    {
        Tile currentTile = GridManager.Instance.GetTileAtPosition(currentGridPosition);
        if (currentTile == null) return;

        // In a real game, if we are on the hole, we've won, so we don't move.
        if (currentTile.type == TileType.Hole) return;

        int power = currentTile.powerCount;
        if (power <= 0) 
        {
            Debug.Log("Invalid move: Current tile has 0 power.");
            return;
        }

        Vector2Int targetPosition = currentGridPosition + (direction * power);
        Tile targetTile = GridManager.Instance.GetTileAtPosition(targetPosition);

        if (targetTile != null)
        {
            // Valid move on the grid
            currentGridPosition = targetPosition;
            
            // For Step 4: Teleport instantly
            transform.position = targetTile.transform.position;
            
            Debug.Log($"Moved to {targetPosition}");
        }
        else
        {
            // Move went out of bounds
            Debug.Log($"Invalid move: Target {targetPosition} is out of bounds!");
        }
    }
}
