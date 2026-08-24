using NUnit.Framework;

public sealed class SoloSearchCaptureBootstrapTests
{
    [TestCase(true, true, "solosearch", true)]
    [TestCase(false, true, "solosearch", false)]
    [TestCase(true, false, "solosearch", false)]
    [TestCase(true, true, "mainmenu", false)]
    [TestCase(true, true, null, false)]
    public void CaptureRequiresAndroidDevelopmentAndExactScreen(
        bool android,
        bool development,
        string requestedScreen,
        bool expected)
    {
        Assert.That(
            SoloSearchCaptureBootstrap.ShouldCapture(
                android, development, requestedScreen),
            Is.EqualTo(expected));
    }

    [TestCase("el", "el")]
    [TestCase("EL", "el")]
    [TestCase("en", "en")]
    [TestCase("fr", "en")]
    [TestCase(null, "en")]
    public void CaptureLanguageIsRestrictedToSupportedLocales(
        string requested,
        string expected)
    {
        Assert.That(
            SoloSearchCaptureBootstrap.NormalizeLanguage(requested),
            Is.EqualTo(expected));
    }
}
