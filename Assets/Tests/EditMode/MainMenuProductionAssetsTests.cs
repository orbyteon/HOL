using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class MainMenuProductionAssetsTests
{
    [TestCase("reference/hol_logo_exact")]
    [TestCase("reference/mascot_6_exact")]
    [TestCase("reference/mascot_7_exact")]
    [TestCase("reference/mascot_3_exact")]
    [TestCase("phase2a/hol_menu_boy_arms_crossed_r3")]
    [TestCase("phase2a/hol_menu_girl_forward_fist_r3")]
    [TestCase("solo/production/solo_background_v1")]
    [TestCase("solo/production/solo_decorations_v1")]
    [TestCase("solo/production/solo_player_card_shell_v1")]
    [TestCase("solo/production/solo_opponent_card_shell_v1")]
    [TestCase("solo/production/solo_prompt_ribbon_v1")]
    [TestCase("solo/production/solo_back_button_v1")]
    [TestCase("phase2a/hol_cta_gold_r2_9s")]
    [TestCase("phase2a/hol_cta_blue_r2_9s")]
    [TestCase("phase2a/hol_cta_magenta_r2_9s")]
    [TestCase("phase2a/hol_player_chip_r2_9s")]
    [TestCase("phase2a/hol_tip_frame_r2_9s")]
    [TestCase("phase2a/hol_settings_gear_r2")]
    [TestCase("phase2a/hol_mode_solo_r2")]
    [TestCase("phase2a/hol_mode_private_r2")]
    [TestCase("phase2a/hol_mode_daily_r2")]
    [TestCase("phase2a/hol_chevron_r2")]
    [TestCase("phase2a/hol_loading_track_r2_9s")]
    [TestCase("mainmenu/mainmenu_icon_streak")]
    [TestCase("mainmenu/mainmenu_icon_tip_bulb")]
    [TestCase("mainmenu/mainmenu_icon_daily_hunt")]
    [TestCase("cartoon/cartoon_speech_bubble_raster")]
    [TestCase("cartoon/cartoon_vs_burst_base_raster")]
    [TestCase("cartoon/cartoon_friend_base_raster")]
    [TestCase("cartoon/cartoon_radar_base_raster")]
    [TestCase("mainmenu/mainmenu_outer_frame_reference_v1")]
    [TestCase("mainmenu/mainmenu_daily_gift_reference_v1")]
    [TestCase("dailyhunt/production/daily_floor_portal")]
    public void HomeSpriteLoads(string path)
    {
        Assert.That(Resources.Load<Sprite>(path), Is.Not.Null,
            "Missing Resources/" + path);
    }

    [Test]
    public void VsAiBackgroundIsHighResolutionNineBySixteen()
    {
        Sprite sprite = Resources.Load<Sprite>(
            "solo/production/solo_background_v1");
        Assert.That(sprite, Is.Not.Null);
        Assert.That(sprite.texture.width, Is.GreaterThanOrEqualTo(900));
        Assert.That(sprite.texture.height, Is.GreaterThanOrEqualTo(1600));
        Assert.That((float)sprite.texture.width / sprite.texture.height,
            Is.EqualTo(9f / 16f).Within(0.002f));
    }

    [Test]
    public void MascotSixIsSquareReferenceArt()
    {
        Sprite sprite = Resources.Load<Sprite>("reference/mascot_6_exact");
        Assert.That(sprite, Is.Not.Null);
        Assert.That(sprite.texture.width, Is.EqualTo(1024));
        Assert.That(sprite.texture.height, Is.EqualTo(1024));
    }

    [Test]
    public void CurrentHomeAndPlayOwnersNeverLoadRejectedCloudBackground()
    {
        const string rejected = "mainmenu/mainmenu_bg_stairs_clouds";
        foreach (string typeName in new[] { "MainMenuHomeVisuals", "MainMenuPlayVisuals" })
        {
            Type type = RuntimeType(typeName);
            var field = type.GetField("LoadedResources",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, typeName + ".LoadedResources");
            var resources = field.GetValue(null) as string[];
            Assert.That(resources, Is.Not.Null);
            Assert.That(resources, Does.Not.Contain(rejected),
                typeName + " must not restore the rejected cloud/stairs background.");
            string approvedBackground =
                "solo/production/solo_background_v1";
            Assert.That(resources, Does.Contain(approvedBackground),
                typeName + " must use the accepted VS AI background.");
            Assert.That(resources, Does.Contain(
                    "solo/production/solo_player_card_shell_v1"),
                typeName + " must use the accepted cyan VS AI card shell.");
            Assert.That(resources, Does.Contain(
                    "solo/production/solo_opponent_card_shell_v1"),
                typeName + " must use the accepted magenta VS AI card shell.");
            Assert.That(resources, Does.Not.Contain(
                    "dailyhunt/v1/daily_action_guess_v1"),
                typeName + " must not restore the rejected long-button steer.");
            Assert.That(resources, Does.Not.Contain(
                    "dailyhunt/v1/daily_action_share_v1"),
                typeName + " must not restore the rejected long-button steer.");
            Assert.That(resources, Does.Not.Contain(
                    "dailyhunt/v1/daily_action_revive_v1"),
                typeName + " must not restore the rejected long-button steer.");
        }
    }

    [TestCase("phase2a/hol_menu_boy_arms_crossed_r3")]
    [TestCase("phase2a/hol_menu_girl_forward_fist_r3")]
    public void Revision3HeroPoseUsesNativeAlpha(string path)
    {
        Sprite sprite = Resources.Load<Sprite>(path);
        Assert.That(sprite, Is.Not.Null);
        Assert.That(sprite.texture.width, Is.GreaterThanOrEqualTo(1024));
        Assert.That(sprite.texture.height, Is.GreaterThanOrEqualTo(1024));
        Assert.That(sprite.texture.format.ToString(), Does.Not.Contain("RGB24"));
    }

    [Test]
    public void HomeAndPlayHubCopyDescribeOnlyRealModesInBothLanguages()
    {
        var l10n = RuntimeType("L10n");
        var original = l10n.GetProperty("Current").GetValue(null, null);
        try
        {
            AssertCopy(0,
                "Play", "Choose your game mode",
                "DAILY HUNT", "A new challenge every day, big rewards!",
                "CHOOSE A MODE", "What do you want to play?",
                "VS AI", "A number duel against the computer",
                "PLAY WITH A FRIEND", "Create or join a private room");
            AssertCopy(1,
                "Παίξε", "Διάλεξε τρόπο παιχνιδιού",
                "ΗΜΕΡΗΣΙΑ ΔΟΚΙΜΑΣΙΑ", "Πρόκληση κάθε μέρα, μεγάλα έπαθλα!",
                "ΔΙΑΛΕΞΕ ΤΡΟΠΟ", "Τι θέλεις να παίξεις;",
                "ΕΝΑΝΤΙΟΝ AI", "Μονομαχία αριθμών με τον υπολογιστή",
                "ΠΑΙΞΕ ΜΕ ΦΙΛΟ", "Δημιούργησε ή μπες σε ιδιωτικό δωμάτιο");
        }
        finally
        {
            SetLanguage(original);
        }
    }

    static void AssertCopy(int language, params string[] expected)
    {
        string[] keys =
        {
            "play", "home_play_subtitle",
            "home_daily_title", "home_daily_subtitle",
            "play_hub_title", "play_hub_subtitle",
            "play_hub_solo_title", "play_hub_solo_subtitle",
            "play_hub_friend_title", "play_hub_friend_subtitle"
        };
        Assert.That(expected.Length, Is.EqualTo(keys.Length));
        SetLanguage(Enum.ToObject(RuntimeType("L10n").GetNestedType("Language"), language));
        for (int i = 0; i < keys.Length; i++)
            Assert.That(GetCopy(keys[i]), Is.EqualTo(expected[i]), keys[i]);
    }

    static string GetCopy(string key)
    {
        return (string)RuntimeType("L10n").GetMethod("Get")
            .Invoke(null, new object[] { key, new object[0] });
    }

    static void SetLanguage(object language)
    {
        RuntimeType("L10n").GetMethod("SetLanguage")
            .Invoke(null, new[] { language });
    }

    static Type RuntimeType(string name)
    {
        var type = Type.GetType(name + ", Assembly-CSharp");
        Assert.That(type, Is.Not.Null, name);
        return type;
    }
}
