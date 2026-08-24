using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public static class CartoonThemeBaker
{
    const string Root = "Assets/newdesign/Resources/Themes/Cartoon";
    const string FontRoot = Root + "/Fonts";
    const string CatalogPath = Root + "/CartoonThemeCatalog.asset";

    sealed class FontRecipe
    {
        public readonly string source;
        public readonly string output;
        public readonly bool fullCharacterSet;

        public FontRecipe(string source, string output,
            bool fullCharacterSet)
        {
            this.source = source;
            this.output = output;
            this.fullCharacterSet = fullCharacterSet;
        }
    }

    static readonly FontRecipe[] Recipes =
    {
        new FontRecipe("Montserrat-ExtraBold.ttf",
            "Cartoon Montserrat ExtraBold SDF.asset", false),
        new FontRecipe("Montserrat-Bold.ttf",
            "Cartoon Montserrat Bold SDF.asset", false),
        new FontRecipe("PlusJakartaSans-SemiBold.ttf",
            "Cartoon Plus Jakarta Sans SemiBold SDF.asset", false),
        new FontRecipe("PlusJakartaSans-Medium.ttf",
            "Cartoon Plus Jakarta Sans Medium SDF.asset", false),
        new FontRecipe("PlusJakartaSans-Regular.ttf",
            "Cartoon Plus Jakarta Sans Regular SDF.asset", false),
        new FontRecipe("NotoSans-ExtraBold.ttf",
            "Cartoon Noto Sans ExtraBold SDF.asset", true),
        new FontRecipe("NotoSans-Bold.ttf",
            "Cartoon Noto Sans Bold SDF.asset", true),
        new FontRecipe("NotoSans-SemiBold.ttf",
            "Cartoon Noto Sans SemiBold SDF.asset", true),
        new FontRecipe("NotoSans-Medium.ttf",
            "Cartoon Noto Sans Medium SDF.asset", true),
        new FontRecipe("NotoSans-Regular.ttf",
            "Cartoon Noto Sans Regular SDF.asset", true)
    };

    [MenuItem("HOL/Cartoon/Rebuild Theme Catalog And Fonts")]
    public static void RebuildAll()
    {
        Directory.CreateDirectory(FontRoot);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        var built = new Dictionary<string, TMP_FontAsset>();
        foreach (var recipe in Recipes)
            built[recipe.output] = BuildFont(recipe);

        var liberation = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Resources/Fonts & Materials/" +
            "LiberationSans SDF - Fallback.asset");
        ConfigureFallbacks(built, liberation);
        ValidateCombinedCoverage(built);
        BuildCatalog(built);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Debug.Log("[CartoonThemeBaker] Rebuilt catalog and ten static fonts.");
    }

    public static void RebuildAllBatch()
    {
        try
        {
            RebuildAll();
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    static TMP_FontAsset BuildFont(FontRecipe recipe)
    {
        string sourcePath = FontRoot + "/" + recipe.source;
        string outputPath = FontRoot + "/" + recipe.output;
        var source = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
        if (source == null)
            throw new InvalidOperationException("Missing source font: " +
                                                sourcePath);

        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(outputPath) != null)
            AssetDatabase.DeleteAsset(outputPath);

        var asset = TMP_FontAsset.CreateFontAsset(source, 90, 9,
            GlyphRenderMode.SDFAA, 4096, 4096,
            AtlasPopulationMode.Dynamic, false);
        if (asset == null)
            throw new InvalidOperationException("TMP failed to create " +
                                                outputPath);

        asset.name = Path.GetFileNameWithoutExtension(recipe.output);
        string characters = recipe.fullCharacterSet
            ? CartoonCharacterSet.Build()
            : CartoonCharacterSet.BuildEnglish();
        string missing;
        asset.TryAddCharacters(characters, out missing, true);
        if (!string.IsNullOrEmpty(missing))
            Debug.Log("[CartoonThemeBaker] " + recipe.source +
                      " delegates to static fallbacks: " + missing);

        asset.atlasPopulationMode = AtlasPopulationMode.Static;
        AssetDatabase.CreateAsset(asset, outputPath);
        if (asset.material != null)
        {
            asset.material.name = asset.name + " Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);
        }

        foreach (var texture in asset.atlasTextures)
        {
            if (texture == null) continue;
            texture.name = asset.name + " Atlas";
            AssetDatabase.AddObjectToAsset(texture, asset);
        }

        EditorUtility.SetDirty(asset);
        AssetDatabase.ImportAsset(outputPath,
            ImportAssetOptions.ForceSynchronousImport);
        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(outputPath);
    }

    static void ConfigureFallbacks(
        Dictionary<string, TMP_FontAsset> fonts,
        TMP_FontAsset liberation)
    {
        TMP_FontAsset notoExtra = Get(fonts,
            "Cartoon Noto Sans ExtraBold SDF.asset");
        TMP_FontAsset notoBold = Get(fonts,
            "Cartoon Noto Sans Bold SDF.asset");
        TMP_FontAsset notoSemi = Get(fonts,
            "Cartoon Noto Sans SemiBold SDF.asset");
        TMP_FontAsset notoMedium = Get(fonts,
            "Cartoon Noto Sans Medium SDF.asset");
        TMP_FontAsset notoRegular = Get(fonts,
            "Cartoon Noto Sans Regular SDF.asset");

        SetFallbacks(Get(fonts,
            "Cartoon Montserrat ExtraBold SDF.asset"), notoExtra, liberation);
        SetFallbacks(Get(fonts,
            "Cartoon Montserrat Bold SDF.asset"), notoBold, liberation);
        SetFallbacks(Get(fonts,
            "Cartoon Plus Jakarta Sans SemiBold SDF.asset"), notoSemi,
            liberation);
        SetFallbacks(Get(fonts,
            "Cartoon Plus Jakarta Sans Medium SDF.asset"), notoMedium,
            liberation);
        SetFallbacks(Get(fonts,
            "Cartoon Plus Jakarta Sans Regular SDF.asset"), notoRegular,
            liberation);

        SetFallbacks(notoExtra, liberation);
        SetFallbacks(notoBold, liberation);
        SetFallbacks(notoSemi, liberation);
        SetFallbacks(notoMedium, liberation);
        SetFallbacks(notoRegular, liberation);
    }

    static void ValidateCombinedCoverage(
        Dictionary<string, TMP_FontAsset> fonts)
    {
        string canonical = CartoonCharacterSet.Build();
        foreach (var pair in fonts)
        {
            foreach (char character in canonical)
            {
                if (!pair.Value.HasCharacter(character, true, false))
                    throw new InvalidOperationException(pair.Key +
                        " fallback chain is missing U+" +
                        ((int)character).ToString("X4") + " " + character);
            }
        }
    }

    static TMP_FontAsset Get(Dictionary<string, TMP_FontAsset> fonts,
        string name)
    {
        TMP_FontAsset value;
        if (!fonts.TryGetValue(name, out value) || value == null)
            throw new InvalidOperationException("Missing baked font " + name);
        return value;
    }

    static void SetFallbacks(TMP_FontAsset asset,
        params TMP_FontAsset[] fallbacks)
    {
        if (asset.fallbackFontAssetTable == null)
            asset.fallbackFontAssetTable = new List<TMP_FontAsset>();
        else
            asset.fallbackFontAssetTable.Clear();
        foreach (var fallback in fallbacks)
            if (fallback != null && fallback != asset &&
                !asset.fallbackFontAssetTable.Contains(fallback))
                asset.fallbackFontAssetTable.Add(fallback);
        asset.atlasPopulationMode = AtlasPopulationMode.Static;
        EditorUtility.SetDirty(asset);
    }

    static void BuildCatalog(Dictionary<string, TMP_FontAsset> fonts)
    {
        var catalog = AssetDatabase.LoadAssetAtPath<CartoonThemeCatalog>(
            CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<CartoonThemeCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }

        catalog.typography.montserratExtraBold = Get(fonts,
            "Cartoon Montserrat ExtraBold SDF.asset");
        catalog.typography.montserratBold = Get(fonts,
            "Cartoon Montserrat Bold SDF.asset");
        catalog.typography.plusJakartaSemiBold = Get(fonts,
            "Cartoon Plus Jakarta Sans SemiBold SDF.asset");
        catalog.typography.plusJakartaMedium = Get(fonts,
            "Cartoon Plus Jakarta Sans Medium SDF.asset");
        catalog.typography.plusJakartaRegular = Get(fonts,
            "Cartoon Plus Jakarta Sans Regular SDF.asset");
        catalog.typography.notoSansExtraBold = Get(fonts,
            "Cartoon Noto Sans ExtraBold SDF.asset");
        catalog.typography.notoSansBold = Get(fonts,
            "Cartoon Noto Sans Bold SDF.asset");
        catalog.typography.notoSansSemiBold = Get(fonts,
            "Cartoon Noto Sans SemiBold SDF.asset");
        catalog.typography.notoSansMedium = Get(fonts,
            "Cartoon Noto Sans Medium SDF.asset");
        catalog.typography.notoSansRegular = Get(fonts,
            "Cartoon Noto Sans Regular SDF.asset");

        catalog.shared.logo = Sprite("Assets/newdesign/Resources/reference/hol_logo_exact.png");
        catalog.shared.mascotSix = Sprite("Assets/newdesign/Resources/reference/mascot_6_exact.png");
        catalog.shared.mascotSeven = Sprite("Assets/newdesign/Resources/reference/mascot_7_exact.png");
        catalog.shared.mascotThree = Sprite("Assets/newdesign/Resources/reference/mascot_3_exact.png");
        catalog.shared.heroBoy = Sprite("Assets/newdesign/Resources/reference/char_boy_exact.png");
        catalog.shared.heroGirl = Sprite("Assets/newdesign/Resources/reference/char_girl_exact.png");
        catalog.shared.playerPortrait = Sprite("Assets/newdesign/Resources/reference/player_cyan_exact.png");
        catalog.shared.opponent = Sprite("Assets/newdesign/Resources/reference/opponent_purple_exact.png");
        catalog.shared.primaryButton = Sprite("Assets/newdesign/Resources/mainmenu/mainmenu_cta_gold_9s.png");
        catalog.shared.secondaryBlueButton = Sprite("Assets/newdesign/Resources/mainmenu/mainmenu_cta_blue_9s.png");
        catalog.shared.secondaryMagentaButton = Sprite("Assets/newdesign/Resources/mainmenu/mainmenu_cta_magenta_9s.png");
        catalog.shared.neutralPanel = Sprite("Assets/newdesign/Resources/mainmenu/mainmenu_tip_frame_9s.png");
        catalog.shared.playerChip = Sprite("Assets/newdesign/Resources/mainmenu/mainmenu_player_chip_frame_9s.png");
        catalog.shared.chevron = Sprite("Assets/newdesign/Resources/phase2a/hol_chevron_r2.png");

        catalog.splash.background = Sprite("Assets/newdesign/Resources/splash/splash_bg_stairs_clouds.png");
        catalog.splash.heroBoy = Sprite("Assets/newdesign/Resources/splash/splash_char_boy.png");
        catalog.splash.heroGirl = Sprite("Assets/newdesign/Resources/splash/splash_char_girl.png");
        catalog.splash.stars = Sprite("Assets/newdesign/Resources/splash/splash_deco_stars.png");
        catalog.splash.lightning = Sprite("Assets/newdesign/Resources/splash/splash_deco_lightning.png");
        catalog.splash.confetti = Sprite("Assets/newdesign/Resources/splash/splash_deco_confetti.png");
        catalog.splash.numbers = Sprite("Assets/newdesign/Resources/splash/splash_deco_numbers.png");
        catalog.splash.logoGlow = Sprite("Assets/newdesign/Resources/splash/splash_logo_glow.png");
        catalog.splash.loadingTrack = Sprite("Assets/newdesign/Resources/phase2a/hol_loading_track_r2_9s.png");

        catalog.home.background = Sprite("Assets/newdesign/Resources/phase2a/hol_neon_reference_bg_r3.png");
        catalog.home.heroBoy = Sprite("Assets/newdesign/Resources/phase2a/hol_menu_boy_arms_crossed_r3.png");
        catalog.home.heroGirl = Sprite("Assets/newdesign/Resources/phase2a/hol_menu_girl_forward_fist_r3.png");
        catalog.home.settingsGear = Sprite("Assets/newdesign/Resources/phase2a/hol_settings_gear_r2.png");
        catalog.home.soloIcon = Sprite("Assets/newdesign/Resources/phase2a/hol_mode_solo_r2.png");
        catalog.home.privateRoomIcon = Sprite("Assets/newdesign/Resources/phase2a/hol_mode_private_r2.png");
        catalog.home.dailyHuntIcon = Sprite("Assets/newdesign/Resources/phase2a/hol_mode_daily_r2.png");
        catalog.home.streakIcon = Sprite("Assets/newdesign/Resources/mainmenu/mainmenu_icon_streak.png");
        catalog.home.tipIcon = Sprite("Assets/newdesign/Resources/mainmenu/mainmenu_icon_tip_bulb.png");

        catalog.settings.background = Sprite("Assets/newdesign/Resources/settings/hol_settings_bg_r1.png");
        catalog.settings.playerIcon = Sprite("Assets/newdesign/Resources/settings/settings_icon_player_3d.png");
        catalog.settings.languageIcon = Sprite("Assets/newdesign/Resources/settings/settings_icon_language_3d.png");
        catalog.settings.musicIcon = Sprite("Assets/newdesign/Resources/settings/settings_icon_music_3d.png");
        catalog.settings.difficultyIcon = Sprite("Assets/newdesign/Resources/settings/settings_icon_difficulty_3d.png");
        catalog.settings.privacyIcon = Sprite("Assets/newdesign/Resources/settings/settings_icon_privacy_3d.png");
        catalog.settings.blueButton = Sprite("Assets/newdesign/Resources/mainmenu/mainmenu_cta_blue_9s.png");
        catalog.settings.goldButton = Sprite("Assets/newdesign/Resources/mainmenu/mainmenu_cta_gold_9s.png");
        catalog.settings.neutralButton = Sprite("Assets/newdesign/Resources/mainmenu/mainmenu_tip_frame_9s.png");
        catalog.settings.playerChip = Sprite("Assets/newdesign/Resources/mainmenu/mainmenu_player_chip_frame_9s.png");
        catalog.settings.chevron = Sprite("Assets/newdesign/Resources/phase2a/hol_chevron_r2.png");

        EditorUtility.SetDirty(catalog);
        HolTheme.ResetCache();
    }

    static Sprite Sprite(string path)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            throw new InvalidOperationException("Missing approved sprite: " +
                                                path);
        return sprite;
    }
}
