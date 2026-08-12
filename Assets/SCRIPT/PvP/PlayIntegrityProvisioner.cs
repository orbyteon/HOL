using System;
using System.Collections;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
#if UNITY_ANDROID && !UNITY_EDITOR
using Google.Play.Integrity;
#endif

// Production bootstrap for a fresh PlayFab installation. The PlayFab secret
// never enters Unity: a Google Play Integrity token is sent to the trusted
// provisioning service, which validates it before creating the PlayFab account.
public class PlayIntegrityProvisioner : MonoBehaviour
{
    [Tooltip("Optional debug/local override. Empty uses ReleaseConfig.ProvisioningUrl.")]
    public string endpointUrl = "";

    [Tooltip("Optional debug/local override. Zero uses the required positive ReleaseConfig.GoogleCloudProjectNumber.")]
    public long cloudProjectNumber;

#if UNITY_ANDROID && !UNITY_EDITOR
    // Google Play Integrity Unity 2.0.0 currently has an upstream compile bug
    // in its V2 managers (wrong failure-callback signature). HOL uses the
    // stable 1.4.1 Standard API, which still supports requestHash binding.
    StandardIntegrityManager integrityManager;
    StandardIntegrityTokenProvider tokenProvider;
#endif

    [Serializable]
    class ProvisionRequest
    {
        public string customId;
        public string appVersion;
        public string integrityToken;
    }

    [Serializable]
    class ProvisionResponse
    {
        public bool ok;
        public string error;
    }

    string ResolvedEndpoint => string.IsNullOrWhiteSpace(endpointUrl)
        ? ReleaseConfig.ProvisioningUrl
        : endpointUrl.Trim();

    long ResolvedCloudProjectNumber => cloudProjectNumber > 0
        ? cloudProjectNumber
        : ReleaseConfig.GoogleCloudProjectNumber;

    public void Provision(string customId, Action<bool> done)
    {
        string endpoint = ResolvedEndpoint;
        long projectNumber = ResolvedCloudProjectNumber;
        Uri endpointUri;
        if (string.IsNullOrWhiteSpace(customId) || projectNumber <= 0 ||
            !Uri.TryCreate(endpoint, UriKind.Absolute, out endpointUri) ||
            endpointUri.Scheme != Uri.UriSchemeHttps || string.IsNullOrWhiteSpace(endpointUri.Host) ||
            !string.IsNullOrEmpty(endpointUri.UserInfo))
        {
            Debug.LogError("Play Integrity provisioning is not configured with a valid HTTPS endpoint and positive Google Cloud project number.");
            done?.Invoke(false);
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        StartCoroutine(ProvisionAndroid(customId, endpoint, projectNumber, done));
#else
        Debug.LogWarning("Play Integrity provisioning is available only on an Android device build.");
        done?.Invoke(false);
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    IEnumerator ProvisionAndroid(string customId, string endpoint, long projectNumber, Action<bool> done)
    {
        bool providerReady = false;
        yield return EnsureProvider(projectNumber, ok => providerReady = ok);
        if (!providerReady)
        {
            done?.Invoke(false);
            yield break;
        }

        string requestHash = CanonicalRequestHash(Application.identifier, customId, Application.version);
        var tokenOperation = tokenProvider.Request(new StandardIntegrityTokenRequest(requestHash));
        yield return tokenOperation;

        if (!tokenOperation.IsSuccessful)
        {
            Debug.LogError("Play Integrity token request failed: " + tokenOperation.Error);
            done?.Invoke(false);
            yield break;
        }

        var tokenResult = tokenOperation.GetResult();
        string token = tokenResult != null ? tokenResult.Token : "";
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("Play Integrity returned an empty token.");
            done?.Invoke(false);
            yield break;
        }

        var payload = new ProvisionRequest
        {
            customId = customId,
            appVersion = Application.version,
            integrityToken = token,
        };

        var req = new UnityWebRequest(endpoint, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload)));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("Accept", "application/json");
        req.timeout = 15;

        yield return req.SendWebRequest();

        bool ok = false;
        if (req.result == UnityWebRequest.Result.Success)
        {
            try
            {
                var response = JsonUtility.FromJson<ProvisionResponse>(req.downloadHandler.text ?? "");
                ok = response != null && response.ok;
            }
            catch { }
        }

        if (!ok)
            Debug.LogError("Production account provisioning failed (HTTP " + req.responseCode + ").");

        req.Dispose();
        done?.Invoke(ok);
    }

    IEnumerator EnsureProvider(long projectNumber, Action<bool> done)
    {
        if (tokenProvider != null)
        {
            done?.Invoke(true);
            yield break;
        }

        if (integrityManager == null)
            integrityManager = new StandardIntegrityManager();

        var prepare = integrityManager.PrepareIntegrityToken(
            new PrepareIntegrityTokenRequest(projectNumber));
        yield return prepare;

        if (!prepare.IsSuccessful)
        {
            Debug.LogError("Play Integrity provider preparation failed: " + prepare.Error);
            done?.Invoke(false);
            yield break;
        }

        tokenProvider = prepare.GetResult();
        done?.Invoke(tokenProvider != null);
    }
#endif

    public static string CanonicalRequestHash(string packageName, string customId, string appVersion)
    {
        string canonical = (packageName ?? "") + "\n" + (customId ?? "") + "\n" + (appVersion ?? "");
        using (var sha = SHA256.Create())
        {
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(canonical));
            var sb = new StringBuilder(hash.Length * 2);
            for (int i = 0; i < hash.Length; i++)
                sb.Append(hash[i].ToString("x2"));
            return sb.ToString();
        }
    }
}
