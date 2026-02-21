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

    [Header("Input & Trajectory")]
    public float swipeThreshold = 50f; // px distance to register a swipe
    private LineRenderer trajectoryLine;
    private Vector2 touchStartPos;
    private Vector2 touchCurrentPos;
    private bool isAiming;
    private Vector2Int currentAimDirection;

    private void Start()
    {
        // Now handled by GridManager spawning the ball at the correct start position.
        if (GridManager.Instance != null)
        {
            currentGridPosition = GridManager.Instance.startPosition;
        }

        SetupLineRenderer();
    }

    private void SetupLineRenderer()
    {
        // Create line renderer dynamically so the user doesn't have to configure it manually
        trajectoryLine = GetComponent<LineRenderer>();
        if (trajectoryLine == null)
        {
            trajectoryLine = gameObject.AddComponent<LineRenderer>();
        }
        
        trajectoryLine.enabled = false;
        trajectoryLine.startWidth = 0.15f;
        trajectoryLine.endWidth = 0.05f; // Tapering effect
        
        // Find standard sprite shader so it blends well in 2D
        Shader spriteShader = Shader.Find("Sprites/Default");
        if (spriteShader != null)
        {
            trajectoryLine.material = new Material(spriteShader);
        }
        
        trajectoryLine.sortingOrder = 5; // Draw behind the ball (which is 10)
        trajectoryLine.useWorldSpace = true;
    }

    private void Update()
    {
        // Don't accept input if ball is already animating a move, or if the game is already won
        if (isMoving) return;
        if (GameManager.Instance != null && GameManager.Instance.HasWon) return;

        HandleKeyboardInput();
        HandleTouchInput();
    }

    private void HandleKeyboardInput()
    {
        Vector2Int inputDirection = GetInputDirection();
        if (inputDirection != Vector2Int.zero && !isAiming) // Prioritize keyboard if not swiping
        {
            AttemptMove(inputDirection);
        }
    }

    private void HandleTouchInput()
    {
        // Input.GetMouseButton(0) works universally for both Mouse Clicks and Mobile Finger Touches
        if (Input.GetMouseButtonDown(0))
        {
            touchStartPos = Input.mousePosition;
            isAiming = true;
            currentAimDirection = Vector2Int.zero;
        }

        if (Input.GetMouseButton(0) && isAiming)
        {
            touchCurrentPos = Input.mousePosition;
            Vector2 swipeDelta = touchCurrentPos - touchStartPos;

            // Only register drag if we've moved our finger/mouse past a threshold (prevents accidental taps)
            if (swipeDelta.magnitude > swipeThreshold)
            {
                currentAimDirection = GetSnapDirection(swipeDelta);
                UpdateTrajectory();
            }
            else
            {
                currentAimDirection = Vector2Int.zero;
                HideTrajectory();
            }
        }

        if (Input.GetMouseButtonUp(0) && isAiming)
        {
            isAiming = false;
            HideTrajectory();
            
            // Execute move upon release if we had a valid target direction
            if (currentAimDirection != Vector2Int.zero)
            {
                AttemptMove(currentAimDirection);
            }
        }
    }

    private Vector2Int GetSnapDirection(Vector2 rawDirection)
    {
        // Calculate angle from swipe. Mathf.Atan2 returns -PI to PI
        float angle = Mathf.Atan2(rawDirection.y, rawDirection.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;

        // Snapping into 8 distinct directional slices (45 degrees each)
        // Offset by 22.5 degrees so that angles exactly along cardinal directions sit right in the middle of a slice
        angle = (angle + 22.5f) % 360f;
        int slice = Mathf.FloorToInt(angle / 45f);

        switch (slice)
        {
            case 0: return Vector2Int.right;
            case 1: return new Vector2Int(1, 1);
            case 2: return Vector2Int.up;
            case 3: return new Vector2Int(-1, 1);
            case 4: return Vector2Int.left;
            case 5: return new Vector2Int(-1, -1);
            case 6: return Vector2Int.down;
            case 7: return new Vector2Int(1, -1);
            default: return Vector2Int.zero; 
        }
    }

    private void UpdateTrajectory()
    {
        if (trajectoryLine == null) return;

        Tile currentTile = GridManager.Instance.GetTileAtPosition(currentGridPosition);
        if (currentTile == null || currentTile.type == TileType.Hole) 
        {
            HideTrajectory();
            return;
        }

        int power = currentTile.powerCount;
        if (power <= 0)
        {
            HideTrajectory();
            return;
        }

        Vector2Int targetPosition = currentGridPosition + (currentAimDirection * power);
        Tile targetTile = GridManager.Instance.GetTileAtPosition(targetPosition);

        trajectoryLine.enabled = true;
        trajectoryLine.positionCount = 2;
        trajectoryLine.SetPosition(0, transform.position);

        if (targetTile != null)
        {
            // Valid move visual: Green line pointing directly to the center of the target tile
            trajectoryLine.SetPosition(1, targetTile.transform.position);
            trajectoryLine.startColor = new Color(0.2f, 1f, 0.2f, 0.8f); // Solid green
            trajectoryLine.endColor = new Color(0.2f, 1f, 0.2f, 0.2f);   // Faded green
        }
        else
        {
            // Invalid/Out Of Bounds move visual: Red line
            float tileSize = GridManager.Instance.tileSize;
            float spacing = GridManager.Instance.spacing;
            Vector3 direction3D = new Vector3(currentAimDirection.x * (tileSize + spacing), currentAimDirection.y * (tileSize + spacing), 0);
            
            trajectoryLine.SetPosition(1, transform.position + (direction3D * power));
            trajectoryLine.startColor = new Color(1f, 0.2f, 0.2f, 0.8f); // Solid red
            trajectoryLine.endColor = new Color(1f, 0.2f, 0.2f, 0.2f);   // Faded red
        }
    }

    private void HideTrajectory()
    {
        if (trajectoryLine != null)
            trajectoryLine.enabled = false;
    }

    private Vector2Int GetInputDirection()
    {
        // Keep keyboard logic for PC testing
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) return Vector2Int.up;
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) return Vector2Int.down;
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) return Vector2Int.left;
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) return Vector2Int.right;

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
            StartCoroutine(MoveAndAnimateBall(targetTile.transform.position, targetTile, targetPosition));
            Debug.Log($"Moving to {targetPosition}");
        }
        else
        {
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
        
        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            float t = elapsed / moveDuration;
            
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            transform.position = Vector3.Lerp(startPosition, targetWorldPos, smoothT);

            float arc = 4f * t * (1f - t);
            transform.localScale = baseScale + new Vector3(arc * jumpHeightScale, arc * jumpHeightScale, 0f);

            float rotationAmount = 360f * rotationsPerMove * Time.deltaTime / moveDuration;
            transform.Rotate(Vector3.forward, -rotationAmount);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = targetWorldPos;
        transform.localScale = baseScale;
        currentGridPosition = targetGridPos;
        
        isMoving = false;

        if (targetTile.type == TileType.Hole)
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnHoleReached();
        }
    }
}
