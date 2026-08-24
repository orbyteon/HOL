using System;
using System.Reflection;
using NUnit.Framework;

// Reflection keeps this asmdef decoupled from Unity's predefined
// Assembly-CSharp assembly while still locking the development-capture seam.
public sealed class DailyHuntCaptureBootstrapTests
{
    [TestCase(true, true, "dailyhunt", true)]
    [TestCase(false, true, "dailyhunt", false)]
    [TestCase(true, false, "dailyhunt", false)]
    [TestCase(true, true, "mainmenu", false)]
    [TestCase(true, true, "DailyHunt", false)]
    [TestCase(true, true, null, false)]
    public void CaptureRequiresAndroidDevelopmentAndExactScreen(
        bool android,
        bool development,
        string requestedScreen,
        bool expected)
    {
        Assert.That(
            Invoke<bool>("ShouldCapture", android, development, requestedScreen),
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
            Invoke<string>("NormalizeLanguage", requested),
            Is.EqualTo(expected));
    }

    static T Invoke<T>(string methodName, params object[] arguments)
    {
        Type type = Type.GetType(
            "DailyHuntCaptureBootstrap, Assembly-CSharp");
        Assert.That(type, Is.Not.Null,
            "DailyHuntCaptureBootstrap must compile into Assembly-CSharp.");

        MethodInfo method = type.GetMethod(
            methodName,
            BindingFlags.Public | BindingFlags.Static);
        Assert.That(method, Is.Not.Null, methodName);
        return (T)method.Invoke(null, arguments);
    }
}
