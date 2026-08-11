using UnityEngine;
using Unity.Services.LevelPlay;

public class AdsManager : MonoBehaviour
{
    // Review #10: platform-safe, single-place ad configuration
#if UNITY_IOS
    const string GameId = "SET_IOS_APP_KEY"; // iOS needs its own LevelPlay app + key in the dashboard
#else
    const string GameId = "6076495";
#endif
#if UNITY_IOS
    const string InterstitialUnit = "Interstitial_iOS"; // create this unit in the LevelPlay dashboard before an iOS release
#else
    const string InterstitialUnit = "Interstitial_Android";
#endif

    const string ConsentPrefKey = "AdsConsent";
    const float ShowAdSafetyTimeout = 10f; // review #1: never block the player longer than this
    const float MinSecondsBetweenAds = 60f; // interstitial frequency cap (Play policy)
    const int MaxInitRetries = 3;

    private LevelPlayInterstitialAd interstitialAd;

    public System.Action onAdFinished;

    bool adInProgress;
    float lastAdShowTime = -999f;
    int initRetries;

    void Start()
    {
        LevelPlay.OnInitSuccess += OnInitSuccess;
        LevelPlay.OnInitFailed += OnInitFailed;

        // Review #9: only initialize ads once the player has made a consent choice.
        // On first launch ConsentManager shows the dialog and calls OnConsentChosen.
        if (PlayerPrefs.HasKey(ConsentPrefKey))
            InitAds();
    }

    void OnDestroy()
    {
        LevelPlay.OnInitSuccess -= OnInitSuccess;
        LevelPlay.OnInitFailed -= OnInitFailed;

        if (interstitialAd != null)
        {
            interstitialAd.OnAdClosed -= OnAdClosed;
            interstitialAd.OnAdDisplayFailed -= OnAdDisplayFailed;
            interstitialAd.OnAdLoadFailed -= OnAdLoadFailed;
        }
    }

    // Called by ConsentManager after the player answers the consent dialog.
    public void OnConsentChosen(bool consent)
    {
        PlayerPrefs.SetInt(ConsentPrefKey, consent ? 1 : 0);
        PlayerPrefs.Save();
        InitAds();
    }

    void InitAds()
    {
        bool consent = PlayerPrefs.GetInt(ConsentPrefKey, 0) == 1;

        // Requires com.unity.services.levelplay 9.5.0+ (set in Packages/manifest.json).
        // Privacy flags must be set BEFORE LevelPlay.Init, and can be updated
        // any time after — so a consent change from Settings applies live.
        LevelPlayPrivacySettings.SetGDPRConsent(consent);

        // Init exactly once: re-choosing consent must not re-initialize the SDK.
        if (!initialized)
            LevelPlay.Init(GameId);
    }

    bool initialized;

    void OnInitSuccess(LevelPlayConfiguration config)
    {
        Debug.Log("Ads Initialized");

        initialized = true;
        initRetries = 0;

        interstitialAd = new LevelPlayInterstitialAd(InterstitialUnit);

        interstitialAd.OnAdClosed += OnAdClosed;
        interstitialAd.OnAdDisplayFailed += OnAdDisplayFailed; // review #1: a failed display must never soft-lock Play
        interstitialAd.OnAdLoadFailed += OnAdLoadFailed;

        interstitialAd.LoadAd();
    }

    void OnAdClosed(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Ad Closed");

        FinishAndContinue();

        interstitialAd.LoadAd(); // preload the next ad
    }

    void OnAdDisplayFailed(LevelPlayAdDisplayInfoError error)
    {
        Debug.Log("Ad display failed: " + error);

        FinishAndContinue(); // let the player through anyway

        interstitialAd.LoadAd();
    }

    void OnAdLoadFailed(LevelPlayAdError error)
    {
        Debug.Log("Ad load failed: " + error);

        Invoke(nameof(RetryLoad), 30f); // gentle retry, no tight loop
    }

    void RetryLoad()
    {
        if (interstitialAd != null)
            interstitialAd.LoadAd();
    }

    public void ShowAd(System.Action callback)
    {
        // Re-entrancy + frequency cap: never stack interstitials or show
        // them back-to-back (Play interstitial policy). Player continues.
        if (adInProgress || Time.realtimeSinceStartup - lastAdShowTime < MinSecondsBetweenAds)
        {
            Debug.Log("Ad skipped (in progress or frequency cap) → continue");
            onAdFinished = callback;
            FinishAndContinue();
            return;
        }

        onAdFinished = callback;

        if (interstitialAd != null && interstitialAd.IsAdReady())
        {
            adInProgress = true;
            lastAdShowTime = Time.realtimeSinceStartup;

            // Review #1: if no close/fail event arrives, force the game to continue.
            Invoke(nameof(ForceContinue), ShowAdSafetyTimeout);
            interstitialAd.ShowAd();
        }
        else
        {
            Debug.Log("Ad not ready → start anyway");
            FinishAndContinue();
        }
    }

    void ForceContinue()
    {
        Debug.Log("Ad safety timeout hit → continuing without ad callback");
        FinishAndContinue();
    }

    // Single exit point: cancels the safety timer and guarantees the
    // callback fires exactly once per ShowAd call.
    void FinishAndContinue()
    {
        CancelInvoke(nameof(ForceContinue));

        adInProgress = false;

        var cb = onAdFinished;
        onAdFinished = null;
        cb?.Invoke();
    }

    void OnInitFailed(LevelPlayInitError error)
    {
        Debug.Log("Ads Init Failed: " + error);

        // Transient failures (e.g. no network at launch) shouldn't kill ads
        // for the whole session — retry a few times, spaced out.
        if (initRetries < MaxInitRetries)
            Invoke(nameof(RetryInit), 60f);
    }

    void RetryInit()
    {
        initRetries++;

        if (PlayerPrefs.HasKey(ConsentPrefKey))
            InitAds();
    }
}
