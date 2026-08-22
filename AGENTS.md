# AGENTS.md — HOL (Higher or Lower)

Unity 2022.3 Android game. Repo: `orbyteon/HOL` (private). Bundle id
`com.Orbyteon.HOL`. EditMode integrity tests live under `Assets/Tests/EditMode/`;
GameCI runs those tests plus an Android compile build once the required Unity
license credentials are configured. CI intentionally fails when they are
missing so a skipped compile cannot look green.

## Layout

- `Assets/Scenes/` — `SplashScene.unity` (loader) and `MainMenu.unity`
  (everything: menu, solo game, PvP UI hooks).
- `Assets/SCRIPT/` — gameplay (`GameManager`, `NumberManager`, `DuelRules`,
  `FakeMatchmaking`, `MenuManager`), ads (`AdsManager`, `ConsentManager`),
  ops (`ForceUpdate` — PlayFab TitleData `minVersion` gate, fail-open),
  `ReleaseConfig` + `ReleaseBootstrap` (public production runtime config),
  `PvP/` (backend abstraction + PlayFab client + controller +
  `Signals` quick-chat table),
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
  create/read/join/guess/signal/rematch/leave/cleanup.
- `tools/playfab/deploy-cloudscript.mjs` — publishes/verifies Legacy CloudScript,
  manages the expired-room scheduled task, and optionally adds Client Shared
  Group API deny policy statements.
- `tools/test/` — Node tests, run with `node --test tools/test/*.test.mjs` (the
  bare directory form does not resolve). `cloudscript.test.mjs` drives the real
  CloudScript against an in-memory Shared Group store;
  `room-state-contract.test.mjs` checks every key the server emits against
  `PvpBackend.RoomState`, because JsonUtility binds by exact field name and a
  mismatch fails silently; `lock-policy-sim.mjs` balances the Lock. No Unity
  needed for any of them.
- `tools/release/write-release-config.mjs` — validates/injects public production
  config into the temporary release-build workspace.
- `docs/privacy.html` — privacy policy; keep it truthful when data practices change.

## Conventions (follow these)

### HOL Production UI Asset Fidelity Contract — Mandatory

This repository-wide contract applies to every existing and future HOL button,
card, panel, icon, and piece of UI artwork. It applies to Main Menu, Settings,
Private Room, Daily Hunt, Splash, gameplay UI, and every future screen. No agent
or visual implementation may bypass it without the user's prior, explicit visual
approval.

1. When an approved production sprite, PNG, or SVG exists, that asset is the
   sole visual source of truth.
2. Approved artwork must not be replaced by simpler procedural graphics,
   polygons, gradients, or code-drawn approximations without prior, explicit
   visual approval from the user.
3. A production sprite must remain visible with alpha `1` in its normal state.
   Techniques such as alpha `0.002f` that hide the real asset are prohibited.
4. Assets whose names contain `_9s` must use `Image.Type.Sliced` with their
   approved sprite borders.
5. Only additive overlays for glow, pulse, highlight, pressed, or disabled
   states are permitted. They must not cover, replace, or visually degrade the
   base artwork.
6. TMP labels and localization must remain separate from the artwork.
7. Preserve every existing callback, navigation path, hit target, and
   accessibility behavior.
8. UI imports must use Sprite (2D and UI), sRGB enabled, alpha transparency
   enabled, mipmaps disabled, an appropriate Max Size, and quality-first Android
   compression for gradients and outlines.
9. Do not upscale artwork beyond the source asset's genuinely useful
   resolution.
10. Final visual quality is judged from a native-resolution Unity/device capture,
    not from the source image or a reduced Game View preview.
11. Every visual phase requires a side-by-side comparison with the approved
    reference before it can be considered complete.
12. Procedural rendering is permitted only when no approved production asset
    exists or when the user explicitly approves it.
13. Apply this contract project-wide, while keeping each implementation phase
    within its explicitly approved scope. Do not use enforcement of this
    contract as authorization for a mass refactor of unrelated screens; record
    out-of-scope violations for later controlled phases.

