using UnityEngine;
using UnityEngine.SceneManagement;

// Applies public release configuration to runtime-wired systems before their
// Start methods run. Scene values remain usable for debug/local development;
// a valid release payload always selects the hardened PlayFab backend.
public static class ReleaseBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        var pvp = Object.FindFirstObjectByType<PvpRuntimeUI>();
        if (pvp == null) return;

        string error;
        if (!ReleaseConfig.IsConfigured(out error))
        {
            if (!Debug.isDebugBuild)
                Debug.LogError("Release configuration is incomplete: " + error);
            return;
        }

        pvp.usePlayFab = true;
        pvp.playFabTitleId = ReleaseConfig.PlayFabTitleId;
    }
}
