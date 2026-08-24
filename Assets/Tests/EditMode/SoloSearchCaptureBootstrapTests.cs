using System;
using System.Reflection;
using NUnit.Framework;

public sealed class SoloSearchCaptureBootstrapTests
{
    static Type BootstrapType
    {
        get
        {
            Type type = Type.GetType(
                "SoloSearchCaptureBootstrap, Assembly-CSharp");
            Assert.That(type, Is.Not.Null,
                "SoloSearchCaptureBootstrap must compile into Assembly-CSharp.");
            return type;
        }
    }

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
        object value = Invoke(
            "ShouldCapture", android, development, requestedScreen);
        Assert.That(value, Is.TypeOf<bool>());
        Assert.That((bool)value, Is.EqualTo(expected));
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
        object value = Invoke("NormalizeLanguage", requested);
        Assert.That(value, Is.TypeOf<string>());
        Assert.That((string)value, Is.EqualTo(expected));
    }

    static object Invoke(string methodName, params object[] arguments)
    {
        MethodInfo method = BootstrapType.GetMethod(
            methodName,
            BindingFlags.Public | BindingFlags.Static);
        Assert.That(method, Is.Not.Null, methodName);
        return method.Invoke(null, arguments);
    }
}
