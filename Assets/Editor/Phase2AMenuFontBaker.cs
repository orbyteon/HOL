using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public static class Phase2AMenuFontBaker
{
    public const string BoldSourcePath =
        "Assets/newdesign/Resources/phase2a/fonts/RobotoCondensed-Bold.ttf";
    public const string RegularSourcePath =
        "Assets/newdesign/Resources/phase2a/fonts/RobotoCondensed-Regular.ttf";
    public const string DisplayAssetPath =
        "Assets/newdesign/Resources/phase2a/fonts/HOL Menu Display SDF.asset";
    public const string BodyAssetPath =
        "Assets/newdesign/Resources/phase2a/fonts/HOL Menu Body SDF.asset";

    const int PointSize = 96;
    const int AtlasSize = 1024;
    const int AtlasPadding = 10;
    static readonly GlyphRenderMode RenderMode = (GlyphRenderMode)4169;

    // The Main Menu owns only these live strings. Keeping the bake set focused
    // makes both atlases deterministic while covering English, Greek, player
    // identity, streak values, punctuation and all current responsive copies.
    static readonly string[] MenuStrings =
    {
        "PLAY SOLO VS AI", "PLAY NOW",
        "PRIVATE ROOM", "PLAY WITH A FRIEND",
        "DAILY HUNT", "NEW CHALLENGE EVERY DAY",
        "TIP:", "Every guess narrows the range!",
        "ΠΑΙΞΕ SOLO ΜΕ AI", "ΑΜΕΣΟ ΠΑΙΧΝΙΔΙ",
        "ΙΔΙΩΤΙΚΟ ΔΩΜΑΤΙΟ", "ΠΑΙΞΕ ΜΕ ΦΙΛΟ",
        "ΚΥΝΗΓΙ ΗΜΕΡΑΣ", "ΝΕΑ ΠΡΟΚΛΗΣΗ ΚΑΘΕ ΜΕΡΑ",
        "ΣΥΜΒΟΥΛΗ:", "Κάθε μαντεψιά μικραίνει το εύρος!",
        "Player", "Παίκτης", "Streak", "Σερί", "Andreas",
        "0123456789-:!"
    };

    [MenuItem("HOL/Phase 2A/Rebuild Main Menu Fonts")]
    public static void Bake()
    {
        uint[] required = RequiredCodePoints();
        BuildOrVerify(BoldSourcePath, DisplayAssetPath,
            "HOL Menu Display SDF", required);
        BuildOrVerify(RegularSourcePath, BodyAssetPath,
            "HOL Menu Body SDF", required);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Debug.Log("HOL_PHASE2A_FONT_BAKE_PASS count=" + required.Length +
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
        foreach (string text in MenuStrings)
        {
            for (int i = 0; i < text.Length; i++)
            {
                int codePoint = char.ConvertToUtf32(text, i);
                if (char.IsHighSurrogate(text[i])) i++;
                if (codePoint != '\r' && codePoint != '\n' && codePoint != '\t')
                    result.Add((uint)codePoint);
            }
        }
        return result.ToArray();
    }

    static void BuildOrVerify(string sourcePath, string assetPath,
        string assetName, uint[] required)
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

        var fontAsset = TMP_FontAsset.CreateFontAsset(source, PointSize,
            AtlasPadding, RenderMode, AtlasSize, AtlasSize,
            AtlasPopulationMode.Dynamic, false);
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

        var persisted = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        Verify(persisted, source, assetPath, required);
    }

    static void Verify(TMP_FontAsset asset, Font source, string path,
        IEnumerable<uint> required)
    {
        if (asset == null)
            throw new InvalidOperationException(path + " did not persist.");
        // Static TMP assets intentionally clear the runtime source reference;
        // Unity preserves the editor-only reference used for deterministic
        // rebuilds. Verify that serialized reference instead of m_SourceFontFile.
        var serialized = new SerializedObject(asset);
        var sourceReference = serialized.FindProperty("m_SourceFontFile_EditorRef");
        if (sourceReference == null || sourceReference.objectReferenceValue != source)
            throw new InvalidOperationException(path + " source font mismatch.");
        if (asset.atlasPopulationMode != AtlasPopulationMode.Static)
            throw new InvalidOperationException(path + " is not Static.");
        var available = new HashSet<uint>(
            asset.characterTable.Select(character => character.unicode));
        uint[] missing = required.Where(codePoint => !available.Contains(codePoint))
            .OrderBy(codePoint => codePoint).ToArray();
        if (missing.Length > 0) throw MissingCharacters(path, missing);
    }

    static Exception MissingCharacters(string path, IEnumerable<uint> missing)
    {
        return new InvalidOperationException(path + " missing " +
            CodePointList(missing ?? new uint[0]));
    }

    static string CodePointList(IEnumerable<uint> codePoints)
    {
        return string.Join(" ", codePoints.Select(value =>
            "U+" + value.ToString("X4", CultureInfo.InvariantCulture)).ToArray());
    }
}
