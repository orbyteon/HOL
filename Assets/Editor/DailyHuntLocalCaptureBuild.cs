using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class DailyHuntLocalCaptureBuild
{
    const string OutputEnvironment = "HOL_DAILY_HUNT_WINDOWS_BUILD";
    const string ProductionAssetFolder =
        "Assets/newdesign/Resources/dailyhunt/production";

    public static void Build()
    {
        string output = Environment.GetEnvironmentVariable(OutputEnvironment);
        if (string.IsNullOrWhiteSpace(output))
            throw new InvalidOperationException(OutputEnvironment + " is not set.");

        string directory = Path.GetDirectoryName(output);
        if (string.IsNullOrWhiteSpace(directory))
            throw new InvalidOperationException("Capture build directory is invalid.");
        Directory.CreateDirectory(directory);

        ConfigureProductionTextures();

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
                "Daily Hunt local capture build failed: " +
                report.summary.result);

        Debug.Log("HOL_DAILY_HUNT_WINDOWS_BUILD_READY " + output);
    }

    static void ConfigureProductionTextures()
    {
        string[] guids = AssetDatabase.FindAssets(
            "t:Texture2D", new[] { ProductionAssetFolder });
        if (guids.Length == 0)
            throw new InvalidOperationException(
                "No Daily Hunt production textures were imported.");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            bool changed =
                importer.textureType != TextureImporterType.Sprite ||
                importer.spriteImportMode != SpriteImportMode.Single ||
                importer.mipmapEnabled ||
                !importer.sRGBTexture ||
                !importer.alphaIsTransparency ||
                importer.maxTextureSize != 2048;
            if (!changed) continue;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.sRGBTexture = true;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
        }

        AssetDatabase.SaveAssets();
    }
}
