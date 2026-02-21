using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public bool HasWon { get; private set; } = false;
    public bool HasLost { get; private set; } = false;

    public int MaxStrokes { get; private set; } = 5;
    public int CurrentStrokes { get; private set; } = 0;

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

    public void InitializeLevel(int maxStrokes)
    {
        MaxStrokes = maxStrokes;
        CurrentStrokes = 0;
        HasWon = false;
        HasLost = false;
    }

    public void OnMoveMade()
    {
        if (HasWon || HasLost) return;
        
        CurrentStrokes++;
        Debug.Log($"Stroke used! Current: {CurrentStrokes} / {MaxStrokes}");
    }

    public void CheckStrokeLimit()
    {
        if (HasWon || HasLost) return;

        if (CurrentStrokes >= MaxStrokes)
        {
            HasLost = true;
            Debug.Log("💀 Out of strokes! Game Over! 💀");
            Debug.Log("Press 'R' to restart the level.");
        }
    }

    public void OnHoleReached()
    {
        if (HasWon || HasLost) return; // Prevent multiple triggers
        
        HasWon = true;
        Debug.Log($"🎉 You Won! Hole Reached in {CurrentStrokes} strokes! 🎉");
        Debug.Log("Press 'R' to restart the level.");
    }

    public void OnInvalidMove()
    {
        if (HasWon) return;

        Debug.Log("❌ Invalid Move! (e.g., attempt to move off grid)");
        
        // For MVP, we simply log it and prevent the move.
        // In a stricter puzzle setting, this could trigger a Game Over.
    }

    public void RestartLevel()
    {
        // Simple scene reload for MVP puzzle reset
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void Update()
    {
        // Quick restart input for testing
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartLevel();
        }
    }
}
