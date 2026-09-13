# Existing local Windows AAB entry point

`Assets/Editor/HolLocalReleaseBuild.cs` is the same local helper used for the
September 13 signed HOL 0.4.1 / versionCode 7 AAB. It is Editor-only and does not
run on startup. This handoff does not authorize another build, backend deployment
or Play Console publication. The production Actions workflows and their `main`,
environment and confirmation guards remain unchanged.

## Opening and playing the project

Use Unity **2022.3.62f3** and the pinned UPM dependencies. Start from
`Assets/Scenes/SplashScene.unity`; MainMenu is the second enabled scene.
No signing key, release configuration or personal preferences are needed to
import the project or reproduce its local UI. Internet access is needed to
restore UPM/OpenUPM packages on first import.

Real online PvP additionally requires the intended PlayFab environment and a
valid account through the existing authentication flow. The code does not
provide an offline replacement server. Do not copy Marinos's or Andreas's
saved account identifiers. For isolated two-device development use the existing
[test plan](pvp-isolated-playtest.md) and test-build workflow, not a
production secret in the client. A fresh production account requires the normal
Google Play/Play Integrity provisioning path; a successful local import or an
operator/API login does not prove that end-to-end path.

## Inputs for a separately authorized local release

Save and close Unity before invoking a batch build against its project. Install
Android Build Support, SDK/NDK and OpenJDK for this exact Unity version. The
helper's upload-certificate probe uses the bundled Windows `keytool.exe`.

Keep a public JSON configuration **outside the Unity project** and point
`HOL_LOCAL_AAB_CONFIG` to its absolute path. Its fields are:

| Field | Value to supply |
| --- | --- |
| `playFabTitleId` | The explicitly authorized public-release Title |
| `provisioningUrl` | Its verified HTTPS first-install provisioner |
| `googleCloudProjectNumber` | The positive Play Integrity Cloud project number |
| `version` | Semantic version, e.g. `0.4.2` for a future authorized update |
| `versionCode` | A code confirmed unused in Play Console; **7 is already used** |
| `outputAab` | A new absolute `.aab` path outside the project; existing files are rejected |
| `uploadCertificateSha256` | The 64-hex upload certificate fingerprint verified in Play Console |

The helper rejects the operator-only test Title, isolated-test compilation,
wrong Unity version, wrong startup scene order and unsafe output location.
`Assets/Resources/HOLReleaseConfig.json` must remain committed with empty values.
Do not place production values in that tracked file as developer setup.

Signing inputs are process-local environment variables supplied only to the
owned Unity process:

- `HOL_LOCAL_KEYSTORE_PATH`: existing authorized upload keystore, outside Git.
- `HOL_LOCAL_KEY_ALIAS`: the existing private-key alias.
- `HOL_LOCAL_STORE_PASS` and `HOL_LOCAL_KEY_PASS`: obtain through masked local
  prompts or an approved secret provider, never command-line arguments, shell
  history, chat, logs, checked-in JSON or a committed launch script.

Use the existing upload key; do not generate a replacement. The helper checks
the alias is a `PrivateKeyEntry` and its public certificate matches the supplied
Play Console fingerprint. Clear/restore process-local secret environment values
after the owned process exits. No server secret is a build input.

## Entry points and preservation

With `HOL_LOCAL_AAB_CONFIG` supplied, the read-only public-input entry point is
`HolLocalReleaseBuild.ValidatePublicConfiguration` via Unity's `-executeMethod`.
It validates public inputs only, not signing, account provisioning or gameplay.

The build entry point is `HolLocalReleaseBuild.Build`, invoked with Unity's
`-batchmode -buildTarget Android -holReleaseBuild -executeMethod` options,
the actual `-projectPath`, an external `-logFile`, and `-quit`. The existing
`ReleaseBuildGuard` remains mandatory. Run only one Unity process and wait for
that process's exit; launching a Windows GUI executable alone may return before
the build is complete.

Before a build/import, preserve the exact saved `ProjectSettings/ProjectSettings.asset`
and `Assets/Resources/HOLReleaseConfig.json` outside the project. Keep authoring
closed until completion. The helper restores the values it changes in `finally`,
but Unity may additionally normalize serialized fields. The September 13 build
normalized `overrideDefaultApplicationIdentifier` and the empty keystore-path
representation; the original bytes were separately preserved and restored after
inspection. Compare before/after files and preserve any new authoring work;
never blindly restore a backup over subsequent changes. If Unity crashes, retain
logs and inspect the temporary config/signing state before any commit or retry.

The build uses `com.Orbyteon.HOL`, non-Development IL2CPP, ARMv7 plus ARM64, API 36,
and SplashScene followed by MainMenu. It writes an external AAB and SHA-256 file;
it neither uploads it nor deploys a service. Afterwards independently verify the
signature/certificate, bundletool manifest, versionCode, 16-KB native alignment,
effective public configuration and absence of key containers before delivery.

The machine-specific launcher and verifier used by Marinos remain in his
external release archive. Their absolute machine paths and signing-prerequisite
record are not portable project dependencies; use the documented entry points
with the operator's own authorized inputs instead of copying those scripts
unchanged.

## Recorded evidence, not a new release

The existing helper compiled and built once under Unity 2022.3.62f3. The retained
log contains `Build Finished, Result: Success` and `HOL_LOCAL_AAB_BUILT`.
The existing AAB independently passed jarsigner, bundletool, package/configuration
and ARM64 16-KB checks. The initial wrapper's post-build jarsigner quoting error
was corrected without rebuilding. Artifact SHA-256:

`0DFAA68A4EA9F871238B8607DEB0DDE573FAC3E2438634C03C5EEDEC1C0A78D7`

Source completeness, build verification, Google review and real two-device
gameplay acceptance are distinct. No new build, capture matrix, live login or
backend validation is claimed by this source synchronization.
