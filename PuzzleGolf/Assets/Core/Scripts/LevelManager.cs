using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class PlayerProfile
{
    // High score/progress for each difficulty
    // Unlocked up to Level N (1-indexed)
    public int unlockedEasy = 1;
    public int unlockedMedium = 1;
    public int unlockedHard = 1;

    // Stars earned for each level (0-indexed array, so index 0 = Level 1)
    public int[] easyStars = new int[100];
    public int[] mediumStars = new int[100];
    public int[] hardStars = new int[100];

    // What the player was last playing, to allow a generic "Play" button resume
    public Difficulty lastPlayedDifficulty = Difficulty.Easy;
    public int lastPlayedLevelIndex = 1;

    // Settings
    public bool soundEffectsEnabled = true;
    public bool musicEnabled = true;
    public bool vibrationEnabled = true;

    // FTUE Training
    public bool hasCompletedTutorial = false;

    // DDA (Dynamic Difficulty Adjustment)
    public int consecutiveFailures = 0;
    public int consecutivePerfects = 0;
}

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    public PlayerProfile CurrentProfile { get; private set; }
    
    public const int MAX_LEVELS = 100;

    // These state variables control what happens when a game scene is loaded
    public Difficulty SelectedDifficulty { get; private set; } = Difficulty.Easy;
    public int SelectedLevelIndex { get; private set; } = 1;
    public GameMode SelectedGameMode { get; private set; } = GameMode.Classic;
    public bool IsTutorialMode { get; set; } = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null); // Ensure it's a root object for DontDestroyOnLoad
            DontDestroyOnLoad(gameObject);
            LoadProfile();
            ResumeLastPlayedLevel(); // Automatically load saved level progress on start!
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadProfile()
    {
        string json = PlayerPrefs.GetString("PlayerProfile", "");
        if (!string.IsNullOrEmpty(json))
        {
            CurrentProfile = JsonUtility.FromJson<PlayerProfile>(json);
        }
        else
        {
            CurrentProfile = new PlayerProfile();
        }
    }

    public void SaveProfile()
    {
        if (CurrentProfile != null)
        {
            string json = JsonUtility.ToJson(CurrentProfile);
            PlayerPrefs.SetString("PlayerProfile", json);
            PlayerPrefs.Save();
        }
    }

    // Called from Main Menu when selecting a specific level to play
    public void SetLevelToPlay(Difficulty diff, int levelIndex, GameMode mode = GameMode.Classic)
    {
        SelectedDifficulty = diff;
        SelectedLevelIndex = levelIndex;
        SelectedGameMode = mode;
        
        // Tell the generator which strategy to use
        if (LevelGenerator.Instance != null)
            LevelGenerator.Instance.SetGameMode(mode);

        // Update "last played" feature
        CurrentProfile.lastPlayedDifficulty = diff;
        CurrentProfile.lastPlayedLevelIndex = levelIndex;
        SaveProfile();
    }

    // Called from Main Menu "Play" quick button to resume from highest unlocked
    public void ResumeLastPlayedLevel()
    {
        SelectedDifficulty = CurrentProfile.lastPlayedDifficulty;
        SelectedLevelIndex = CurrentProfile.lastPlayedLevelIndex;
    }

    public void ResetProgress()
    {
        CurrentProfile = new PlayerProfile();
        SaveProfile();
        SaveManager.ClearSave(); // Clear mid-level save to make sure board is wiped
    }

    // Helper for progression limits
    public int GetUnlockedLevelCount(Difficulty diff)
    {
        if (CurrentProfile == null) return 1;
        switch (diff)
        {
            case Difficulty.Easy: return CurrentProfile.unlockedEasy;
            case Difficulty.Medium: return CurrentProfile.unlockedMedium;
            case Difficulty.Hard: return CurrentProfile.unlockedHard;
            default: return 1;
        }
    }

    public int GetStars(Difficulty diff, int levelIndex)
    {
        if (CurrentProfile == null || levelIndex < 1 || levelIndex > MAX_LEVELS) return 0;
        int arrIndex = levelIndex - 1; // 1-indexed to 0-indexed array
        switch (diff)
        {
            case Difficulty.Easy: return CurrentProfile.easyStars[arrIndex];
            case Difficulty.Medium: return CurrentProfile.mediumStars[arrIndex];
            case Difficulty.Hard: return CurrentProfile.hardStars[arrIndex];
            default: return 0;
        }
    }

    public void SetStars(Difficulty diff, int levelIndex, int stars)
    {
        if (CurrentProfile == null || levelIndex < 1 || levelIndex > MAX_LEVELS) return;
        int arrIndex = levelIndex - 1;
        switch (diff)
        {
            case Difficulty.Easy: CurrentProfile.easyStars[arrIndex] = Mathf.Max(CurrentProfile.easyStars[arrIndex], stars); break;
            case Difficulty.Medium: CurrentProfile.mediumStars[arrIndex] = Mathf.Max(CurrentProfile.mediumStars[arrIndex], stars); break;
            case Difficulty.Hard: CurrentProfile.hardStars[arrIndex] = Mathf.Max(CurrentProfile.hardStars[arrIndex], stars); break;
        }
    }

    // Called by GameManager when a level is beaten
    public void CompleteCurrentLevel()
    {
        bool unlockedNew = false;
        if (SelectedDifficulty == Difficulty.Easy && SelectedLevelIndex == CurrentProfile.unlockedEasy && SelectedLevelIndex < MAX_LEVELS)
        {
            CurrentProfile.unlockedEasy++;
            unlockedNew = true;
        }
        else if (SelectedDifficulty == Difficulty.Medium && SelectedLevelIndex == CurrentProfile.unlockedMedium && SelectedLevelIndex < MAX_LEVELS)
        {
            CurrentProfile.unlockedMedium++;
            unlockedNew = true;
        }
        else if (SelectedDifficulty == Difficulty.Hard && SelectedLevelIndex == CurrentProfile.unlockedHard && SelectedLevelIndex < MAX_LEVELS)
        {
            CurrentProfile.unlockedHard++;
            unlockedNew = true;
        }

        if (unlockedNew)
        {
            SaveProfile();
        }
    }

    // Calculate next level
    public bool HasNextLevel()
    {
        return SelectedLevelIndex < MAX_LEVELS;
    }

    public void ProgressToNextLevel()
    {
        if (HasNextLevel())
        {
            SetLevelToPlay(SelectedDifficulty, SelectedLevelIndex + 1);
        }
    }
}
