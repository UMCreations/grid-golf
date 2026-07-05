using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.WebGL;
using UnityEngine;

public static class WebGLBuildConfigurator
{
    private const string BuildOutputPath = "Builds/WebGL";

    [MenuItem("Tools/Puzzle Golf/WebGL/Apply GitHub Pages Release Settings")]
    public static void ApplyGitHubPagesReleaseSettings()
    {
        PlayerSettings.runInBackground = false;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.WebGL, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.WebGL, ManagedStrippingLevel.Medium);
        PlayerSettings.stripEngineCode = true;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
        PlayerSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.Off;
        PlayerSettings.WebGL.template = "APPLICATION:Default";
        UserBuildSettings.codeOptimization = WasmCodeOptimization.DiskSizeLTO;

        AssetDatabase.SaveAssets();

        Debug.Log("Applied GitHub Pages-safe WebGL release settings for PuzzleGolf. Compression is disabled to avoid host header issues.");
    }

    [MenuItem("Tools/Puzzle Golf/WebGL/Apply GitHub Pages Development Settings")]
    public static void ApplyGitHubPagesDevelopmentSettings()
    {
        PlayerSettings.runInBackground = false;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.WebGL, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.WebGL, ManagedStrippingLevel.Minimal);
        PlayerSettings.stripEngineCode = false;
        PlayerSettings.WebGL.dataCaching = false;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
        PlayerSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.Off;
        PlayerSettings.WebGL.template = "APPLICATION:Default";
        UserBuildSettings.codeOptimization = WasmCodeOptimization.None;

        AssetDatabase.SaveAssets();

        Debug.Log("Applied GitHub Pages-safe WebGL development settings for PuzzleGolf.");
    }

    [MenuItem("Tools/Puzzle Golf/WebGL/Build Release")]
    public static void BuildRelease()
    {
        ApplyGitHubPagesReleaseSettings();
        BuildWebGL(BuildOptions.None);
    }

    [MenuItem("Tools/Puzzle Golf/WebGL/Build Development")]
    public static void BuildDevelopment()
    {
        ApplyGitHubPagesDevelopmentSettings();
        BuildWebGL(BuildOptions.Development | BuildOptions.AllowDebugging);
    }

    private static void BuildWebGL(BuildOptions buildOptions)
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
        {
            bool switched = EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
            if (!switched)
            {
                Debug.LogError("Failed to switch active build target to WebGL.");
                return;
            }
        }

        if (Directory.Exists(BuildOutputPath))
        {
            Directory.Delete(BuildOutputPath, true);
        }
        Directory.CreateDirectory(BuildOutputPath);

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = GetEnabledScenes(),
            locationPathName = BuildOutputPath,
            target = BuildTarget.WebGL,
            options = buildOptions
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
