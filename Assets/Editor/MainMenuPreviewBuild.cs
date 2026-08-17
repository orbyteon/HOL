using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public static class MainMenuPreviewBuild
{
    const string Scene = "Assets/Scenes/MainMenu.unity";
    const string Output = "build/Android/HOL-mainmenu-debug.apk";

    public static void Build()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            throw new BuildFailedException(
                "Main Menu preview builds require the Android editor target.");

        var group = BuildTargetGroup.Android;
        ScriptingImplementation originalBackend =
            PlayerSettings.GetScriptingBackend(group);
        AndroidArchitecture originalArchitectures =
            PlayerSettings.Android.targetArchitectures;
        bool originalBuildAppBundle = EditorUserBuildSettings.buildAppBundle;
        bool originalUseCustomKeystore =
            PlayerSettings.Android.useCustomKeystore;

        try
        {
            PlayerSettings.SetScriptingBackend(
                group, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures =
                AndroidArchitecture.X86_64;
            EditorUserBuildSettings.buildAppBundle = false;
            PlayerSettings.Android.useCustomKeystore = false;

            string outputPath = Path.GetFullPath(Output);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            BuildReport report = BuildPipeline.BuildPlayer(
                new BuildPlayerOptions
                {
                    scenes = new[] { Scene },
                    locationPathName = outputPath,
                    target = BuildTarget.Android,
                    options = BuildOptions.Development
                });

            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException(
                    "Main Menu preview build failed: " +
                    report.summary.result);
        }
        finally
        {
            PlayerSettings.SetScriptingBackend(group, originalBackend);
            PlayerSettings.Android.targetArchitectures =
                originalArchitectures;
            EditorUserBuildSettings.buildAppBundle =
                originalBuildAppBundle;
            PlayerSettings.Android.useCustomKeystore =
                originalUseCustomKeystore;
        }
    }
}
