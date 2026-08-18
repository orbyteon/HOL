using NUnit.Framework;
using UnityEngine;

public sealed class MainMenuProductionAssetsTests
{
    [TestCase("reference/hol_logo_exact")]
    [TestCase("reference/mascot_6_exact")]
    [TestCase("reference/mascot_7_exact")]
    [TestCase("reference/char_boy_exact")]
    [TestCase("reference/char_girl_exact")]
    [TestCase("mainmenu/mainmenu_bg_stairs_clouds")]
    [TestCase("mainmenu/mainmenu_deco_stars")]
    [TestCase("mainmenu/mainmenu_deco_lightning")]
    [TestCase("mainmenu/mainmenu_deco_confetti")]
    [TestCase("mainmenu/mainmenu_deco_numbers")]
    [TestCase("mainmenu/mainmenu_cta_gold_9s")]
    [TestCase("mainmenu/mainmenu_cta_blue_9s")]
    [TestCase("mainmenu/mainmenu_cta_magenta_9s")]
    [TestCase("mainmenu/mainmenu_player_chip_frame_9s")]
    [TestCase("mainmenu/mainmenu_tip_frame_9s")]
    [TestCase("mainmenu/mainmenu_gear_glossy")]
    [TestCase("mainmenu/mainmenu_icon_solo")]
    [TestCase("mainmenu/mainmenu_icon_private_room")]
    [TestCase("mainmenu/mainmenu_icon_daily_hunt")]
    [TestCase("mainmenu/mainmenu_icon_streak")]
    [TestCase("mainmenu/mainmenu_icon_tip_bulb")]
    public void HomeSpriteLoads(string path)
    {
        Assert.That(Resources.Load<Sprite>(path), Is.Not.Null,
            "Missing Resources/" + path);
    }

    [Test]
    public void BackgroundIsNativePortraitResolution()
    {
        Sprite sprite = Resources.Load<Sprite>("mainmenu/mainmenu_bg_stairs_clouds");
        Assert.That(sprite, Is.Not.Null);
        Assert.That(sprite.texture.width, Is.EqualTo(1080));
        Assert.That(sprite.texture.height, Is.EqualTo(1920));
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
    public void NeonArcadeBackgroundIsNotAHomeResource()
    {
        Assert.That(
            Resources.Load<Sprite>("mainmenu/mainmenu_bg_night_arcade"),
            Is.Null);
    }
}
