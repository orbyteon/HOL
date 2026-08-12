// Non-secret production configuration compiled into the app.
// Secrets belong only in GitHub/Azure/PlayFab/Google server settings.
public static class ReleaseConfig
{
    // Set after deploying services/provisioner. Release validation must reject
    // an empty value before a public Play Store build is promoted.
    public const string ProvisioningUrl = "";

    // For Play-distributed apps linked to a Cloud project, Play Integrity can
    // use 0 when supported by the title setup. Set the numeric project number
    // here if the Play Integrity configuration requires it.
    public const long GoogleCloudProjectNumber = 0;
}
