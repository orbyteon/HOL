using UnityEngine;
using Unity.Services.LevelPlay;

public class AdsManager : MonoBehaviour
{
#if UNITY_IOS
    const string GameId = "SET_IOS_APP_KEY";
#else
    const string GameId = "6076495";
#endif
#if UNITY_IOS
    const string InterstitialUnit = "Interstitial_iOS";
    const string RewardedUnit = "Rewarded_iOS";
#else
    const string InterstitialUnit = "Interstitial_Android";
    const string RewardedUnit = "Rewarded_Android";
#endif

    const string ConsentPrefKey = "AdsConsent";
    public const string PendingStreakRestoreKey = "PendingStreakRestore";
    public const string PendingRewardEarnedKey = "PendingRewardEarned";
    const float ShowAdSafetyTimeout = 120f;
    const float MinSecondsBetweenAds = 60f;
    const int MaxInitRetries = 3;
    const float RewardGraceSeconds = 5f;

    LevelPlayInterstitialAd interstitialAd;
    LevelPlayRewardedAd rewardedAd;

    public System.Action onAdFinished;

    bool adInProgress;
    float lastAdShowTime = -999f;
    int initRetries;
    // LevelPlay initialization is process-global while this component is
    // scene-local and recreated on every MainMenu reload; instance flags here
    // would re-run LevelPlay.Init on an already-initialized SDK, which never
    // re-fires OnInitSuccess for the new instance.
    static bool sdkInitialized;
    static bool sdkInitInFlight;
    bool adsAllowedThisSession;

    System.Action onRewardEarned;
    System.Action onRewardUnavailable;
    bool rewardGranted;

    void Start()
    {
        LevelPlay.OnInitSuccess += OnInitSuccess;
        LevelPlay.OnInitFailed += OnInitFailed;

        // Consent is an opt-in to initialize the third-party ads SDK at all.
        // A stored decline keeps LevelPlay completely uninitialized on launch.
        adsAllowedThisSession = PlayerPrefs.GetInt(ConsentPrefKey, 0) == 1;
        if (adsAllowedThisSession)
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

    // Called by ConsentManager. Accepting allows SDK initialization. Declining
    // (or withdrawing later) blocks all future ad loads/shows in this session;
    // the next launch will not initialize LevelPlay at all.
    public void OnConsentChosen(bool consent)
    {
        PlayerPrefs.SetInt(ConsentPrefKey, consent ? 1 : 0);
        PlayerPrefs.Save();

        adsAllowedThisSession = consent;
        LevelPlayPrivacySettings.SetGDPRConsent(consent);

        if (consent)
        {
            InitAds();
            return;
        }

        // An initialized SDK cannot be uninitialized safely mid-session, so
        // withdrawal is enforced by never loading/showing another ad. The SDK
        // is left idle and will not initialize on the next launch.
        CancelInvoke(nameof(RetryInit));
        CancelInvoke(nameof(RetryLoad));
        CancelInvoke(nameof(RetryRewardedLoad));
    }

    void InitAds()
    {
        if (!adsAllowedThisSession)
            return;

        // Already-initialized SDK (consent re-granted mid-session, or a fresh
        // instance after a scene reload): skip Init and go straight to the ad
        // objects, or ads would stay dead for the rest of the session.
        if (sdkInitialized)
        {
            EnsureAdObjects();
            return;
        }

        if (sdkInitInFlight)
            return;

        sdkInitInFlight = true;
        LevelPlayPrivacySettings.SetGDPRConsent(true);
        LevelPlay.Init(GameId);
    }

    void OnInitSuccess(LevelPlayConfiguration config)
    {
        Debug.Log("Ads Initialized");

        sdkInitialized = true;
        sdkInitInFlight = false;
        initRetries = 0;

        // Consent could have been withdrawn while initialization was in flight.
        if (!adsAllowedThisSession)
            return;

        EnsureAdObjects();
    }

    void EnsureAdObjects()
    {
        if (interstitialAd == null)
        {
            interstitialAd = new LevelPlayInterstitialAd(InterstitialUnit);
            interstitialAd.OnAdClosed += OnAdClosed;
            interstitialAd.OnAdDisplayFailed += OnAdDisplayFailed;
            interstitialAd.OnAdLoadFailed += OnAdLoadFailed;
        }
        if (!interstitialAd.IsAdReady())
            interstitialAd.LoadAd();

        if (rewardedAd == null)
        {
            rewardedAd = new LevelPlayRewardedAd(RewardedUnit);
            rewardedAd.OnAdClosed += OnRewardedClosed;
            rewardedAd.OnAdDisplayFailed += OnRewardedDisplayFailed;
            rewardedAd.OnAdLoadFailed += OnRewardedLoadFailed;
            rewardedAd.OnAdRewarded += OnRewardedEarned;
        }
        if (!rewardedAd.IsAdReady())
            rewardedAd.LoadAd();
    }

    void OnAdClosed(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Ad Closed");
        lastAdShowTime = Time.realtimeSinceStartup;
        FinishAndContinue();

        if (adsAllowedThisSession && interstitialAd != null)
            interstitialAd.LoadAd();
    }

    void OnAdDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        Debug.Log("Ad display failed: " + error);
        FinishAndContinue();

        if (adsAllowedThisSession && interstitialAd != null)
            interstitialAd.LoadAd();
    }

