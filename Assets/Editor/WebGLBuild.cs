using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class WebGLBuild
{
    const string DefaultOutputPath = "Build/WebGL";

    [MenuItem("Tools/Build/WebGL")]
    public static void BuildFromMenu()
    {
        Build(DefaultOutputPath, exitEditor: false);
    }

    /// <summary>Точка входа для batchmode: Unity -executeMethod WebGLBuild.BuildFromCommandLine</summary>
    public static void BuildFromCommandLine()
    {
        string outputPath = GetCommandLineArg("-outputPath") ?? DefaultOutputPath;
        bool success = Build(outputPath, exitEditor: true);
        EditorApplication.Exit(success ? 0 : 1);
    }

    static bool Build(string outputPath, bool exitEditor)
    {
        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
            throw new InvalidOperationException("No enabled scenes found in Build Settings.");

        Directory.CreateDirectory(outputPath);

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        bool success = report.summary.result == BuildResult.Succeeded;

        if (success)
        {
            UnityEngine.Debug.Log("WebGL build completed: " + Path.GetFullPath(outputPath));
        }
        else
        {
            UnityEngine.Debug.LogError("WebGL build failed: " + report.summary.result);
        }

        if (!exitEditor && success)
            EditorUtility.RevealInFinder(outputPath);

        return success;
    }

    static string GetCommandLineArg(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == name)
                return args[i + 1];
        }

        return null;
    }
}
