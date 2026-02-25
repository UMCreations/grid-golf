using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public bool HasWon { get; private set; } = false;
    public bool HasLost { get; private set; } = false;

    public int MaxStrokes { get; private set; } = 5;
    public int CurrentStrokes { get; private set; } = 0;

    // UI Events
    public event Action<int, int> OnStrokeMadeEvent; // Current, Max
    public event Action OnGameWonEvent;
    public event Action OnGameLostEvent;

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

    public void InitializeLevel(int maxStrokes, int currentStrokes = 0)
    {
        MaxStrokes = maxStrokes;
        CurrentStrokes = currentStrokes;
        HasWon = false;
        HasLost = false;
        
        // Broadcast initial state
        OnStrokeMadeEvent?.Invoke(CurrentStrokes, MaxStrokes);
    }

    public void OnMoveMade()
    {
        if (HasWon || HasLost) return;
        
        CurrentStrokes++;
        Debug.Log($"Stroke used! Current: {CurrentStrokes} / {MaxStrokes}");
        
        OnStrokeMadeEvent?.Invoke(CurrentStrokes, MaxStrokes);
    }

    public void CheckStrokeLimit()
    {
        if (HasWon || HasLost) return;

        if (CurrentStrokes >= MaxStrokes)
        {
            HasLost = true;
            Debug.Log("💀 Out of strokes! Game Over! 💀");
            Debug.Log("Press 'R' to restart the level.");
            
            OnGameLostEvent?.Invoke();
        }
    }

    public void OnHoleReached()
    {
        if (HasWon || HasLost) return; // Prevent multiple triggers
        
        HasWon = true;
        Debug.Log($"🎉 You Won! Hole Reached in {CurrentStrokes} strokes! 🎉");
        Debug.Log("Press 'R' to restart the level.");

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.CompleteCurrentLevel();
        }
        
        OnGameWonEvent?.Invoke();
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
        if (GridManager.Instance != null)
        {
            GridManager.Instance.ResetLevelState();
            GridManager.Instance.InitializeGame(); // Instead of Scene reload
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowGameplayHUD();
        }
    }

    public void LoadNextLevel()
    {
        if (LevelManager.Instance != null && LevelManager.Instance.HasNextLevel())
        {
            // Update LevelManager to point to the next level index
            LevelManager.Instance.ProgressToNextLevel();
            
            // Clear mid-level save so GridManager creates the NEW level
            SaveManager.ClearSave();
            
            if (GridManager.Instance != null) GridManager.Instance.InitializeGame();
            if (UIManager.Instance != null) UIManager.Instance.ShowGameplayHUD();
        }
        else
        {
            Debug.Log("You beat the final level/Difficulty! Returning to menu.");
            SaveManager.ClearSave();

            if (GridManager.Instance != null) GridManager.Instance.ClearGrid();
            if (UIManager.Instance != null) UIManager.Instance.ShowMainMenu();
        }
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
