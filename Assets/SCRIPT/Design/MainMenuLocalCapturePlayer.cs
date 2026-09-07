#if UNITY_STANDALONE_WIN && DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Standalone-only visual evidence seam. It never participates in a normal
// player because installation requires an explicit capture output argument.
public sealed class MainMenuLocalCapturePlayer : MonoBehaviour
{
    const string PathArgument = "-holMainMenuCapturePath";
    const string LanguageArgument = "-holMainMenuCaptureLanguage";
    const string WidthArgument = "-holMainMenuCaptureWidth";
    const string HeightArgument = "-holMainMenuCaptureHeight";
    const string ScaleArgument = "-holMainMenuCaptureScale";
    const string AvatarArgument = "-holMainMenuCaptureAvatar";
    const string ScreenArgument = "-holMainMenuCaptureScreen";
    const string HomeScreen = "home";
    const string PanelPlayScreen = "panelplay";

    static string capturePath;
    static string language;
    static int width;
    static int height;
    static int captureScale;
    static string captureScreen;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
        capturePath = ReadArgument(PathArgument);
        if (string.IsNullOrWhiteSpace(capturePath))
            return;

        language = string.Equals(
            ReadArgument(LanguageArgument), "el",
            StringComparison.OrdinalIgnoreCase) ? "el" : "en";
        width = ReadPositiveInt(WidthArgument, 1080);
        height = ReadPositiveInt(HeightArgument, 1920);
        captureScale = ReadPositiveInt(ScaleArgument, 1);
        captureScreen = ReadArgument(ScreenArgument);
        if (string.IsNullOrWhiteSpace(captureScreen))
            captureScreen = HomeScreen;
        captureScreen = captureScreen.ToLowerInvariant();
        if (captureScreen != HomeScreen && captureScreen != PanelPlayScreen)
            throw new InvalidOperationException(
                "Capture screen must be 'home' or 'panelplay'.");
        if (width % captureScale != 0 || height % captureScale != 0)
            throw new InvalidOperationException(
                "Capture dimensions must be divisible by capture scale.");

        string directory = Path.GetDirectoryName(capturePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);
        if (File.Exists(capturePath))
            File.Delete(capturePath);

        L10n.SetLanguage(language == "el"
            ? L10n.Language.Greek
            : L10n.Language.English);
        PlayerPrefs.SetInt("AdsConsent", 0);
        PlayerPrefs.SetString(
            "PlayerName", language == "el" ? "Παίκτης" : "Player");
        PlayerPrefs.SetInt("StatWins", 2450);
        ApplyCaptureAvatar(ReadArgument(AvatarArgument));
        PlayerPrefs.Save();

        Screen.SetResolution(
            width / captureScale,
            height / captureScale,
            FullScreenMode.Windowed);
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (string.IsNullOrWhiteSpace(capturePath) ||
            scene.name != "MainMenu")
            return;

