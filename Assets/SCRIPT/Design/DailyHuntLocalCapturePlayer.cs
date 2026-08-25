#if UNITY_STANDALONE_WIN && DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class DailyHuntLocalCapturePlayer : MonoBehaviour
{
    const string PathArgument = "-holDailyCapturePath";
    const string LanguageArgument = "-holDailyCaptureLanguage";
    const string WidthArgument = "-holDailyCaptureWidth";
    const string HeightArgument = "-holDailyCaptureHeight";
    const string ScaleArgument = "-holDailyCaptureScale";
    const string ViewArgument = "-holDailyCaptureView";
    const string StateArgument = "-holDailyCaptureState";

    static readonly string[] DailyKeys =
    {
        "DailyHuntDay",
        "DailyHuntUsed",
        "DailyHuntTrail",
        "DailyHuntDone",
        "DailyHuntFound",
        "DailyHuntRevived",
        "DailyHuntMin",
        "DailyHuntMax",
        "DailyHuntStreak",
        "DailyHuntLastFound",
        "DailyHuntPendingRevive",
        "DailyChallengeDay",
        "DailyChallengeWins",
        "DailyChallengeCorrectGuesses",
        "DailyChallengeRoomsShared",
        "DailyChallengeRewardClaimed",
        "DailyChallengePoints",
    };

    static string capturePath;
    static string language;
    static int width;
    static int height;
    static int captureScale;
    static string view;
    static bool completeMissionState;

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
        view = string.Equals(
            ReadArgument(ViewArgument), "hunt",
            StringComparison.OrdinalIgnoreCase) ? "hunt" : "missions";
        completeMissionState = string.Equals(
            ReadArgument(StateArgument), "complete",
            StringComparison.OrdinalIgnoreCase);
        if (width % captureScale != 0 || height % captureScale != 0)
            throw new InvalidOperationException(
                "Capture dimensions must be divisible by the capture scale.");

        string directory = Path.GetDirectoryName(capturePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);
        if (File.Exists(capturePath))
            File.Delete(capturePath);

        L10n.SetLanguage(language == "el"
            ? L10n.Language.Greek
            : L10n.Language.English);
        PlayerPrefs.SetInt("AdsConsent", 0);
        PlayerPrefs.DeleteKey("PlayerName");
        foreach (string key in DailyKeys)
            PlayerPrefs.DeleteKey(key);
        if (completeMissionState)
        {
            DailyChallengeProgress.RecordWin();
            DailyChallengeProgress.RecordCorrectGuess();
            DailyChallengeProgress.RecordCorrectGuess();
            DailyChallengeProgress.RecordCorrectGuess();
            DailyChallengeProgress.RecordRoomShared();
            // A deterministic visual fixture for the real live player chip;
            // no production gameplay path reads a fake value from artwork.
            PlayerPrefs.SetInt("DailyChallengePoints", 1250);
        }
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

        var host = new GameObject(nameof(DailyHuntLocalCapturePlayer));
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<DailyHuntLocalCapturePlayer>();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    IEnumerator Start()
    {
        DailyHunt hunt = null;
        DailyHuntVisuals visuals = null;
        for (int frame = 0; frame < 600; frame++)
        {
            HideCaptureOverlays();
            hunt = FindObjectOfType<DailyHunt>(true);
            visuals = hunt == null
                ? null
                : hunt.GetComponent<DailyHuntVisuals>();
            if (hunt != null && visuals != null && visuals.IsReady)
                break;
            yield return null;
        }

        if (hunt == null || visuals == null || !visuals.IsReady)
        {
            Debug.LogError("HOL_DAILY_HUNT_LOCAL_CAPTURE_NOT_READY");
            Application.Quit(2);
            yield break;
        }

        if (completeMissionState)
            visuals.SetCapturePlayerChipFixture(1250, 650);

        ApplyCaptureLanguage();
        hunt.Open();
        if (view == "hunt")
            hunt.StartChallenge();
        HideCaptureOverlays();
        yield return new WaitForSecondsRealtime(4.00f);
        // Standalone development players share PlayerPrefs. A concurrent
        // capture must not silently turn this artifact into mixed EN/EL.
        ApplyCaptureLanguage();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        TMP_Text stateHeading = FindText(
            hunt.transform,
            view == "hunt" ? "Title" : "DailyMissionHeading");
        Transform activeState = FindTransform(
            hunt.transform,
            view == "hunt" ? "DailyNumberHuntRoot" : "DailyMissionDashboard");
        if (!hunt.gameObject.activeInHierarchy ||
            activeState == null || !activeState.gameObject.activeInHierarchy ||
            stateHeading == null || !stateHeading.gameObject.activeInHierarchy ||
            string.IsNullOrWhiteSpace(stateHeading.text))
        {
            Debug.LogError("HOL_DAILY_HUNT_LOCAL_CAPTURE_INVALID_UI");
            Application.Quit(3);
            yield break;
        }

        HideCaptureOverlays();
        ScreenCapture.CaptureScreenshot(capturePath, captureScale);
        for (int frame = 0; frame < 600; frame++)
        {
            if (File.Exists(capturePath) &&
                new FileInfo(capturePath).Length > 1024)
            {
                Debug.Log(
                    "HOL_DAILY_HUNT_LOCAL_CAPTURE_READY " + view + " " + language + " " +
                    Screen.width + "x" + Screen.height + " x" +
                    captureScale + " " + capturePath);
                Application.Quit(0);
                yield break;
            }
            yield return null;
        }

        Debug.LogError("HOL_DAILY_HUNT_LOCAL_CAPTURE_TIMEOUT");
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

    static TMP_Text FindText(Transform root, string objectName)
    {
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            if (text.name == objectName)
                return text;
        return null;
    }

    static Transform FindTransform(Transform root, string objectName)
    {
        if (root == null) return null;
        if (root.name == objectName) return root;
        for (int index = 0; index < root.childCount; index++)
        {
            Transform found = FindTransform(root.GetChild(index), objectName);
            if (found != null) return found;
        }
        return null;
    }

    static int ReadPositiveInt(string key, int fallback)
    {
        return int.TryParse(ReadArgument(key), out int value) && value > 0
            ? value
            : fallback;
    }

    static void ApplyCaptureLanguage()
    {
        L10n.SetLanguage(language == "el"
            ? L10n.Language.Greek
            : L10n.Language.English);
    }

    static string ReadArgument(string key)
    {
        string[] arguments = Environment.GetCommandLineArgs();
        for (int i = 0; i + 1 < arguments.Length; i++)
            if (string.Equals(arguments[i], key,
                    StringComparison.OrdinalIgnoreCase))
                return arguments[i + 1];
        return null;
    }
}
#endif
