using System;
using UnityEngine;

// Public, non-secret configuration for the separately installed operator-provisioned
// test APK. No preferences, room rules or production provisioning configuration.
public static class PvpIsolatedPlaytest
{
    public const string TitleId = "11CB9E";
    public const string PackageId = "com.Orbyteon.HOL.pvptest";
    public const string ProductName = "HOL PvP Test";
    public const string CloudScriptSha256 =
        "EBB9DEE03FE4D147E63B555DA36EA5D56AAFEE85DB91F72BC56938B7963DEEB5";

    public static bool Enabled
    {
        get
        {
#if HOL_PVP_ISOLATED_PLAYTEST
            // Keep the guard active even in a misbuilt release so it fails closed.
            return true;
#else
            return false;
#endif
        }
    }

    public static bool IsValidTarget(string title, string package, bool android, bool development)
    {
        return android && development && title == TitleId && package == PackageId;
    }

    public static bool AllowsRequest(string title, string url, string package,
        bool android, bool development)
    {
        if (!IsValidTarget(title, package, android, development)) return false;
        Uri uri;
        return Uri.TryCreate(url, UriKind.Absolute, out uri) &&
            uri.Scheme == "https" && uri.Port == 443 &&
            string.Equals(uri.Host, TitleId + ".playfabapi.com", StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrEmpty(uri.UserInfo) && string.IsNullOrEmpty(uri.Query) &&
            string.IsNullOrEmpty(uri.Fragment) && uri.AbsolutePath.StartsWith("/Client/", StringComparison.Ordinal);
    }

    public static bool AllowsClientCreation(bool isolated, bool development, bool requested)
    {
        return !isolated && development && requested;
    }

    public static bool AllowsProductionProvisioning(bool isolated, bool clientCreates,
        bool allowProvisioning, bool missingAccount)
    {
        return !isolated && !clientCreates && allowProvisioning && missingAccount;
    }

    public static void Configure(PlayFabPvpClient client)
    {
        if (!Enabled) return;
        client.titleId = TitleId;
        client.allowClientAccountCreationInDebugBuilds = false;
        client.provisioningUrl = "";
        client.googleCloudProjectNumber = 0;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ReportDevice()
    {
        if (!Enabled) return;
        if (!IsValidTarget(TitleId, Application.identifier,
            Application.platform == RuntimePlatform.Android, Debug.isDebugBuild))
        {
            Debug.LogError("HOL_PVP_TEST_INVALID_BUILD: network access is disabled.");
            return;
        }
        // Actual Android device-bound ID used by LoginWithCustomID; no fabricated
        // identities, session tickets or server credentials. Operator reads via adb.
        Debug.Log("HOL_PVP_TEST_DEVICE title=" + TitleId + " package=" + Application.identifier +
            " customId=" + SystemInfo.deviceUniqueIdentifier);
    }
}
