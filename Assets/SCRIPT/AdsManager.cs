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
    const string RewardedUnit = "Rewarded_iOS";         // same — dashboard unit needed
#else
    const string InterstitialUnit = "Interstitial_Android";
    const string RewardedUnit = "Rewarded_Android"; // create this unit in the LevelPlay dashboard
#endif

    const string ConsentPrefKey = "AdsConsent";
    const float ShowAdSafetyTimeout = 120f; // review #1: only a genuinely lost callback should trip this — normal interstitials run 15-30s
    const float MinSecondsBetweenAds = 60f; // interstitial frequency cap (Play policy)
    const int MaxInitRetries = 3;

    private LevelPlayInterstitialAd interstitialAd;
    private LevelPlayRewardedAd rewardedAd;

    public System.Action onAdFinished;

    bool adInProgress;
    float lastAdShowTime = -999f;
    int initRetries;

    System.Action onRewardEarned;
    System.Action onRewardUnavailable;
    bool rewardGranted;

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

        OnDestroyRewardedCleanup();
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

        rewardedAd = new LevelPlayRewardedAd(RewardedUnit);

        rewardedAd.OnAdClosed += OnRewardedClosed;
        rewardedAd.OnAdDisplayFailed += OnRewardedDisplayFailed;
        rewardedAd.OnAdLoadFailed += OnRewardedLoadFailed;
        rewardedAd.OnAdRewarded += OnRewardedEarned;

        rewardedAd.LoadAd();
    }

    void OnAdClosed(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Ad Closed");

        // Frequency cap stamps here — only an ad that was actually shown and
        // dismissed counts, not a skipped/failed attempt.
        lastAdShowTime = Time.realtimeSinceStartup;

        FinishAndContinue();

        interstitialAd.LoadAd(); // preload the next ad
    }

    void OnAdDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
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

    // ---------------------------------------------------------- rewarded

    // Used to decide whether to offer a rewarded placement (e.g. the
    // save-your-streak button) at all.
    public bool IsRewardedReady()
    {
        return rewardedAd != null && rewardedAd.IsAdReady();
    }

    // Shows a rewarded ad. onReward fires only if the player earns the
    // reward (watched through); closes just end the flow quietly.
    // onUnavailable fires when no ad can be shown right now (not loaded or
    // the display failed) so the caller can keep its UI alive and tell the
    // player. Returns false in that case, true once an ad is on its way.
    public bool ShowRewardedAd(System.Action onReward, System.Action onUnavailable = null)
    {
        if (!IsRewardedReady())
        {
            Debug.Log("Rewarded ad not ready");
            onUnavailable?.Invoke();
            return false;
        }

        onRewardEarned = onReward;
        onRewardUnavailable = onUnavailable;
        rewardGranted = false;
        rewardedAd.ShowAd();
        return true;
    }

    void OnRewardedEarned(LevelPlayAdInfo adInfo, LevelPlayReward reward)
    {
        rewardGranted = true;
    }

    void OnRewardedClosed(LevelPlayAdInfo adInfo)
    {
        FinishRewarded();
        rewardedAd.LoadAd(); // preload the next one
    }

    void OnRewardedDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        Debug.Log("Rewarded display failed: " + error);
        var unavailable = onRewardUnavailable;
        FinishRewarded();
        unavailable?.Invoke(); // caller keeps its UI alive and tells the player
        rewardedAd.LoadAd();
    }

    void OnRewardedLoadFailed(LevelPlayAdError error)
    {
        Debug.Log("Rewarded load failed: " + error);
        Invoke(nameof(RetryRewardedLoad), 30f);
    }

    void RetryRewardedLoad()
    {
        if (rewardedAd != null)
            rewardedAd.LoadAd();
    }

    void FinishRewarded()
    {
        var cb = rewardGranted ? onRewardEarned : null;
        onRewardEarned = null;
        onRewardUnavailable = null;
        rewardGranted = false;
        cb?.Invoke();
    }

    void OnDestroyRewardedCleanup()
    {
        if (rewardedAd != null)
        {
            rewardedAd.OnAdClosed -= OnRewardedClosed;
            rewardedAd.OnAdDisplayFailed -= OnRewardedDisplayFailed;
            rewardedAd.OnAdLoadFailed -= OnRewardedLoadFailed;
            rewardedAd.OnAdRewarded -= OnRewardedEarned;
        }
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