        var host = new GameObject(nameof(MainMenuLocalCapturePlayer));
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<MainMenuLocalCapturePlayer>();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    IEnumerator Start()
    {
        MainMenuHomeVisuals visuals = null;
        for (int frame = 0; frame < 600; frame++)
        {
            HideCaptureOverlays();
            visuals = FindObjectOfType<MainMenuHomeVisuals>(true);
            if (visuals != null && visuals.IsReady && visuals.IsSettled)
                break;
            yield return null;
        }

        if (visuals == null || !visuals.IsReady || !visuals.IsSettled)
        {
            Debug.LogError(
                "HOL_MAINMENU_LOCAL_CAPTURE_NOT_READY " + captureScreen);
            Application.Quit(2);
            yield break;
        }

        L10n.SetLanguage(language == "el"
            ? L10n.Language.Greek
            : L10n.Language.English);
        HideCaptureOverlays();

        Transform captureRoot = visuals.transform;
        if (captureScreen == PanelPlayScreen)
        {
            MenuManager menu = FindObjectOfType<MenuManager>(true);
            if (menu == null)
            {
                Debug.LogError(
                    "HOL_MAINMENU_LOCAL_CAPTURE_NOT_READY panelplay menu");
                Application.Quit(2);
                yield break;
            }

            // Exercise the real production Home callback; never force the
            // selector hierarchy active from this evidence-only seam.
            menu.OnPlayPressed();
            MainMenuPlayVisuals play = null;
            for (int frame = 0; frame < 600; frame++)
            {
                HideCaptureOverlays();
                play = FindObjectOfType<MainMenuPlayVisuals>(true);
                bool homeHidden = menu.mainMenuPanel == null ||
                                  !menu.mainMenuPanel.activeInHierarchy;
                bool selectorVisible = menu.panelPlay != null &&
                                       menu.panelPlay.activeInHierarchy;
                if (play != null && play.IsReady && play.IsSettled &&
                    homeHidden && selectorVisible)
                    break;
                yield return null;
            }

            if (play == null || !play.IsReady || !play.IsSettled ||
                menu.panelPlay == null || !menu.panelPlay.activeInHierarchy ||
                (menu.mainMenuPanel != null &&
                 menu.mainMenuPanel.activeInHierarchy))
            {
                Debug.LogError(
                    "HOL_MAINMENU_LOCAL_CAPTURE_NOT_READY panelplay owner");
                Application.Quit(2);
                yield break;
            }
            captureRoot = play.transform;
        }

        yield return new WaitForSecondsRealtime(2.0f);
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        Transform expectedRoot = FindRect(
            captureRoot,
            captureScreen == HomeScreen
                ? MainMenuHomeVisuals.VisualRootName
                : MainMenuPlayVisuals.VisualRootName);
        if (expectedRoot == null || !expectedRoot.gameObject.activeInHierarchy)
        {
            Debug.LogError(
                "HOL_MAINMENU_LOCAL_CAPTURE_INVALID_UI " + captureScreen);
            Application.Quit(3);
            yield break;
        }

        DumpLayout(captureRoot);
        if (!ValidateRenderedText(expectedRoot))
        {
            Debug.LogError("HOL_MAINMENU_TEXT_CENTERING_FAILED " + captureScreen);
            Application.Quit(5);
            yield break;
        }
        ScreenCapture.CaptureScreenshot(capturePath, captureScale);
        for (int frame = 0; frame < 600; frame++)
        {
            if (File.Exists(capturePath) &&
                new FileInfo(capturePath).Length > 1024)
            {
                Debug.Log(
                    "HOL_MAINMENU_LOCAL_CAPTURE_READY " + captureScreen + " " +
                    language + " " +
                    Screen.width + "x" + Screen.height + " x" +
                    captureScale + " " + capturePath);
                Application.Quit(0);
                yield break;
            }
            yield return null;
        }

        Debug.LogError(
            "HOL_MAINMENU_LOCAL_CAPTURE_TIMEOUT " + captureScreen);
        Application.Quit(4);
    }

    static void HideCaptureOverlays()
    {
        foreach (GameObject root in
                 SceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (Transform item in
                     root.GetComponentsInChildren<Transform>(true))
            {
                if (item.name == "ConsentPanel" ||
                    item.name == "ForceUpdatePanel")
                    item.gameObject.SetActive(false);
            }
        }
    }

