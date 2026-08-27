using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public static class DailyHuntFontBaker
{
    public const string DisplaySourcePath =
        "Assets/newdesign/Resources/phase2a/fonts/RobotoCondensed-Bold.ttf";
    public const string BodySourcePath =
        "Assets/newdesign/Resources/phase2a/fonts/RobotoCondensed-Regular.ttf";
    public const string DisplayAssetPath =
        "Assets/newdesign/Resources/dailyhunt/production/fonts/HOL Daily Display SDF.asset";
    public const string BodyAssetPath =
        "Assets/newdesign/Resources/dailyhunt/production/fonts/HOL Daily Body SDF.asset";

    const int PointSize = 96;
    const int AtlasSize = 2048;
    const int AtlasPadding = 10;
    static readonly GlyphRenderMode RenderMode = (GlyphRenderMode)4169;

    static readonly string[] DailyLocalizationKeys =
    {
        "ad_not_ready",
        "between_range",
        "daily_all_missions_complete",
        "daily_challenge_title",
        "daily_come_back",
        "daily_failed",
        "daily_found",
        "daily_hunt",
        "daily_hunt_number",
        "daily_intro",
        "daily_mission_correct",
        "daily_mission_share_room",
        "daily_mission_win",
        "daily_missions_heading",
        "daily_missions_progress",
        "daily_reset_label",
        "daily_reward_collected",
        "daily_reward_heading",
        "daily_reward_pending",
        "daily_share",
        "daily_start",
        "daily_streak",
        "daily_streak_heading",
        "guesses_left",
        "home_daily_title",
        "invalid_number",
        "number_placeholder",
        "player_default",
        "pvp_guess",
        "result_attempts",
        "second_chance",
        "share_copied",
        "share_result",
        "your_guess",
    };

    [MenuItem("HOL/Daily Hunt/Rebuild Production Fonts")]
    public static void Bake()
    {
        uint[] required = RequiredCodePoints();
        string directory = Path.GetDirectoryName(DisplayAssetPath);
        if (string.IsNullOrEmpty(directory))
            throw new InvalidOperationException("Daily Hunt font directory is invalid.");
        Directory.CreateDirectory(directory);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        BuildOrVerify(DisplaySourcePath, DisplayAssetPath,
            "HOL Daily Display SDF", required);
        BuildOrVerify(BodySourcePath, BodyAssetPath,
            "HOL Daily Body SDF", required);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Debug.Log("HOL_DAILY_FONT_BAKE_PASS count=" + required.Length +
                  " codepoints=" + CodePointList(required));
    }

    public static void BakeFromCommandLine()
    {
        try
        {
            Bake();
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            throw;
        }
    }

    public static uint[] RequiredCodePoints()
    {
        var result = new SortedSet<uint>();
        Type l10n = Type.GetType("L10n, Assembly-CSharp");
        FieldInfo field = l10n == null ? null : l10n.GetField(
            "Table", BindingFlags.NonPublic | BindingFlags.Static);
        if (field == null || !(field.GetValue(null) is IDictionary table))
            throw new InvalidOperationException("Could not read L10n.Table.");

        foreach (string key in DailyLocalizationKeys)
        {
            if (!table.Contains(key) || !(table[key] is string[] values))
                throw new InvalidOperationException(
                    "Missing Daily Hunt localization key: " + key);
            foreach (string value in values)
            {
                AddCodePoints(result, value);
                AddCodePoints(result, value.ToUpperInvariant());
            }
        }

        AddCodePoints(result,
            "Player Παίκτης Andreas 0123456789 -/:,.!?%+×↑↓•");
        return result.ToArray();
    }

    static void BuildOrVerify(
        string sourcePath,
        string assetPath,
        string assetName,
        uint[] required)
    {
        var source = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
        if (source == null)
            throw new InvalidOperationException("Missing source font: " + sourcePath);

        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (existing != null)
        {
            Verify(existing, source, assetPath, required);
            return;
        }

        var fontAsset = TMP_FontAsset.CreateFontAsset(
            source, PointSize, AtlasPadding, RenderMode,
            AtlasSize, AtlasSize, AtlasPopulationMode.Dynamic, false);
        if (fontAsset == null)
            throw new InvalidOperationException("Could not create " + assetPath);

        fontAsset.name = assetName;
        fontAsset.material.name = assetName + " Material";
        fontAsset.atlasTexture.name = assetName + " Atlas";

        uint[] missing;
        if (!fontAsset.TryAddCharacters(required, out missing, true))
            throw MissingCharacters(assetPath, missing);

        fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;
        AssetDatabase.CreateAsset(fontAsset, assetPath);
        AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        foreach (Texture2D atlas in fontAsset.atlasTextures)
            if (atlas != null) AssetDatabase.AddObjectToAsset(atlas, fontAsset);
        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.ImportAsset(assetPath,
            ImportAssetOptions.ForceSynchronousImport |
            ImportAssetOptions.ForceUpdate);

        Verify(
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath),
            source, assetPath, required);
    }

    static void Verify(
        TMP_FontAsset asset,
        Font source,
        string path,
        IEnumerable<uint> required)
    {
        if (asset == null)
            throw new InvalidOperationException(path + " did not persist.");
        var serialized = new SerializedObject(asset);
        SerializedProperty sourceReference = serialized.FindProperty(
            "m_SourceFontFile_EditorRef");
        if (sourceReference == null ||
            sourceReference.objectReferenceValue != source)
            throw new InvalidOperationException(path + " source font mismatch.");
        if (asset.atlasPopulationMode != AtlasPopulationMode.Static)
            throw new InvalidOperationException(path + " is not Static.");

        var available = new HashSet<uint>(
            asset.characterTable.Select(character => character.unicode));
        uint[] missing = required.Where(codePoint =>
                !available.Contains(codePoint))
            .OrderBy(codePoint => codePoint).ToArray();
        if (missing.Length > 0)
            throw MissingCharacters(path, missing);
    }

    static void AddCodePoints(ISet<uint> destination, string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        for (int index = 0; index < text.Length; index++)
        {
            int codePoint = char.ConvertToUtf32(text, index);
            if (char.IsHighSurrogate(text[index])) index++;
            if (codePoint == '\r' || codePoint == '\n' || codePoint == '\t')
                continue;
            destination.Add((uint)codePoint);
        }
    }

    static Exception MissingCharacters(
        string path,
        IEnumerable<uint> missing)
    {
        return new InvalidOperationException(
            path + " missing " + CodePointList(missing ?? new uint[0]));
    }

    static string CodePointList(IEnumerable<uint> codePoints)
    {
        return string.Join(" ", codePoints.Select(value =>
            "U+" + value.ToString("X4", CultureInfo.InvariantCulture)).ToArray());
    }
}
