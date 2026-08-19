using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public static class ProductionTextFontBaker
{
    public const string FallbackAssetPath =
        "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset";
    public const string PrimaryAssetPath =
        "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    public const string SourceFontPath = "Assets/TextMesh Pro/Fonts/LiberationSans.ttf";
    public const string ExpectedFallbackGuid = "2e498d1c8094910479dc3e1b768306a4";

    const int PointSize = 86;
    const int AtlasSize = 1024;
    const int AtlasPadding = 9;
    static readonly GlyphRenderMode RenderMode = (GlyphRenderMode)4169; // SDFAA_HINTED
    const uint UnsupportedStar = 0x2605;
    const uint UnsupportedBackspace = 0x232B;

    // These are the localization entries that production presentation code
    // actually uppercases. Including the transformed result is important for
    // accented Greek, whose uppercase code point can differ from the source.
    static readonly string[] UppercaseLocalizationKeys =
    {
        "back",
        "daily_hunt",
        "find_challenger",
        "hud_tip",
        "play",
        "prebattle_opponent",
        "private_room_title",
        "pvp_create_room",
        "pvp_join_room",
        "rematch",
        "stats_streak",
        "stats_wins",
        "you"
    };

    // Visible production literals that do not come from L10n.Table. This list
    // deliberately contains only current text presentation, not test strings,
    // logs, deferred artwork, or the unsupported star/backspace code points.
    static readonly string[] VisibleDesignLiterals =
    {
        "TIP:",
        "PLAY WITH\nA FRIEND",
        "ΠΑΙΞΕ\nΜΕ ΦΙΛΟ",
        "GUESS THE NUMBER!",
        "ΜΑΝΤΕΨΕ ΤΟΝ ΑΡΙΘΜΟ!",
        "PLAY SOLO",
        "ΠΑΙΞΕ SOLO",
        "Beat the adaptive opponent",
        "Νίκησε τον προσαρμοστικό αντίπαλο",
        "PLAY WITH A FRIEND",
        "ΠΑΙΞΕ ΜΕ ΦΙΛΟ",
        "Create or join a private room",
        "Δημιούργησε ή μπες σε ιδιωτικό δωμάτιο",
        "DAILY HUNT",
        "ΚΑΘΗΜΕΡΙΝΟ ΚΥΝΗΓΙ",
        "One shared number every day",
        "Ένας κοινός αριθμός κάθε μέρα",
        "0123456789",
        // The join arrow is replaced by approved board_join_exact artwork
        // before final presentation; the failure mark exists only in clipboard
        // share text. Neither is a rendered TMP font requirement.
        "☻ϟ•×←→▲▼●"
    };

    [MenuItem("HOL/Fonts/Rebuild Production Greek Fallback")]
    public static void Bake()
    {
        string guidBefore = AssetDatabase.AssetPathToGUID(FallbackAssetPath);
        if (guidBefore != ExpectedFallbackGuid)
            throw new InvalidOperationException(
                "Fallback GUID changed before bake. Expected " + ExpectedFallbackGuid +
                ", found " + guidBefore + ".");

        var source = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
        var primary = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PrimaryAssetPath);
        var fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackAssetPath);
        if (source == null || primary == null || fallback == null)
            throw new InvalidOperationException("Required Liberation Sans font assets are missing.");

        if (fallback.atlasPadding != AtlasPadding || fallback.atlasRenderMode != RenderMode ||
            Mathf.RoundToInt(fallback.faceInfo.pointSize) != PointSize)
        {
            throw new InvalidOperationException(
                "Fallback generation settings changed; expected point 86, padding 9, SDFAA_HINTED. " +
                "Actual point=" + fallback.faceInfo.pointSize +
                " atlas=" + fallback.atlasWidth + "x" + fallback.atlasHeight +
                " padding=" + fallback.atlasPadding +
                " renderMode=" + (int)fallback.atlasRenderMode +
                " SDFAA_HINTED=" + (int)RenderMode + ".");
        }

        uint[] canonical = CanonicalCodePoints();
        uint[] required = RequiredFallbackCodePoints(primary, canonical);
        if (canonical.Contains(UnsupportedStar) || canonical.Contains(UnsupportedBackspace) ||
            required.Contains(UnsupportedStar) || required.Contains(UnsupportedBackspace))
        {
            throw new InvalidOperationException("Unsupported deferred symbols remain in production text.");
        }

        // Prove source support and atlas capacity before touching the persistent
        // asset. The probe uses the exact same settings and sorted code points.
        var probe = TMP_FontAsset.CreateFontAsset(source, PointSize, AtlasPadding,
            RenderMode, AtlasSize, AtlasSize,
            AtlasPopulationMode.Dynamic, false);
        if (probe == null)
            throw new InvalidOperationException("Could not create the Liberation Sans preflight font asset.");

        try
        {
            uint[] probeMissing;
            if (!probe.TryAddCharacters(required, out probeMissing, true))
                throw MissingCharacters("Preflight failed", probeMissing);
        }
        finally
        {
            if (probe.material != null) UnityEngine.Object.DestroyImmediate(probe.material);
            if (probe.atlasTextures != null)
                foreach (var texture in probe.atlasTextures)
                    if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(probe);
        }

        var serializedFallback = new SerializedObject(fallback);
        serializedFallback.FindProperty("m_AtlasWidth").intValue = AtlasSize;
        serializedFallback.FindProperty("m_AtlasHeight").intValue = AtlasSize;
        serializedFallback.ApplyModifiedPropertiesWithoutUndo();

        fallback.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        if (fallback.sourceFontFile != source)
            throw new InvalidOperationException("Fallback source is not LiberationSans.ttf.");

        fallback.ClearFontAssetData(true);
        uint[] missing;
        if (!fallback.TryAddCharacters(required, out missing, true))
            throw MissingCharacters("Persistent bake failed", missing);

        fallback.material.SetTexture(ShaderUtilities.ID_MainTex, fallback.atlasTexture);
        fallback.material.SetFloat(ShaderUtilities.ID_TextureWidth, AtlasSize);
        fallback.material.SetFloat(ShaderUtilities.ID_TextureHeight, AtlasSize);
        fallback.material.SetFloat(ShaderUtilities.ID_GradientScale, AtlasPadding + 1);

        var settings = fallback.creationSettings;
        settings.sourceFontFileName = "LiberationSans.ttf";
        settings.sourceFontFileGUID = AssetDatabase.AssetPathToGUID(SourceFontPath);
        settings.pointSizeSamplingMode = 0;
        settings.pointSize = PointSize;
        settings.padding = AtlasPadding;
        settings.packingMode = 4;
        settings.atlasWidth = AtlasSize;
        settings.atlasHeight = AtlasSize;
        settings.characterSetSelectionMode = 1;
        settings.characterSequence = string.Join(", ",
            required.Select(codePoint => codePoint.ToString(CultureInfo.InvariantCulture)).ToArray());
        settings.referencedFontAssetGUID = AssetDatabase.AssetPathToGUID(PrimaryAssetPath);
        settings.referencedTextAssetGUID = string.Empty;
        settings.fontStyle = 0;
        settings.fontStyleModifier = 0;
        settings.renderMode = (int)RenderMode;
        settings.includeFontFeatures = true;
        fallback.creationSettings = settings;
        fallback.atlasPopulationMode = AtlasPopulationMode.Static;

        foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(FallbackAssetPath))
            EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        AssetDatabase.ForceReserializeAssets(new[] { FallbackAssetPath });
        AssetDatabase.ImportAsset(FallbackAssetPath,
            ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

        VerifyBakedAsset();
        Debug.Log("HOL_FONT_CANONICAL_COUNT=" + canonical.Length);
        Debug.Log("HOL_FONT_CANONICAL_CODEPOINTS=" + CodePointList(canonical));
        Debug.Log("HOL_FONT_FALLBACK_COUNT=" + required.Length);
        Debug.Log("HOL_FONT_FALLBACK_CODEPOINTS=" + CodePointList(required));
        Debug.Log("HOL_FONT_BAKE_PASS guid=" + guidBefore +
                  " pointSize=" + PointSize + " atlas=" + AtlasSize + "x" + AtlasSize +
                  " padding=" + AtlasPadding + " renderMode=" + RenderMode);
    }

    public static void BakeFromCommandLine()
    {
        try
        {
            Bake();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            throw;
        }
    }

    public static uint[] CanonicalCodePoints()
    {
        var codePoints = new SortedSet<uint>();
        IDictionary table = LocalizationTable();
        var entries = new List<DictionaryEntry>();
        foreach (DictionaryEntry entry in table)
            entries.Add(entry);

        foreach (DictionaryEntry entry in entries
                     .OrderBy(item => (string)item.Key, StringComparer.Ordinal))
        {
            foreach (string value in (string[])entry.Value)
                AddCodePoints(codePoints, value);
        }

        foreach (string key in UppercaseLocalizationKeys)
        {
            if (!table.Contains(key))
                throw new InvalidOperationException("Missing uppercase localization key: " + key);
            foreach (string value in (string[])table[key])
                AddCodePoints(codePoints, value.ToUpperInvariant());
        }

        foreach (string literal in VisibleDesignLiterals)
            AddCodePoints(codePoints, literal);

        return codePoints.ToArray();
    }

    public static uint[] RequiredFallbackCodePoints()
    {
        var primary = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PrimaryAssetPath);
        if (primary == null)
            throw new InvalidOperationException("Primary Liberation Sans font asset is missing.");
        return RequiredFallbackCodePoints(primary, CanonicalCodePoints());
    }

    public static void VerifyBakedAsset()
    {
        string guid = AssetDatabase.AssetPathToGUID(FallbackAssetPath);
        var primary = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PrimaryAssetPath);
        var fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackAssetPath);
        if (guid != ExpectedFallbackGuid || primary == null || fallback == null)
            throw new InvalidOperationException("Fallback asset identity verification failed.");
        if (fallback.atlasPopulationMode != AtlasPopulationMode.Static)
            throw new InvalidOperationException("Fallback atlas population mode is not Static.");

        uint[] canonical = CanonicalCodePoints();
        uint[] required = RequiredFallbackCodePoints(primary, canonical);
        var primaryCharacters = new HashSet<uint>(primary.characterTable.Select(character => character.unicode));
        var fallbackCharacters = new HashSet<uint>(fallback.characterTable.Select(character => character.unicode));

        uint[] missingCanonical = canonical
            .Where(codePoint => !primaryCharacters.Contains(codePoint) &&
                                !fallbackCharacters.Contains(codePoint)).ToArray();
        if (missingCanonical.Length > 0)
            throw MissingCharacters("Canonical coverage verification failed", missingCanonical);

        uint[] missingFallback = required.Where(codePoint => !fallbackCharacters.Contains(codePoint)).ToArray();
        if (missingFallback.Length > 0)
            throw MissingCharacters("Fallback coverage verification failed", missingFallback);
        if (fallbackCharacters.Contains(UnsupportedStar) || fallbackCharacters.Contains(UnsupportedBackspace))
            throw new InvalidOperationException("Fallback contains a deferred unsupported symbol.");
    }

    static uint[] RequiredFallbackCodePoints(TMP_FontAsset primary, IEnumerable<uint> canonical)
    {
        var primaryCharacters = new HashSet<uint>(primary.characterTable.Select(character => character.unicode));
        return canonical.Where(codePoint => !primaryCharacters.Contains(codePoint)).OrderBy(value => value).ToArray();
    }

    static IDictionary LocalizationTable()
    {
        Type l10n = Type.GetType("L10n, Assembly-CSharp");
        FieldInfo field = l10n == null ? null : l10n.GetField("Table",
            BindingFlags.NonPublic | BindingFlags.Static);
        if (field == null || !(field.GetValue(null) is IDictionary table))
            throw new InvalidOperationException("Could not read L10n.Table.");
        return table;
    }

    static void AddCodePoints(ISet<uint> destination, string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        for (int i = 0; i < text.Length; i++)
        {
            int codePoint = char.ConvertToUtf32(text, i);
            if (char.IsHighSurrogate(text[i])) i++;
            if (codePoint == '\r' || codePoint == '\n' || codePoint == '\t') continue;
            destination.Add((uint)codePoint);
        }
    }

    static Exception MissingCharacters(string prefix, IEnumerable<uint> missing)
    {
        uint[] values = missing == null ? new uint[0] : missing.OrderBy(value => value).ToArray();
        return new InvalidOperationException(prefix + ": " + CodePointList(values));
    }

    static string CodePointList(IEnumerable<uint> codePoints)
    {
        return string.Join(" ", codePoints.Select(value => "U+" + value.ToString("X4")).ToArray());
    }
}
