using UnityEngine;
using Unity.Services.LevelPlay;

public class AdsManager : MonoBehaviour
{
    private LevelPlayInterstitialAd interstitialAd;

    public System.Action onAdFinished;

    void Start()
    {
        LevelPlay.OnInitSuccess += OnInitSuccess;
        LevelPlay.OnInitFailed += OnInitFailed;

        LevelPlay.Init("6076495"); // 🔥 Game ID σου
    }

    void OnInitSuccess(LevelPlayConfiguration config)
    {
        Debug.Log("Ads Initialized");

        interstitialAd = new LevelPlayInterstitialAd("Interstitial_Android");

        interstitialAd.OnAdClosed += OnAdClosed;

        interstitialAd.LoadAd();
    }

    void OnAdClosed(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Ad Closed");

        onAdFinished?.Invoke(); // 🔥 συνεχίζει μετά το ad

        interstitialAd.LoadAd(); // 🔥 φορτώνει επόμενο ad
    }

    public void ShowAd(System.Action callback)
    {
        onAdFinished = callback;

        if (interstitialAd != null && interstitialAd.IsAdReady())
        {
            interstitialAd.ShowAd();
        }
        else
        {
            Debug.Log("Ad not ready → start anyway");
            callback?.Invoke();
        }
    }

    void OnInitFailed(LevelPlayInitError error)
    {
        Debug.Log("Ads Init Failed: " + error);
    }
}