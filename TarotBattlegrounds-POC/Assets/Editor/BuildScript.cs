using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

public static class BuildScript
{
    private const string BuildRoot = "WebGLBuild";
    private const string BuildPrefix = "WebGLBuild";
    private const int MaxBuildsToKeep = 10;

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

        // Determine next build number
        int nextNumber = GetNextBuildNumber();
        string buildDir = Path.Combine(BuildRoot, BuildPrefix + nextNumber.ToString("D2"));

        Debug.Log($"[BuildScript] Building to {buildDir} (build #{nextNumber})");

        BuildPlayerOptions buildOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = buildDir,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        // Set WebGL-specific settings
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
        PlayerSettings.WebGL.template = "PROJECT:TarotBattlegrounds";

        Debug.Log("[BuildScript] Starting WebGL build...");
        var report = BuildPipeline.BuildPlayer(buildOptions);

        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"[BuildScript] WebGL build succeeded: {report.summary.totalSize} bytes → {buildDir}");
            CleanOldBuilds();
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

    private static int GetNextBuildNumber()
    {
        if (!Directory.Exists(BuildRoot))
            return 1;

        int max = 0;
        foreach (string dir in Directory.GetDirectories(BuildRoot, BuildPrefix + "*"))
        {
            string name = Path.GetFileName(dir);
            string numPart = name.Substring(BuildPrefix.Length);
            if (int.TryParse(numPart, out int num) && num > max)
                max = num;
        }
        return max + 1;
    }

    private static void CleanOldBuilds()
    {
        if (!Directory.Exists(BuildRoot))
            return;

        var buildDirs = Directory.GetDirectories(BuildRoot, BuildPrefix + "*")
            .OrderBy(d =>
            {
                string numPart = Path.GetFileName(d).Substring(BuildPrefix.Length);
                return int.TryParse(numPart, out int n) ? n : 0;
            })
            .ToList();

        while (buildDirs.Count > MaxBuildsToKeep)
        {
            string oldest = buildDirs[0];
            Debug.Log($"[BuildScript] Removing old build: {oldest}");
            Directory.Delete(oldest, true);
            buildDirs.RemoveAt(0);
        }
    }
}
