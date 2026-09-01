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
    const string PlayerNameKey = "PlayerName";
    bool hadPlayerName;
    string previousPlayerName;

    [SetUp]
    public void UseReturningPlayerSplashFixture()
    {
        hadPlayerName = PlayerPrefs.HasKey(PlayerNameKey);
        previousPlayerName = PlayerPrefs.GetString(
            PlayerNameKey, string.Empty);
        PlayerPrefs.SetString(PlayerNameKey, "SplashTester");
        PlayerPrefs.Save();
    }

    [TearDown]
    public void RestoreReturningPlayerSplashFixture()
    {
        if (hadPlayerName)
            PlayerPrefs.SetString(
                PlayerNameKey, previousPlayerName);
        else
            PlayerPrefs.DeleteKey(PlayerNameKey);
        PlayerPrefs.Save();
    }

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

        yield return SceneManager.LoadSceneAsync("SplashScene", LoadSceneMode.Single);
        yield return null;

        var scene = SceneManager.GetActiveScene();
        Assert.That(scene.name, Is.EqualTo("SplashScene"));

        var splashDesign = FindInScene(scene, RuntimeType("SplashDesign"));
        Assert.That(splashDesign, Is.Not.Null);

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
            "SplashDecoStars",
            "SplashDecoLightning",
            "SplashDecoConfetti",
            "SplashDecoNumbers",
            "SplashLogoGlow",
            "SplashLogo",
            "SplashHeroBoy",
            "SplashHeroGirl",
            "SplashMascotSix",
            "SplashMascotSeven",
            "SplashProgressTrack");

        var progressTrack = DirectChild(safeRoot, "SplashProgressTrack");
        Assert.That(progressTrack, Is.Not.Null);
        AssertDirectChildren(progressTrack, "SplashProgressFill");
        var progressFill = DirectChild(progressTrack, "SplashProgressFill").GetComponent<Image>();
        Assert.That(progressFill, Is.Not.Null);
        Assert.That(progressFill.sprite, Is.Not.Null,
            "A filled Image needs a sprite for fillAmount to affect its mesh.");
        Assert.That(progressFill.type, Is.EqualTo(Image.Type.Filled));

        var expectedLayout = new[]
        {
            new LayoutExpectation("SplashDecoStars",
                Vector2.zero, new Vector2(1080f, 1920f)),
            new LayoutExpectation("SplashDecoLightning",
                Vector2.zero, new Vector2(1080f, 1920f)),
            new LayoutExpectation("SplashDecoConfetti",
                Vector2.zero, new Vector2(1080f, 1920f)),
            new LayoutExpectation("SplashDecoNumbers",
                Vector2.zero, new Vector2(1080f, 1920f)),
            new LayoutExpectation("SplashLogoGlow",
                new Vector2(0f, 280f), new Vector2(960f, 620f)),
            new LayoutExpectation("SplashLogo",
                new Vector2(0f, 280f), new Vector2(820f, 546f)),
            new LayoutExpectation("SplashHeroBoy",
                new Vector2(-155f, -40f), new Vector2(380f, 460f)),
            new LayoutExpectation("SplashHeroGirl",
                new Vector2(155f, -40f), new Vector2(380f, 460f)),
            new LayoutExpectation("SplashMascotSix",
                new Vector2(-340f, -420f), new Vector2(240f, 320f)),
            new LayoutExpectation("SplashMascotSeven",
                new Vector2(340f, -420f), new Vector2(230f, 320f)),
            new LayoutExpectation("SplashProgressTrack",
                new Vector2(0f, -770f), new Vector2(480f, 8f))
        };
        foreach (var expected in expectedLayout)
            AssertLayout(safeRoot, expected.Name, expected.Position, expected.Size);

        var boy = (RectTransform)DirectChild(safeRoot, "SplashHeroBoy");
        var girl = (RectTransform)DirectChild(safeRoot, "SplashHeroGirl");
        var six = (RectTransform)DirectChild(safeRoot, "SplashMascotSix");
        var seven = (RectTransform)DirectChild(safeRoot, "SplashMascotSeven");
        Assert.That(boy.anchoredPosition.x, Is.LessThan(0f));
        Assert.That(girl.anchoredPosition.x, Is.GreaterThan(0f));
        Assert.That(boy.anchoredPosition.x, Is.LessThan(girl.anchoredPosition.x));
        Assert.That(six.anchoredPosition.x, Is.LessThan(seven.anchoredPosition.x));

        AssertPreservesAspect(safeRoot, "SplashLogo");
        AssertPreservesAspect(safeRoot, "SplashHeroBoy");
        AssertPreservesAspect(safeRoot, "SplashHeroGirl");
        AssertPreservesAspect(safeRoot, "SplashMascotSix");
        AssertPreservesAspect(safeRoot, "SplashMascotSeven");

        AssertDecoUsesAuthoringBox(safeRoot, "SplashDecoStars");
        AssertDecoUsesAuthoringBox(safeRoot, "SplashDecoLightning");
        AssertDecoUsesAuthoringBox(safeRoot, "SplashDecoConfetti");
        AssertDecoUsesAuthoringBox(safeRoot, "SplashDecoNumbers");

        AssertSprite(visualRoot, "SplashBackground", "splash/splash_bg_stairs_clouds");
        AssertSprite(safeRoot, "SplashDecoStars", "splash/splash_deco_stars");
        AssertSprite(safeRoot, "SplashDecoLightning", "splash/splash_deco_lightning");
        AssertSprite(safeRoot, "SplashDecoConfetti", "splash/splash_deco_confetti");
        AssertSprite(safeRoot, "SplashDecoNumbers", "splash/splash_deco_numbers");
        AssertSprite(safeRoot, "SplashLogoGlow", "splash/splash_logo_glow");
        AssertSprite(safeRoot, "SplashLogo", "reference/hol_logo_exact");
        AssertSprite(safeRoot, "SplashHeroBoy", "splash/splash_char_boy");
        AssertSprite(safeRoot, "SplashHeroGirl", "splash/splash_char_girl");
        AssertSprite(safeRoot, "SplashMascotSix", "reference/mascot_6_exact");
        AssertSprite(safeRoot, "SplashMascotSeven", "reference/mascot_7_exact");

        foreach (var image in visualRoot.GetComponentsInChildren<Image>(true))
            Assert.That(image.raycastTarget, Is.False,
                image.name + " must not intercept SplashLoader's whole-screen tap.");

        Assert.That(ComponentsInScene<Button>(scene), Is.Empty,
            "The presentation-only Splash must not create a Button.");
        AssertNoMainMenuChrome(scene);

        var legacyPanel = DirectChild(canvas.transform, "Panel");
        var legacyLogo = DirectChild(canvas.transform, "Image");
        Assert.That(legacyPanel, Is.Not.Null);
        Assert.That(legacyPanel.GetComponent<Image>().enabled, Is.False);
        // Git history is the backup: a retired logo may be purged completely or
        // remain disabled during a controlled scene migration, but it must never
        // be required for the current production Splash to pass.
        if (legacyLogo != null)
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

    [UnityTest]
    public IEnumerator ProgressFillAdvancesMonotonicallyAcrossWaitTime()
    {
        yield return SceneManager.LoadSceneAsync("SplashScene", LoadSceneMode.Single);
        yield return null;

        var scene = SceneManager.GetActiveScene();
        var loader = FindInScene(scene, RuntimeType("SplashLoader"));
        Assert.That(loader, Is.Not.Null);
        ((MonoBehaviour)loader).CancelInvoke("LoadMenu");

        var progress = FindByName(scene, "SplashProgressFill");
        Assert.That(progress, Is.Not.Null);
        var fill = progress.GetComponent<Image>();
        Assert.That(fill, Is.Not.Null);

        float previous = fill.fillAmount;
        float deadline = Time.realtimeSinceStartup + 2.6f;
        while (Time.realtimeSinceStartup < deadline)
        {
            yield return null;
            Assert.That(fill.fillAmount, Is.GreaterThanOrEqualTo(previous));
            previous = fill.fillAmount;
        }
        Assert.That(fill.fillAmount, Is.EqualTo(1f).Within(Tolerance));
    }

    [UnityTest]
    public IEnumerator SharedLoadMenuSkipPathReachesMainMenuExactlyOnce()
    {
        yield return SceneManager.LoadSceneAsync("SplashScene", LoadSceneMode.Single);
        yield return null;

        var loader = FindInScene(SceneManager.GetActiveScene(), RuntimeType("SplashLoader"));
        Assert.That(loader, Is.Not.Null);
        var update = loader.GetType().GetMethod(
            "Update",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.That(update, Is.Not.Null,
            "SplashLoader.Update must remain the product tap-to-skip surface.");
        var waitTime = loader.GetType().GetField(
            "waitTime", BindingFlags.Instance | BindingFlags.Public);
        Assert.That(waitTime, Is.Not.Null);
        Assert.That((float)waitTime.GetValue(loader), Is.EqualTo(2.5f));
        ((MonoBehaviour)loader).CancelInvoke("LoadMenu");

        int mainMenuLoads = 0;
        UnityEngine.Events.UnityAction<Scene, LoadSceneMode> onLoaded = (scene, mode) =>
        {
            if (scene.name == "MainMenu") mainMenuLoads++;
        };
        SceneManager.sceneLoaded += onLoaded;
        try
        {
            loader.SendMessage("LoadMenu", SendMessageOptions.RequireReceiver);
            float deadline = Time.realtimeSinceStartup + 5f;
            while (SceneManager.GetActiveScene().name != "MainMenu" &&
                   Time.realtimeSinceStartup < deadline)
                yield return null;
            if (SceneManager.GetActiveScene().name != "MainMenu")
                Assert.Fail("Shared SplashLoader.LoadMenu skip path did not reach MainMenu within 5 seconds.");
            yield return null;
            Assert.That(mainMenuLoads, Is.EqualTo(1));
        }
        finally
        {
            SceneManager.sceneLoaded -= onLoaded;
        }
    }

    [UnityTest]
    public IEnumerator AutomaticTransitionReachesMainMenuExactlyOnce()
    {
        yield return SceneManager.LoadSceneAsync("SplashScene", LoadSceneMode.Single);
        yield return null;

        int mainMenuLoads = 0;
        UnityEngine.Events.UnityAction<Scene, LoadSceneMode> onLoaded = (scene, mode) =>
        {
            if (scene.name == "MainMenu") mainMenuLoads++;
        };
        SceneManager.sceneLoaded += onLoaded;
        try
        {
            float deadline = Time.realtimeSinceStartup + 4f;
            while (SceneManager.GetActiveScene().name == "SplashScene" &&
                   Time.realtimeSinceStartup < deadline)
                yield return null;

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"));
            yield return null;
            Assert.That(mainMenuLoads, Is.EqualTo(1));
        }
        finally
        {
            SceneManager.sceneLoaded -= onLoaded;
        }
    }

    [Test]
    public void RequiredArtReadyNeedsAllSevenApprovedSprites()
    {
        var sprites = RequiredSprites();
        for (int i = 0; i < sprites.Length; i++)
            Assert.That(sprites[i], Is.Not.Null, "Missing production test Sprite at index " + i);

        var predicate = RuntimeType("SplashDesign").GetMethod(
            "RequiredArtReady", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(predicate, Is.Not.Null);
        Assert.That((bool)predicate.Invoke(null, sprites), Is.True);

        for (int i = 0; i < sprites.Length; i++)
        {
            var missingOne = (object[])sprites.Clone();
            missingOne[i] = null;
            Assert.That((bool)predicate.Invoke(null, missingOne), Is.False,
                "Readiness must reject a missing required Sprite at index " + i);
        }
    }

    [UnityTest]
    public IEnumerator MissingRequiredArtClearsSettledStateAndDoesNotBlockSplashLoader()
    {
        yield return SceneManager.LoadSceneAsync("SplashScene", LoadSceneMode.Single);
        yield return null;

        var scene = SceneManager.GetActiveScene();
        var splashDesign = FindInScene(scene, RuntimeType("SplashDesign"));
        Assert.That(splashDesign, Is.Not.Null);

        var loader = FindInScene(scene, RuntimeType("SplashLoader"));
        Assert.That(loader, Is.Not.Null);
        var waitTime = loader.GetType().GetField(
            "waitTime", BindingFlags.Instance | BindingFlags.Public);
        Assert.That(waitTime, Is.Not.Null);
        Assert.That((float)waitTime.GetValue(loader), Is.EqualTo(2.5f));
        ((MonoBehaviour)loader).CancelInvoke("LoadMenu");

        var ready = splashDesign.GetType().GetProperty(
            "IsReady", BindingFlags.Instance | BindingFlags.Public);
        var settled = splashDesign.GetType().GetProperty(
            "IsSettled", BindingFlags.Instance | BindingFlags.Public);
        float settleDeadline = Time.realtimeSinceStartup + 1.5f;
        while (!(bool)settled.GetValue(splashDesign, null) &&
               Time.realtimeSinceStartup < settleDeadline)
            yield return null;
        Assert.That((bool)ready.GetValue(splashDesign, null), Is.True);
        Assert.That((bool)settled.GetValue(splashDesign, null), Is.True,
            "The complete production art must settle before simulating a missing Sprite.");

        var sprites = RequiredSprites();
        sprites[0] = null;
        var applyReadiness = splashDesign.GetType().GetMethod(
            "ApplyRequiredArtReadiness", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(applyReadiness, Is.Not.Null);
        applyReadiness.Invoke(splashDesign, sprites);

        Assert.That((bool)ready.GetValue(splashDesign, null), Is.False);
        Assert.That((bool)settled.GetValue(splashDesign, null), Is.False);

        yield return new WaitForSecondsRealtime(0.1f);
        Assert.That((bool)settled.GetValue(splashDesign, null), Is.False,
            "Missing required art must keep the presentation unsettled.");
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("SplashScene"),
            "Only the deliberate LoadMenu call below may transition this test.");

        loader.SendMessage("LoadMenu", SendMessageOptions.RequireReceiver);
        while (SceneManager.GetActiveScene().name != "MainMenu")
            yield return null;
    }

    [TestCase(0f, 0f, 1080f, 1920f, 0f, 0f, 1f, 1f)]
    [TestCase(0f, 80f, 1080f, 1760f, 0f, 0.0416667f, 1f, 0.9166667f)]
    [TestCase(60f, 0f, 1020f, 1920f, 0.0555556f, 0f, 0.9444444f, 1f)]
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

    static object[] RequiredSprites()
    {
        return new object[]
        {
            Resources.Load<Sprite>("splash/splash_bg_stairs_clouds"),
            Resources.Load<Sprite>("splash/splash_logo_glow"),
            Resources.Load<Sprite>("reference/hol_logo_exact"),
            Resources.Load<Sprite>("splash/splash_char_boy"),
            Resources.Load<Sprite>("splash/splash_char_girl"),
            Resources.Load<Sprite>("reference/mascot_6_exact"),
            Resources.Load<Sprite>("reference/mascot_7_exact")
        };
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

    static void AssertDecoUsesAuthoringBox(Transform root, string name)
    {
        var found = DirectChild(root, name) as RectTransform;
        Assert.That(found, Is.Not.Null);
        AssertVector(found.anchoredPosition, Vector2.zero, name + " position");
        AssertVector(found.sizeDelta, new Vector2(1080f, 1920f), name + " size");
        AssertVector(found.anchorMin, new Vector2(0.5f, 0.5f), name + " minimum anchor");
        AssertVector(found.anchorMax, new Vector2(0.5f, 0.5f), name + " maximum anchor");

        var image = found.GetComponent<Image>();
        Assert.That(image, Is.Not.Null);
        Assert.That(image.preserveAspect, Is.False);
        Assert.That(image.raycastTarget, Is.False);
    }

    static void AssertSprite(Transform root, string name, string resourcePath)
    {
        var found = DirectChild(root, name);
        Assert.That(found, Is.Not.Null);
        var image = found.GetComponent<Image>();
        Assert.That(image, Is.Not.Null);
        var approved = Resources.Load<Sprite>(resourcePath);
        Assert.That(approved, Is.Not.Null, "Missing approved Sprite " + resourcePath);
        Assert.That(image.sprite, Is.SameAs(approved),
            name + " must use only its approved splash/reference resource.");
    }

    static void AssertNoMainMenuChrome(Scene scene)
    {
        var forbidden = new[]
        {
            "Settings", "PlayerChip", "Tip", "Solo", "Daily", "Gear", "Trophy", "1V1"
        };
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                foreach (var fragment in forbidden)
                    Assert.That(child.name.IndexOf(
                        fragment, System.StringComparison.OrdinalIgnoreCase), Is.LessThan(0),
                        child.name + " invents Main Menu chrome on Splash.");
            }
        }
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

    static Transform FindByName(Scene scene, string name)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child;
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
