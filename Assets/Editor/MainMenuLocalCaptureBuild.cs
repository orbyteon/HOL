using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class MainMenuLocalCaptureBuild
{
    const string OutputEnvironment = "HOL_MAINMENU_WINDOWS_BUILD";

    [MenuItem("HOL/Build Main Menu Capture Player")]
    public static void Build()
    {
        string output = Environment.GetEnvironmentVariable(OutputEnvironment);
        if (string.IsNullOrWhiteSpace(output))
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            output = Path.Combine(
                projectRoot ?? throw new InvalidOperationException(
                    "Unable to resolve the project root."),
                "artifacts", "mainmenu-v2", "build", "HOLMainMenu.exe");
        }

        string directory = Path.GetDirectoryName(output);
        if (string.IsNullOrWhiteSpace(directory))
            throw new InvalidOperationException("Capture build directory is invalid.");
        Directory.CreateDirectory(directory);

        BuildReport report = BuildPipeline.BuildPlayer(
            new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/MainMenu.unity" },
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development,
            });

        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException(
                "Main Menu local capture build failed: " +
                report.summary.result);

        Debug.Log("HOL_MAINMENU_WINDOWS_BUILD_READY " + output);
    }
}
