using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class PlayerProfile
{
    // High score/progress for each difficulty
    public int unlockedEasy = 1;
    public int unlockedMedium = 1;
    public int unlockedHard = 1;

    public int[] easyStars = new int[100];
    public int[] mediumStars = new int[100];
    public int[] hardStars = new int[100];

    public Difficulty lastPlayedDifficulty = Difficulty.Easy;
    public int lastPlayedLevelIndex = 1;

    // Settings
    public bool soundEffectsEnabled = true;
    public bool musicEnabled = true;
    public bool vibrationEnabled = true;

    // FTUE Training
    public bool hasCompletedTutorial = false;

    // DDA
    public int consecutiveFailures = 0;
    public int consecutivePerfects = 0;

    // --- Adventure Mode: Separate 100-level progression ---
    public int adventureUnlocked = 1; // Highest unlocked Adventure level
    public int[] adventureStars = new int[100];
    public int lastAdventureLevelIndex = 1;
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

    [Header("Handcrafted Content")]
    public List<HandcraftedLevelSO> handcraftedAdventureLevels;

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
        bool profileChanged = false;

        // 1. Advance within the current difficulty
        if (SelectedDifficulty == Difficulty.Easy && SelectedLevelIndex == CurrentProfile.unlockedEasy && SelectedLevelIndex < MAX_LEVELS)
        {
            CurrentProfile.unlockedEasy++;
            profileChanged = true;
        }
        else if (SelectedDifficulty == Difficulty.Medium && SelectedLevelIndex == CurrentProfile.unlockedMedium && SelectedLevelIndex < MAX_LEVELS)
        {
            CurrentProfile.unlockedMedium++;
            profileChanged = true;
        }
        else if (SelectedDifficulty == Difficulty.Hard && SelectedLevelIndex == CurrentProfile.unlockedHard && SelectedLevelIndex < MAX_LEVELS)
        {
            CurrentProfile.unlockedHard++;
            profileChanged = true;
        }

        // 2. Unlock NEXT difficulty if the last level of current difficulty was beaten
        if (SelectedLevelIndex == MAX_LEVELS)
        {
            if (SelectedDifficulty == Difficulty.Easy)
            {
                // Ensure Medium level 1 is unlocked
                if (CurrentProfile.unlockedMedium < 1) 
                {
                    CurrentProfile.unlockedMedium = 1;
                    profileChanged = true;
                }
            }
            else if (SelectedDifficulty == Difficulty.Medium)
            {
                // Ensure Hard level 1 is unlocked
                if (CurrentProfile.unlockedHard < 1)
                {
                    CurrentProfile.unlockedHard = 1;
                    profileChanged = true;
                }
            }
        }

        if (profileChanged)
        {
            SaveProfile();
        }
    }

    // Calculate next level (Classic)
    public bool HasNextLevel()
    {
        // Still have levels in current difficulty
        if (SelectedLevelIndex < MAX_LEVELS) return true;

        // At the end of current difficulty, check if we can move to next difficulty
        if (SelectedDifficulty == Difficulty.Easy) return true; // Can move to Medium 1
        if (SelectedDifficulty == Difficulty.Medium) return true; // Can move to Hard 1

        return false; // Hard level 100 is the absolute end
    }

    public void ProgressToNextLevel()
    {
        if (SelectedLevelIndex < MAX_LEVELS)
        {
            SetLevelToPlay(SelectedDifficulty, SelectedLevelIndex + 1);
        }
        else
        {
            // At the end of the difficulty, switch to the next one
            if (SelectedDifficulty == Difficulty.Easy)
            {
                SetLevelToPlay(Difficulty.Medium, 1);
            }
            else if (SelectedDifficulty == Difficulty.Medium)
            {
                SetLevelToPlay(Difficulty.Hard, 1);
            }
        }
    }

    // -------------------------------------------------------
    // ADVENTURE MODE: separate 100-level progression
    // -------------------------------------------------------
    public int AdventureLevelIndex { get; private set; } = 1;

    public void SetAdventureLevelToPlay(int levelIndex)
    {
        AdventureLevelIndex = Mathf.Clamp(levelIndex, 1, MAX_LEVELS);
        SelectedLevelIndex = AdventureLevelIndex; // For UI consistency
        SelectedGameMode = GameMode.Adventure;

        if (LevelGenerator.Instance != null)
            LevelGenerator.Instance.SetGameMode(GameMode.Adventure);

        if (CurrentProfile != null)
        {
            CurrentProfile.lastAdventureLevelIndex = AdventureLevelIndex;
            SaveProfile();
        }
    }

    public HandcraftedLevelSO GetHandcraftedLevel(int index)
    {
        if (handcraftedAdventureLevels == null || index < 1 || index > handcraftedAdventureLevels.Count)
            return null;
            
        return handcraftedAdventureLevels[index - 1];
    }

    public int GetAdventureStars(int levelIndex)
    {
        if (CurrentProfile == null || levelIndex < 1 || levelIndex > MAX_LEVELS) return 0;
        return CurrentProfile.adventureStars[levelIndex - 1];
    }

    public void SetAdventureStars(int levelIndex, int stars)
    {
        if (CurrentProfile == null || levelIndex < 1 || levelIndex > MAX_LEVELS) return;
        int arrIndex = levelIndex - 1;
        CurrentProfile.adventureStars[arrIndex] = Mathf.Max(CurrentProfile.adventureStars[arrIndex], stars);
        SaveProfile();
    }

    public bool HasNextAdventureLevel()
    {
        return AdventureLevelIndex < MAX_LEVELS;
    }

    public void CompleteCurrentAdventureLevel()
    {
        AdventureLevelIndex++;
        AdventureLevelIndex = Mathf.Min(AdventureLevelIndex, MAX_LEVELS);

        if (CurrentProfile != null)
        {
            if (AdventureLevelIndex > CurrentProfile.adventureUnlocked)
                CurrentProfile.adventureUnlocked = AdventureLevelIndex;

            CurrentProfile.lastAdventureLevelIndex = AdventureLevelIndex;
            SaveProfile();
        }
    }
}
