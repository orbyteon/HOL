using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class Phase2AVisualCapturePlayModeTests
{
    const string CaptureDirectoryVariable = "HOL_PHASE2A_CAPTURE_DIR";
    const BindingFlags StaticFlags =
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    struct CaptureCase
    {
        public readonly string Scene;
        public readonly int Language;
        public readonly int Width;
        public readonly int Height;
        public readonly int BottomInset;
        public readonly int TopInset;
        public readonly string FileName;

        public CaptureCase(string scene, int language,
            int width, int height, int bottomInset, int topInset, string fileName)
        {
            Scene = scene;
            Language = language;
            Width = width;
            Height = height;
            BottomInset = bottomInset;
            TopInset = topInset;
            FileName = fileName;
        }
    }

    [UnityTest]
    public IEnumerator CaptureApprovedSplashAndMainMenuMatrixWhenRequested()
    {
        string outputDirectory = Environment.GetEnvironmentVariable(
            CaptureDirectoryVariable);
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            Assert.Pass(CaptureDirectoryVariable + " is not set; capture is opt-in.");
            yield break;
        }

        Directory.CreateDirectory(outputDirectory);
        InvokeInstaller("MainMenuHomeVisuals");
        var originalLanguage = CurrentLanguage();
        try
        {
            var captures = new[]
            {
                new CaptureCase("SplashScene", 0,
                    1080, 1920, 0, 0, "splash-en-1080x1920.png"),
                new CaptureCase("SplashScene", 1,
                    1080, 1920, 0, 0, "splash-el-1080x1920.png"),
                new CaptureCase("MainMenu", 0,
                    1080, 1920, 0, 0, "mainmenu-en-1080x1920.png"),
                new CaptureCase("MainMenu", 1,
                    1080, 1920, 0, 120, "mainmenu-el-1080x1920-topcutout.png"),
                new CaptureCase("MainMenu", 0,
                    1080, 2400, 0, 120, "mainmenu-en-1080x2400-topcutout.png"),
                new CaptureCase("MainMenu", 1,
                    1080, 2400, 90, 120, "mainmenu-el-1080x2400-insets.png"),
                new CaptureCase("MainMenu", 1,
                    720, 1280, 48, 80, "mainmenu-el-720x1280-insets.png")
            };

            foreach (var capture in captures)
            {
                yield return Capture(outputDirectory, capture);
                string path = Path.Combine(outputDirectory, capture.FileName);
                Assert.That(File.Exists(path), Is.True, path);
                Assert.That(new FileInfo(path).Length, Is.GreaterThan(10000), path);
            }
        }
        finally
        {
            SetLanguage(originalLanguage);
        }
    }

    static IEnumerator Capture(string outputDirectory, CaptureCase capture)
    {
        SetLanguage(System.Enum.ToObject(
            RuntimeType("L10n").GetNestedType("Language"), capture.Language));
        Screen.SetResolution(capture.Width, capture.Height, false);
        yield return null;
        yield return SceneManager.LoadSceneAsync(capture.Scene, LoadSceneMode.Single);

        var scene = SceneManager.GetActiveScene();
        if (capture.Scene == "SplashScene")
        {
            var loader = FindComponent(scene, "SplashLoader") as MonoBehaviour;
            Assert.That(loader, Is.Not.Null);
            loader.CancelInvoke("LoadMenu");
            yield return WaitUntilSettled(scene, "SplashDesign");
        }
        else
        {
            HideOverlay(scene, "ConsentPanel");
            HideOverlay(scene, "ForceUpdatePanel");
            yield return WaitUntilSettled(scene, "MainMenuHomeVisuals");
        }

        Canvas canvas = FindRootCanvas(scene);
        Assert.That(canvas, Is.Not.Null);
        string safeRootName = capture.Scene == "SplashScene"
            ? "SplashSafeAreaRoot"
            : "HomeSafeAreaRoot";
        var safeRoot = Find(scene, safeRootName) as RectTransform;
        Assert.That(safeRoot, Is.Not.Null, safeRootName);
        var safeOwner = safeRoot.GetComponent(
            RuntimeType("ResponsiveSafeAreaRoot"));
        Assert.That(safeOwner, Is.Not.Null, safeRootName);

        var viewport = new Vector2(capture.Width, capture.Height);
        var safePixels = new Rect(
            0f,
            capture.BottomInset,
            capture.Width,
            capture.Height - capture.BottomInset - capture.TopInset);
        var geometry = RuntimeType("ResponsiveViewportGeometry");
        var canvasSize = (Vector2)geometry.GetMethod("CanvasSizeForViewport")
            .Invoke(null, new object[]
            {
                viewport, new Vector2(1080f, 1920f), 0.5f
            });
        safeOwner.GetType().GetMethod("ApplyViewport").Invoke(safeOwner,
            new object[]
            {
                new Rect(Vector2.zero, viewport), safePixels, canvasSize
            });
        if (capture.Scene == "MainMenu")
        {
            var home = FindComponent(scene, "MainMenuHomeVisuals");
            Assert.That(home, Is.Not.Null);
            home.GetType().GetMethod("ApplyResponsiveLayoutForViewport",
                    BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(home, new object[]
                {
                    capture.Width, capture.Height, true
                });
        }
        Canvas.ForceUpdateCanvases();

        string outputPath = Path.Combine(outputDirectory, capture.FileName);
        RenderCanvas(canvas, capture.Width, capture.Height, outputPath);
        yield return null;
    }

    static IEnumerator WaitUntilSettled(Scene scene, string ownerTypeName)
    {
        Component owner = null;
        PropertyInfo settled = null;
        float deadline = Time.realtimeSinceStartup + 5f;
        while (Time.realtimeSinceStartup < deadline)
        {
            if (owner == null)
            {
                owner = FindComponent(scene, ownerTypeName);
                if (owner != null)
                    settled = owner.GetType().GetProperty("IsSettled");
            }
            if (owner != null && settled != null &&
                (bool)settled.GetValue(owner, null))
                break;
            yield return null;
        }
        Assert.That(owner, Is.Not.Null, ownerTypeName);
        Assert.That(settled, Is.Not.Null, ownerTypeName + ".IsSettled");
        Assert.That((bool)settled.GetValue(owner, null), Is.True,
            ownerTypeName + " did not settle before capture.");
    }

    static void RenderCanvas(Canvas canvas, int width, int height, string path)
    {
        var cameraObject = new GameObject("Phase2ACaptureCamera");
        var camera = cameraObject.AddComponent<Camera>();
        var texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        texture.Create();

        var oldMode = canvas.renderMode;
        var oldCamera = canvas.worldCamera;
        float oldPlaneDistance = canvas.planeDistance;
        try
        {
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.01f, 0.005f, 0.06f, 1f);
            camera.orthographic = true;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.targetTexture = texture;

            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            Canvas.ForceUpdateCanvases();
            camera.Render();

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = texture;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                image.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                image.Apply(false, false);
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(image);
                RenderTexture.active = previous;
            }
        }
        finally
        {
            canvas.renderMode = oldMode;
            canvas.worldCamera = oldCamera;
            canvas.planeDistance = oldPlaneDistance;
            camera.targetTexture = null;
            texture.Release();
            UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }
    }

    static void HideOverlay(Scene scene, string name)
    {
        var overlay = Find(scene, name);
        if (overlay != null) overlay.gameObject.SetActive(false);
    }

    static Canvas FindRootCanvas(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
            {
                if (canvas.isRootCanvas && canvas.renderMode != RenderMode.WorldSpace)
                    return canvas;
            }
        }
        return null;
    }

    static Component FindComponent(Scene scene, string typeName)
    {
        var type = Type.GetType(typeName + ", Assembly-CSharp");
        Assert.That(type, Is.Not.Null, typeName);
        foreach (var root in scene.GetRootGameObjects())
        {
            var component = root.GetComponentInChildren(type, true);
            if (component != null) return component;
        }
        return null;
    }

    static Transform Find(Scene scene, string name)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            var found = Find(root.transform, name);
            if (found != null) return found;
        }
        return null;
    }

    static Transform Find(Transform root, string name)
    {
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var found = Find(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    static void InvokeInstaller(string typeName)
    {
        var type = Type.GetType(typeName + ", Assembly-CSharp");
        Assert.That(type, Is.Not.Null, typeName);
        var install = type.GetMethod("Install", StaticFlags);
        Assert.That(install, Is.Not.Null, typeName + ".Install");
        install.Invoke(null, null);
    }

    static object CurrentLanguage()
    {
        return RuntimeType("L10n").GetProperty("Current")
            .GetValue(null, null);
    }

    static void SetLanguage(object language)
    {
        RuntimeType("L10n").GetMethod("SetLanguage")
            .Invoke(null, new[] { language });
    }

    static Type RuntimeType(string name)
    {
        var type = Type.GetType(name + ", Assembly-CSharp");
        Assert.That(type, Is.Not.Null, name);
        return type;
    }
}
