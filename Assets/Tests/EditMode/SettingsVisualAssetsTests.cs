using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class SettingsVisualAssetsTests
{
    [Test]
    public void SettingsBackgroundIsNativePortraitSprite()
    {
        var sprite = Resources.Load<Sprite>("settings/hol_settings_bg_r1");
        Assert.That(sprite, Is.Not.Null);
        Assert.That(sprite.texture.width, Is.GreaterThanOrEqualTo(900));
        Assert.That(sprite.texture.height, Is.GreaterThanOrEqualTo(1500));
        Assert.That((float)sprite.texture.height / sprite.texture.width,
            Is.EqualTo(16f / 9f).Within(0.02f));
    }

    [Test]
    public void SettingsRowIconsAreNativeTransparentSprites()
    {
        string[] resources =
        {
            "settings/settings_icon_player_3d",
            "settings/settings_icon_language_3d",
            "settings/settings_icon_music_3d",
            "settings/settings_icon_difficulty_3d",
            "settings/settings_icon_privacy_3d"
        };
        foreach (string resource in resources)
        {
            var sprite = Resources.Load<Sprite>(resource);
            Assert.That(sprite, Is.Not.Null, "Missing Settings icon: " + resource);
            Assert.That(sprite.texture.width, Is.GreaterThanOrEqualTo(1200), resource);
            Assert.That(sprite.texture.height, Is.EqualTo(sprite.texture.width), resource);
            Assert.That(sprite.texture.alphaIsTransparency, Is.True,
                resource + " must retain its transparent edge pixels.");
        }
    }

    [Test]
    public void ApprovedProductionUiSpritesHonorTheImportAndNineSliceContract()
    {
        string[] paths =
        {
            "Assets/newdesign/Resources/mainmenu/mainmenu_cta_blue_9s.png",
            "Assets/newdesign/Resources/mainmenu/mainmenu_cta_gold_9s.png",
            "Assets/newdesign/Resources/mainmenu/mainmenu_tip_frame_9s.png",
            "Assets/newdesign/Resources/mainmenu/mainmenu_player_chip_frame_9s.png"
        };
        foreach (string path in paths)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.That(importer, Is.Not.Null, "Missing production importer: " + path);
            Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), path);
            Assert.That(importer.sRGBTexture, Is.True, path);
            Assert.That(importer.alphaIsTransparency, Is.True, path);
            Assert.That(importer.mipmapEnabled, Is.False, path);
            Assert.That(importer.maxTextureSize, Is.GreaterThanOrEqualTo(1024), path);
            Assert.That(importer.spriteBorder.sqrMagnitude, Is.GreaterThan(0f),
                path + " requires approved nine-slice borders.");

            var android = importer.GetPlatformTextureSettings("Android");
            Assert.That(android.maxTextureSize, Is.GreaterThanOrEqualTo(1024), path);
            Assert.That(android.compressionQuality, Is.GreaterThanOrEqualTo(50), path);
            Assert.That(android.crunchedCompression, Is.False, path);
        }
    }

    [Test]
    public void RepositoryDeclaresMandatoryProductionUiAssetFidelityContract()
    {
        string repositoryRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string agents = File.ReadAllText(Path.Combine(repositoryRoot, "AGENTS.md"));
        StringAssert.Contains("HOL Production UI Asset Fidelity Contract — Mandatory", agents);
        StringAssert.Contains("alpha `1`", agents);
        StringAssert.Contains("`Image.Type.Sliced`", agents);
        StringAssert.Contains("callback preservation", agents);
        StringAssert.Contains("absence of a procedural replacement", agents);
    }

    [Test]
    public void SettingsUsesOneCurrentProductionOwnerAndRetiredProceduralTypesAreAbsent()
    {
        Assert.That(RuntimeType("SettingsVisuals"), Is.Not.Null);
        Assert.That(RuntimeType("SettingsButtonFeedback"), Is.Not.Null,
            "Additive pressed-state feedback remains part of the current Settings owner.");

        foreach (string retiredType in new[]
        {
            "SettingsSurfaceGraphic",
            "SettingsIconGraphic",
            "SettingsToggleGraphic"
        })
        {
            Assert.That(RuntimeType(retiredType), Is.Null,
                "Retired procedural Settings type returned: " + retiredType);
        }
    }

    [Test]
    public void ApprovedSettingsCopyHasEnglishAndGreekEntries()
    {
        var l10n = RuntimeType("L10n");
        var tableField = l10n.GetField("Table",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(tableField, Is.Not.Null);
        var table = (IDictionary)tableField.GetValue(null);
        string[] keys =
        {
            "settings_title_display", "settings_player_name",
            "settings_language", "settings_music",
            "settings_ai_difficulty", "settings_ads_privacy",
            "settings_change_display", "settings_save_display",
            "language_english", "language_greek"
        };
        foreach (string key in keys)
        {
            Assert.That(table.Contains(key), Is.True, "Missing L10n key: " + key);
            var pair = (string[])table[key];
            Assert.That(pair.Length, Is.EqualTo(2));
            Assert.That(pair[0], Is.Not.Empty, key + " EN is empty.");
            Assert.That(pair[1], Is.Not.Empty, key + " EL is empty.");
        }
    }

    static System.Type RuntimeType(string name)
    {
        return System.Type.GetType(name + ", Assembly-CSharp");
    }
}
