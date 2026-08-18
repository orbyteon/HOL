using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public static class SplashPreviewBuild
{
    const string OutputPath = "build/Android/HOL-splash-debug.apk";

    public static void Build()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            throw new BuildFailedException(
                "Splash preview builds require the Android editor target.");

        const BuildTargetGroup group = BuildTargetGroup.Android;
        ScriptingImplementation previousBackend =
            PlayerSettings.GetScriptingBackend(group);
        AndroidArchitecture previousArchitectures =
            PlayerSettings.Android.targetArchitectures;
        bool previousAppBundle = EditorUserBuildSettings.buildAppBundle;
        bool previousCustomKeystore = PlayerSettings.Android.useCustomKeystore;

        try
        {
            PlayerSettings.SetScriptingBackend(
                group, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.X86_64;
            EditorUserBuildSettings.buildAppBundle = false;
            PlayerSettings.Android.useCustomKeystore = false;

            string outputDirectory = Path.GetDirectoryName(OutputPath);
            if (!string.IsNullOrEmpty(outputDirectory))
                Directory.CreateDirectory(outputDirectory);

            var options = new BuildPlayerOptions
            {
                scenes = new[]
                {
                    "Assets/Scenes/SplashScene.unity",
                    "Assets/Scenes/MainMenu.unity"
                },
                locationPathName = OutputPath,
                target = BuildTarget.Android,
                options = BuildOptions.Development
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException(
                    "Splash preview APK build failed: " + report.summary.result);
        }
        finally
        {
            PlayerSettings.SetScriptingBackend(group, previousBackend);
            PlayerSettings.Android.targetArchitectures = previousArchitectures;
            EditorUserBuildSettings.buildAppBundle = previousAppBundle;
            PlayerSettings.Android.useCustomKeystore = previousCustomKeystore;
        }
    }
}
