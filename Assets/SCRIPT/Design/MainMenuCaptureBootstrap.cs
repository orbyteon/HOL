using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Development-build QA seam for deterministic Android Main Menu Home screenshots.
// Normal builds never inspect Android intent state or add this component.
public sealed class MainMenuCaptureBootstrap : MonoBehaviour
{
    const string CaptureExtra = "hol_capture_screen";
    const string MainMenuScreen = "mainmenu";
    const string ReadyMarker = "HOL_MAINMENU_CAPTURE_READY";
    const string AdsConsentPrefKey = "AdsConsent";

    static bool markerLogged;
    MainMenuHomeVisuals homeVisuals;
    bool presentationWaitStarted;
    int presentationBarriersPassed;

    public static bool CaptureRequested { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState()
    {
        SceneManager.sceneLoaded -= InstallForScene;
        CaptureRequested = false;
        markerLogged = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
#if UNITY_ANDROID && DEVELOPMENT_BUILD && !UNITY_EDITOR
        string requestedScreen = ReadRequestedScreen();
        if (!ShouldCapture(true, true, requestedScreen))
            return;

        CaptureRequested = true;
        if (!PlayerPrefs.HasKey(AdsConsentPrefKey))
        {
            PlayerPrefs.SetInt(AdsConsentPrefKey, 0);
            PlayerPrefs.Save();
        }
        SceneManager.sceneLoaded -= InstallForScene;
        SceneManager.sceneLoaded += InstallForScene;
#endif
    }

    public static bool ShouldCapture(
        bool android, bool development, string requestedScreen)
    {
        return android &&
               development &&
               string.Equals(requestedScreen, MainMenuScreen, StringComparison.Ordinal);
    }

#if UNITY_ANDROID && DEVELOPMENT_BUILD && !UNITY_EDITOR
    static string ReadRequestedScreen()
    {
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
        using (var intent = activity.Call<AndroidJavaObject>("getIntent"))
        {
            return intent == null
                ? null
                : intent.Call<string>("getStringExtra", CaptureExtra);
        }
    }
#endif

    static void InstallForScene(Scene scene, LoadSceneMode mode)
    {
        if (!CaptureRequested || scene.name != "MainMenu")
            return;

        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.GetComponentInChildren<MainMenuCaptureBootstrap>(true) != null)
            {
                SceneManager.sceneLoaded -= InstallForScene;
                return;
            }
        }

        GameObject host = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            var menu = root.GetComponentInChildren<MenuManager>(true);
            if (menu == null)
                continue;
            host = menu.gameObject;
            break;
        }

        var roots = scene.GetRootGameObjects();
        if (host == null && roots.Length > 0)
            host = roots[0];
        if (host != null)
            host.AddComponent<MainMenuCaptureBootstrap>();

        SceneManager.sceneLoaded -= InstallForScene;
    }

    void Update()
    {
        if (!CaptureRequested)
        {
            if (presentationWaitStarted)
                StopAllCoroutines();
            presentationWaitStarted = false;
            enabled = false;
            return;
        }
        if (markerLogged)
        {
            if (presentationWaitStarted)
                StopAllCoroutines();
            presentationWaitStarted = false;
            enabled = false;
            return;
        }

        HideCaptureOverlays();

        if (homeVisuals == null)
            homeVisuals = FindHomeInScene();
        if (homeVisuals == null || !homeVisuals.IsReady || !homeVisuals.IsSettled)
            return;

        if (!presentationWaitStarted)
        {
            presentationWaitStarted = true;
            presentationBarriersPassed = 0;
            StartCoroutine(LogReadyAfterPresentation());
        }
    }

    IEnumerator LogReadyAfterPresentation()
    {
        yield return new WaitForEndOfFrame();
        presentationBarriersPassed++;
        yield return new WaitForEndOfFrame();
        presentationBarriersPassed++;

        presentationWaitStarted = false;
        if (!CaptureRequested ||
            markerLogged ||
            homeVisuals == null ||
            !homeVisuals.IsReady ||
            !homeVisuals.IsSettled)
        {
            if (!CaptureRequested || markerLogged)
                enabled = false;
            yield break;
        }

        HideCaptureOverlays();
        markerLogged = true;
        Debug.Log(ReadyMarker);
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

    MainMenuHomeVisuals FindHomeInScene()
    {
        foreach (var root in gameObject.scene.GetRootGameObjects())
        {
            var found = root.GetComponentInChildren<MainMenuHomeVisuals>(true);
            if (found != null)
                return found;
        }
        return null;
    }

}
