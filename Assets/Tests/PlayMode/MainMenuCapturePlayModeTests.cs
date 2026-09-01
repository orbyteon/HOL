using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class MainMenuCapturePlayModeTests
{
    const BindingFlags StaticFlags =
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    const BindingFlags InstanceFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    [TestCase(true, true, "mainmenu", true)]
    [TestCase(false, true, "mainmenu", false)]
    [TestCase(true, false, "mainmenu", false)]
    [TestCase(true, true, null, false)]
    [TestCase(true, true, "splash", false)]
    [TestCase(true, true, "menu", false)]
    public void CaptureRequiresAndroidDevelopmentMainMenuIntent(
        bool android, bool development, string requestedScreen, bool expected)
    {
        var method = RuntimeType("MainMenuCaptureBootstrap").GetMethod(
            "ShouldCapture", StaticFlags);
        Assert.That(method, Is.Not.Null);

        bool actual = (bool)method.Invoke(
            null, new object[] { android, development, requestedScreen });

        Assert.That(actual, Is.EqualTo(expected));
    }

    [UnityTest]
    public IEnumerator NoCaptureIntentLeavesMainMenuHierarchyUntouched()
    {
        ResetCaptureState();
        InvokeHomeInstaller();
        yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
        for (int i = 0; i < 16; i++)
            yield return null;
        yield return new WaitForSecondsRealtime(0.35f);

        var scene = SceneManager.GetActiveScene();
        var hierarchyBefore = HierarchyPaths(scene);
        InvokeInstallForScene(scene);
        yield return null;

        Assert.That(CaptureRequested(), Is.False);
        Assert.That(FindInScene(scene, RuntimeType("MainMenuCaptureBootstrap")), Is.Null);
        CollectionAssert.AreEqual(hierarchyBefore, HierarchyPaths(scene));
        ResetCaptureState();
    }

    [UnityTest]
    public IEnumerator CaptureIteratorHasTwoBarriersAndLogsExactlyOnceWhenAdvanced()
    {
        ResetCaptureState();
        SetCaptureRequested(true);
        InvokeHomeInstaller();

        Component owner = null;
        Component bootstrap = null;
        PropertyInfo settled = null;
        PropertyInfo ready = null;
        bool previousIgnoreFailingMessages = LogAssert.ignoreFailingMessages;

        try
        {
            LogAssert.ignoreFailingMessages = true;
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            for (int i = 0; i < 16; i++)
                yield return null;
            yield return new WaitForSecondsRealtime(0.35f);

            var scene = SceneManager.GetActiveScene();
            owner = FindInScene(scene, RuntimeType("MainMenuHomeVisuals"));
            Assert.That(owner, Is.Not.Null);
            settled = owner.GetType().GetProperty("IsSettled", InstanceFlags);
            ready = owner.GetType().GetProperty("IsReady", InstanceFlags);
            Assert.That(settled, Is.Not.Null);
            Assert.That(ready, Is.Not.Null);

            float settleDeadline = Time.realtimeSinceStartup + 2f;
            while ((!(bool)ready.GetValue(owner, null) ||
                    !(bool)settled.GetValue(owner, null)) &&
                   Time.realtimeSinceStartup < settleDeadline)
                yield return null;
            Assert.That((bool)ready.GetValue(owner, null), Is.True);
            Assert.That((bool)settled.GetValue(owner, null), Is.True);

            InvokeInstallForScene(scene);
            InvokeInstallForScene(scene);
            var bootstraps = ComponentsInScene(
                scene, RuntimeType("MainMenuCaptureBootstrap"));
            Assert.That(bootstraps, Has.Count.EqualTo(1));
            bootstrap = bootstraps[0];
            var markerLogged = bootstrap.GetType().GetField(
                "markerLogged", StaticFlags);
            Assert.That(markerLogged, Is.Not.Null);
            Assert.That((bool)markerLogged.GetValue(null), Is.False);

            var bootstrapBehaviour = (MonoBehaviour)bootstrap;
            bootstrapBehaviour.StopAllCoroutines();
            bootstrapBehaviour.enabled = false;

            var presentationBarriers = bootstrap.GetType().GetField(
                "presentationBarriersPassed", InstanceFlags);
            Assert.That(presentationBarriers, Is.Not.Null);
            var waitStarted = bootstrap.GetType().GetField(
                "presentationWaitStarted", InstanceFlags);
            Assert.That(waitStarted, Is.Not.Null);
            var routineMethod = bootstrap.GetType().GetMethod(
                "LogReadyAfterPresentation", InstanceFlags);
            Assert.That(routineMethod, Is.Not.Null);

            presentationBarriers.SetValue(bootstrap, 0);
            waitStarted.SetValue(bootstrap, false);
            var ownerField = bootstrap.GetType().GetField(
                "homeVisuals", InstanceFlags);
            Assert.That(ownerField, Is.Not.Null);
            ownerField.SetValue(bootstrap, owner);
            var routine = (IEnumerator)routineMethod.Invoke(bootstrap, null);
            AssertEndOfFrame(routine, presentationBarriers, bootstrap, 0);
            AssertEndOfFrame(routine, presentationBarriers, bootstrap, 1);
            Assert.That((bool)markerLogged.GetValue(null), Is.False);
            LogAssert.Expect(LogType.Log, "HOL_MAINMENU_CAPTURE_READY");
            Assert.That(routine.MoveNext(), Is.False);
            Assert.That((int)presentationBarriers.GetValue(bootstrap), Is.EqualTo(2));
            Assert.That((bool)markerLogged.GetValue(null), Is.True);

            var duplicateRoutine =
                (IEnumerator)routineMethod.Invoke(bootstrap, null);
            AssertEndOfFrame(
                duplicateRoutine, presentationBarriers, bootstrap, 2);
            AssertEndOfFrame(
                duplicateRoutine, presentationBarriers, bootstrap, 3);
            Assert.That(duplicateRoutine.MoveNext(), Is.False);
            Assert.That((bool)markerLogged.GetValue(null), Is.True);
        }
        finally
        {
            LogAssert.ignoreFailingMessages = previousIgnoreFailingMessages;
            if (bootstrap != null)
            {
                ((MonoBehaviour)bootstrap).StopAllCoroutines();
                ((MonoBehaviour)bootstrap).enabled = false;
            }
            ResetCaptureState();
        }
    }

    static void AssertEndOfFrame(
        IEnumerator routine, FieldInfo barriers, Component bootstrap, int expectedCount)
    {
        Assert.That(routine.MoveNext(), Is.True);
        Assert.That(routine.Current, Is.TypeOf<WaitForEndOfFrame>());
        Assert.That((int)barriers.GetValue(bootstrap), Is.EqualTo(expectedCount));
    }

    static bool CaptureRequested()
    {
        var property = RuntimeType("MainMenuCaptureBootstrap").GetProperty(
            "CaptureRequested", StaticFlags);
        Assert.That(property, Is.Not.Null);
        return (bool)property.GetValue(null, null);
    }

    static void SetCaptureRequested(bool value)
    {
        var property = RuntimeType("MainMenuCaptureBootstrap").GetProperty(
            "CaptureRequested", StaticFlags);
        Assert.That(property, Is.Not.Null);
        var setter = property.GetSetMethod(true);
        Assert.That(setter, Is.Not.Null);
        setter.Invoke(null, new object[] { value });
    }

    static void ResetCaptureState()
    {
        var type = RuntimeTypeOrNull("MainMenuCaptureBootstrap");
        if (type == null) return;
        var reset = type.GetMethod("ResetState", StaticFlags);
        Assert.That(reset, Is.Not.Null);
        reset.Invoke(null, null);
    }

    static void InvokeInstallForScene(Scene scene)
    {
        var method = RuntimeType("MainMenuCaptureBootstrap").GetMethod(
            "InstallForScene", StaticFlags);
        Assert.That(method, Is.Not.Null);
        method.Invoke(null, new object[] { scene, LoadSceneMode.Single });
    }

    static void InvokeHomeInstaller()
    {
        var install = RuntimeType("MainMenuHomeVisuals").GetMethod(
            "Install", StaticFlags);
        Assert.That(install, Is.Not.Null);
        install.Invoke(null, null);
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
