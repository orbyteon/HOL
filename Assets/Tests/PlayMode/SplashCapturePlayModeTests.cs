using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class SplashCapturePlayModeTests
{
    const BindingFlags StaticFlags =
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    const BindingFlags InstanceFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    [TestCase(true, true, "splash", true)]
    [TestCase(false, true, "splash", false)]
    [TestCase(true, false, "splash", false)]
    [TestCase(true, true, null, false)]
    [TestCase(true, true, "menu", false)]
    public void CaptureRequiresAndroidDevelopmentSplashIntent(
        bool android, bool development, string requestedScreen, bool expected)
    {
        var method = RuntimeType("SplashCaptureBootstrap").GetMethod(
            "ShouldCapture", StaticFlags);
        Assert.That(method, Is.Not.Null);

        bool actual = (bool)method.Invoke(
            null, new object[] { android, development, requestedScreen });

        Assert.That(actual, Is.EqualTo(expected));
    }

    [UnityTest]
    public IEnumerator NoCaptureIntentLeavesSplashStateAndTimingUntouched()
    {
        ResetCaptureState();
        bool hadLanguage = PlayerPrefs.HasKey("Language");
        int originalLanguage = PlayerPrefs.GetInt("Language", 0);
        PlayerPrefs.SetInt("Language", 1);
        PlayerPrefs.Save();

        try
        {
            object languageBefore = CurrentLanguage();
            yield return SceneManager.LoadSceneAsync("SplashScene", LoadSceneMode.Single);
            yield return null;

            var scene = SceneManager.GetActiveScene();
            var loader = FindInScene(scene, RuntimeType("SplashLoader"));
            Assert.That(loader, Is.Not.Null);
            ((MonoBehaviour)loader).CancelInvoke("LoadMenu");

            var waitTime = loader.GetType().GetField("waitTime", InstanceFlags);
            Assert.That(waitTime, Is.Not.Null);
            Assert.That((float)waitTime.GetValue(loader), Is.EqualTo(2.5f));
            Assert.That(ResolveTimeout(false, (float)waitTime.GetValue(loader)),
                Is.EqualTo(2.5f));

            var hierarchyBefore = HierarchyPaths(scene);
            InvokeInstallForScene(scene);
            yield return null;

            Assert.That(CaptureRequested(), Is.False);
            Assert.That(FindInScene(scene, RuntimeType("SplashCaptureBootstrap")), Is.Null);
            CollectionAssert.AreEqual(hierarchyBefore, HierarchyPaths(scene));
            Assert.That(PlayerPrefs.HasKey("Language"), Is.True);
            Assert.That(PlayerPrefs.GetInt("Language"), Is.EqualTo(1));
            Assert.That(CurrentLanguage(), Is.EqualTo(languageBefore));
        }
        finally
        {
            if (hadLanguage)
                PlayerPrefs.SetInt("Language", originalLanguage);
            else
                PlayerPrefs.DeleteKey("Language");
            PlayerPrefs.Save();
            ResetCaptureState();
        }
    }

    [Test]
    public void CaptureModeUsesThirtySecondEffectiveTimeout()
    {
        Assert.That(ResolveTimeout(true, 2.5f), Is.EqualTo(30f));
        Assert.That(ResolveTimeout(false, 2.5f), Is.EqualTo(2.5f));
    }

    [UnityTest]
    public IEnumerator CaptureMarkerIsLoggedOnceAndOnlyAfterDesignSettles()
    {
        ResetCaptureState();
        SetCaptureRequested(true);

        int markerCount = 0;
        bool markerObservedBeforeSettled = false;
        Component design = null;
        PropertyInfo settled = null;
        Application.LogCallback callback = (condition, stackTrace, type) =>
        {
            if (condition != "HOL_SPLASH_CAPTURE_READY") return;
            markerCount++;
            if (design == null || settled == null ||
                !(bool)settled.GetValue(design, null))
                markerObservedBeforeSettled = true;
        };
        Application.logMessageReceived += callback;

        try
        {
            yield return SceneManager.LoadSceneAsync("SplashScene", LoadSceneMode.Single);
            yield return null;

            var scene = SceneManager.GetActiveScene();
            var loader = FindInScene(scene, RuntimeType("SplashLoader"));
            Assert.That(loader, Is.Not.Null);
            Assert.That(ResolveTimeout(CaptureRequested(), 2.5f), Is.EqualTo(30f));
            ((MonoBehaviour)loader).CancelInvoke("LoadMenu");

            design = FindInScene(scene, RuntimeType("SplashDesign"));
            Assert.That(design, Is.Not.Null);
            settled = design.GetType().GetProperty("IsSettled", InstanceFlags);
            Assert.That(settled, Is.Not.Null);

            InvokeInstallForScene(scene);
            Assert.That(ComponentsInScene(scene, RuntimeType("SplashCaptureBootstrap")),
                Has.Count.EqualTo(1));

            float deadline = Time.realtimeSinceStartup + 2f;
            while (markerCount == 0 && Time.realtimeSinceStartup < deadline)
                yield return null;

            Assert.That(markerCount, Is.EqualTo(1));
            Assert.That(markerObservedBeforeSettled, Is.False);
            Assert.That((bool)settled.GetValue(design, null), Is.True);

            for (int i = 0; i < 5; i++)
                yield return null;
            Assert.That(markerCount, Is.EqualTo(1));
        }
        finally
        {
            Application.logMessageReceived -= callback;
            ResetCaptureState();
        }
    }

    [UnityTest]
    public IEnumerator LoadingMenuTwiceTransitionsOnlyOnce()
    {
        ResetCaptureState();
        yield return SceneManager.LoadSceneAsync("SplashScene", LoadSceneMode.Single);
        yield return null;

        var loader = FindInScene(SceneManager.GetActiveScene(), RuntimeType("SplashLoader"));
        Assert.That(loader, Is.Not.Null);
        ((MonoBehaviour)loader).CancelInvoke("LoadMenu");

        int mainMenuLoads = 0;
        UnityEngine.Events.UnityAction<Scene, LoadSceneMode> onLoaded = (scene, mode) =>
        {
            if (scene.name == "MainMenu") mainMenuLoads++;
        };
        SceneManager.sceneLoaded += onLoaded;
        try
        {
            var loadMenu = loader.GetType().GetMethod("LoadMenu", InstanceFlags);
            Assert.That(loadMenu, Is.Not.Null);
            loadMenu.Invoke(loader, null);
            loadMenu.Invoke(loader, null);
            yield return null;

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"));
            Assert.That(mainMenuLoads, Is.EqualTo(1));
        }
        finally
        {
            SceneManager.sceneLoaded -= onLoaded;
            ResetCaptureState();
        }
    }

    static float ResolveTimeout(bool captureRequested, float waitTime)
    {
        var method = RuntimeType("SplashLoader").GetMethod("ResolveTimeout", StaticFlags);
        Assert.That(method, Is.Not.Null);
        return (float)method.Invoke(null, new object[] { captureRequested, waitTime });
    }

    static bool CaptureRequested()
    {
        var property = RuntimeType("SplashCaptureBootstrap").GetProperty(
            "CaptureRequested", StaticFlags);
        Assert.That(property, Is.Not.Null);
        return (bool)property.GetValue(null, null);
    }

    static void SetCaptureRequested(bool value)
    {
        var property = RuntimeType("SplashCaptureBootstrap").GetProperty(
            "CaptureRequested", StaticFlags);
        Assert.That(property, Is.Not.Null);
        var setter = property.GetSetMethod(true);
        Assert.That(setter, Is.Not.Null);
        setter.Invoke(null, new object[] { value });
    }

    static void ResetCaptureState()
    {
        var type = RuntimeTypeOrNull("SplashCaptureBootstrap");
        if (type == null) return;
        var reset = type.GetMethod("ResetState", StaticFlags);
        Assert.That(reset, Is.Not.Null);
        reset.Invoke(null, null);
    }

    static void InvokeInstallForScene(Scene scene)
    {
        var method = RuntimeType("SplashCaptureBootstrap").GetMethod(
            "InstallForScene", StaticFlags);
        Assert.That(method, Is.Not.Null);
        method.Invoke(null, new object[] { scene, LoadSceneMode.Single });
    }

    static object CurrentLanguage()
    {
        var property = RuntimeType("L10n").GetProperty("Current", StaticFlags);
        Assert.That(property, Is.Not.Null);
        return property.GetValue(null, null);
    }

    static List<string> HierarchyPaths(Scene scene)
    {
        var paths = new List<string>();
        foreach (var root in scene.GetRootGameObjects())
            AddPaths(root.transform, root.name, paths);
        paths.Sort(StringComparer.Ordinal);
        return paths;
    }

    static void AddPaths(Transform parent, string path, List<string> paths)
    {
        paths.Add(path);
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            AddPaths(child, path + "/" + child.name, paths);
        }
    }

    static Component FindInScene(Scene scene, Type type)
    {
        var components = ComponentsInScene(scene, type);
        return components.Count == 0 ? null : components[0];
    }

    static List<Component> ComponentsInScene(Scene scene, Type type)
    {
        var found = new List<Component>();
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var component in root.GetComponentsInChildren(type, true))
                found.Add((Component)component);
        }
        return found;
    }

    static Type RuntimeType(string name)
    {
        var type = RuntimeTypeOrNull(name);
        Assert.That(type, Is.Not.Null, "Missing runtime component: " + name);
        return type;
    }

    static Type RuntimeTypeOrNull(string name)
    {
        return Type.GetType(name + ", Assembly-CSharp");
    }
}
