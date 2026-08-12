# PlayFab production authentication boundary

HOL's Unity client identifies an installation with `SystemInfo.deviceUniqueIdentifier`
and uses that value as a PlayFab Custom ID. Production builds intentionally call
`Client/LoginWithCustomID` with `CreateAccount:false`.

That means a **fresh production installation must be provisioned by a trusted
service before the Unity client can log in**. Do not put the PlayFab Title Secret
Key in Unity and do not enable client-side anonymous account creation as a
production shortcut.

## Required service behavior

The trusted service should:

1. Receive a request to provision the installation's Custom ID through an
   authenticated/attested and rate-limited path appropriate for the deployment.
2. Call PlayFab `Server/LoginWithCustomID` over HTTPS with:
   - header `X-SecretKey: <PlayFab Title Secret Key>`
   - the HOL Title ID
   - the same Custom ID the Unity client will use
   - `CreateAccount: true`
3. Return only a success/failure result needed by the client bootstrap flow.
   Do not return the Title Secret Key or otherwise expose server credentials.
4. After successful provisioning, the Unity app uses its normal
   `Client/LoginWithCustomID(CreateAccount:false)` flow. PvP and ForceUpdate
   then share that client session.

Official API reference:
`https://learn.microsoft.com/en-us/rest/api/playfab/server/authentication/login-with-custom-id`

PlayFab's anonymous-login guidance:
`https://learn.microsoft.com/en-us/xbox/playfab/identity/player-identity/platform-specific-authentication/anonymous-login`

## What is intentionally not implemented in this repository

This Unity repository cannot create a trustworthy public provisioning endpoint by
itself. A bare internet endpoint that accepts any caller-supplied Custom ID and
uses the Title Secret Key to create accounts simply moves client-side account
creation behind an unauthenticated proxy; it does not create a meaningful trust
boundary.

Choose the deployment/attestation mechanism first (for example an existing
backend with authenticated sessions or a platform-attested bootstrap service),
then implement the server endpoint there. Store the PlayFab Title Secret Key only
in that service's secret manager/environment.

## Release verification

- Provision a brand-new test installation through the trusted service.
- Verify `Client/LoginWithCustomID(CreateAccount:false)` succeeds afterward.
- Verify an unprovisioned Custom ID cannot create an account from the Unity app.
- Verify PlayFab API Access Policy blocks Client Shared Group operations while
  `ExecuteCloudScript` PvP continues to work.
