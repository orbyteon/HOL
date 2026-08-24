using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Development-build QA seam for deterministic native Android Daily Hunt
// screenshots. Normal builds never inspect Android intent state or install this
// component. DailyHunt remains the sole state/gameplay authority.
public sealed class DailyHuntCaptureBootstrap : MonoBehaviour
{
    const string CaptureExtra = "hol_capture_screen";
    const string LanguageExtra = "hol_capture_language";
    const string DailyHuntScreen = "dailyhunt";
    const string ReadyMarkerEnglish = "HOL_DAILYHUNT_CAPTURE_READY_EN";
    const string ReadyMarkerGreek = "HOL_DAILYHUNT_CAPTURE_READY_EL";
    const string AdsConsentPrefKey = "AdsConsent";
    const string PlayerNamePrefKey = "PlayerName";

    static readonly string[] DailyStateKeys =
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
    };

    static bool markerLogged;
    static string requestedLanguage = "en";

    DailyHunt hunt;
    DailyHuntVisuals visuals;
    bool opened;
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
        bool android,
        bool development,
        string requestedScreen)
    {
        return android && development &&
               string.Equals(requestedScreen, DailyHuntScreen,
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
        foreach (string key in DailyStateKeys)
            PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
    }

    static void InstallForScene(Scene scene, LoadSceneMode mode)
    {
        if (!CaptureRequested || scene.name != "MainMenu")
            return;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.GetComponentInChildren<DailyHuntCaptureBootstrap>(true) != null)
            {
                SceneManager.sceneLoaded -= InstallForScene;
                return;
            }
        }

        GameObject host = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            MenuManager menu = root.GetComponentInChildren<MenuManager>(true);
            if (menu == null) continue;
            host = menu.gameObject;
            break;
        }

        GameObject[] roots = scene.GetRootGameObjects();
        if (host == null && roots.Length > 0)
            host = roots[0];
        if (host != null)
            host.AddComponent<DailyHuntCaptureBootstrap>();

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

        if (hunt == null)
            hunt = FindInScene<DailyHunt>();
        if (visuals == null && hunt != null)
            visuals = hunt.GetComponent<DailyHuntVisuals>();
        if (hunt == null || visuals == null || !visuals.IsReady ||
            visuals.ProductionFont == null)
            return;

        if (!opened)
        {
            hunt.Open();
            opened = true;
            return;
        }

        if (!hunt.gameObject.activeInHierarchy)
            return;

        Transform root = Find(hunt.transform, DailyHuntVisuals.VisualRootName);
        TMP_Text title = FindText(root, "Title");
        TMP_Text status = FindText(root, "Status");
        TMP_Text challenge = FindText(root, "DailyChallengeHeading");
        if (root == null || title == null || status == null || challenge == null ||
            string.IsNullOrWhiteSpace(title.text) ||
            string.IsNullOrWhiteSpace(status.text) ||
            string.IsNullOrWhiteSpace(challenge.text) ||
            title.font != visuals.ProductionFont ||
            status.font != visuals.ProductionFont)
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
        if (!CaptureRequested || markerLogged || hunt == null ||
            visuals == null || !visuals.IsReady ||
            !hunt.gameObject.activeInHierarchy)
            yield break;

        Transform root = Find(hunt.transform, DailyHuntVisuals.VisualRootName);
        TMP_Text title = FindText(root, "Title");
        TMP_Text status = FindText(root, "Status");
        if (root == null || title == null || status == null ||
            string.IsNullOrWhiteSpace(title.text) ||
            string.IsNullOrWhiteSpace(status.text))
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

    static TMP_Text FindText(Transform root, string name)
    {
        if (root == null) return null;
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            if (text.name == name)
                return text;
        return null;
    }

    static Transform Find(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = Find(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }
}
