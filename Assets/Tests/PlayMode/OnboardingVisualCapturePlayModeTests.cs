using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using TMPro;
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
    readonly List<string> feedbackViolations = new List<string>();

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
            feedbackViolations.Clear();
            if (HasArgument("-holOnboardingFeedbackMatrix"))
            {
                outputDirectory = Path.Combine(outputDirectory,
                    DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff"));
                Directory.CreateDirectory(outputDirectory);
                foreach (Vector2Int viewport in new[]
                {
                    new Vector2Int(1080, 1920), new Vector2Int(1080, 2340),
                    new Vector2Int(1179, 2556),
                })
                    foreach (string locale in new[] { "en", "el" })
                        yield return CaptureStates(viewport.x, viewport.y, locale,
                            Path.Combine(outputDirectory, viewport.x + "x" + viewport.y, locale));
                File.WriteAllLines(Path.Combine(outputDirectory, "layout-violations.txt"),
                    feedbackViolations);
                Assert.That(feedbackViolations, Is.Empty,
                    string.Join("\n", feedbackViolations));
            }
            else
                yield return CaptureStates(width, height, language, outputDirectory);
        }
        finally
        {
            RestorePreferences();
        }
    }

    IEnumerator CaptureStates(int width, int height, string language, string outputDirectory)
    {
            Directory.CreateDirectory(outputDirectory);
            SetGameViewResolution(width, height);
            float resolutionDeadline = Time.realtimeSinceStartup + 10f;
            while ((Screen.width != width || Screen.height != height) &&
                   Time.realtimeSinceStartup < resolutionDeadline)
                yield return null;
            Assert.That(Screen.width, Is.EqualTo(width));
            Assert.That(Screen.height, Is.EqualTo(height));

            string[] steps = HasArgument("-holOnboardingAvatarFraming")
                ? new[] { "avatar" } : new[]
            {
                "welcome", "name", "gender", "avatar", "age",
            };
            foreach (string step in steps)
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

                if (HasArgument("-holOnboardingFeedbackMatrix"))
                    ValidateFeedbackLayout(design, step, language, width, height);

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
                if (HasArgument("-holOnboardingFeedbackMatrix"))
                {
                    // Live language repaint must recenter without rebuilding.
                    Type localization = RuntimeType("L10n");
                    MethodInfo setLanguage = localization.GetMethod("SetLanguage", StaticFlags);
                    Type languageType = setLanguage.GetParameters()[0].ParameterType;
                    foreach (string locale in new[] { language == "el" ? "en" : "el", language })
                    {
                        setLanguage.Invoke(null, new[] { Enum.ToObject(languageType, locale == "el" ? 1 : 0) });
                        for (int frame = 0; frame < 3; frame++) yield return null;
                        yield return new WaitForEndOfFrame();
                        ValidateFeedbackLayout(design, step, locale, width, height);
                    }
                    if (step == "name")
                    {
                        TMP_InputField input = GameObject.Find("OnboardingNameInput").GetComponent<TMP_InputField>();
                        foreach (string nickname in new[] { "Marinos", "Κωνσταντίνος" })
                        {
                            input.text = nickname;
                            for (int frame = 0; frame < 3; frame++) yield return null;
                            yield return new WaitForEndOfFrame();
                            Assert.That(input.text, Is.EqualTo(nickname));
                            ValidateFeedbackLayout(design, step, language, width, height);
                        }
                    }
                }
            }

            if (HasArgument(InteractionFlag))
                yield return CaptureInteractionEvidence(
                    width, height, language, outputDirectory);
    }

    void ValidateFeedbackLayout(Component design, string step, string language, int width, int height)
    {
        string context = language + " " + step + " " + width + "x" + height;
        // SplashDesign is attached to Splash, not to its sibling Canvas. Never
        // let a zero-label traversal produce a vacuous rendered-layout pass.
        GameObject root = GameObject.Find("HOLOnboardingRoot");
        Assert.That(root, Is.Not.Null);
        int checkedLabels = 0;
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (!text.gameObject.activeInHierarchy || string.IsNullOrWhiteSpace(text.text)) continue;
            text.ForceMeshUpdate();
            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            bool ink = false;
            for (int index = 0; index < text.textInfo.characterCount; index++)
            {
                TMP_CharacterInfo glyph = text.textInfo.characterInfo[index];
                if (!glyph.isVisible) continue;
                min = Vector2.Min(min, glyph.bottomLeft);
                max = Vector2.Max(max, glyph.topRight);
                ink = true;
            }
            if (!ink) continue;
            checkedLabels++;
            Rect rect = text.rectTransform.rect;
            if (text.isTextOverflowing || text.isTextTruncated ||
                min.x < rect.xMin - 1f || max.x > rect.xMax + 1f ||
                min.y < rect.yMin - 1f || max.y > rect.yMax + 1f)
                feedbackViolations.Add(context + " " + text.name +
                    " glyph=" + min + ".." + max + " rect=" + rect +
                    " font=" + text.fontSize + " text=" + text.text);

            Rect safeText = ExpectedTextFace(text, step);
            {
                Transform parent = text.transform.parent;
                Vector2 actualMin = parent.InverseTransformPoint(text.transform.TransformPoint(min));
                Vector2 actualMax = parent.InverseTransformPoint(text.transform.TransformPoint(max));
                Vector2 actualCenter = (actualMin + actualMax) * .5f;
                Vector2 pixels = (Vector2)parent.TransformPoint(actualCenter) -
                    (Vector2)parent.TransformPoint(safeText.center);
                if (Mathf.Abs(pixels.x) > 4f || Mathf.Abs(pixels.y) > 4f)
                    feedbackViolations.Add(context + " " + text.name + " glyph-center-px=" + pixels);
                if (actualMin.x < safeText.xMin - 1f || actualMax.x > safeText.xMax + 1f ||
                    actualMin.y < safeText.yMin - 1f || actualMax.y > safeText.yMax + 1f)
                    feedbackViolations.Add(context + " " + text.name + " escaped authored text face " + safeText);
            }
        }
        Assert.That(checkedLabels, Is.GreaterThanOrEqualTo(8), context + " no rendered text coverage");
        if (step == "avatar")
        {
            Transform avatarScreen = Array.Find(root.GetComponentsInChildren<Transform>(true),
                candidate => candidate.name == "OnboardingAvatarScreen");
            OnboardingFlowPlayModeTests.AssertAvatarArtwork(avatarScreen);
            Debug.Log("HOL_ONBOARDING_AVATAR_INK_CHECKED " + context + " portraits=12 preview=1");
        }
        Debug.Log("HOL_ONBOARDING_GLYPHS_CHECKED " + context + " labels=" + checkedLabels);
    }

    // Independent, fixed artwork/text-region contract: do not read corrected
    // text rects or production centering targets to manufacture expectations.
    static Rect ExpectedTextFace(TMP_Text text, string step)
    {
        string parent = text.transform.parent.name;
        switch (text.name)
        {
            case "WelcomeHeading": return Face(0, -475, 900, 260);
            case "WelcomeBody": return Face(0, -660, 820, 125);
            case "ProgressNumber": return Face(0, 0, 42, 42);
            case "OnboardingTitle": return step == "age" ? Face(0, 575, 940, 120) :
                Face(0, step == "avatar" ? 615 : 590, 940, 78);
            case "OnboardingSubtitle": return step == "age" ? Face(0, 475, 900, 94) :
                Face(0, step == "avatar" ? 535 : 510, 900, 66);
            case "NameHint": return Face(-300, -610, 340, 52);
            case "NameCounter": return Face(360, -610, 220, 52);
            case "Text": case "Placeholder": return Face(0, 0, 806, 128);
            case "GenderOtherHint": return Face(0, -407, 254, 106);
            case "AvatarPreviewPrompt": return Face(0, 15, 180, 170);
            case "AvatarSelectedStatus": return Face(0, -105, 248, 30);
            case "Availability": return Face(0, -99, 280, 46);
            case "AgePrivacyText": return Face(34, 2, 760, 124);
            case "Arrow": return parent == "WelcomeContinue" ? Face(288.6f, 9, 90, 70) :
                Face(340.4f, 10.25f, 90, 70);
            case "Label":
                if (parent == "WelcomeContinue") return Face(0, 9, 580, 110);
                if (parent.EndsWith("Continue", StringComparison.Ordinal)) return Face(0, 10.25f, 740, 110);
                if (parent.StartsWith("GenderCard", StringComparison.Ordinal)) return Face(0, -330, 274, 70);
                if (parent.StartsWith("AgeCard", StringComparison.Ordinal)) return Face(0, 0, 520, 90);
                if (parent.StartsWith("AvatarFilter", StringComparison.Ordinal)) return Face(0, 0, 154, 62);
                if (parent == "OnboardingGenderSkip") return Face(0, 0, 170, 64);
                break;
        }
        Assert.Fail("Missing independent text-face contract: " + parent + "/" + text.name);
        return default(Rect);
    }

    static Rect Face(float x, float y, float width, float height)
    {
        return new Rect(x - width * .5f, y - height * .5f, width, height);
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
