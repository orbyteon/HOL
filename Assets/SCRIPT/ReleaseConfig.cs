// Non-secret production configuration compiled into the app.
// Secrets belong only in GitHub/Azure/PlayFab/Google server settings.
public static class ReleaseConfig
{
    // Set after deploying services/provisioner. Release validation must reject
    // an empty value before a public Play Store build is promoted.
    public const string ProvisioningUrl = "";

    // Numeric Google Cloud project number linked to HOL's Play Integrity setup.
    // Keep this public value separate from service-account credentials/secrets.
    public const long GoogleCloudProjectNumber = 0;
}