    // Evidence-only measurement, independent of the positioning algorithm.
    // This reads the settled mesh; it never fixes text or changes capture timing.
    static bool ValidateRenderedText(Transform root)
    {
        MainMenuCenteredTextRegion[] regions = captureScreen == HomeScreen
            ? FindObjectOfType<MainMenuHomeVisuals>(true).CenteredTextRegions
            : FindObjectOfType<MainMenuPlayVisuals>(true).CenteredTextRegions;
        if (regions == null || regions.Length != 8) return false;
        bool valid = true;
        int measured = 0;
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(false))
        {
            MainMenuCenteredTextRegion region = Array.Find(regions, item => item.Text == text);
            if (region == null) { valid = false; continue; }
            RectTransform rect = text.rectTransform;
            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            int glyphs = 0;
            for (int index = 0; index < text.textInfo.characterCount; index++)
            {
                TMP_CharacterInfo glyph = text.textInfo.characterInfo[index];
                if (!glyph.isVisible) continue;
                foreach (Vector3 vertex in new[]
                         { glyph.bottomLeft, glyph.topLeft, glyph.topRight, glyph.bottomRight })
                {
                    Vector2 pixel = RectTransformUtility.WorldToScreenPoint(null,
                        rect.TransformPoint(vertex)) * captureScale;
                    min = Vector2.Min(min, pixel);
                    max = Vector2.Max(max, pixel);
                }
                glyphs++;
            }
            Vector2 safeMin = RectTransformUtility.WorldToScreenPoint(null,
                rect.parent.TransformPoint(region.SafeRect.min)) * captureScale;
            Vector2 safeMax = RectTransformUtility.WorldToScreenPoint(null,
                rect.parent.TransformPoint(region.SafeRect.max)) * captureScale;
            Vector2 delta = (min + max - safeMin - safeMax) * 0.5f;
            bool contained = min.x >= safeMin.x && min.y >= safeMin.y &&
                             max.x <= safeMax.x && max.y <= safeMax.y;
            bool passed = glyphs > 0 && contained &&
                          Mathf.Abs(delta.x) <= 4f && Mathf.Abs(delta.y) <= 4f &&
                          !text.isTextOverflowing && !text.isTextTruncated;
            valid &= passed;
            measured++;
            Debug.Log(string.Format(CultureInfo.InvariantCulture,
                "HOL_MAINMENU_GLYPH_CENTER {0} {1} {2} deltaPx=({3:F3},{4:F3}) " +
                "glyphPx=({5:F3},{6:F3},{7:F3},{8:F3}) safePx=({9:F3},{10:F3},{11:F3},{12:F3}) " +
                "font={13:F3} lines={14} glyphs={15} contained={16} overflow={17} truncated={18} pass={19}",
                captureScreen, language, text.name, delta.x, delta.y,
                min.x, min.y, max.x, max.y, safeMin.x, safeMin.y, safeMax.x, safeMax.y,
                text.fontSize, text.textInfo.lineCount, glyphs, contained,
                text.isTextOverflowing, text.isTextTruncated, passed));
        }
        Debug.Log("HOL_MAINMENU_GLYPH_CENTER_TOTAL " + measured + "/8 pass=" + valid);
        return valid && measured == 8;
    }

    static void DumpLayout(Transform root)
    {
        foreach (string objectName in new[]
        {
            "HomeSafeAreaRoot",
            "HomeLogo",
            "HomeHeroBoy",
            "HomeHeroGirl",
            "HomeSpeechBubble",
            "HomePlayerChip",
            "ButtonPlay",
            "ButtonPvP",
            "DailyHuntButton",
            "HomeDailyPromo",
            "HomeMascotSix",
            "HomeMascotSeven",
            "PlaySafeAreaRoot",
            "PlayLogo",
            "PlayHubTitle",
            "PlayHubSubtitle",
            "ButtonChallenger",
            "PlaySoloTitle",
            "PlaySoloSubtitle",
            "PlayFriendTitle",
            "PlayFriendSubtitle",
            "ButtonBack",
        })
        {
            RectTransform rect = FindRect(root, objectName);
            if (rect == null) continue;

            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Debug.Log(
                "HOL_MAINMENU_LAYOUT " + objectName +
                " anchored=" + rect.anchoredPosition +
                " size=" + rect.sizeDelta +
                " scale=" + rect.lossyScale +
                " worldBL=" + corners[0] +
                " worldTR=" + corners[2] +
                " parent=" + (rect.parent == null ? "<none>" : rect.parent.name));
        }
    }

    static RectTransform FindRect(Transform root, string objectName)
    {
        if (root == null) return null;
        if (root.name == objectName) return root as RectTransform;
        for (int index = 0; index < root.childCount; index++)
        {
            RectTransform found = FindRect(root.GetChild(index), objectName);
            if (found != null) return found;
        }
        return null;
    }

    static int ReadPositiveInt(string key, int fallback)
    {
        string value = ReadArgument(key);
        return int.TryParse(value, out int parsed) && parsed > 0
            ? parsed
            : fallback;
    }

    static void ApplyCaptureAvatar(string value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            string.Equals(value, "missing", StringComparison.OrdinalIgnoreCase))
        {
            PlayerPrefs.DeleteKey(OnboardingProfile.AvatarKey);
            PlayerPrefs.SetInt(
                OnboardingProfile.VersionKey, OnboardingProfile.CurrentVersion);
            return;
        }

        if (!int.TryParse(value, out int avatarIndex) ||
            !OnboardingProfile.IsValidAvatar(avatarIndex))
            throw new InvalidOperationException(
                "Capture avatar must be a valid Onboarding index or 'missing'.");

        PlayerPrefs.SetInt(OnboardingProfile.AvatarKey, avatarIndex);
        PlayerPrefs.SetInt(
            OnboardingProfile.VersionKey, OnboardingProfile.CurrentVersion);
    }

    static string ReadArgument(string key)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int index = 0; index < args.Length - 1; index++)
            if (string.Equals(args[index], key, StringComparison.Ordinal))
                return args[index + 1];
        return null;
    }
}
#endif
