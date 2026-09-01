#if UNITY_STANDALONE_WIN && DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
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

    static string capturePath;
    static string language;
    static int width;
    static int height;
    static int captureScale;

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
            Debug.LogError("HOL_MAINMENU_LOCAL_CAPTURE_NOT_READY");
            Application.Quit(2);
            yield break;
        }

        L10n.SetLanguage(language == "el"
            ? L10n.Language.Greek
            : L10n.Language.English);
        HideCaptureOverlays();
        yield return new WaitForSecondsRealtime(2.0f);
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        if (!visuals.gameObject.activeInHierarchy)
        {
            Debug.LogError("HOL_MAINMENU_LOCAL_CAPTURE_INVALID_UI");
            Application.Quit(3);
            yield break;
        }

        DumpLayout(visuals.transform);
        ScreenCapture.CaptureScreenshot(capturePath, captureScale);
        for (int frame = 0; frame < 600; frame++)
        {
            if (File.Exists(capturePath) &&
                new FileInfo(capturePath).Length > 1024)
            {
                Debug.Log(
                    "HOL_MAINMENU_LOCAL_CAPTURE_READY " + language + " " +
                    Screen.width + "x" + Screen.height + " x" +
                    captureScale + " " + capturePath);
                Application.Quit(0);
                yield break;
            }
            yield return null;
        }

        Debug.LogError("HOL_MAINMENU_LOCAL_CAPTURE_TIMEOUT");
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
            "ButtonPrivateRoom",
            "DailyHuntButton",
            "HomeDailyPromo",
            "HomeMascotSix",
            "HomeMascotSeven",
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
