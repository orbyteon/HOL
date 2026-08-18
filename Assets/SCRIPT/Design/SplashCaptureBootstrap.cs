using System;
using UnityEngine;
using UnityEngine.SceneManagement;

// Development-build QA seam for deterministic Android Splash screenshots.
// Normal builds never inspect Android intent state or add this component.
public sealed class SplashCaptureBootstrap : MonoBehaviour
{
    const string CaptureExtra = "hol_capture_screen";
    const string SplashScreen = "splash";
    const string ReadyMarker = "HOL_SPLASH_CAPTURE_READY";

    static bool markerLogged;
    SplashDesign splashDesign;

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
        SceneManager.sceneLoaded -= InstallForScene;
        SceneManager.sceneLoaded += InstallForScene;
#endif
    }

    public static bool ShouldCapture(
        bool android, bool development, string requestedScreen)
    {
        return android &&
               development &&
               string.Equals(requestedScreen, SplashScreen, StringComparison.Ordinal);
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
        if (!CaptureRequested || scene.name != "SplashScene")
            return;

        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.GetComponentInChildren<SplashCaptureBootstrap>(true) != null)
            {
                SceneManager.sceneLoaded -= InstallForScene;
                return;
            }
        }

        GameObject host = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.GetComponentInChildren<SplashLoader>(true) == null)
                continue;
            host = root;
            break;
        }

        var roots = scene.GetRootGameObjects();
        if (host == null && roots.Length > 0)
            host = roots[0];
        if (host != null)
            host.AddComponent<SplashCaptureBootstrap>();

        SceneManager.sceneLoaded -= InstallForScene;
    }

    void Update()
    {
        if (!CaptureRequested)
        {
            enabled = false;
            return;
        }
        if (markerLogged)
        {
            enabled = false;
            return;
        }

        if (splashDesign == null)
            splashDesign = FindDesignInScene();
        if (splashDesign == null || !splashDesign.IsSettled)
            return;

        markerLogged = true;
        Debug.Log(ReadyMarker);
        enabled = false;
    }

    SplashDesign FindDesignInScene()
    {
        foreach (var root in gameObject.scene.GetRootGameObjects())
        {
            var found = root.GetComponentInChildren<SplashDesign>(true);
            if (found != null)
                return found;
        }
        return null;
    }
}
