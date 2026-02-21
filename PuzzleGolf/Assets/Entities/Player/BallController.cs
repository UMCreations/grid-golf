using UnityEngine;
using System.Collections;

public class BallController : MonoBehaviour
{
    [Header("State")]
    public Vector2Int currentGridPosition;
    public bool isMoving;

    [Header("Animation Settings")]
    public float moveDuration = 0.4f;
    public float jumpHeightScale = 0.3f; // How much it grows to simulate flying "up"
    public float rotationsPerMove = 2f;

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
        // Don't accept input if ball is already animating a move, or if the game is already won
        if (isMoving) return;
        if (GameManager.Instance != null && GameManager.Instance.HasWon) return;

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
            // Start the movement coroutine instead of an instant teleport
            StartCoroutine(MoveAndAnimateBall(targetTile.transform.position, targetTile, targetPosition));
            Debug.Log($"Moving to {targetPosition}");
        }
        else
        {
            // Move went out of bounds
            Debug.Log($"Invalid move: Target {targetPosition} is out of bounds!");
            if (GameManager.Instance != null)
                GameManager.Instance.OnInvalidMove();
        }
    }

    private IEnumerator MoveAndAnimateBall(Vector3 targetWorldPos, Tile targetTile, Vector2Int targetGridPos)
    {
        isMoving = true;
        
        Vector3 startPosition = transform.position;
        Vector3 baseScale = transform.localScale;
        // Depending on distance, we might want to rotate more or take longer, but for puzzle games a uniform time feels snappy
        
        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            float t = elapsed / moveDuration;
            
            // 1. Smoothly interpolate position (Ease Out)
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            transform.position = Vector3.Lerp(startPosition, targetWorldPos, smoothT);

            // 2. Simulate "Arc/Flight" in 2D by scaling it up in the middle of the journey
            // Equation of an inverted parabola from 0 to 1 back to 0: 4 * t * (1 - t)
            float arc = 4f * t * (1f - t);
            transform.localScale = baseScale + new Vector3(arc * jumpHeightScale, arc * jumpHeightScale, 0f);

            // 3. Simulate "Rolling" by rotating around the Z axis
            // We rotate smoothly over the duration
            float rotationAmount = 360f * rotationsPerMove * Time.deltaTime / moveDuration;
            transform.Rotate(Vector3.forward, -rotationAmount);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Snap to final destination exactly to be safe
        transform.position = targetWorldPos;
        transform.localScale = baseScale;
        currentGridPosition = targetGridPos;
        
        isMoving = false;

        // Step 5: Check if we landed on the hole after we finish animating
        if (targetTile.type == TileType.Hole)
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnHoleReached();
        }
    }
}
