using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class SplashAuthoritativeVisualsPlayModeTests
{
    const float Tolerance = 0.01f;

    struct LayoutExpectation
    {
        public readonly string Name;
        public readonly Vector2 Position;
        public readonly Vector2 Size;

        public LayoutExpectation(string name, Vector2 position, Vector2 size)
        {
            Name = name;
            Position = position;
            Size = size;
        }
    }

    [UnityTest]
    public IEnumerator RealSplashSceneHasOneSafeAreaAwareVisualOwner()
    {
        InstallRuntimePresenter("ExactReferenceVisuals");
        InstallRuntimePresenter("AttachmentReskinVisuals");
        InstallRuntimePresenter("AttachmentReskinPolish");
        InstallRuntimePresenter("AttachmentReskinCanvasBindings");

        yield return SceneManager.LoadSceneAsync("SplashScene", LoadSceneMode.Single);
        yield return null;

        var scene = SceneManager.GetActiveScene();
        Assert.That(scene.name, Is.EqualTo("SplashScene"));

        var splashDesign = FindInScene(scene, RuntimeType("SplashDesign"));
        Assert.That(splashDesign, Is.Not.Null);
        Assert.That(FindInScene(scene, RuntimeType("ExactReferenceVisuals")), Is.Null);
        Assert.That(FindInScene(scene, RuntimeType("AttachmentReskinVisuals")), Is.Null);
        Assert.That(FindInScene(scene, RuntimeType("AttachmentReskinPolish")), Is.Null);
        Assert.That(FindInScene(scene, RuntimeType("AttachmentReskinCanvasBindings")), Is.Null);

        var canvases = ComponentsInScene<Canvas>(scene);
        Assert.That(canvases, Has.Count.EqualTo(1), "Splash must reuse its one scene-authored Canvas.");
        var canvas = canvases[0];
        Assert.That(canvas.isRootCanvas, Is.True);
        Assert.That(canvas.renderMode, Is.Not.EqualTo(RenderMode.WorldSpace));

        var visualRoots = DirectChildren(canvas.transform, "SplashVisualRoot");
        Assert.That(visualRoots, Has.Count.EqualTo(1));
        var visualRoot = visualRoots[0];
        AssertDirectChildren(visualRoot, "SplashBackground", "SplashSafeAreaRoot");

        var background = DirectChild(visualRoot, "SplashBackground");
        var safeRoot = DirectChild(visualRoot, "SplashSafeAreaRoot");
        Assert.That(background, Is.Not.Null);
        Assert.That(safeRoot, Is.Not.Null);
        AssertDirectChildren(safeRoot,
            "SplashLogoGlow",
            "SplashLogo",
            "SplashMascotSix",
            "SplashMascotSeven",
            "SplashProgressTrack");

        var progressTrack = DirectChild(safeRoot, "SplashProgressTrack");
        Assert.That(progressTrack, Is.Not.Null);
        AssertDirectChildren(progressTrack, "SplashProgressFill");

        var expectedLayout = new[]
        {
            new LayoutExpectation("SplashLogoGlow",
                new Vector2(0f, 260f), new Vector2(960f, 620f)),
            new LayoutExpectation("SplashLogo",
                new Vector2(0f, 260f), new Vector2(820f, 546f)),
            new LayoutExpectation("SplashMascotSix",
                new Vector2(-285f, -330f), new Vector2(270f, 350f)),
            new LayoutExpectation("SplashMascotSeven",
                new Vector2(285f, -330f), new Vector2(250f, 350f)),
            new LayoutExpectation("SplashProgressTrack",
                new Vector2(0f, -770f), new Vector2(480f, 8f))
        };
        foreach (var expected in expectedLayout)
            AssertLayout(safeRoot, expected.Name, expected.Position, expected.Size);

        var six = (RectTransform)DirectChild(safeRoot, "SplashMascotSix");
        var seven = (RectTransform)DirectChild(safeRoot, "SplashMascotSeven");
        Assert.That(six.anchoredPosition.x, Is.LessThan(0f));
        Assert.That(seven.anchoredPosition.x, Is.GreaterThan(0f));

        AssertPreservesAspect(safeRoot, "SplashLogo");
        AssertPreservesAspect(safeRoot, "SplashMascotSix");
        AssertPreservesAspect(safeRoot, "SplashMascotSeven");

        foreach (var image in visualRoot.GetComponentsInChildren<Image>(true))
            Assert.That(image.raycastTarget, Is.False,
                image.name + " must not intercept SplashLoader's whole-screen tap.");

        Assert.That(ComponentsInScene<Button>(scene), Is.Empty,
            "The presentation-only Splash must not create a Button.");

        var legacyPanel = DirectChild(canvas.transform, "Panel");
        var legacyLogo = DirectChild(canvas.transform, "Image");
        Assert.That(legacyPanel, Is.Not.Null);
        Assert.That(legacyPanel.GetComponent<Image>().enabled, Is.False);
        Assert.That(legacyLogo, Is.Not.Null);
        Assert.That(legacyLogo.gameObject.activeSelf, Is.False);

        var normalizedSafeArea = InvokeNormalizedSafeArea(
            Screen.safeArea, Screen.width, Screen.height);
        var safeRect = (RectTransform)safeRoot;
        AssertVector(safeRect.anchorMin, normalizedSafeArea.min, "safe-area minimum anchor");
        AssertVector(safeRect.anchorMax, normalizedSafeArea.max, "safe-area maximum anchor");

        var ready = splashDesign.GetType().GetProperty(
            "IsReady", BindingFlags.Instance | BindingFlags.Public);
        Assert.That(ready, Is.Not.Null);
        Assert.That((bool)ready.GetValue(splashDesign, null), Is.True);

        var settled = splashDesign.GetType().GetProperty(
            "IsSettled", BindingFlags.Instance | BindingFlags.Public);
        Assert.That(settled, Is.Not.Null);
        Assert.That((bool)settled.GetValue(splashDesign, null), Is.False);

        yield return new WaitForSecondsRealtime(0.75f);
        Assert.That((bool)settled.GetValue(splashDesign, null), Is.True);
    }

    [TestCase(0f, 0f, 1080f, 1920f, 0f, 0f, 1f, 1f)]
    [TestCase(0f, 80f, 1080f, 1760f, 0f, 0.0416667f, 1f, 0.9583333f)]
    [TestCase(60f, 0f, 1020f, 1920f, 0.0555556f, 0f, 1f, 1f)]
    public void NormalizedSafeAreaConvertsPixelsToNormalizedAnchors(
        float x, float y, float width, float height,
        float expectedX, float expectedY, float expectedWidth, float expectedHeight)
    {
        var result = InvokeNormalizedSafeArea(
            new Rect(x, y, width, height), 1080f, 1920f);

        Assert.That(result.x, Is.EqualTo(expectedX).Within(0.0001f));
        Assert.That(result.y, Is.EqualTo(expectedY).Within(0.0001f));
        Assert.That(result.width, Is.EqualTo(expectedWidth).Within(0.0001f));
        Assert.That(result.height, Is.EqualTo(expectedHeight).Within(0.0001f));
    }

    [TestCase(0f, 1920f)]
    [TestCase(1080f, 0f)]
    public void NormalizedSafeAreaFallsBackToFullRectForInvalidScreenSize(
        float screenWidth, float screenHeight)
    {
        var result = InvokeNormalizedSafeArea(
            new Rect(60f, 80f, 900f, 1600f), screenWidth, screenHeight);

        Assert.That(result, Is.EqualTo(new Rect(0f, 0f, 1f, 1f)));
    }

    static void InstallRuntimePresenter(string typeName)
    {
        var install = RuntimeType(typeName).GetMethod(
            "Install", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(install, Is.Not.Null, "Missing runtime installer for " + typeName);
        install.Invoke(null, null);
    }

    static Rect InvokeNormalizedSafeArea(Rect safe, float width, float height)
    {
        var method = RuntimeType("SplashDesign").GetMethod(
            "NormalizedSafeArea", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        return (Rect)method.Invoke(null, new object[] { safe, width, height });
    }

    static void AssertLayout(
        Transform root, string name, Vector2 expectedPosition, Vector2 expectedSize)
    {
        var found = DirectChild(root, name) as RectTransform;
        Assert.That(found, Is.Not.Null, "Missing layout element " + name);
        AssertVector(found.anchoredPosition, expectedPosition, name + " position");
        AssertVector(found.sizeDelta, expectedSize, name + " size");
    }

    static void AssertPreservesAspect(Transform root, string name)
    {
        var found = DirectChild(root, name);
        Assert.That(found, Is.Not.Null);
        var image = found.GetComponent<Image>();
        Assert.That(image, Is.Not.Null);
        Assert.That(image.preserveAspect, Is.True, name + " must preserve aspect ratio.");
    }

    static void AssertDirectChildren(Transform parent, params string[] expectedNames)
    {
        Assert.That(parent, Is.Not.Null);
        Assert.That(parent.childCount, Is.EqualTo(expectedNames.Length));
        for (int i = 0; i < expectedNames.Length; i++)
            Assert.That(parent.GetChild(i).name, Is.EqualTo(expectedNames[i]),
                "Unexpected hierarchy child at index " + i + " under " + parent.name);
    }

    static void AssertVector(Vector2 actual, Vector2 expected, string label)
    {
        Assert.That(actual.x, Is.EqualTo(expected.x).Within(Tolerance), label + " x");
        Assert.That(actual.y, Is.EqualTo(expected.y).Within(Tolerance), label + " y");
    }

    static List<T> ComponentsInScene<T>(Scene scene) where T : Component
    {
        var found = new List<T>();
        foreach (var root in scene.GetRootGameObjects())
            found.AddRange(root.GetComponentsInChildren<T>(true));
        return found;
    }

    static Component FindInScene(Scene scene, System.Type type)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            var components = root.GetComponentsInChildren(type, true);
            if (components.Length > 0)
                return components[0] as Component;
        }
        return null;
    }

    static List<Transform> DirectChildren(Transform parent, string name)
    {
        var found = new List<Transform>();
        for (int i = 0; i < parent.childCount; i++)
            if (parent.GetChild(i).name == name)
                found.Add(parent.GetChild(i));
        return found;
    }

    static Transform DirectChild(Transform parent, string name)
    {
        var found = DirectChildren(parent, name);
        return found.Count == 0 ? null : found[0];
    }

    static System.Type RuntimeType(string name)
    {
        var type = System.Type.GetType(name + ", Assembly-CSharp");
        Assert.That(type, Is.Not.Null, "Missing runtime component: " + name);
        return type;
    }
}
