using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.WebGL;
using UnityEngine;

public static class WebGLBuildConfigurator
{
    private const string BuildOutputPath = "Builds/WebGL";

    [MenuItem("Tools/Puzzle Golf/WebGL/Apply Recommended Settings")]
    public static void ApplyRecommendedSettings()
    {
        PlayerSettings.runInBackground = false;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.WebGL, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.WebGL, ManagedStrippingLevel.Medium);
        PlayerSettings.stripEngineCode = true;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
        PlayerSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.Off;
        PlayerSettings.WebGL.template = "APPLICATION:Default";
        UserBuildSettings.codeOptimization = WasmCodeOptimization.DiskSizeLTO;

        AssetDatabase.SaveAssets();

        Debug.Log("Applied recommended WebGL settings for PuzzleGolf. Set Web resolution manually in Player Settings if needed.");
    }

    [MenuItem("Tools/Puzzle Golf/WebGL/Build Release")]
    public static void BuildRelease()
    {
        ApplyRecommendedSettings();

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
        {
            bool switched = EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
            if (!switched)
            {
                Debug.LogError("Failed to switch active build target to WebGL.");
                return;
            }
        }

        Directory.CreateDirectory(BuildOutputPath);

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = GetEnabledScenes(),
            locationPathName = BuildOutputPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"WebGL build succeeded: {report.summary.outputPath}");
        }
        else
        {
            Debug.LogError($"WebGL build failed: {report.summary.result}");
        }
    }

    private static string[] GetEnabledScenes()
    {
        var scenes = EditorBuildSettings.scenes;
        int count = 0;
        foreach (var scene in scenes)
        {
            if (scene.enabled)
                count++;
        }

        string[] enabled = new string[count];
        int index = 0;
        foreach (var scene in scenes)
        {
            if (!scene.enabled)
                continue;

            enabled[index++] = scene.path;
        }

        return enabled;
    }
}
