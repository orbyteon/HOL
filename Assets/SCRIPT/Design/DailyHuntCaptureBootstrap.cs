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
    const string ChallengeDayPrefKey = "DailyChallengeDay";
    const string ChallengeWinsPrefKey = "DailyChallengeWins";
    const string ChallengeCorrectPrefKey = "DailyChallengeCorrectGuesses";
    const string ChallengeRoomsPrefKey = "DailyChallengeRoomsShared";
    const string ChallengeRewardPrefKey = "DailyChallengeRewardClaimed";
    const string ChallengePointsPrefKey = "DailyChallengePoints";
    const int CapturePoints = 1250;
    const int CaptureMilestoneProgress = 650;

    // PanelAnimator uses a 0.28 second entrance. Native acceptance must wait
    // beyond that duration and then cross two render barriers before logging.
    public const float PresentationSettleSeconds = 0.36f;

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
        ChallengeDayPrefKey,
        ChallengeWinsPrefKey,
        ChallengeCorrectPrefKey,
        ChallengeRoomsPrefKey,
        ChallengeRewardPrefKey,
        ChallengePointsPrefKey,
    };

    static bool markerLogged;
    static string requestedLanguage = "en";

    DailyHunt hunt;
    DailyHuntVisuals visuals;
    bool opened;
#if DEVELOPMENT_BUILD
    bool fixtureApplied;
#endif
    bool presentationWaitStarted;
    string lastReadinessFailure;
    float nextReadinessLog;

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
        PlayerPrefs.SetInt(
            ChallengeDayPrefKey, DailyChallengeProgress.CurrentUtcDayNumber);
        PlayerPrefs.SetInt(
            ChallengeWinsPrefKey, DailyChallengeProgress.WinTarget);
        PlayerPrefs.SetInt(
            ChallengeCorrectPrefKey, DailyChallengeProgress.CorrectGuessTarget);
        PlayerPrefs.SetInt(
            ChallengeRoomsPrefKey, DailyChallengeProgress.RoomShareTarget);
        PlayerPrefs.SetInt(ChallengeRewardPrefKey, 1);
        PlayerPrefs.SetInt(ChallengePointsPrefKey, CapturePoints);
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
            visuals.DisplayFont == null || visuals.BodyFont == null)
            return;

#if DEVELOPMENT_BUILD
        if (!fixtureApplied)
        {
            visuals.SetCapturePlayerChipFixture(
                CapturePoints, CaptureMilestoneProgress);
            fixtureApplied = true;
        }
