using System.IO;
using System;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

public static class MainMenuPreviewBuild
{
    const string OutputPath = "build/Android/HOL-mainmenu-debug.apk";

    public static void Build()
    {
        BuildPreview(false);
    }

    public static void BuildIsolatedPvpPlaytest()
    {
        BuildPreview(true);
    }

    static void BuildPreview(bool isolated)
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            throw new BuildFailedException(
                "Main Menu preview builds require the Android editor target.");

        const BuildTargetGroup group = BuildTargetGroup.Android;
        string previousPackage = PlayerSettings.GetApplicationIdentifier(group);
        string previousProduct = PlayerSettings.productName;
        if (isolated)
        {
            if (Application.unityVersion != "2022.3.62f3")
                throw new BuildFailedException("Isolated playtest requires Unity 2022.3.62f3.");
            // CI checks out LF source. Refuse a changed server payload or an
            // accidentally production-configured build workspace.
            using (var sha = SHA256.Create())
            {
                string hash = BitConverter.ToString(sha.ComputeHash(
                    File.ReadAllBytes("playfab/cloudscript.js"))).Replace("-", "");
                if (hash != PvpIsolatedPlaytest.CloudScriptSha256)
                    throw new BuildFailedException("Pinned playtest CloudScript hash mismatch.");
            }
            if (!string.IsNullOrEmpty(ReleaseConfig.PlayFabTitleId) ||
                !string.IsNullOrEmpty(ReleaseConfig.ProvisioningUrl) ||
                ReleaseConfig.GoogleCloudProjectNumber != 0)
                throw new BuildFailedException("Isolated playtest must not contain production release configuration.");
            if (File.Exists(OutputPath))
                throw new BuildFailedException("Refusing to overwrite a previous playtest APK.");
        }
        ScriptingImplementation previousBackend =
            PlayerSettings.GetScriptingBackend(group);
        AndroidArchitecture previousArchitectures =
            PlayerSettings.Android.targetArchitectures;
        bool previousAppBundle = EditorUserBuildSettings.buildAppBundle;
        bool previousCustomKeystore = PlayerSettings.Android.useCustomKeystore;
        bool previousAutomaticGraphics =
            PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android);
        GraphicsDeviceType[] previousGraphicsApis =
            PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);

        try
        {
            if (isolated)
            {
                PlayerSettings.SetApplicationIdentifier(group, PvpIsolatedPlaytest.PackageId);
                PlayerSettings.productName = PvpIsolatedPlaytest.ProductName;
            }
            PlayerSettings.SetScriptingBackend(
                group, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures =
                AndroidArchitecture.ARM64 | AndroidArchitecture.X86_64;
            // Swiftshader emulators abort adb shortly after Unity's Vulkan
            // probe on the heavier cartoon Home; pin GLES3 for this APK only.
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(
                BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
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
            if (isolated)
                options.extraScriptingDefines = new[] { "HOL_PVP_ISOLATED_PLAYTEST" };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException(
                    "Main Menu preview APK build failed: " + report.summary.result);
            if (isolated)
            {
                File.WriteAllText(Path.Combine(outputDirectory, "isolated-playtest-manifest.json"),
                    JsonUtility.ToJson(new PlaytestManifest
                    {
                        titleId = PvpIsolatedPlaytest.TitleId,
                        packageId = PlayerSettings.GetApplicationIdentifier(group),
                        productName = PlayerSettings.productName,
                        unityVersion = Application.unityVersion,
                        cloudScriptSha256 = PvpIsolatedPlaytest.CloudScriptSha256,
                        scenes = options.scenes,
                        development = (options.options & BuildOptions.Development) != 0,
                        operatorProvisioningOnly = true
                    }, true));
            }
        }
        finally
        {
            if (isolated)
            {
                PlayerSettings.SetApplicationIdentifier(group, previousPackage);
                PlayerSettings.productName = previousProduct;
            }
            PlayerSettings.SetScriptingBackend(group, previousBackend);
            PlayerSettings.Android.targetArchitectures = previousArchitectures;
            PlayerSettings.SetUseDefaultGraphicsAPIs(
                BuildTarget.Android, previousAutomaticGraphics);
            if (previousGraphicsApis != null && previousGraphicsApis.Length > 0)
                PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, previousGraphicsApis);
            EditorUserBuildSettings.buildAppBundle = previousAppBundle;
            PlayerSettings.Android.useCustomKeystore = previousCustomKeystore;
        }
    }

    [Serializable]
    sealed class PlaytestManifest
    {
        public string titleId, packageId, productName, unityVersion, cloudScriptSha256;
        public string[] scenes;
        public bool development, operatorProvisioningOnly;
    }
}
