using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class OnboardingVisualCapturePlayModeTests
{
    const BindingFlags StaticFlags =
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    const BindingFlags InstanceFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    const string CaptureFlag = "-holOnboardingEditorCapture";
    const string InteractionFlag = "-holOnboardingInteractionCapture";

    readonly string[] preferenceKeys =
    {
        "PlayerName",
        "HOL.Onboarding.Version",
        "HOL.Onboarding.Gender",
        "HOL.Onboarding.Avatar",
        "HOL.Onboarding.AgeCategory",
        "Language",
    };

    bool[] hadKey;
    string savedName;
    int[] savedInts;

    [UnityTest]
    public IEnumerator CaptureAllFiveStatesAtRequestedGameViewResolution()
    {
        if (!HasArgument(CaptureFlag))
            Assert.Ignore("Explicit visual capture seam.");

        int width = ReadIntArgument("-holOnboardingWidth", 1080);
        int height = ReadIntArgument("-holOnboardingHeight", 1920);
        string language = ReadArgument("-holOnboardingLanguage") ?? "en";
        string outputDirectory = ReadArgument("-holOnboardingOutput") ??
            Path.Combine("artifacts", "onboarding", width + "x" + height, language);
        outputDirectory = Path.GetFullPath(outputDirectory);
        Directory.CreateDirectory(outputDirectory);

        SavePreferences();
        try
        {
            SetGameViewResolution(width, height);
            float resolutionDeadline = Time.realtimeSinceStartup + 10f;
            while ((Screen.width != width || Screen.height != height) &&
                   Time.realtimeSinceStartup < resolutionDeadline)
                yield return null;
            Assert.That(Screen.width, Is.EqualTo(width));
            Assert.That(Screen.height, Is.EqualTo(height));

            foreach (string step in new[]
            {
                "welcome", "name", "gender", "avatar", "age",
            })
            {
                ClearOnboardingPreferences(language);
                yield return SceneManager.LoadSceneAsync(
                    "SplashScene", LoadSceneMode.Single);
                yield return null;

                Scene scene = SceneManager.GetActiveScene();
                Component design = FindInScene(scene, RuntimeType("SplashDesign"));
                Component controller = FindInScene(
                    scene, RuntimeType("SplashOnboardingController"));
                PropertyInfo ready = design.GetType().GetProperty(
                    "IsReady", InstanceFlags);
                PropertyInfo settled = design.GetType().GetProperty(
                    "IsSettled", InstanceFlags);
                PropertyInfo visible = design.GetType().GetProperty(
                    "IsOnboardingVisible", InstanceFlags);

                float deadline = Time.realtimeSinceStartup + 10f;
                while (Time.realtimeSinceStartup < deadline &&
                       (!(bool)ready.GetValue(design, null) ||
                        !(bool)settled.GetValue(design, null) ||
                        !(bool)visible.GetValue(design, null)))
                    yield return null;
                Assert.That((bool)ready.GetValue(design, null), Is.True);
                Assert.That((bool)settled.GetValue(design, null), Is.True);
                Assert.That((bool)visible.GetValue(design, null), Is.True);

                Rect safeRect = ApplyDeterministicSafeArea(
                    scene, width, height);
                DriveToStep(controller, step);
                for (int frame = 0; frame < 3; frame++) yield return null;
                yield return new WaitForEndOfFrame();
                yield return new WaitForEndOfFrame();

                string path = Path.Combine(outputDirectory, step + ".png");
                if (File.Exists(path)) File.Delete(path);
                ScreenCapture.CaptureScreenshot(path);
                deadline = Time.realtimeSinceStartup + 10f;
                while (Time.realtimeSinceStartup < deadline &&
                       (!File.Exists(path) || new FileInfo(path).Length == 0))
                    yield return null;
                Assert.That(File.Exists(path), Is.True, path);
                Assert.That(new FileInfo(path).Length, Is.GreaterThan(0), path);
                Debug.Log("HOL_ONBOARDING_EDITOR_CAPTURE_READY " +
                    step + " " + language + " " +
                    Screen.width + "x" + Screen.height + " safe=" +
                    RectToken(safeRect) + " " + path);
            }

            if (HasArgument(InteractionFlag))
                yield return CaptureInteractionEvidence(
                    width, height, language, outputDirectory);
        }
        finally
        {
            RestorePreferences();
        }
    }

    IEnumerator CaptureInteractionEvidence(
        int width, int height, string language, string matrixDirectory)
    {
        string outputDirectory = ReadArgument(
            "-holOnboardingInteractionOutput") ??
            Path.Combine(matrixDirectory, "interaction");
        outputDirectory = Path.GetFullPath(outputDirectory);
        Directory.CreateDirectory(outputDirectory);

        ClearOnboardingPreferences(language);
        yield return SceneManager.LoadSceneAsync(
            "SplashScene", LoadSceneMode.Single);
        yield return null;
        Scene scene = SceneManager.GetActiveScene();
        Component design = FindInScene(scene, RuntimeType("SplashDesign"));
        Component controller = FindInScene(
            scene, RuntimeType("SplashOnboardingController"));
        PropertyInfo ready = design.GetType().GetProperty(
            "IsReady", InstanceFlags);
        float deadline = Time.realtimeSinceStartup + 10f;
        while (Time.realtimeSinceStartup < deadline &&
               !(bool)ready.GetValue(design, null))
            yield return null;
        Assert.That((bool)ready.GetValue(design, null), Is.True);

        Rect safeRect = ApplyDeterministicSafeArea(scene, width, height);
        Type type = controller.GetType();
        MethodInfo advance = type.GetMethod("Advance", InstanceFlags);
        advance.Invoke(controller, null);
        yield return CaptureInteractionFrame(
            outputDirectory, "name-disabled.png", safeRect);
        type.GetMethod("SetName", InstanceFlags)
            .Invoke(controller, new object[] { "Marinos" });
        yield return CaptureInteractionFrame(
            outputDirectory, "name-valid.png", safeRect);

        advance.Invoke(controller, null);
        yield return CaptureInteractionFrame(
            outputDirectory, "gender-unselected.png", safeRect);
        type.GetMethod("SelectGender", InstanceFlags)
            .Invoke(controller, new object[] { 0 });
        yield return CaptureInteractionFrame(
            outputDirectory, "gender-selected.png", safeRect);

        advance.Invoke(controller, null);
        yield return CaptureInteractionFrame(
            outputDirectory, "avatar-unselected-locked-visible.png", safeRect);
        type.GetMethod("SelectAvatar", InstanceFlags)
            .Invoke(controller, new object[] { 0 });
        yield return CaptureInteractionFrame(
            outputDirectory, "avatar1-free-selected.png", safeRect);

        advance.Invoke(controller, null);
        yield return CaptureInteractionFrame(
            outputDirectory, "age-unselected.png", safeRect);
        type.GetMethod("SelectAge", InstanceFlags)
            .Invoke(controller, new object[] { 0 });
        yield return CaptureInteractionFrame(
            outputDirectory, "age-selected.png", safeRect);
    }

    static IEnumerator CaptureInteractionFrame(
        string outputDirectory, string fileName, Rect safeRect)
    {
        for (int frame = 0; frame < 3; frame++) yield return null;
        yield return new WaitForEndOfFrame();
        string path = Path.Combine(outputDirectory, fileName);
        if (File.Exists(path)) File.Delete(path);
        ScreenCapture.CaptureScreenshot(path);
        float deadline = Time.realtimeSinceStartup + 10f;
        while (Time.realtimeSinceStartup < deadline &&
               (!File.Exists(path) || new FileInfo(path).Length == 0))
            yield return null;
        Assert.That(File.Exists(path), Is.True, path);
        Assert.That(new FileInfo(path).Length, Is.GreaterThan(0), path);
        Debug.Log("HOL_ONBOARDING_INTERACTION_CAPTURE_READY " +
            fileName + " " + Screen.width + "x" + Screen.height +
            " safe=" + RectToken(safeRect) + " " + path);
    }

    static Rect ApplyDeterministicSafeArea(
        Scene scene, int width, int height)
    {
        int defaultTop = Mathf.RoundToInt(height * 0.045f);
        int defaultBottom = Mathf.RoundToInt(height * 0.025f);
        int top = ReadNonNegativeIntArgument(
            "-holOnboardingSafeTop", defaultTop);
        int bottom = ReadNonNegativeIntArgument(
            "-holOnboardingSafeBottom", defaultBottom);
        Assert.That(top + bottom, Is.LessThan(height));

        Component safeArea = FindInScene(
            scene, RuntimeType("ResponsiveSafeAreaRoot"));
        Canvas canvas = safeArea.GetComponentInParent<Canvas>();
        Assert.That(canvas, Is.Not.Null);
        RectTransform canvasRect = canvas.transform as RectTransform;
        Vector2 canvasSize = canvasRect != null &&
            canvasRect.rect.width > 0f && canvasRect.rect.height > 0f
                ? canvasRect.rect.size
                : new Vector2(1080f, 1920f);
        Rect safePixels = new Rect(
            0f, bottom, width, height - top - bottom);
        safeArea.GetType().GetMethod("ApplyViewport", InstanceFlags)
            .Invoke(safeArea, new object[]
            {
                new Rect(0f, 0f, width, height), safePixels, canvasSize,
            });
        Canvas.ForceUpdateCanvases();
        return (Rect)safeArea.GetType().GetProperty(
            "LastSafeRect", InstanceFlags).GetValue(safeArea, null);
    }

    static string RectToken(Rect rect)
    {
        return Mathf.RoundToInt(rect.x) + "," +
            Mathf.RoundToInt(rect.y) + "," +
            Mathf.RoundToInt(rect.width) + "," +
            Mathf.RoundToInt(rect.height);
    }

    static void DriveToStep(Component controller, string step)
    {
        Type type = controller.GetType();
        MethodInfo advance = type.GetMethod("Advance", InstanceFlags);
        if (step == "welcome") return;
        advance.Invoke(controller, null);
        if (step == "name") return;
        type.GetMethod("SetName", InstanceFlags)
            .Invoke(controller, new object[] { "Marinos" });
        advance.Invoke(controller, null);
        type.GetMethod("SelectGender", InstanceFlags)
            .Invoke(controller, new object[] { 0 });
        if (step == "gender") return;
        advance.Invoke(controller, null);
        type.GetMethod("SelectAvatar", InstanceFlags)
            .Invoke(controller, new object[] { 0 });
        if (step == "avatar") return;
        advance.Invoke(controller, null);
        type.GetMethod("SelectAge", InstanceFlags)
            .Invoke(controller, new object[] { 0 });
    }

    static void SetGameViewResolution(int width, int height)
    {
        Type utility = Type.GetType(
            "OnboardingGameViewCapture, Assembly-CSharp-Editor");
        Assert.That(utility, Is.Not.Null);
        utility.GetMethod("SetResolution", StaticFlags)
            .Invoke(null, new object[] { width, height });
    }

    void SavePreferences()
    {
        hadKey = new bool[preferenceKeys.Length];
        savedInts = new int[preferenceKeys.Length];
        for (int index = 0; index < preferenceKeys.Length; index++)
        {
            hadKey[index] = PlayerPrefs.HasKey(preferenceKeys[index]);
            if (index == 0)
                savedName = PlayerPrefs.GetString(preferenceKeys[index], string.Empty);
            else
                savedInts[index] = PlayerPrefs.GetInt(preferenceKeys[index], 0);
        }
    }

    static void ClearOnboardingPreferences(string language)
    {
        PlayerPrefs.DeleteKey("PlayerName");
        PlayerPrefs.DeleteKey("HOL.Onboarding.Version");
        PlayerPrefs.DeleteKey("HOL.Onboarding.Gender");
        PlayerPrefs.DeleteKey("HOL.Onboarding.Avatar");
        PlayerPrefs.DeleteKey("HOL.Onboarding.AgeCategory");
        PlayerPrefs.SetInt("Language",
            string.Equals(language, "el", StringComparison.OrdinalIgnoreCase)
                ? 1
                : 0);
        PlayerPrefs.Save();

        Type l10n = RuntimeType("L10n");
        Type languageType = l10n.GetNestedType("Language", BindingFlags.Public);
        object value = Enum.ToObject(languageType,
            string.Equals(language, "el", StringComparison.OrdinalIgnoreCase)
                ? 1
                : 0);
        l10n.GetMethod("SetLanguage", StaticFlags)
            .Invoke(null, new[] { value });
    }

    void RestorePreferences()
    {
        if (hadKey == null) return;
        for (int index = 0; index < preferenceKeys.Length; index++)
        {
            if (!hadKey[index])
            {
                PlayerPrefs.DeleteKey(preferenceKeys[index]);
                continue;
            }
            if (index == 0)
                PlayerPrefs.SetString(preferenceKeys[index], savedName);
            else
                PlayerPrefs.SetInt(preferenceKeys[index], savedInts[index]);
        }
        PlayerPrefs.Save();
    }

    static Component FindInScene(Scene scene, Type type)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Component found = root.GetComponentInChildren(type, true) as Component;
            if (found != null) return found;
        }
        Assert.Fail(type.Name + " is missing from " + scene.name);
        return null;
    }

    static Type RuntimeType(string name)
    {
        Type type = Type.GetType(name + ", Assembly-CSharp");
        Assert.That(type, Is.Not.Null, name + " runtime type is missing.");
        return type;
    }

    static bool HasArgument(string name)
    {
        foreach (string argument in Environment.GetCommandLineArgs())
            if (string.Equals(argument, name, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    static string ReadArgument(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int index = 0; index + 1 < args.Length; index++)
            if (string.Equals(args[index], name, StringComparison.OrdinalIgnoreCase))
                return args[index + 1];
        return null;
    }

    static int ReadIntArgument(string name, int fallback)
    {
        return int.TryParse(ReadArgument(name), out int value) && value > 0
            ? value
            : fallback;
    }

    static int ReadNonNegativeIntArgument(string name, int fallback)
    {
        return int.TryParse(ReadArgument(name), out int value) && value >= 0
            ? value
            : fallback;
    }
}