#endif

        if (!opened)
        {
            hunt.Open();
            opened = true;
            return;
        }

        if (!hunt.gameObject.activeInHierarchy)
            return;

        Transform root = Find(hunt.transform, DailyHuntVisuals.VisualRootName);
        if (!ApprovedPresentationReady(root, visuals, out string failure))
        {
            LogReadinessWait(failure);
            return;
        }

        if (!presentationWaitStarted)
        {
            presentationWaitStarted = true;
            StartCoroutine(LogReadyAfterPresentation());
        }
    }

    IEnumerator LogReadyAfterPresentation()
    {
        yield return new WaitForSecondsRealtime(PresentationSettleSeconds);
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        presentationWaitStarted = false;
        if (!CaptureRequested || markerLogged || hunt == null ||
            visuals == null || !visuals.IsReady ||
            !hunt.gameObject.activeInHierarchy)
            yield break;

        var group = hunt.GetComponent<CanvasGroup>();
        var rect = hunt.transform as RectTransform;
        if ((group != null && group.alpha < 0.999f) ||
            (rect != null && Vector3.Distance(rect.localScale, Vector3.one) > 0.01f))
            yield break;

        Transform root = Find(hunt.transform, DailyHuntVisuals.VisualRootName);
        if (!ApprovedPresentationReady(root, visuals, out string failure))
        {
            LogReadinessWait(failure);
            yield break;
        }

        HideCaptureOverlays();
        markerLogged = true;
        Debug.Log(requestedLanguage == "el"
            ? ReadyMarkerGreek
            : ReadyMarkerEnglish);
        enabled = false;
    }

    static bool ApprovedPresentationReady(
        Transform root,
        DailyHuntVisuals owner,
        out string failure)
    {
        if (root == null)
        {
            failure = "visual-root-missing";
            return false;
        }
        if (owner == null)
        {
            failure = "visual-owner-missing";
            return false;
        }
        if (owner.DisplayFont == null)
        {
            failure = "display-font-missing";
            return false;
        }
        if (owner.BodyFont == null)
        {
            failure = "body-font-missing";
            return false;
        }

        Transform dashboard = Find(root, "DailyMissionDashboard");
        Transform startButton = Find(root, "DailyMissionStartButton");
        if (dashboard == null)
        {
            failure = "mission-dashboard-missing";
            return false;
        }
        if (!dashboard.gameObject.activeInHierarchy)
        {
            failure = "mission-dashboard-inactive";
            return false;
        }
        if (startButton == null)
        {
            failure = "mission-start-missing";
            return false;
        }
        if (!startButton.gameObject.activeInHierarchy)
        {
            failure = "mission-start-inactive";
            return false;
        }

        string[] displayNames =
        {
            "DailyRibbonTitle",
            "DailyPlayerName",
            "DailyPlayerProgress",
            "DailyMissionHeading",
            "DailyMissionRewardHeading",
        };
        foreach (string name in displayNames)
        {
            Transform searchRoot = name.StartsWith("DailyMission",
                StringComparison.Ordinal) ? dashboard : root;
            if (!LiveTextReady(
                    FindText(searchRoot, name), owner.DisplayFont,
                    name, out failure))
                return false;
        }

        if (!LiveTextReady(
                FindText(startButton, "Label"), owner.DisplayFont,
                "DailyMissionStartButton/Label", out failure))
            return false;

        for (int index = 1; index <= 3; index++)
        {
            string labelName = "DailyMissionLabel" + index;
            string progressName = "DailyMissionProgress" + index;
            TMP_Text label = FindText(dashboard, labelName);
            TMP_Text progress = FindText(dashboard, progressName);
            if (!LiveTextReady(
                    label, owner.DisplayFont, labelName, out failure))
                return false;
            if (!LiveTextReady(
                    progress, owner.BodyFont, progressName, out failure))
                return false;

            Transform track = Find(
                dashboard, "DailyMissionTrack" + index);
            if (track == null)
            {
                failure = "DailyMissionTrack" + index + "-missing";
                return false;
            }
            if (RectsOverlap(
                    label.rectTransform, track as RectTransform))
            {
                failure = labelName + "-overlaps-progress";
                return false;
            }
        }

        failure = null;
        return true;
    }

    static bool RectsOverlap(RectTransform first, RectTransform second)
    {
        if (first == null || second == null)
            return false;

        var firstCorners = new Vector3[4];
        var secondCorners = new Vector3[4];
        first.GetWorldCorners(firstCorners);
        second.GetWorldCorners(secondCorners);
        const float separation = 1f;
        return firstCorners[0].x < secondCorners[2].x - separation &&
               firstCorners[2].x > secondCorners[0].x + separation &&
               firstCorners[0].y < secondCorners[2].y - separation &&
               firstCorners[2].y > secondCorners[0].y + separation;
    }

    static bool LiveTextReady(
        TMP_Text text,
        TMP_FontAsset expectedFont,
        string contractName,
        out string failure)
    {
        if (text == null)
        {
            failure = contractName + "-missing";
            return false;
        }
        if (!text.gameObject.activeInHierarchy)
        {
            failure = contractName + "-inactive";
            return false;
        }
        if (string.IsNullOrWhiteSpace(text.text))
        {
            failure = contractName + "-empty";
            return false;
        }
        if (text.font != expectedFont)
        {
            string actual = text.font == null ? "null" : text.font.name;
            string expected = expectedFont == null ? "null" : expectedFont.name;
            failure = contractName + "-font-" + actual + "-expected-" + expected;
            return false;
        }

        failure = null;
        return true;
    }

    void LogReadinessWait(string failure)
    {
        if (string.IsNullOrEmpty(failure)) return;
        if (failure == lastReadinessFailure && Time.unscaledTime < nextReadinessLog)
            return;

        lastReadinessFailure = failure;
        nextReadinessLog = Time.unscaledTime + 1f;
        Debug.Log("[DailyHuntCaptureBootstrap] WAIT " + failure);
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
