using UnityEditor;
using UnityEngine;
using System.Linq;

public static class BuildScript
{
    [MenuItem("Build/WebGL")]
    public static void BuildWebGL()
    {
        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            Debug.LogError("[BuildScript] No scenes in Build Settings. Add scenes before building.");
            return;
        }

        BuildPlayerOptions buildOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = "WebGLBuild",
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        // Set WebGL-specific settings
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
        PlayerSettings.WebGL.template = "PROJECT:TarotBattlegrounds";
        PlayerSettings.SetIl2CppCodeGeneration(
            NamedBuildTarget.WebGL,
            Il2CppCodeGeneration.OptimizeSize
        );

        Debug.Log("[BuildScript] Starting WebGL build...");
        var report = BuildPipeline.BuildPlayer(buildOptions);

        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"[BuildScript] WebGL build succeeded: {report.summary.totalSize} bytes");
        }
        else
        {
            Debug.LogError($"[BuildScript] WebGL build failed: {report.summary.result}");
            EditorApplication.Exit(1);
        }
    }

    /// <summary>
    /// CLI entry point: Unity -executeMethod BuildScript.BuildWebGLCLI
    /// </summary>
    public static void BuildWebGLCLI()
    {
        BuildWebGL();
    }
}
