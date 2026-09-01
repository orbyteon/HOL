using UnityEngine;
using UnityEngine.SceneManagement;

public class SplashLoader : MonoBehaviour
{
    const float CaptureTimeout = 30f;

    // Review #5: shortened from 5s. NOTE: if the scene's Inspector has its own
    // value for waitTime, the Inspector wins — update it there too.
    public float waitTime = 2.5f;

    bool loading = false;

    void Start()
    {
        if (OnboardingProfile.ShouldRun)
            return;

        float timeout = ResolveTimeout(
            SplashCaptureBootstrap.CaptureRequested, waitTime);
        Invoke(nameof(LoadMenu), timeout);
    }

    void Update()
    {
        if (OnboardingProfile.ShouldRun)
            return;

        // Tap/click anywhere to skip the splash.
        if (Input.GetMouseButtonDown(0))
            LoadMenu();
    }

    public void ContinueToMainMenu()
    {
        LoadMenu();
    }

    void LoadMenu()
    {
        if (loading) return;
        loading = true;

        CancelInvoke();
        SceneManager.LoadScene("MainMenu");
    }

    static float ResolveTimeout(bool captureRequested, float normalTimeout)
    {
        return captureRequested ? CaptureTimeout : normalTimeout;
    }
}
