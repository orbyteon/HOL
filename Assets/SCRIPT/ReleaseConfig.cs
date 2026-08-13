using System;
using UnityEngine;

// Public production configuration shipped with the app. The committed
// Resources payload is intentionally empty; release automation replaces it in
// the Actions workspace before a signed build. Secrets never belong here.
public static class ReleaseConfig
{
    const string ResourceName = "HOLReleaseConfig";

    [Serializable]
    sealed class Payload
    {
        public string playFabTitleId = "";
        public string provisioningUrl = "";
        public long googleCloudProjectNumber;
    }

    static bool loaded;
    static Payload payload;

    public static string PlayFabTitleId
    {
        get { EnsureLoaded(); return (payload.playFabTitleId ?? "").Trim(); }
    }

    public static string ProvisioningUrl
    {
        get { EnsureLoaded(); return (payload.provisioningUrl ?? "").Trim(); }
    }

    public static long GoogleCloudProjectNumber
    {
        get { EnsureLoaded(); return payload.googleCloudProjectNumber; }
    }

    public static bool IsConfigured(out string error)
    {
        EnsureLoaded();
        return ValidateValues(PlayFabTitleId, ProvisioningUrl, GoogleCloudProjectNumber, out error);
    }

    // Kept public so EditMode tests and release tooling can share the exact
    // client-side validity contract without loading a scene.
    public static bool ValidateValues(string playFabTitleId, string provisioningUrl,
        long googleCloudProjectNumber, out string error)
    {
        string title = (playFabTitleId ?? "").Trim();
        if (title.Length < 3 || title.Length > 32)
        {
            error = "PlayFab Title ID must be 3-32 characters.";
            return false;
        }
        for (int i = 0; i < title.Length; i++)
        {
            if (!char.IsLetterOrDigit(title[i]))
            {
                error = "PlayFab Title ID must contain only letters and digits.";
                return false;
            }
        }

        Uri uri;
        if (!Uri.TryCreate((provisioningUrl ?? "").Trim(), UriKind.Absolute, out uri) ||
            uri.Scheme != Uri.UriSchemeHttps || string.IsNullOrWhiteSpace(uri.Host) ||
            !string.IsNullOrEmpty(uri.UserInfo))
        {
            error = "Provisioning URL must be an absolute HTTPS URL without embedded credentials.";
            return false;
        }

        // Standard Play Integrity token preparation requires a Cloud project
        // number; zero is not a valid production configuration.
        if (googleCloudProjectNumber <= 0)
        {
            error = "Google Cloud project number must be greater than zero.";
            return false;
        }

        error = "";
        return true;
    }

    static void EnsureLoaded()
    {
        if (loaded) return;
        loaded = true;
        payload = new Payload();

        var asset = Resources.Load<TextAsset>(ResourceName);
        if (asset == null || string.IsNullOrWhiteSpace(asset.text))
            return;

        try
        {
            var parsed = JsonUtility.FromJson<Payload>(asset.text);
            if (parsed != null)
                payload = parsed;
        }
        catch (Exception ex)
        {
            Debug.LogError("ReleaseConfig: invalid Resources/" + ResourceName + ".json: " + ex.Message);
        }
    }

#if UNITY_EDITOR
    public static void ResetForTests()
    {
        loaded = false;
        payload = null;
    }
#endif
}
