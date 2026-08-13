# HOL — Release checklist

Release blockers and go-live steps, in order. Repository automation is designed
to fail closed; a green PR does **not** deploy production or publish to Google Play.

## 0. Continuous-integration gate

`.github/workflows/ci.yml` runs static integrity checks, Node provisioner tests,
Unity EditMode tests, and an Android compile/package build. Missing Unity
credentials fail CI instead of skipping the Unity jobs.

Required repository/production secrets for CI:

- `UNITY_LICENSE`
- `UNITY_EMAIL`
- `UNITY_PASSWORD`

Before merging a release candidate:

- [ ] `Static integrity` is green
- [ ] `Provisioner tests` is green
- [ ] `EditMode tests` is green
- [ ] `Build Android (compile check)` is green
- [ ] the debug Android artifact is produced

## 1. Configure the protected `production` GitHub Environment

Create a GitHub Environment named exactly `production`. All production deploy
and signed-build workflows reference it.

Recommended environment protection:

- [ ] restrict deployment branches/tags to `main`
- [ ] require manual reviewer approval when the repository plan supports it
- [ ] keep production credentials as environment secrets rather than source files

Production **variables** (non-secret):

- `PLAYFAB_TITLE_ID` — HOL PlayFab Title ID
- `AZURE_FUNCTIONAPP_NAME` — Azure Function App hosting the provisioner
- `AZURE_RESOURCE_GROUP` — Azure resource group containing that app
- `GOOGLE_PLAY_PACKAGE_NAME` — `com.Orbyteon.HOL`
- `GOOGLE_PLAY_CERT_SHA256` — allowed Play App Signing certificate digest(s)
- `PROVISIONING_URL` — public HTTPS URL ending in `/api/provision`
- `GOOGLE_CLOUD_PROJECT_NUMBER` — positive numeric project number used by
  Standard Play Integrity token preparation

Production **secrets**:

