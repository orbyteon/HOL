# PlayFab production authentication boundary

HOL's Unity client identifies an installation with `SystemInfo.deviceUniqueIdentifier`
and uses that value as a PlayFab Custom ID. Production builds intentionally call
`Client/LoginWithCustomID` with `CreateAccount:false`.

A fresh production installation is therefore provisioned by the trusted service
in `services/provisioner/` before the Unity client retries its normal login. The
PlayFab Title Secret Key never ships in Unity.

## Implemented production flow

1. Unity first calls `Client/LoginWithCustomID(CreateAccount:false)`.
2. If PlayFab reports that the anonymous account does not exist, Unity prepares
   a **standard Google Play Integrity** token provider and requests a token whose
   `requestHash` binds the package name, Custom ID, and app version.
3. Unity sends only the Custom ID, app version, and encrypted integrity token to
   `POST /api/provision` on the Azure Functions service.
4. The service asks Google to decode the token and rejects the request unless the
   package, request hash, freshness, Play recognition, device integrity, license
   status, and Play App Signing certificate all match production policy.
5. Only after those checks does the service call PlayFab
   `Server/LoginWithCustomID(CreateAccount:true)` using the server-held Title
   Secret Key.
6. Unity retries `Client/LoginWithCustomID(CreateAccount:false)` once. PvP and
   ForceUpdate then share that authenticated client session.

The service returns only success/failure plus the non-sensitive `newlyCreated`
flag. It does not return PlayFab server credentials or the Play Integrity token.

## Request binding

Both client and service calculate the SHA-256 hex digest of this exact UTF-8
serialization:

```text
<packageName>\n<customId>\n<appVersion>
```

EditMode and Node tests carry the same regression vector so a serialization
change on one side cannot silently weaken the binding.

## Signing-certificate pin

Google Play Integrity returns `certificateSha256Digest` as a URL-safe Base64
SHA-256 digest. Operators often obtain the same Play App Signing certificate as
a colon-separated hexadecimal SHA-256 fingerprint from Play Console or keytool.
The provisioner accepts either representation (plus standard padded Base64) and
normalizes it before comparison. Every comma-separated configured digest must be
a valid 32-byte SHA-256 value; malformed configuration is rejected rather than
silently disabling the certificate check.

Pin the **Play App Signing** certificate used by Google Play to sign installs,
not the developer upload-key certificate. Multiple comma-separated pins are
supported for an intentional signing-certificate transition.

## Deployment

`.github/workflows/deploy-provisioner.yml` is the production deployment path. It
is manual, uses the `production` GitHub Environment, refuses refs other than
`main`, and requires the operator to type `DEPLOY`. Production deployment also
requires the `com.Orbyteon.HOL` package value and a valid non-empty Play App
Signing certificate digest, then probes the deployed endpoint and requires an
invalid empty request to return HTTP 400 before reporting success.

Required production settings are documented in `services/provisioner/README.md`.
The PlayFab and Google credentials are secrets. The function-app name, resource
group, package name, signing-certificate digest, PlayFab title ID used by the app,
provisioning URL, and Google Cloud project number are non-secret configuration.

After the function is deployed, the signed Android release workflow
`.github/workflows/build-release.yml` injects these public app values into
`Assets/Resources/HOLReleaseConfig.json` **only in the Actions workspace**:

- `PLAYFAB_TITLE_ID`
- `PROVISIONING_URL`
- `GOOGLE_CLOUD_PROJECT_NUMBER`

The committed JSON must remain empty. `ReleaseBuildGuard` rejects a production
build if any required value is missing or malformed. The release workflow also
fails if the workspace contains changes other than this injected JSON, verifies
the resulting AAB signature with `jarsigner`, and records its SHA-256 checksum.

## Abuse controls

Google Play Integrity is the primary attestation boundary. The request hash binds
an integrity token to the exact installation ID and app version being provisioned,
and the service accepts only fresh tokens. Standard Play Integrity requests also
receive Google's replay protection when their token is decoded.

The function also has a small in-process IP limiter, but that limiter is not a
distributed quota across serverless instances. Configure platform-level rate
limiting/WAF controls (Azure API Management, Front Door/WAF, or equivalent)
before public release.

## Release verification

- Deploy the provisioner from `main` using the manual workflow.
- Use a Google Play-distributed test build on a certified physical Android device.
- Verify a brand-new install provisions successfully and the subsequent
  `Client/LoginWithCustomID(CreateAccount:false)` succeeds.
- Verify an unprovisioned Custom ID cannot create an account directly from Unity.
- Verify a tampered package/request hash, stale token, unlicensed app, wrong
  signing certificate, malformed certificate configuration, or failed
  device-integrity verdict is rejected.
- Verify PlayFab API Access Policy blocks Client Shared Group operations while
  `ExecuteCloudScript` PvP continues to work.

See `services/provisioner/README.md` for Azure/Google setup details and
`docs/release-checklist.md` for the full release gate.