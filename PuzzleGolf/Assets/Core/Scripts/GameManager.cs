using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public bool HasWon { get; private set; } = false;

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

    public void OnHoleReached()
    {
        if (HasWon) return; // Prevent multiple triggers
        
        HasWon = true;
        Debug.Log("🎉 You Won! Hole Reached! 🎉");
        Debug.Log("Press 'R' to restart the level.");
        
        // In the future, we will trigger the Win UI here and show the total strokes
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
