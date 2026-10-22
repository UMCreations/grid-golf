using UnityEngine;

public static class SaveManager
{
    private const string LEVEL_SAVE_KEY = "SavedLevelData";
    private const string COMMUNITY_LEVEL_SAVE_PREFIX = "SavedCommunityLevelData_";

    public static void SaveLevel(LevelData levelData)
    {
        levelData.Flatten(); // Convert 2D array to 1D limit for JSON serialization
        string json = JsonUtility.ToJson(levelData);
        PlayerPrefs.SetString(GetSaveKey(levelData.communityLevelId), json);
        PlayerPrefs.Save();
    }

    public static LevelData LoadLevel(string communityLevelId = null)
    {
        string saveKey = GetSaveKey(communityLevelId);
        if (PlayerPrefs.HasKey(saveKey))
        {
            string json = PlayerPrefs.GetString(saveKey);
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

    public static void ClearCommunitySave(string communityLevelId)
    {
        if (string.IsNullOrEmpty(communityLevelId)) return;
        PlayerPrefs.DeleteKey(GetSaveKey(communityLevelId));
        PlayerPrefs.Save();
    }

    private static string GetSaveKey(string communityLevelId)
    {
        return string.IsNullOrEmpty(communityLevelId)
            ? LEVEL_SAVE_KEY
            : COMMUNITY_LEVEL_SAVE_PREFIX + communityLevelId;
    }
}