- `PLAYFAB_DEV_SECRET_KEY`
- `GOOGLE_SERVICE_ACCOUNT_JSON_B64`
- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`
- `UNITY_LICENSE`
- `UNITY_EMAIL`
- `UNITY_PASSWORD`
- `ANDROID_KEYSTORE_BASE64`
- `ANDROID_KEYSTORE_PASS`
- `ANDROID_KEYALIAS_NAME`
- `ANDROID_KEYALIAS_PASS`

Do not duplicate the public PlayFab Title ID as a secret; all release workflows
read the same `PLAYFAB_TITLE_ID` production variable.

## 2. Deploy the attested first-install provisioner

Release builds use `Client/LoginWithCustomID(CreateAccount:false)`. An anonymous
PlayFab identity that does not already exist is created only by
`services/provisioner/` after server-side Google Play Integrity verification.

- [ ] Link HOL in Play Console to the Google Cloud project used for Play Integrity
- [ ] Enable the Play Integrity API and authorize the service account used by Azure
- [ ] Configure the Play App Signing certificate digest in
      `GOOGLE_PLAY_CERT_SHA256` — production deployment refuses an empty value
- [ ] Configure distributed/platform throttling (Azure API Management,
      Front Door/WAF, or equivalent); the in-process limiter is supplemental only
- [ ] Run **Deploy Provisioning Service** from `main` and type `DEPLOY`
- [ ] The deployment health probe reaches `/api/provision` and receives HTTP 400
      for its intentionally invalid empty JSON request
- [ ] Confirm Azure app settings contain the expected package/title configuration
      without printing or exposing the PlayFab/Google credentials
- [ ] Confirm `PROVISIONING_URL` points at the deployed HTTPS endpoint

See `services/provisioner/README.md` and `docs/playfab-auth-provisioning.md`.

## 3. Deploy PlayFab server authority

`playfab/cloudscript.js` owns all PlayFab PvP Shared Group reads/writes. Clients
use `ExecuteCloudScript`; they must never receive live secret numbers or choose
their own host/guest identity.

- [ ] Run **Deploy PlayFab Production** from `main` and type `DEPLOY`
- [ ] The workflow successfully reads the published CloudScript revision back
      and verifies `cloudscript.js` matches repository source exactly
- [ ] The workflow verifies explicit deny statements for all direct Client Shared
      Group APIs; this hardening is mandatory in the production workflow
- [ ] Confirm the deployed revision exposes `createRoom`, `joinRoom`, `getRoom`,
      `submitGuess`, `ackResult`, and `leaveRoom`
- [ ] Verify a modified client cannot call Client `GetSharedGroupData`
- [ ] Verify a modified client cannot call Client `UpdateSharedGroupData`
- [ ] Verify normal PvP still works through `ExecuteCloudScript`

### Version gate

- [ ] Optional: PlayFab Title Data → `minVersion` (for example `0.2.0`)
- [ ] ForceUpdate remains fail-open when PlayFab/title data is unavailable

## 4. Produce the signed Android release candidate

The committed `Assets/Resources/HOLReleaseConfig.json` intentionally contains
empty/default values. Never hand-edit production identifiers into it.

Use **Build Android Release Candidate** from `main`:

1. enter the public semantic version;
2. enter a positive Google Play `versionCode` that is higher than every uploaded
   build;
3. type `BUILD`.

The workflow validates all production variables/secrets, injects public release
configuration only into the temporary Actions workspace, refuses to build if any
other workspace file changed, runs Unity with the release build guard, signs an
Android App Bundle using the secret keystore, verifies the AAB JAR signature,
and uploads the artifact with `JARSIGNER_VERIFY.txt` plus `SHA256SUMS`.

- [ ] workflow completes successfully
- [ ] exactly one `.aab` exists in the artifact
- [ ] `JARSIGNER_VERIFY.txt` contains `jar verified.`
- [ ] SHA-256 matches `SHA256SUMS`
- [ ] app version/versionCode are the intended Play Console values
- [ ] upload key/keystore is backed up offline; it is never committed to git

## 5. Smoke-test the intended release build

Use the signed candidate (or a Play internal-testing build made from that exact
candidate) on physical Android devices.

### Splash

- [ ] Indigo gradient background, faint drifting digits
- [ ] Logo blooms/breathes; cyan→magenta seam + tagline
- [ ] Gold loading hairline fills; tap skips splash

### Main menu / solo

- [ ] Consent dialog appears on first launch
- [ ] Choose **No** → LevelPlay remains disabled and the game still plays normally
- [ ] Choose **Yes** → ads initialize; Settings → Ads privacy can withdraw permission
- [ ] Reopen Ads privacy after switching EN/EL → dialog follows current language
- [ ] First launch on a Greek-language device starts in Greek
- [ ] Blank player name uses the localized default
- [ ] Difficulty buttons highlight the active choice in gold
- [ ] Stats label shows wins/losses/streak/best/fastest win
- [ ] Play → searching → Cancel and Android back both stop the search
- [ ] Opponent-found stinger plays only when an opponent is found
- [ ] Buttons click/squash and panels fade+rise
- [ ] Match end → Rematch returns to number entry; win confetti/stinger work
- [ ] Solo Android back mid-match: first press shows localized warning; second
      press within two seconds exits cleanly
- [ ] Simulated-opponent disclosure is visible on solo matchmaking screens

### PvP / provisioning

- [ ] A brand-new Play-distributed Android identity provisions successfully before
      its normal `Client/LoginWithCustomID(CreateAccount:false)` retry
- [ ] Existing identities log in without invoking provisioning
- [ ] Uninstall/reinstall the same Play-distributed build on the same Android
      user/device → the existing anonymous PlayFab identity is reused rather than
      creating a second account
- [ ] Tampered/unlicensed/non-device-integrity builds cannot provision
- [ ] A build signed with an unexpected certificate cannot provision
- [ ] Create room produces a 5-character code and second device can join
- [ ] A third simultaneous join attempt is rejected as room full
- [ ] Live `getRoom` client state contains no host/guest secret or PlayFab IDs
- [ ] Adding an invented `side` field to a guess request gives no authority;
      CloudScript derives identity from `currentPlayerId`
- [ ] Double-submit the same turn → only one server turn claim succeeds
- [ ] Higher/lower/correct hint is returned by the server and displays correctly
- [ ] Win count stays correct if result polling wins the race with submit callback
- [ ] A loss reveals only the opponent secret after `phase == done`
- [ ] Normal Leave removes the PlayFab room and notifies the other client
- [ ] Completed match is removed after both clients acknowledge the result
- [ ] Back out during create/join, then try the old code → no late UI hijack
- [ ] PvP Android back navigates create/join/menu; mid-match uses explicit Leave

### Other regressions

- [ ] Solo out-of-range guess is rejected without clearing typed input
- [ ] STOP GAME and number placeholder follow selected language
- [ ] Save-streak reward restores the streak without a second MatchEnded event
- [ ] Reload MainMenu twice on the same day → DailyStreak event fires only once
- [ ] Force-update `minVersion` higher than build → Update + Quit dialog
- [ ] Player Settings icon is HOL artwork, not Unity default

## 6. Advertising consent / CMP / Play Console

The in-app choice gates LevelPlay initialization and uses
`LevelPlayPrivacySettings.SetGDPRConsent`. Production compliance still depends on
the **actual mediated networks and launch regions** configured in LevelPlay/Play
Console. When serving regulated EEA/UK traffic, configure the appropriate
TCF-compatible CMP or supported Google consent framework for the final mediation
stack; a custom two-button app dialog alone must not be assumed to satisfy every
network/platform consent requirement.

- [ ] LevelPlay production app key/bundle `com.Orbyteon.HOL` are correct
- [ ] `Interstitial_Android` and `Rewarded_Android` units are active
- [ ] final mediated-network list has been reviewed for consent requirements
- [ ] CMP/Google Additional Consent configuration is completed where required
- [ ] consent **No** produces no LevelPlay initialization on a fresh launch
- [ ] consent **Yes** loads interstitial/rewarded normally
- [ ] withdrawing consent blocks new loads/shows for the rest of the session and
      prevents LevelPlay initialization on the next launch
- [ ] Play Console Data safety answers match `docs/privacy.html` and the final
      mediation/configuration stack

## 7. Privacy policy hosting

Host `docs/privacy.html` at a stable public HTTPS URL before publishing.

- [ ] open the policy URL without authentication/incognito
- [ ] link that exact URL in Play Console
- [ ] re-read the policy after final Azure, PlayFab, Play Integrity, LevelPlay,
      and CMP configuration; update it if actual handling differs

## 8. Security and release hygiene

- [ ] rotate any development credentials exposed outside intended secret stores
- [ ] confirm no Title Secret Key, keystore, passwords, service credentials, or
      private provisioning tokens are committed
- [ ] run repository secret scanning before changing repository visibility
- [ ] keep production workflows manual; do not add automatic deployment on merge
- [ ] preserve `main` + typed-confirmation guards on production workflows
- [ ] do not promote the Play Store build until every external/manual checkbox
      above that applies to the release has been verified