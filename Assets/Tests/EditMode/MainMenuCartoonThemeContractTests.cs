using System.IO;
using NUnit.Framework;
using UnityEngine;

public sealed class MainMenuCartoonThemeContractTests
{
    [Test]
    public void CanonicalCartoonThemeDocumentsExist()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        Assert.That(File.Exists(Path.Combine(projectRoot, "design", "cartoon-theme.md")), Is.True);
        Assert.That(File.Exists(Path.Combine(projectRoot, "Assets", "newdesign",
            "cartoon-theme-authority.md")), Is.True);
    }

    [TestCase("reference/hol_logo_exact")]
    [TestCase("reference/mascot_6_exact")]
    [TestCase("reference/mascot_7_exact")]
    [TestCase("phase2a/hol_neon_reference_bg_r3")]
    [TestCase("phase2a/hol_menu_boy_arms_crossed_r3")]
    [TestCase("phase2a/hol_menu_girl_forward_fist_r3")]
    [TestCase("phase2a/hol_cta_gold_r2_9s")]
    [TestCase("phase2a/hol_cta_blue_r2_9s")]
    [TestCase("phase2a/hol_cta_magenta_r2_9s")]
    [TestCase("phase2a/hol_player_chip_r2_9s")]
    [TestCase("phase2a/hol_tip_frame_r2_9s")]
    [TestCase("phase2a/hol_settings_gear_r2")]
    [TestCase("phase2a/hol_mode_private_r2")]
    [TestCase("phase2a/hol_mode_daily_r2")]
    public void CanonicalCartoonHomeAssetsLoad(string path)
    {
        Assert.That(Resources.Load<Sprite>(path), Is.Not.Null,
            "Missing canonical HOL Cartoon Theme resource: " + path);
    }

    [Test]
    public void LegacyCloudBackgroundIsNotCanonicalHomeBackground()
    {
        var canonical = Resources.Load<Sprite>("phase2a/hol_neon_reference_bg_r3");
        var legacy = Resources.Load<Sprite>("mainmenu/mainmenu_bg_stairs_clouds");
        Assert.That(canonical, Is.Not.Null);
        Assert.That(legacy, Is.Not.Null);
        Assert.That(canonical, Is.Not.SameAs(legacy));
    }
}
