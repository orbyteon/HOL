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

The service also requires, by default:

- the expected Play package name;
- a fresh token (maximum age 120 seconds);
- `PLAY_RECOGNIZED` app integrity;
- a signing-certificate digest match when configured;
- `MEETS_DEVICE_INTEGRITY`;
- `LICENSED` app licensing.

After those checks, the service calls PlayFab `Server/LoginWithCustomID(CreateAccount:true)` using the Title Secret Key and returns only `{ "ok": true }` plus the non-sensitive `newlyCreated` flag.

## Required Azure app settings

| Setting | Purpose |
| --- | --- |
| `PLAYFAB_TITLE_ID` | HOL PlayFab Title ID |
| `PLAYFAB_DEV_SECRET_KEY` | PlayFab Title Secret Key; server only |
| `GOOGLE_PLAY_PACKAGE_NAME` | Android package, currently `com.Orbyteon.HOL` |
| `GOOGLE_SERVICE_ACCOUNT_JSON_B64` | Base64 of a Google service-account JSON credential with Play Integrity API access; omit only when Azure workload identity/ADC is configured |
| `GOOGLE_PLAY_CERT_SHA256` | Comma-separated certificate SHA-256 digests returned by Play Integrity (recommended) |
| `REQUIRE_LICENSED` | Defaults to `true`; do not disable in production |
| `INTEGRITY_MAX_AGE_MS` | Defaults to `120000` |

Do not log, commit, or return the service-account JSON, PlayFab secret, integrity token, or Custom ID.

## Google setup

1. Link HOL's Google Play Console app to a Google Cloud project for Play Integrity.
2. Enable the Play Integrity API in that Cloud project.
3. Create a service account used only by this function and grant it the access required to decode HOL Play Integrity tokens.
4. Add the service account in Play Console's Play Integrity API setup where required.
5. Record the Play App Signing certificate SHA-256 digest in `GOOGLE_PLAY_CERT_SHA256`.

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

Copy that URL into HOL's production provisioning configuration after the app is deployed.

## Local validation

```bash
cd services/provisioner
npm ci
npm test
npm run check
```

The service cannot produce a valid Play Integrity token from a local desktop test. End-to-end verification must use a Google Play-distributed test build on a physical/certified Android device.
