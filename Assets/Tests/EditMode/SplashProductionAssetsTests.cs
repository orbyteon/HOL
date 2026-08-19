using NUnit.Framework;
using UnityEngine;

public sealed class SplashProductionAssetsTests
{
    [TestCase("reference/hol_logo_exact")]
    [TestCase("reference/mascot_6_exact")]
    [TestCase("reference/mascot_7_exact")]
    [TestCase("splash/splash_bg_stairs_clouds")]
    [TestCase("splash/splash_logo_glow")]
    [TestCase("splash/splash_deco_stars")]
    [TestCase("splash/splash_deco_lightning")]
    [TestCase("splash/splash_deco_confetti")]
    [TestCase("splash/splash_deco_numbers")]
    [TestCase("splash/splash_char_boy")]
    [TestCase("splash/splash_char_girl")]
    public void SplashSpriteLoads(string path)
    {
        Assert.That(Resources.Load<Sprite>(path), Is.Not.Null,
            "Missing Resources/" + path);
    }

    [Test]
    public void BackgroundIsNativePortraitResolution()
    {
        Sprite sprite = Resources.Load<Sprite>("splash/splash_bg_stairs_clouds");
        Assert.That(sprite.texture.width, Is.EqualTo(1080));
        Assert.That(sprite.texture.height, Is.EqualTo(1920));
    }

    [Test]
    public void LogoGlowIsNativeOverlayResolution()
    {
        Sprite sprite = Resources.Load<Sprite>("splash/splash_logo_glow");
        Assert.That(sprite.texture.width, Is.EqualTo(960));
        Assert.That(sprite.texture.height, Is.EqualTo(620));
    }

    [Test]
    public void MascotSixIsSquareReferenceArt()
    {
        Sprite sprite = Resources.Load<Sprite>("reference/mascot_6_exact");
        Assert.That(sprite.texture.width, Is.EqualTo(1024));
        Assert.That(sprite.texture.height, Is.EqualTo(1024));
    }
}
