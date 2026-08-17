using System.Collections;
using UnityEngine;

// Development-Android-only bootstrap used by the Main Menu screenshot workflow.
// Release players never read the intent or create the capture runner.
[DefaultExecutionOrder(-32000)]
public sealed class MainMenuCaptureBootstrap : MonoBehaviour
{
    const string IntentExtra = "hol_capture_language";
    const string ConsentPrefKey = "AdsConsent";
    const string ReadyMarker = "HOL_MAINMENU_CAPTURE_READY:";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Debug.isDebugBuild)
            return;

        string extra = ReadIntentExtra();
        if (!ShouldCapture(Application.platform == RuntimePlatform.Android,
                Debug.isDebugBuild, extra))
            return;

        ApplyCapture(extra);

        var runner = new GameObject("MainMenuCaptureBootstrap")
            .AddComponent<MainMenuCaptureBootstrap>();
        DontDestroyOnLoad(runner.gameObject);
        runner.StartCoroutine(WaitForReady(extra));
#endif
    }

    internal static bool ShouldCapture(
        bool isAndroid, bool isDevelopment, string extra)
    {
        return isAndroid &&
               isDevelopment &&
               (extra == "en" || extra == "el");
    }

    internal static void ApplyCapture(string extra)
    {
        if (!ShouldCapture(true, true, extra))
            return;

        PlayerPrefs.SetInt(ConsentPrefKey, 0);
        L10n.SetLanguage(extra == "el"
            ? L10n.Language.Greek
            : L10n.Language.English);
    }

    internal static IEnumerator WaitForReady(string extra)
    {
        if (!ShouldCapture(true, true, extra))
            yield break;

        while (true)
        {
            var owner = FindFirstObjectByType<MainMenuAuthoritativeVisuals>();
            if (owner != null && owner.IsReady && owner.OwnsHome)
            {
                // Keep the same ready, owned Home through two full frames
                // so late runtime wiring and layout have settled before capture.
                yield return null;
                if (owner != null && owner.IsReady && owner.OwnsHome)
                {
                    yield return null;
                    if (owner != null && owner.IsReady && owner.OwnsHome)
                        break;
                }
            }

            yield return null;
        }

        Debug.Log(ReadyMarker + extra);
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    static string ReadIntentExtra()
    {
        try
        {
            using (var player = new AndroidJavaClass(
                       "com.unity3d.player.UnityPlayer"))
            using (var activity =
                   player.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var intent =
                   activity.Call<AndroidJavaObject>("getIntent"))
            {
                return intent.Call<string>("getStringExtra", IntentExtra);
            }
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning(
                "Main Menu capture intent could not be read: " +
                exception.Message);
            return null;
        }
    }
#endif
}