    void OnAdLoadFailed(LevelPlayAdError error)
    {
        Debug.Log("Ad load failed: " + error);
        if (adsAllowedThisSession)
            Invoke(nameof(RetryLoad), 30f);
    }

    // ---------------------------------------------------------- rewarded

    public bool IsRewardedReady()
    {
        return adsAllowedThisSession && rewardedAd != null && rewardedAd.IsAdReady();
    }

    public bool ShowRewardedAd(System.Action onReward, System.Action onUnavailable = null)
    {
        if (!IsRewardedReady())
        {
            Debug.Log("Rewarded ad not ready or ads consent not granted");
            onUnavailable?.Invoke();
            return false;
        }

        CancelInvoke(nameof(FinishRewardedAfterGrace));
        onRewardEarned = onReward;
        onRewardUnavailable = onUnavailable;
        rewardGranted = false;
        rewardedAd.ShowAd();
        return true;
    }

    void OnRewardedEarned(LevelPlayAdInfo adInfo, LevelPlayReward reward)
    {
        rewardGranted = true;
        PlayerPrefs.SetInt(PendingRewardEarnedKey, 1);
        PlayerPrefs.Save();

        // If the close event already ran, its grace timer is waiting on
        // exactly this reward — deliver it now.
        if (IsInvoking(nameof(FinishRewardedAfterGrace)))
        {
            CancelInvoke(nameof(FinishRewardedAfterGrace));
            FinishRewarded();
        }
    }

    void OnRewardedClosed(LevelPlayAdInfo adInfo)
    {
        // LevelPlay may deliver OnAdRewarded after OnAdClosed. Treating a
        // close-without-reward as "not earned" immediately would wipe the
        // pending-restore state the late reward needs, so give it a grace
        // window before settling.
        if (rewardGranted)
            FinishRewarded();
        else
            Invoke(nameof(FinishRewardedAfterGrace), RewardGraceSeconds);

        if (adsAllowedThisSession && rewardedAd != null)
            rewardedAd.LoadAd();
    }

    void FinishRewardedAfterGrace()
    {
        FinishRewarded();
    }

    void OnRewardedDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        Debug.Log("Rewarded display failed: " + error);
        var unavailable = onRewardUnavailable;
        FinishRewarded();
        unavailable?.Invoke();
        if (adsAllowedThisSession && rewardedAd != null)
            rewardedAd.LoadAd();
    }

    void OnRewardedLoadFailed(LevelPlayAdError error)
    {
        Debug.Log("Rewarded load failed: " + error);
        if (adsAllowedThisSession)
            Invoke(nameof(RetryRewardedLoad), 30f);
    }

    void RetryRewardedLoad()
    {
        if (adsAllowedThisSession && rewardedAd != null)
            rewardedAd.LoadAd();
    }

    void FinishRewarded()
    {
        var cb = rewardGranted ? onRewardEarned : null;
        onRewardEarned = null;
        onRewardUnavailable = null;

        if (!rewardGranted)
        {
            PlayerPrefs.DeleteKey(PendingStreakRestoreKey);
            PlayerPrefs.DeleteKey(PendingRewardEarnedKey);
            PlayerPrefs.Save();
        }

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
        if (adsAllowedThisSession && interstitialAd != null)
            interstitialAd.LoadAd();
    }

    public void ShowAd(System.Action callback)
    {
        if (!adsAllowedThisSession)
        {
            Debug.Log("Ads disabled by privacy choice → continue");
            callback?.Invoke();
            return;
        }

        if (adInProgress)
        {
            Debug.Log("Ad already in progress → continue");
            callback?.Invoke();
            return;
        }

        if (Time.realtimeSinceStartup - lastAdShowTime < MinSecondsBetweenAds)
        {
            Debug.Log("Ad skipped (frequency cap) → continue");
            callback?.Invoke();
            return;
        }

        onAdFinished = callback;

        if (interstitialAd != null && interstitialAd.IsAdReady())
        {
            adInProgress = true;
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
        sdkInitInFlight = false;
        if (adsAllowedThisSession && initRetries < MaxInitRetries)
            Invoke(nameof(RetryInit), 60f);
    }

    void RetryInit()
    {
        initRetries++;
        if (adsAllowedThisSession)
            InitAds();
    }
}
