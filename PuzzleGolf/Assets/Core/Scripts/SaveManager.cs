using UnityEngine;

public static class SaveManager
{
    private const string LEVEL_SAVE_KEY = "SavedLevelData";

    public static void SaveLevel(LevelData levelData)
    {
        levelData.Flatten(); // Convert 2D array to 1D limit for JSON serialization
        string json = JsonUtility.ToJson(levelData);
        PlayerPrefs.SetString(LEVEL_SAVE_KEY, json);
        PlayerPrefs.Save();
    }

    public static LevelData LoadLevel()
    {
        if (PlayerPrefs.HasKey(LEVEL_SAVE_KEY))
        {
            string json = PlayerPrefs.GetString(LEVEL_SAVE_KEY);
            LevelData data = JsonUtility.FromJson<LevelData>(json);
            
            if (data != null)
            {
                data.Unflatten(); // Rebuild 2D array
                return data;
            }
        }
        return null; // No saved level found
    }

    public static void ClearSave()
    {
        PlayerPrefs.DeleteKey(LEVEL_SAVE_KEY);
        PlayerPrefs.Save();
    }
}
