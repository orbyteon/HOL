using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class SettingsVisualCapturePlayModeTests
{
    [UnityTest]
    public IEnumerator CaptureApprovedSettingsDeviceMatrix()
    {
        string output = System.Environment.GetEnvironmentVariable(
            "HOL_SETTINGS_CAPTURE_DIR");
        if (string.IsNullOrWhiteSpace(output))
            Assert.Ignore("HOL_SETTINGS_CAPTURE_DIR is not set.");
        Directory.CreateDirectory(output);

        int oldLanguage = PlayerPrefs.GetInt("Language", 0);
        bool hadLanguage = PlayerPrefs.HasKey("Language");
        string oldPlayerName = PlayerPrefs.GetString("PlayerName", "");
        bool hadPlayerName = PlayerPrefs.HasKey("PlayerName");
        try
        {
            PlayerPrefs.SetString("PlayerName", "Andreas");
            InvokeInstaller(RuntimeType("SettingsVisuals"));
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            for (int i = 0; i < 30; i++) yield return null;

            var menu = Object.FindObjectOfType(RuntimeType("MenuManager")) as Component;
            Assert.That(menu, Is.Not.Null);
            menu.SendMessage("OpenSettings", SendMessageOptions.RequireReceiver);
            for (int i = 0; i < 8; i++) yield return null;
            yield return new WaitForSecondsRealtime(0.35f);
            var visuals = Object.FindObjectOfType(RuntimeType("SettingsVisuals"))
                as Component;
            Assert.That(visuals, Is.Not.Null);
            var canvas = visuals.GetComponent<Canvas>();
            Assert.That(canvas, Is.Not.Null);

            SetLanguage(0);
            yield return null;
            Capture(canvas, output, "settings-en-1080x1920.png", 1080, 1920,
                new Rect(0f, 0f, 1080f, 1920f));
            SetLanguage(1);
            yield return null;
            Capture(canvas, output, "settings-el-1080x1920.png", 1080, 1920,
                new Rect(0f, 0f, 1080f, 1920f));
            Capture(canvas, output, "settings-el-720x1280.png", 720, 1280,
                new Rect(0f, 0f, 720f, 1280f));
            Capture(canvas, output, "settings-el-1080x2400-cutout.png", 1080, 2400,
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

    static void Capture(Canvas canvas, string directory, string fileName,
        int width, int height, Rect safeArea)
    {
        string path = Path.Combine(directory, fileName);
        if (File.Exists(path)) File.Delete(path);

        RenderMode previousMode = canvas.renderMode;
        Camera previousCamera = canvas.worldCamera;
        float previousPlane = canvas.planeDistance;
        var cameraObject = new GameObject("SettingsCaptureCamera");
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
            ApplySettingsSafeArea(canvas, width, height, safeArea);
            Canvas.ForceUpdateCanvases();
            Transform settingsRoot = Find(canvas.transform, "SettingsVisualRoot");
            Assert.That(settingsRoot, Is.Not.Null, "Missing SettingsVisualRoot.");
            AssertPainted(settingsRoot, "SettingsReferenceShell");
            AssertPainted(settingsRoot, "SettingsNameRow");
            AssertPainted(settingsRoot, "SettingsPlayerChip");
            AssertNoVisibleTextOverflow(settingsRoot, fileName);
            AssertHorizontalGap(settingsRoot, "InputField (TMP)",
                "Buttonsave", 8f, fileName);
            AssertHorizontalGap(settingsRoot, "EnglishButton",
                "GreekButton", 8f, fileName);
            for (int i = 0; i < 3; i++)
                AssertHorizontalGap(settingsRoot, "Difficulty" + i,
                    "Difficulty" + (i + 1), 8f, fileName);
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

    static void AssertPainted(Transform root, string name)
    {
        Transform node = Find(root, name);
        Assert.That(node, Is.Not.Null);
        var graphic = node.GetComponent<Graphic>();
        Assert.That(graphic, Is.Not.Null, name + " has no Graphic.");
        var mesh = graphic.canvasRenderer.GetMesh();
        Assert.That(mesh, Is.Not.Null, name + " has no painted mesh.");
        Assert.That(mesh.vertexCount, Is.GreaterThan(3),
            name + " has no painted vertices.");
        Assert.That(graphic.canvasRenderer.GetColor().a, Is.GreaterThan(0.01f));
    }

    static void AssertNoVisibleTextOverflow(Transform root, string fileName)
    {
        foreach (var text in root.GetComponentsInChildren<TMP_Text>(false))
        {
            if (!text.gameObject.activeInHierarchy) continue;
            text.ForceMeshUpdate();
            Assert.That(text.isTextOverflowing, Is.False,
                fileName + ": visible TMP overflow at " + text.name +
                " ('" + text.text + "').");
        }
    }

    static void AssertHorizontalGap(Transform root, string firstName,
        string secondName, float minimum, string fileName)
    {
        var first = Find(root, firstName) as RectTransform;
        var second = Find(root, secondName) as RectTransform;
        Assert.That(first, Is.Not.Null, "Missing " + firstName);
        Assert.That(second, Is.Not.Null, "Missing " + secondName);

        var firstCorners = new Vector3[4];
        var secondCorners = new Vector3[4];
        first.GetWorldCorners(firstCorners);
        second.GetWorldCorners(secondCorners);
        RectTransform left = first.position.x <= second.position.x ? first : second;
        Vector3[] leftCorners = first.position.x <= second.position.x
            ? firstCorners : secondCorners;
        Vector3[] rightCorners = left == first ? secondCorners : firstCorners;
        Canvas canvas = root.GetComponentInParent<Canvas>();
        Assert.That(canvas, Is.Not.Null, "Capture root has no Canvas.");
        float leftEdge = RectTransformUtility.WorldToScreenPoint(
            canvas.worldCamera, leftCorners[2]).x;
        float rightEdge = RectTransformUtility.WorldToScreenPoint(
            canvas.worldCamera, rightCorners[0]).x;
        float gap = rightEdge - leftEdge;
        Assert.That(gap, Is.GreaterThanOrEqualTo(minimum),
            fileName + ": " + firstName + " and " + secondName +
            " have only " + gap.ToString("F2") + " px horizontal gap. " +
            "Local positions/sizes: " + first.anchoredPosition + "/" + first.rect.size +
            " and " + second.anchoredPosition + "/" + second.rect.size +
            "; screen edges " + leftEdge.ToString("F2") + " -> " +
            rightEdge.ToString("F2") + ".");
    }

    static void ApplySettingsSafeArea(Canvas canvas, int width, int height,
        Rect safeArea)
    {
        Transform safe = Find(canvas.transform, "SettingsSafeRoot");
        Assert.That(safe, Is.Not.Null);
        var owner = safe.GetComponent(RuntimeType("ResponsiveSafeAreaRoot"));
        Assert.That(owner, Is.Not.Null);
        var apply = owner.GetType().GetMethod("ApplyViewport",
            BindingFlags.Instance | BindingFlags.Public);
        Assert.That(apply, Is.Not.Null);
        var viewport = new Rect(0f, 0f, width, height);
        apply.Invoke(owner, new object[]
        {
            viewport, safeArea, new Vector2(1080f, 1920f)
        });
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

    static void SetLanguage(int value)
    {
        var type = RuntimeType("L10n");
        var enumType = type.GetNestedType("Language", BindingFlags.Public);
        var method = type.GetMethod("SetLanguage",
            BindingFlags.Public | BindingFlags.Static);
        method.Invoke(null, new[] { System.Enum.ToObject(enumType, value) });
    }

    static void InvokeInstaller(System.Type type)
    {
        var install = type.GetMethod("Install",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(install, Is.Not.Null);
        install.Invoke(null, null);
    }

    static System.Type RuntimeType(string name)
    {
        var type = System.Type.GetType(name + ", Assembly-CSharp");
        Assert.That(type, Is.Not.Null, "Missing runtime type: " + name);
        return type;
    }
}
