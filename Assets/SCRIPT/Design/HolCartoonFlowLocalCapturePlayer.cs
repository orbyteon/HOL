using System;
using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Development-player QA seam only. It drives the real MainMenu flows and
// captures their actual presentation owners; it does not create or restyle UI.
public sealed class HolCartoonFlowLocalCapturePlayer : MonoBehaviour
{
    const string ScreenArgument = "-holCaptureScreen";
    const string LanguageArgument = "-holCaptureLanguage";
    const string OutputArgument = "-holCapturePath";
    const string WidthArgument = "-holCaptureWidth";
    const string HeightArgument = "-holCaptureHeight";
    const string ScaleArgument = "-holCaptureScale";

    static string requestedScreen;
    static string requestedLanguage;
    static string capturePath;
    static int captureWidth;
    static int captureHeight;
    static int captureScale;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
        requestedScreen = ReadArgument(ScreenArgument);
        capturePath = ReadArgument(OutputArgument);
        if (string.IsNullOrWhiteSpace(requestedScreen) ||
            string.IsNullOrWhiteSpace(capturePath))
            return;

        requestedScreen = requestedScreen.Trim().ToLowerInvariant();
        requestedLanguage = string.Equals(
            ReadArgument(LanguageArgument), "el",
            StringComparison.OrdinalIgnoreCase) ? "el" : "en";
        captureWidth = ReadPositiveInt(WidthArgument, 1080);
        captureHeight = ReadPositiveInt(HeightArgument, 1920);
        captureScale = ReadPositiveInt(ScaleArgument, 2);
        if (captureWidth % captureScale != 0 ||
            captureHeight % captureScale != 0)
            throw new InvalidOperationException(
                "Capture dimensions must be divisible by capture scale.");
        Screen.SetResolution(
            captureWidth / captureScale,
            captureHeight / captureScale,
            FullScreenMode.Windowed);
        ApplyDeterministicState();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!scene.IsValid() || !scene.isLoaded || scene.name != "MainMenu")
            return;
        var host = new GameObject(nameof(HolCartoonFlowLocalCapturePlayer));
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<HolCartoonFlowLocalCapturePlayer>();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    IEnumerator Start()
    {
        bool ready = false;
        switch (requestedScreen)
        {
            case "home":
                yield return WaitForHome(value => ready = value);
                break;
            case "private":
                yield return WaitForPrivateRoom(value => ready = value);
                break;
            case "search":
                yield return WaitForSearch(value => ready = value);
                break;
            case "solo":
                yield return WaitForSolo(false, value => ready = value);
                break;
            case "result":
                yield return WaitForSolo(true, value => ready = value);
                break;
            case "pvp":
                yield return WaitForPvp(false, value => ready = value);
                break;
            case "pvpresult":
                yield return WaitForPvp(true, value => ready = value);
                break;
        }

        if (!ready)
        {
            Debug.LogError("HOL_CARTOON_FLOW_CAPTURE_NOT_READY " + requestedScreen);
            Application.Quit(2);
            yield break;
        }

        HideCaptureOverlays();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        string directory = Path.GetDirectoryName(capturePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);
        if (File.Exists(capturePath))
            File.Delete(capturePath);
        ScreenCapture.CaptureScreenshot(capturePath, captureScale);

        for (int frame = 0; frame < 300; frame++)
        {
            if (File.Exists(capturePath) && new FileInfo(capturePath).Length > 1024)
            {
                Debug.Log("HOL_CARTOON_FLOW_CAPTURE_READY " + requestedScreen +
                          " " + requestedLanguage + " " + capturePath);
                Application.Quit(0);
                yield break;
            }
            yield return null;
        }

        Debug.LogError("HOL_CARTOON_FLOW_CAPTURE_TIMEOUT " + capturePath);
        Application.Quit(3);
    }

    IEnumerator WaitForHome(Action<bool> complete)
    {
        for (int frame = 0; frame < 600; frame++)
        {
            HideCaptureOverlays();
            var owner = FindInScene<MainMenuHomeVisuals>();
            if (owner != null && owner.IsReady && owner.IsSettled)
            {
                complete(true);
                yield break;
            }
            yield return null;
        }
        complete(false);
    }

    IEnumerator WaitForPrivateRoom(Action<bool> complete)
    {
        bool opened = false;
        for (int frame = 0; frame < 600; frame++)
        {
            HideCaptureOverlays();
            var controller = FindInScene<PvpGameController>();
            var owner = controller == null
                ? null : controller.GetComponent<PrivateRoomVisuals>();
            if (controller == null || owner == null || !owner.IsReady)
            {
                yield return null;
                continue;
            }
            if (!opened)
            {
                controller.OpenPvpMenu();
                opened = true;
                yield return null;
                continue;
            }
            var input = FindNamed<TMP_InputField>("PrivateRoomLandingCodeInput");
            if (input != null && controller.pvpMenuPanel.activeInHierarchy)
            {
                input.SetTextWithoutNotify("MTW8H");
                complete(true);
                yield break;
            }
            yield return null;
        }
        complete(false);
    }

    IEnumerator WaitForSearch(Action<bool> complete)
    {
        bool openedPlay = false;
        bool started = false;
        for (int frame = 0; frame < 600; frame++)
        {
            HideCaptureOverlays();
            var menu = FindInScene<MenuManager>();
            var home = FindInScene<MainMenuHomeVisuals>();
            if (menu == null || home == null || !home.IsSettled)
            {
                yield return null;
                continue;
            }
            if (!openedPlay)
            {
                menu.OnPlayPressed();
                openedPlay = true;
                yield return null;
                continue;
            }
            var play = FindInScene<MainMenuPlayVisuals>();
            var matchmaking = FindInScene<FakeMatchmaking>();
            if (play == null || !play.IsSettled || matchmaking == null)
            {
                yield return null;
                continue;
            }
            if (!started)
            {
                matchmaking.BoardReadyProbe = () => false;
                matchmaking.StartSearch();
                started = true;
                yield return null;
                continue;
            }
            var owner = matchmaking.searchingPanel == null
                ? null : matchmaking.searchingPanel.GetComponent<SoloSearchVisuals>();
            if (owner != null && owner.IsReady && matchmaking.IsPreparing &&
                matchmaking.searchingPanel.activeInHierarchy)
            {
                complete(true);
                yield break;
            }
            yield return null;
        }
        complete(false);
    }

    IEnumerator WaitForSolo(bool result, Action<bool> complete)
    {
        for (int frame = 0; frame < 600; frame++)
        {
            HideCaptureOverlays();
            var layout = FindInScene<HolDuelBoardLayout>();
            var number = FindInScene<NumberManager>();
            if (layout == null || number == null)
            {
                yield return null;
                continue;
            }
            number.gameObject.SetActive(true);
            if (!layout.IsReady)
            {
                yield return null;
                continue;
            }
            layout.BeginNewMatch(requestedLanguage == "el" ? "ΑΝΔΡΕΑΣ" : "ANDREAS");
            layout.RecordPlayerGuess(68);
            layout.RecordPlayerGuess(27);
            layout.RecordPlayerGuess(42);
            layout.PresentPhase(
                result ? SoloBoardPhase.MatchResult : SoloBoardPhase.PlayerGuess,
                result ? SoloBoardPrompt.Win : SoloBoardPrompt.YourGuess,
                3, 28, 67, result ? 4 : 0);
            complete(true);
            yield break;
        }
        complete(false);
    }

    IEnumerator WaitForPvp(bool result, Action<bool> complete)
    {
        for (int frame = 0; frame < 600; frame++)
        {
            HideCaptureOverlays();
            var controller = FindInScene<PvpGameController>();
            var owner = controller == null
                ? null : controller.GetComponent<PvpDuelCartoonVisuals>();
            if (controller == null || owner == null || !owner.IsReady ||
                controller.matchPanel == null)
            {
                yield return null;
                continue;
            }
            controller.pvpMenuPanel.SetActive(false);
            controller.createPanel.SetActive(false);
            controller.joinPanel.SetActive(false);
            controller.matchPanel.SetActive(true);
            if (controller.opponentNameText != null)
                controller.opponentNameText.text = requestedLanguage == "el"
                    ? "ΑΝΔΡΕΑΣ" : "ANDREAS";
            if (controller.roundText != null)
                controller.roundText.text = L10n.Get("round_label_open", 3);
            if (controller.turnText != null)
                controller.turnText.text = L10n.Get("your_guess");
            if (result && controller.resultPresentation != null)
                controller.resultPresentation.ShowLocalized(
                    "you_win", 4, 6, 42, true);
            complete(true);
            yield break;
        }
        complete(false);
    }

    static void ApplyDeterministicState()
    {
        L10n.SetLanguage(requestedLanguage == "el"
            ? L10n.Language.Greek : L10n.Language.English);
        PlayerPrefs.SetInt("AdsConsent", 0);
        PlayerPrefs.SetString("PlayerName", requestedLanguage == "el"
            ? "ΜΑΡΙΝΟΣ" : "MARINOS");
        PlayerPrefs.SetInt("StatWins", 2450);
        PlayerPrefs.SetInt("StatStreak", 3);
        PlayerPrefs.Save();
    }

    static string ReadArgument(string key)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i + 1 < args.Length; i++)
            if (string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        return null;
    }

    static int ReadPositiveInt(string key, int fallback)
    {
        return int.TryParse(ReadArgument(key), out int value) && value > 0
            ? value : fallback;
    }

    static void HideCaptureOverlays()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid()) return;
        foreach (GameObject root in scene.GetRootGameObjects())
        foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
            if ((item.name == "ConsentPanel" || item.name == "ForceUpdatePanel") &&
                item.gameObject.activeSelf)
                item.gameObject.SetActive(false);
    }

    static T FindInScene<T>() where T : Component
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid()) return null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T found = root.GetComponentInChildren<T>(true);
            if (found != null) return found;
        }
        return null;
    }

    static T FindNamed<T>(string objectName) where T : Component
    {
        Scene scene = SceneManager.GetActiveScene();
        foreach (GameObject root in scene.GetRootGameObjects())
        foreach (T found in root.GetComponentsInChildren<T>(true))
            if (found.name == objectName) return found;
        return null;
    }
}
