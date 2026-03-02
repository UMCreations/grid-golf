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
        
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayStrokeSound(CurrentStrokes);
        }
        
        OnStrokeMadeEvent?.Invoke(CurrentStrokes, MaxStrokes);
    }

    public void TriggerGameOver(string reason)
    {
        if (HasWon || HasLost) return;

        HasLost = true;
        Debug.Log($"💀 {reason}! Game Over! 💀");
        
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayLoseSound();
        }

        Debug.Log("Press 'R' to restart the level.");
        
        // Clear mid-level save so we don't resume into a game-over state
        SaveManager.ClearSave();
        
        if (LevelManager.Instance != null && LevelManager.Instance.CurrentProfile != null)
        {
            LevelManager.Instance.CurrentProfile.consecutiveFailures++;
            LevelManager.Instance.CurrentProfile.consecutivePerfects = 0;
            LevelManager.Instance.SaveProfile();
        }

        OnGameLostEvent?.Invoke();
    }

    public void CheckStrokeLimit()
    {
        if (HasWon || HasLost) return;

        if (CurrentStrokes >= MaxStrokes)
        {
            TriggerGameOver("Out of strokes");
        }
    }

    public void OnHoleReached()
    {
        if (HasWon || HasLost) return;
        
        HasWon = true;
        Debug.Log($"🎉 You Won! Hole Reached in {CurrentStrokes} strokes! 🎉");
        
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayWinSound();

        SaveManager.ClearSave();

        bool isAdventure = LevelManager.Instance != null &&
                           LevelManager.Instance.SelectedGameMode == GameMode.Adventure;

        if (LevelManager.Instance != null && LevelManager.Instance.CurrentProfile != null)
        {
            LevelManager.Instance.CurrentProfile.consecutiveFailures = 0;

            if (isAdventure)
            {
                // Adventure Mode: advance to the next Adventure level
                LevelManager.Instance.CompleteCurrentAdventureLevel();
            }
            else
            {
                // Classic Mode: DDA tracking + unlock progression
                if (CurrentStrokes < MaxStrokes)
                    LevelManager.Instance.CurrentProfile.consecutivePerfects++;
                else
                    LevelManager.Instance.CurrentProfile.consecutivePerfects = 0;

                LevelManager.Instance.CompleteCurrentLevel();
            }
        }
        
        OnGameWonEvent?.Invoke();
    }

    public void OnInvalidMove()
    {
        if (HasWon) return;

        Debug.Log("❌ Invalid Move! (e.g., attempt to move off grid)");
        
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayInvalidMove();
        }

        // For MVP, we simply log it and prevent the move.
        // In a stricter puzzle setting, this could trigger a Game Over.
    }

    public void RestartLevel()
    {
        bool isAdventure = LevelManager.Instance != null &&
                           LevelManager.Instance.SelectedGameMode == GameMode.Adventure;

        if (isAdventure)
        {
            // Adventure restart: clear save and regenerate a FRESH board for same level number
            SaveManager.ClearSave();
            if (GridManager.Instance != null)
                GridManager.Instance.GenerateAndLoadNewLevel(
                    LevelManager.Instance.SelectedDifficulty,
                    LevelManager.Instance.AdventureLevelIndex
                );
        }
        else
        {
            // Classic restart: reset strokes and reload the exact same board
            if (GridManager.Instance != null)
            {
                GridManager.Instance.ResetLevelState();
                GridManager.Instance.InitializeGame();
            }
        }

        if (UIManager.Instance != null)
            UIManager.Instance.ShowGameplayHUD();
    }

    public void LoadNextLevel()
    {
        bool isAdventure = LevelManager.Instance != null &&
                           LevelManager.Instance.SelectedGameMode == GameMode.Adventure;

        if (isAdventure)
        {
            if (LevelManager.Instance.HasNextAdventureLevel())
            {
                // Adventure: always generate a completely fresh new board
                SaveManager.ClearSave();
                if (GridManager.Instance != null)
                    GridManager.Instance.GenerateAndLoadNewLevel(
                        LevelManager.Instance.SelectedDifficulty,
                        LevelManager.Instance.AdventureLevelIndex
                    );
                if (UIManager.Instance != null)
                    UIManager.Instance.ShowGameplayHUD();
            }
            else
            {
                Debug.Log("Adventure Complete! All 100 levels beaten!");
                SaveManager.ClearSave();
                if (GridManager.Instance != null) GridManager.Instance.ClearGrid();
                if (UIManager.Instance != null) UIManager.Instance.ShowMainMenu();
            }
        }
        else
        {
            // Classic mode
            if (LevelManager.Instance != null && LevelManager.Instance.HasNextLevel())
            {
                LevelManager.Instance.ProgressToNextLevel();
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
