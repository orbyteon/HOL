using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Development-build QA seam for deterministic Android Solo-vs-AI preparation
// screenshots. Normal builds never inspect Android intent state or add this
// component. The capture holds the real preparation lifecycle before board
// completion; it does not introduce a production delay or fake matchmaking.
public sealed class SoloSearchCaptureBootstrap : MonoBehaviour
{
    const string CaptureExtra = "hol_capture_screen";
    const string LanguageExtra = "hol_capture_language";
    const string SoloSearchScreen = "solosearch";
    const string ReadyMarkerEnglish = "HOL_SOLOSEARCH_CAPTURE_READY_EN";
    const string ReadyMarkerGreek = "HOL_SOLOSEARCH_CAPTURE_READY_EL";
    const string AdsConsentPrefKey = "AdsConsent";
    const string PlayerNamePrefKey = "PlayerName";
    const string StreakPrefKey = "StatStreak";

    static bool markerLogged;
    static string requestedLanguage = "en";

    MenuManager menu;
    MainMenuHomeVisuals homeVisuals;
    MainMenuPlayVisuals playVisuals;
    FakeMatchmaking matchmaking;
    SoloSearchVisuals searchVisuals;
    bool openedPlay;
    bool startedSearch;
    bool presentationWaitStarted;

    public static bool CaptureRequested { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState()
    {
        SceneManager.sceneLoaded -= InstallForScene;
        CaptureRequested = false;
        markerLogged = false;
        requestedLanguage = "en";
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
#if UNITY_ANDROID && DEVELOPMENT_BUILD && !UNITY_EDITOR
        string requestedScreen = ReadIntentExtra(CaptureExtra);
        if (!ShouldCapture(true, true, requestedScreen))
            return;

        CaptureRequested = true;
        requestedLanguage = NormalizeLanguage(ReadIntentExtra(LanguageExtra));
        ApplyDeterministicCaptureState(requestedLanguage);
        SceneManager.sceneLoaded -= InstallForScene;
        SceneManager.sceneLoaded += InstallForScene;
#endif
    }

    public static bool ShouldCapture(
        bool android, bool development, string requestedScreen)
    {
        return android && development &&
               string.Equals(requestedScreen, SoloSearchScreen,
                   StringComparison.Ordinal);
    }

    public static string NormalizeLanguage(string language)
    {
        return string.Equals(language, "el", StringComparison.OrdinalIgnoreCase)
            ? "el"
            : "en";
    }

#if UNITY_ANDROID && DEVELOPMENT_BUILD && !UNITY_EDITOR
    static string ReadIntentExtra(string key)
    {
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
        using (var intent = activity.Call<AndroidJavaObject>("getIntent"))
        {
            return intent == null
                ? null
                : intent.Call<string>("getStringExtra", key);
        }
    }
#endif

    static void ApplyDeterministicCaptureState(string language)
    {
        L10n.SetLanguage(language == "el"
            ? L10n.Language.Greek
            : L10n.Language.English);
        PlayerPrefs.SetInt(AdsConsentPrefKey, 0);
        PlayerPrefs.DeleteKey(PlayerNamePrefKey);
        PlayerPrefs.SetInt(StreakPrefKey, 3);
        PlayerPrefs.Save();
    }

    static void InstallForScene(Scene scene, LoadSceneMode mode)
    {
        if (!CaptureRequested || scene.name != "MainMenu")
            return;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.GetComponentInChildren<SoloSearchCaptureBootstrap>(true) != null)
            {
                SceneManager.sceneLoaded -= InstallForScene;
                return;
            }
        }