Focused automated coverage for production buttons must verify the expected
sprite assignment, normal-state alpha `1`, `Image.Type.Sliced` for `_9s`
assets, callback preservation, and the absence of a procedural replacement
graphic above approved artwork.

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
- **The duel rules live in two places and must stay in step.**
  `Assets/SCRIPT/DuelRules.cs` (solo) and
  `playfab/cloudscript.js` (PlayFab, server-authoritative) implement the same
  round/last-licks/Lock machine. Change one, change the other, and update both
  test suites — `Assets/Tests/EditMode/DuelRulesTests.cs` and
  `tools/test/cloudscript.test.mjs` cover the same cases on each side.
  `DuelRules` deliberately has no UnityEngine reference so it stays testable.
- **Signals carry an index, never text.** `Signals.Table` order is protocol and
  the server validates against its length: append only, never reorder or remove.
  Keeping the vocabulary closed is what keeps HOL free of user-generated
  content — do not add a free-text path without revisiting `docs/privacy.html`
  and the Play Data Safety declaration first.
- **A room outlives its match.** Rematch keeps the room and deals a new match
  in place, so anything per-match must be cleared in `resetForRematch`.
- **Every room mutation uses `withRoomMutation`.** Its revision/epoch fence is
  the protection against last-write-wins Shared Group updates. Never add a
  mutating handler that reads and writes room state outside that lock. Keep the
  client retry for the transient `room busy` response.
- **Room expiry stays discoverable.** `writeState` refreshes the sharded room
  registry, and `cleanupExpiredRooms` is scheduled by the deployment script.
  A new deletion path must unregister the room as well as deleting its group.
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
- CI cost and ordering: see `docs/ci-policy.md`. Fast `CI` checks run on
  every PR push; PlayMode follows a green `CI` run; Android preview APK
  captures require an explicit label or manual dispatch and a green `CI`
  run on the same commit.

## Backend setup state

- PvP backend is **PlayFab** in every build; there is no client-writable fallback.
- Debug/local development may use Inspector PlayFab values. A signed production
  build instead reads `ReleaseConfig`; `ReleaseBootstrap` applies the injected
  Title ID before `PvpRuntimeUI.Start` creates its backend.
- Production PlayFab account creation is closed on the client
  (`CreateAccount=false`). Fresh installs use `PlayIntegrityProvisioner` and the
  Azure service in `services/provisioner/`; the PlayFab Title Secret Key must
  remain server-only. Standard Play Integrity requires a positive Google Cloud
  project number in the production config.
- Ads: the Android LevelPlay App Key lives in `AdsManager.AppKey` (single
  source of truth, from the LevelPlay dashboard — a Unity Ads-shaped game
  id there fails init with 2110); iOS keys are placeholders. Ads are opt-in: declining keeps LevelPlay uninitialized on later
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

## Cursor Cloud specific instructions

- **Only the Node.js surface runs in the cloud VM.** Unity (EditMode/PlayMode
  tests, Android compile) needs the licensed GameCI editor image and Unity
  credential secrets, so it cannot be built or run here — GitHub Actions is the
  authority for anything under `Assets/`. Don't try to install Unity/`mono`/`mcs`;
  the `CLAUDE.md` stub-compile is a human-only local aid and its stubs are not in
  the repo.
- **The startup update script already runs `npm ci` for `services/provisioner`.**
  Provisioner deps (`@azure/functions`, `google-auth-library`) are present after
  startup; no reinstall is needed before running its tests. `tools/test/` and
  `playfab/cloudscript.js` have no dependencies (pure Node built-ins).
- The three headless CI jobs from `.github/workflows/ci.yml` are the ones you can
  reproduce locally. Run them from the repo root:
  - `static-checks`: `node --check` on the JS files plus the grep/`node` integrity
    guards (server-authoritative PlayFab, privacy.html byte-copy, empty
    `HOLReleaseConfig.json`, dependency pinning, `.meta` presence).
  - `rules-tests`: `node --check playfab/cloudscript.js` then
    `node --test tools/test/*.test.mjs` (the glob form is required; the bare
    directory does not resolve). This drives the real production CloudScript in an
    in-memory Shared Group sandbox — the fastest way to exercise PvP end to end
    without Unity or PlayFab.
  - `provisioner-test` (from `services/provisioner/`): `npm test` then
    `npm run check`. Node must be `>=22 <23`.
- The `check-license`, `test`, and `build` CI jobs will always fail here because
  the Unity secrets are absent; that is expected and not something to "fix" in the
  VM.
