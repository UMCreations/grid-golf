using System.IO;
using UnityEditor;
using UnityEngine;

public static class LevelJsonAssetUtility
{
    private const string DefaultImportedLevelsFolder = "Assets/Core/LeveLData/Imported";

    [MenuItem("Tools/Puzzle Golf/Levels/Import JSON To Handcrafted Asset")]
    public static void ImportJsonToHandcraftedAsset()
    {
        string jsonPath = EditorUtility.OpenFilePanel("Import Level JSON", Application.dataPath, "json");
        if (string.IsNullOrWhiteSpace(jsonPath))
            return;

        string json = File.ReadAllText(jsonPath);
        LevelAuthoringDto dto = JsonUtility.FromJson<LevelAuthoringDto>(json);
        LevelAuthoringValidationResult validation = LevelAuthoringValidator.Validate(dto);
        if (!validation.IsValid)
        {
            Debug.LogError("Level JSON import failed:\n" + string.Join("\n", validation.Errors));
            return;
        }

        EnsureFolderExists(DefaultImportedLevelsFolder);

        string defaultFileName = string.IsNullOrWhiteSpace(dto.id) ? "ImportedLevel" : SanitizeFileName(dto.id);
        string assetPath = EditorUtility.SaveFilePanelInProject(
            "Save Imported Handcrafted Level",
            defaultFileName,
            "asset",
            "Choose where to save the handcrafted level asset.",
            DefaultImportedLevelsFolder);

        if (string.IsNullOrWhiteSpace(assetPath))
            return;

        HandcraftedLevelSO asset = ScriptableObject.CreateInstance<HandcraftedLevelSO>();
        LevelAuthoringConverter.ApplyToHandcraftedLevel(asset, dto);
        AssetDatabase.CreateAsset(asset, assetPath);
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = asset;
        Debug.Log($"Imported level JSON into asset: {assetPath}");
    }

    [MenuItem("Tools/Puzzle Golf/Levels/Export Selected Handcrafted Asset To JSON")]
    public static void ExportSelectedHandcraftedAssetToJson()
    {
        HandcraftedLevelSO asset = Selection.activeObject as HandcraftedLevelSO;
        if (asset == null)
        {
            Debug.LogError("Select a HandcraftedLevelSO asset before exporting.");
            return;
        }

        string suggestedFileName = string.IsNullOrWhiteSpace(asset.levelName) ? "level" : SanitizeFileName(asset.levelName);
        string outputPath = EditorUtility.SaveFilePanel("Export Level JSON", Application.dataPath, suggestedFileName, "json");
        if (string.IsNullOrWhiteSpace(outputPath))
            return;

        LevelAuthoringDto dto = LevelAuthoringConverter.FromHandcraftedLevel(asset, suggestedFileName);
        string json = JsonUtility.ToJson(dto, true);
        File.WriteAllText(outputPath, json);
        AssetDatabase.Refresh();

        Debug.Log($"Exported handcrafted level JSON to: {outputPath}");
    }

    private static string SanitizeFileName(string value)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(c, '-');
        }

        return value.Replace(' ', '-').ToLowerInvariant();
    }

    private static void EnsureFolderExists(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
            return;

        string[] parts = folderPath.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }
}