        GameObject host = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            MenuManager found = root.GetComponentInChildren<MenuManager>(true);
            if (found == null) continue;
            host = found.gameObject;
            break;
        }

        GameObject[] roots = scene.GetRootGameObjects();
        if (host == null && roots.Length > 0)
            host = roots[0];
        if (host != null)
            host.AddComponent<SoloSearchCaptureBootstrap>();

        SceneManager.sceneLoaded -= InstallForScene;
    }

    void Update()
    {
        if (!CaptureRequested || markerLogged)
        {
            if (presentationWaitStarted)
                StopAllCoroutines();
            presentationWaitStarted = false;
            enabled = false;
            return;
        }

        HideCaptureOverlays();

        if (menu == null)
            menu = FindInScene<MenuManager>();
        if (homeVisuals == null)
            homeVisuals = FindInScene<MainMenuHomeVisuals>();
        if (menu == null || homeVisuals == null ||
            !homeVisuals.IsReady || !homeVisuals.IsSettled)
            return;

        if (matchmaking == null)
            matchmaking = FindInScene<FakeMatchmaking>();
        if (matchmaking == null || matchmaking.searchingPanel == null)
            return;

        if (!openedPlay)
        {
            // Explicit compatibility-capture seam for the retired
            // preparation presentation. Production ButtonPlay deliberately
            // bypasses PanelPlay/PanelSearching and enters PanelGAME.
            if (menu.settingsPanel != null) menu.settingsPanel.SetActive(false);
            if (menu.mainMenuPanel != null) menu.mainMenuPanel.SetActive(false);
            if (menu.panelSearching != null) menu.panelSearching.SetActive(false);
            if (matchmaking.panelGame != null) matchmaking.panelGame.SetActive(false);
            if (menu.panelPlay != null) menu.panelPlay.SetActive(true);
            SoloSearchVisuals.Install(matchmaking);
            openedPlay = true;
            return;
        }

        if (playVisuals == null)
            playVisuals = FindInScene<MainMenuPlayVisuals>();
        if (playVisuals == null ||
            !playVisuals.IsReady || !playVisuals.IsSettled)
            return;

        if (!startedSearch)
        {
            // Hold only this development capture before the board-ready edge so
            // the real modal remains deterministic while the emulator captures.
            matchmaking.BoardReadyProbe = () => false;
            matchmaking.StartSearch();
            startedSearch = true;
            return;
        }

        if (searchVisuals == null)
        {
            searchVisuals = matchmaking.searchingPanel
                .GetComponent<SoloSearchVisuals>();
        }

        if (searchVisuals == null || !searchVisuals.IsReady ||
            !matchmaking.IsPreparing ||
            !matchmaking.searchingPanel.activeInHierarchy)
            return;

        TMP_Text status = matchmaking.searchingText;
        if (status == null ||
            status.text != L10n.Get("solo_ai_preparing"))
            return;

        if (!presentationWaitStarted)
        {
            presentationWaitStarted = true;
            StartCoroutine(LogReadyAfterPresentation());
        }
    }

    IEnumerator LogReadyAfterPresentation()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        presentationWaitStarted = false;
        if (!CaptureRequested || markerLogged || matchmaking == null ||
            searchVisuals == null || !searchVisuals.IsReady ||
            !matchmaking.IsPreparing ||
            matchmaking.searchingPanel == null ||
            !matchmaking.searchingPanel.activeInHierarchy ||
            matchmaking.searchingText == null ||
            matchmaking.searchingText.text != L10n.Get("solo_ai_preparing"))
            yield break;

        HideCaptureOverlays();
        markerLogged = true;
        Debug.Log(requestedLanguage == "el"
            ? ReadyMarkerGreek
            : ReadyMarkerEnglish);
        enabled = false;
    }

    void HideCaptureOverlays()
    {
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
            {
                if (item.name != "ConsentPanel" &&
                    item.name != "ForceUpdatePanel")
                    continue;
                if (item.gameObject.activeSelf)
                    item.gameObject.SetActive(false);
            }
        }
    }

    T FindInScene<T>() where T : Component
    {
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            T found = root.GetComponentInChildren<T>(true);
            if (found != null) return found;
        }
        return null;
    }
}
