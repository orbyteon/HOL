using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// One Windows development player for deterministic visual validation of the
// approved HOL cartoon flow. Screen selection, language, viewport and output
// path are runtime arguments, so local iteration does not create one-off
// builders or production assets per screen.
public static class HolCartoonFlowLocalCaptureBuild
{
    const string OutputEnvironment = "HOL_CARTOON_FLOW_WINDOWS_BUILD";
    const string CartoonProductionFolder =
        "Assets/newdesign/Resources/cartoonui/v1";

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
                "HOL cartoon flow local capture build failed: " +
                report.summary.result);

        Debug.Log("HOL_CARTOON_FLOW_WINDOWS_BUILD_READY " + output);
    }

    static void ConfigureProductionTextures()
    {
        string[] guids = AssetDatabase.FindAssets(
            "t:Texture2D", new[] { CartoonProductionFolder });
        if (guids.Length == 0)
            throw new InvalidOperationException(
                "No HOL cartoon production textures were imported.");

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
            importer.textureCompression =
                TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
        }

        AssetDatabase.SaveAssets();
    }
}
