# AGENTS.md — HOL (Higher or Lower)

Unity 2022.3 Android game. Repo: `orbyteon/HOL` (private). Bundle id
`com.Orbyteon.HOL`. EditMode integrity tests live under `Assets/Tests/EditMode/`;
GameCI runs those tests plus an Android compile build once the required Unity
license credentials are configured. CI intentionally fails when they are
missing so a skipped compile cannot look green.

## Layout

- `Assets/Scenes/` — `SplashScene.unity` (loader) and `MainMenu.unity`
  (everything: menu, solo game, PvP UI hooks).
- `Assets/SCRIPT/` — gameplay (`GameManager`, `NumberManager`,
  `FakeMatchmaking`, `MenuManager`), ads (`AdsManager`, `ConsentManager`),
  ops (`ForceUpdate` — PlayFab TitleData `minVersion` gate, fail-open),
  `ReleaseConfig` + `ReleaseBootstrap` (public production runtime config),
  `PvP/` (backend abstraction + Firebase/PlayFab clients + controller),
  `Localization/` (`L10n` table, `LocalizedText`, `LanguageSelector`),
  `SmartHooks/` (`GameEvents`, `DailyStreak`, `Haptics`),
  `UIJuice/` (`ButtonJuice`, `PanelAnimator`, `ConfettiBurst`, …),
  `RuntimeUI/` (`RuntimeUI` factory + runtime wiring components),
  `Design/` (Converging Light layer: `ConvergingLight` palette/textures,
  `SplashDesign`, `DesignRuntimeWiring`, `NumberDrift`).
- `Assets/Editor/ReleaseBuildGuard.cs` — release-only fail-closed validation
  activated by `-holReleaseBuild` in the signed-AAB workflow.
- `services/provisioner/` — Azure Functions first-install account provisioner;
  validates Google Play Integrity before using PlayFab Server credentials.
- `playfab/cloudscript.js` — server authority for PlayFab room
  create/read/join/guess/leave/cleanup.
- `tools/playfab/deploy-cloudscript.mjs` — publishes/verifies Legacy CloudScript
  and optionally adds Client Shared Group API deny policy statements.
- `tools/release/write-release-config.mjs` — validates/injects public production
  config into the temporary release-build workspace.
- `docs/privacy.html` — privacy policy; keep it truthful when data practices change.

## Conventions (follow these)

- **Every `.cs` and folder needs a committed `.meta`.** Scripts without
  metas got random GUIDs per machine in the past; don't let it happen
  again. MonoImporter metas use the standard block, new GUID per file.
- **Scene edits are hand-edited YAML.** Use fileIDs in the `20000001xx`
  range for new documents on the `PvpRuntimeUI` root GameObject (next free:
  2000000111), register new roots in the `SceneRoots` block at file end,
  and reuse the ConsentManager block as the template.
- **Prefer runtime wiring over scene surgery** for new UI: the
  `RuntimeUI/` components (`PvpRuntimeUI`, `ExtrasRuntimeWiring`,
  `JuiceRuntimeWiring`) and `Design/DesignRuntimeWiring` build/attach UI
  one frame after `Start`, so runtime-built panels are covered too.
- **All user-facing strings go through `L10n.Get(key)`** with both EN and
  EL entries in `Assets/SCRIPT/Localization/L10n.cs`. Never add hardcoded
  English UI text. Formatted entries take args: `L10n.Get("key", arg)`.
  Runtime-built labels must also follow live language switching: attach
  `LocalizedText` (TMP) or `LocalizedLegacyText` (legacy `Text`) with the
  key instead of baking `L10n.Get` at build time — the
  `RuntimeUI.Localize*(..., key)` helpers do this in one call.
- **UI colors follow Converging Light** (`design/philosophy.md`): indigo
  depth backgrounds, cyan `(0.25, 0.85, 1)` for secondary actions, muted
  gold `(1, 0.78, 0.34)` reserved for the primary CTA, text near-white
  `(0.91, 0.93, 1)` — never pure white or pure black. Gold/cyan buttons
  use dark indigo labels for contrast.
- **Null-guard optional scene references** (`if (x != null)`) — several
  Inspector fields are intentionally unwired and filled at runtime.
- **PlayFab clients never read or write Shared Group Data directly.** All
  gameplay-impacting room operations go through `ExecuteCloudScript`, and
  `playfab/cloudscript.js` derives the player side from `currentPlayerId`.
  Keep Shared Group state `Private`; production deployment keeps the Client
  Shared Group API methods denied as defense in depth.
- **Committed production config stays empty.** Do not put real production
  values into `Assets/Resources/HOLReleaseConfig.json`. The signed release
  workflow injects `PLAYFAB_TITLE_ID`, `PROVISIONING_URL`, and
  `GOOGLE_CLOUD_PROJECT_NUMBER` only in its temporary Actions workspace.
- **Production workflows are manual and fail closed.** Preserve the `main`
  ref checks, typed `BUILD`/`DEPLOY` confirmations, `production` environment,
  and serialized concurrency. Never turn merge-to-main into automatic deploy.
- Git: feature branches merged with `--no-ff` into `main`, pushed
  immediately. Never commit tokens, keystores (`*.keystore` is ignored),
  or `hol.bundle` / `_to_delete/`. Watch for `.git/index.lock` — another
  assistant session sometimes works this repo concurrently.

## Backend setup state

- PvP backend is **PlayFab** for production. Firebase client (`PvpClient`) is a
  development fallback only and needs its RTDB URL in the Inspector.
- Debug/local development may use Inspector PlayFab values. A signed production
  build instead reads `ReleaseConfig`; `ReleaseBootstrap` forces PlayFab and
  applies the injected Title ID before `PvpRuntimeUI.Start` creates its backend.
- Production PlayFab account creation is closed on the client
  (`CreateAccount=false`). Fresh installs use `PlayIntegrityProvisioner` and the
  Azure service in `services/provisioner/`; the PlayFab Title Secret Key must
  remain server-only. Standard Play Integrity requires a positive Google Cloud
  project number in the production config.
- Ads: LevelPlay app key `6076495` (Android) in `AdsManager`; iOS keys are
  placeholders. Ads are opt-in: declining keeps LevelPlay uninitialized on later
  launches and blocks ad loads/shows. Settings → Ads privacy re-opens the choice.
  Interstitial unit `Interstitial_Android` plus rewarded unit
  `Rewarded_Android` powers the save-your-streak offer. Production CMP/mediation
  compliance is an external release setting and must match `docs/privacy.html`.
- Force update: optional PlayFab TitleData key `minVersion` (e.g. `0.2.0`)
  blocks older builds with a store-link dialog. It reuses the PvP PlayFab
  session. Missing key / no authenticated PlayFab session / offline remains
  fail-open.
- Signing: use `.github/workflows/build-release.yml` for production AABs. The
  upload keystore is supplied as `ANDROID_KEYSTORE_BASE64` plus password/alias
  environment secrets; the workflow passes `-holReleaseBuild`, and
  `ReleaseBuildGuard` enables/validates custom signing only for that build.
  Never commit the keystore or its passwords; keep an offline backup.
