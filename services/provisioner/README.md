# HOL production provisioning service

This Azure Functions service creates a PlayFab account for a fresh **production** HOL install only after Google Play Integrity validates the request.

The Unity release client does not carry the PlayFab Title Secret Key and uses `Client/LoginWithCustomID(CreateAccount:false)`. If that login reports that player creation is disabled, the client requests a **standard Play Integrity token**, sends it here, and retries the client login after provisioning succeeds.

## Trust checks

`POST /api/provision` accepts:

```json
{
  "customId": "same CustomId used by Unity",
  "appVersion": "Application.version",
  "integrityToken": "encrypted standard Play Integrity token"
}
```

The token's `requestHash` must equal the SHA-256 hex digest of this exact UTF-8 serialization:

```text
<packageName>\n<customId>\n<appVersion>
```

The production deployment requires:

- the expected Play package name;
- a fresh token (maximum age 120 seconds);
- `PLAY_RECOGNIZED` app integrity;
- a Play App Signing certificate digest match;
- `MEETS_DEVICE_INTEGRITY`;
- `LICENSED` app licensing.

After those checks, the service calls PlayFab `Server/LoginWithCustomID(CreateAccount:true)` using the Title Secret Key and returns only `{ "ok": true }` plus the non-sensitive `newlyCreated` flag.

## Required Azure app settings

| Setting | Purpose |
| --- | --- |
| `PLAYFAB_TITLE_ID` | HOL PlayFab Title ID |
| `PLAYFAB_DEV_SECRET_KEY` | PlayFab Title Secret Key; server only |
| `GOOGLE_PLAY_PACKAGE_NAME` | Android package; production workflow requires `com.Orbyteon.HOL` |
| `GOOGLE_SERVICE_ACCOUNT_JSON_B64` | Base64 of a Google service-account JSON credential with Play Integrity API access |
| `GOOGLE_PLAY_CERT_SHA256` | Comma-separated allowed Play App Signing certificate SHA-256 digest(s); required by production deployment |
| `REQUIRE_LICENSED` | Production workflow sets `true`; do not disable in production |
| `INTEGRITY_MAX_AGE_MS` | Production workflow sets `120000` |

`GOOGLE_PLAY_CERT_SHA256` accepts either the familiar 32-byte SHA-256 certificate fingerprint as 64 hex characters (with or without `:` separators) or the URL-safe Base64 digest returned by Play Integrity. Standard padded Base64 is accepted too and normalized to URL-safe Base64 before comparison. Every comma-separated entry must be a valid SHA-256 digest; malformed configuration fails closed.

Do not log, commit, or return the service-account JSON, PlayFab secret, integrity token, or Custom ID.

## Google setup

1. Link HOL's Google Play Console app to a Google Cloud project for Play Integrity.
2. Enable the Play Integrity API in that Cloud project.
3. Create a service account used only by this function and grant it the access required to decode HOL Play Integrity tokens.
4. Add the service account in Play Console's Play Integrity API setup where required.
5. Record the **Play App Signing** certificate SHA-256 fingerprint (not the upload-key certificate) in `GOOGLE_PLAY_CERT_SHA256`. A colon-separated hex fingerprint copied from Play Console/keytool is accepted directly; the service converts it to the URL-safe Base64 representation used by Play Integrity.

The GitHub deployment workflow expects the service-account JSON as a **base64 GitHub secret**, which avoids multiline quoting problems. For example, generate it locally without sending it through chat:

```bash
base64 -w 0 service-account.json
```

On macOS use `base64 < service-account.json | tr -d '\n'`.

## Azure setup

Create one production Azure Function App using a supported Node 22 runtime. The repository workflow `.github/workflows/deploy-provisioner.yml` deploys the code with GitHub OIDC and configures the app settings.

Use an Azure resource group and Function App dedicated to HOL when practical. Add platform-level throttling (Azure API Management, Front Door/WAF, or equivalent) before public release. The in-process rate limiter is only a low-cost abuse brake and is **not** a distributed production quota.

The endpoint will normally be:

```text
https://<function-app-name>.azurewebsites.net/api/provision
```

The deployment workflow waits for this endpoint after publishing and verifies that an empty JSON request is rejected with HTTP 400. Copy the URL into HOL's `PROVISIONING_URL` production variable only after the deployment is healthy.

## Local validation

```bash
cd services/provisioner
npm ci
npm test
npm run check
```

The service cannot produce a valid Play Integrity token from a local desktop test. End-to-end verification must use a Google Play-distributed test build on a physical/certified Android device.