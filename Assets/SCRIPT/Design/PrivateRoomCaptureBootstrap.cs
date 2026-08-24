using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Development-build QA seam for deterministic Android Private Room screenshots.
// Normal builds never inspect Android intent state or add this component.
public sealed class PrivateRoomCaptureBootstrap : MonoBehaviour
{
    const string CaptureExtra = "hol_capture_screen";
    const string LanguageExtra = "hol_capture_language";
    const string PrivateRoomScreen = "privateroom";
    const string ReadyMarkerEnglish = "HOL_PRIVATEROOM_CAPTURE_READY_EN";
    const string ReadyMarkerGreek = "HOL_PRIVATEROOM_CAPTURE_READY_EL";
    const string AdsConsentPrefKey = "AdsConsent";
    const string PlayerNamePrefKey = "PlayerName";
    const string StreakPrefKey = "StatStreak";
    const string ReferenceCode = "MTW8H";

    static bool markerLogged;
    static string requestedLanguage = "en";

    PvpGameController controller;
    PrivateRoomVisuals visuals;
    TMP_InputField landingCodeInput;
    bool openedPrivateRoom;
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
               string.Equals(requestedScreen, PrivateRoomScreen,
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
            return intent == null ? null : intent.Call<string>("getStringExtra", key);
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

        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.GetComponentInChildren<PrivateRoomCaptureBootstrap>(true) != null)
            {
                SceneManager.sceneLoaded -= InstallForScene;
                return;
            }
        }

        GameObject host = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            var menu = root.GetComponentInChildren<MenuManager>(true);
            if (menu == null) continue;
            host = menu.gameObject;
            break;
        }

        var roots = scene.GetRootGameObjects();
        if (host == null && roots.Length > 0)
            host = roots[0];
        if (host != null)
            host.AddComponent<PrivateRoomCaptureBootstrap>();

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

        if (controller == null)
            controller = FindInScene<PvpGameController>();
        if (visuals == null && controller != null)
            visuals = controller.GetComponent<PrivateRoomVisuals>();
        if (controller == null || visuals == null || !visuals.IsReady ||
            controller.pvpMenuPanel == null)
            return;

        if (!openedPrivateRoom)
        {
            controller.OpenPvpMenu();
            openedPrivateRoom = true;
            return;
        }

        if (!controller.pvpMenuPanel.activeInHierarchy)
            return;

        if (landingCodeInput == null)
            landingCodeInput = FindNamedInput("PrivateRoomLandingCodeInput");
        if (landingCodeInput == null)
            return;
        if (landingCodeInput.text != ReferenceCode)
            landingCodeInput.SetTextWithoutNotify(ReferenceCode);

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
        if (!CaptureRequested || markerLogged || controller == null ||
            visuals == null || !visuals.IsReady ||
            controller.pvpMenuPanel == null ||
            !controller.pvpMenuPanel.activeInHierarchy ||
            landingCodeInput == null || landingCodeInput.text != ReferenceCode)
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
        foreach (var root in gameObject.scene.GetRootGameObjects())
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                if (transform.name != "ConsentPanel" &&
                    transform.name != "ForceUpdatePanel")
                    continue;
                if (transform.gameObject.activeSelf)
                    transform.gameObject.SetActive(false);
            }
        }
    }

    TMP_InputField FindNamedInput(string objectName)
    {
        foreach (var root in gameObject.scene.GetRootGameObjects())
        {
            foreach (var input in root.GetComponentsInChildren<TMP_InputField>(true))
                if (input.name == objectName)
                    return input;
        }
        return null;
    }

    T FindInScene<T>() where T : Component
    {
        foreach (var root in gameObject.scene.GetRootGameObjects())
        {
            var found = root.GetComponentInChildren<T>(true);
            if (found != null) return found;
        }
        return null;
    }
}
