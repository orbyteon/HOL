using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class CartoonEstablishedScreensCapturePlayModeTests
{
    [UnityTest]
    public IEnumerator CaptureHomeAndSplashDeviceMatrix()
    {
        string output = System.Environment.GetEnvironmentVariable(
            "HOL_CARTOON_CAPTURE_DIR");
        if (string.IsNullOrWhiteSpace(output))
            Assert.Ignore("HOL_CARTOON_CAPTURE_DIR is not set.");
        Directory.CreateDirectory(output);

        bool hadLanguage = PlayerPrefs.HasKey("Language");
        int oldLanguage = PlayerPrefs.GetInt("Language", 0);
        bool hadPlayerName = PlayerPrefs.HasKey("PlayerName");
        string oldPlayerName = PlayerPrefs.GetString("PlayerName", "");
        try
        {
            PlayerPrefs.SetString("PlayerName", "Andreas");
            InvokeInstaller("MainMenuHomeVisuals");
            SetLanguage(0);
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            for (int i = 0; i < 45; i++) yield return null;
            yield return new WaitForSecondsRealtime(0.5f);
            HideOverlays();

            Canvas canvas = Object.FindObjectOfType<Canvas>();
            Assert.That(canvas, Is.Not.Null);
            yield return Capture(canvas, output, "home-en-1080x1920.png",
                "HomeSafeAreaRoot", 1080, 1920,
                new Rect(0f, 0f, 1080f, 1920f));

            SetLanguage(1);
            yield return null;
            yield return Capture(canvas, output, "home-el-1080x1920.png",
                "HomeSafeAreaRoot", 1080, 1920,
                new Rect(0f, 0f, 1080f, 1920f));
            yield return Capture(canvas, output, "home-el-720x1280.png",
                "HomeSafeAreaRoot", 720, 1280,
                new Rect(0f, 0f, 720f, 1280f));
            yield return Capture(canvas, output, "home-el-1080x2400-cutout.png",
                "HomeSafeAreaRoot", 1080, 2400,
                new Rect(0f, 120f, 1080f, 2160f));

            SetLanguage(0);
            yield return SceneManager.LoadSceneAsync("SplashScene", LoadSceneMode.Single);
            yield return null;
            Component loader = Object.FindObjectOfType(RuntimeType("SplashLoader"))
                as Component;
            Assert.That(loader, Is.Not.Null);
            ((MonoBehaviour)loader).CancelInvoke("LoadMenu");
            yield return new WaitForSecondsRealtime(0.8f);
            canvas = Object.FindObjectOfType<Canvas>();
            Assert.That(canvas, Is.Not.Null);
            yield return Capture(canvas, output, "splash-en-1080x1920.png",
                "SplashSafeAreaRoot", 1080, 1920,
                new Rect(0f, 0f, 1080f, 1920f));

            SetLanguage(1);
            yield return null;
            yield return Capture(canvas, output, "splash-el-1080x1920.png",
                "SplashSafeAreaRoot", 1080, 1920,
                new Rect(0f, 0f, 1080f, 1920f));
            yield return Capture(canvas, output, "splash-el-720x1280.png",
                "SplashSafeAreaRoot", 720, 1280,
                new Rect(0f, 0f, 720f, 1280f));
            yield return Capture(canvas, output, "splash-el-1080x2400-cutout.png",
                "SplashSafeAreaRoot", 1080, 2400,
                new Rect(0f, 120f, 1080f, 2160f));
        }
        finally
        {
            if (hadLanguage) SetLanguage(oldLanguage);
            else PlayerPrefs.DeleteKey("Language");
            if (hadPlayerName) PlayerPrefs.SetString("PlayerName", oldPlayerName);
            else PlayerPrefs.DeleteKey("PlayerName");
            PlayerPrefs.Save();
        }
    }

    static IEnumerator Capture(Canvas canvas, string directory, string fileName,
        string safeRootName, int width, int height, Rect safeArea)
    {
        string path = Path.Combine(directory, fileName);
        if (File.Exists(path)) File.Delete(path);

        RenderMode previousMode = canvas.renderMode;
        Camera previousCamera = canvas.worldCamera;
        float previousPlane = canvas.planeDistance;
        var cameraObject = new GameObject("CartoonCaptureCamera");
        var camera = cameraObject.AddComponent<Camera>();
        var target = new RenderTexture(width, height, 24,
            RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        var texture = new Texture2D(width, height, TextureFormat.RGB24,
            false, false);
        RenderTexture previousActive = RenderTexture.active;
        try
        {
            camera.enabled = false;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            camera.targetTexture = target;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 10f;
            Canvas.ForceUpdateCanvases();
            ApplySafeArea(canvas, safeRootName, width, height, safeArea);
            Canvas.ForceUpdateCanvases();
            yield return null;
            Canvas.ForceUpdateCanvases();
            AssertNoVisibleTextOverflow(canvas.transform, fileName);
            camera.Render();

            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            texture.Apply(false, false);
            File.WriteAllBytes(path, texture.EncodeToPNG());
        }
        finally
        {
            canvas.renderMode = previousMode;
            canvas.worldCamera = previousCamera;
            canvas.planeDistance = previousPlane;
            camera.targetTexture = null;
            RenderTexture.active = previousActive;
            target.Release();
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(cameraObject);
            Canvas.ForceUpdateCanvases();
        }

        Assert.That(File.Exists(path), Is.True, "Missing capture: " + path);
        Assert.That(new FileInfo(path).Length, Is.GreaterThan(4096),
            "Capture is unexpectedly empty: " + path);
    }

    static void ApplySafeArea(Canvas canvas, string name, int width, int height,
        Rect safeArea)
    {
        Transform safe = Find(canvas.transform, name);
        Assert.That(safe, Is.Not.Null, "Missing " + name);
        Component owner = safe.GetComponent(RuntimeType("ResponsiveSafeAreaRoot"));
        Assert.That(owner, Is.Not.Null, name + " has no safe-area owner.");
        MethodInfo apply = owner.GetType().GetMethod("ApplyViewport",
            BindingFlags.Instance | BindingFlags.Public);
        Assert.That(apply, Is.Not.Null);
        apply.Invoke(owner, new object[]
        {
            new Rect(0f, 0f, width, height), safeArea,
            new Vector2(1080f, 1920f)
        });
    }

    static void AssertNoVisibleTextOverflow(Transform root, string fileName)
    {
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(false))
        {
            if (!text.gameObject.activeInHierarchy) continue;
            text.ForceMeshUpdate();
            Assert.That(text.isTextOverflowing, Is.False,
                fileName + ": visible TMP overflow at " + text.name +
                " ('" + text.text + "').");
        }
    }

    static void HideOverlays()
    {
        foreach (Transform transform in Object.FindObjectsOfType<Transform>(true))
        {
            if (transform.name == "ConsentPanel" ||
                transform.name == "ForceUpdatePanel")
                transform.gameObject.SetActive(false);
        }
    }

    static Transform Find(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = Find(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    static void InvokeInstaller(string typeName)
    {
        MethodInfo install = RuntimeType(typeName).GetMethod("Install",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(install, Is.Not.Null);
        install.Invoke(null, null);
    }

    static void SetLanguage(int value)
    {
        System.Type type = RuntimeType("L10n");
        System.Type enumType = type.GetNestedType("Language", BindingFlags.Public);
        MethodInfo method = type.GetMethod("SetLanguage",
            BindingFlags.Public | BindingFlags.Static);
        method.Invoke(null, new[] { System.Enum.ToObject(enumType, value) });
    }

    static System.Type RuntimeType(string name)
    {
        System.Type type = System.Type.GetType(name + ", Assembly-CSharp");
        Assert.That(type, Is.Not.Null, "Missing runtime type: " + name);
        return type;
    }
}
