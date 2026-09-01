# AGENTS.md — HOL (Higher or Lower)

Unity 2022.3 Android game. Repo: `orbyteon/HOL` (private). Bundle id
`com.Orbyteon.HOL`. EditMode integrity tests live under `Assets/Tests/EditMode/`;
GameCI runs those tests plus an Android compile build once the required Unity
license credentials are configured. CI intentionally fails when they are
missing so a skipped compile cannot look green.

## Layout

- `Assets/Scenes/` — `SplashScene.unity` (loader) and `MainMenu.unity`
  (everything: menu, solo game, PvP UI hooks).
- `Assets/SCRIPT/` — the unmigrated runtime: gameplay (`GameManager`,
  `NumberManager`, `FakeMatchmaking`, `MenuManager`), ads (`AdsManager`,
  `ConsentManager`), ops (`ForceUpdate` — PlayFab TitleData `minVersion` gate,
  fail-open), `ReleaseConfig` + `ReleaseBootstrap` (public production runtime
  config), `PvP/` (backend abstraction + PlayFab client + controller +
  `Signals` quick-chat table), `Localization/` (`L10n` table, `LocalizedText`,
  `LanguageSelector`), `SmartHooks/` (`GameEvents`, `DailyStreak`, `Haptics`),
  `UIJuice/` (`ButtonJuice`, `PanelAnimator`, `ConfettiBurst`, …),
  `RuntimeUI/` (theme-agnostic runtime construction/wiring infrastructure),
  `Design/` (screen-specific production presentation owners only).
- `Assets/SCRIPT/Core/` — `HOL.Core`, a pure C# production assembly for duel
  rules, outcomes and value types. It has `noEngineReferences: true`; keep
  Unity, PlayerPrefs, Resources, PlayFab, ads and UI dependencies out.
- `Assets/newdesign/Resources/` — approved production art grouped by current
  screen/family (`reference`, `phase2a`, `mainmenu`, `settings`, `splash`, etc.).
  Historical generic theme surfaces are not production sources of truth.
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
  mismatch fails silently; `core-assembly-boundary.test.mjs` enforces the
  `HOL.Core` path, dependency and direct-test-reference contract;
  `lock-policy-sim.mjs` balances the Lock. No Unity needed for any of them.
- `tools/release/write-release-config.mjs` — validates/injects public production
  config into the temporary release-build workspace.
- `docs/privacy.html` — privacy policy; keep it truthful when data practices change.

## Conventions (follow these)

### HOL Production UI Asset Fidelity Contract — Mandatory

This repository-wide contract applies to every existing and future HOL button,
card, panel, icon, character, background, ribbon and piece of UI artwork. It
applies to Main Menu, Settings, Private Room, Daily Hunt, Splash, gameplay UI,
results and every future screen. No agent or implementation may bypass it
without the user's prior, explicit visual approval.

1. When an approved production sprite, PNG, SVG or reference export exists,
   that asset is the sole visual source of truth for that element.
2. Approved artwork must not be replaced by simpler procedural graphics,
   polygons, gradients, code-drawn approximations or generic theme surfaces
   without prior explicit visual approval.
3. A production sprite must remain visibly rendered with alpha `1` in its
   normal state. Techniques such as alpha `0.002f` that hide the real asset
   behind a procedural replacement are prohibited.
4. Assets whose names contain `_9s` must use `Image.Type.Sliced` with their
   approved sprite borders.
5. Only additive overlays for glow, pulse, highlight, pressed or disabled
   states are permitted. They must not cover, replace or visually degrade the
   base artwork.
6. TMP labels and localization must remain separate from artwork unless the
   user explicitly approves baked copy for a non-localized decorative asset.
7. Preserve every existing callback, navigation path, hit target and
   accessibility behavior while changing presentation.
8. UI imports must use Sprite (2D and UI), sRGB enabled, alpha transparency
   enabled, mipmaps disabled, an appropriate Max Size and quality-first Android
   compression for gradients/outlines.
9. Do not upscale artwork beyond the source asset's genuinely useful resolution.
10. Final visual quality is judged from a native-resolution Unity/device capture,
    not from the source image or a reduced Game View preview.
11. Every visual phase requires a side-by-side comparison with the approved
    reference before it can be considered complete.
12. Procedural rendering is permitted only when no approved production asset
    exists and it is genuinely infrastructure/decorative, or when the user has
    explicitly approved it.
13. No generic fallback may silently become the production look. Missing required
    artwork must log/fail a test rather than quietly changing theme.

Focused automated coverage for production buttons must verify expected sprite
assignment, normal-state alpha `1`, `Image.Type.Sliced` for `_9s` assets,
callback preservation and the absence of a procedural replacement graphic over
approved artwork.

### HOL Typography, Readability & Layout Fidelity Contract — Mandatory

The approved screen reference is the layout source of truth.

1. Before implementation, measure the approved reference and record each major
   element's visible bounds, center, size, anchor/pivot intent and spacing.
2. Author against the production portrait reference of `1080×1920`. References
   with another aspect ratio must be proportionally adapted; never stretch them
   non-uniformly.
3. Maintain the approved visual hierarchy and density. Extra viewport height is
   absorbed by controlled responsive spacing, never by arbitrary dead zones.
4. Important headings, CTA labels, room codes and gameplay values must not rely
   on unconstrained autosizing that can silently shrink them below the approved
   visual weight.
5. EN and EL must both be validated. Greek expansion is handled by deliberate
   bounds/line breaks and responsive typography, not by making every label tiny.
6. Safe-area adaptation must preserve the composition. No notch/home-indicator
   handling may distort the central layout.
7. Final acceptance requires native-resolution captures at `1080×1920` plus a
   representative tall Android and iPhone portrait viewport.
8. Major bounds/positions should stay within roughly 2–3% of the approved
   composition unless an aspect-ratio adaptation explicitly requires otherwise.

### HOL Visual Ownership & Legacy Theme Purge Contract — Mandatory

The current approved cartoon HOL reference system is the only production visual
language. Historical theme systems are retired and must not return.

1. **One screen = one presentation owner.** A screen may not be restyled by a
   chain of global passes, late overrides or competing `LateUpdate` skins.
2. Global theme writers are prohibited. Shared helpers may create/wire controls,
   but they must be theme-agnostic and may not choose backgrounds, button art,
   palettes or character styles.
3. Do not add or restore `ConvergingLight`, `DesignRuntimeWiring`, `NeonFrame`,
   `NumberDrift`, generic legacy reskin layers, or equivalent renamed systems.
4. Do not build a legacy screen and destroy/replace it in the same startup flow.
   Build only the current production presentation while preserving controller
   logic and references.
5. Historical UI assets that are unused by current production are deleted with
   their `.meta`; do not keep `Old`, `Legacy`, `Backup`, `_to_delete` or theme
   graveyard folders inside production `Assets`. Git history is the backup.
6. Obsolete tests that enforce a retired visual implementation must be replaced
   with tests that enforce current approved production sprites, one-owner
   hierarchy, readability and callback preservation.
7. Comments/docs must describe the current system. Never leave retired theme
   doctrine that could instruct a future developer/agent to recreate it.
8. No production element may intentionally hide its approved sprite at alpha
   near zero to let a custom `Graphic` draw a lookalike.
9. Runtime-only creation is allowed when technically required by dynamic flows,
   but its final presentation must still be deterministic, screen-owned and
   built from approved production assets.
10. A visual cleanup is not done until repository searches show no references to
    retired theme classes/names and Unity has no Missing Script/Missing Sprite
    residue.

- **Every `.cs` and folder needs a committed `.meta`.** Scripts without metas
  got random GUIDs per machine in the past; don't let it happen again.
  MonoImporter metas use the standard block, new GUID per file.
- **Assembly dependency direction is a compile-time contract.** Follow
  `docs/architecture/assembly-boundaries.md`. `HOL.Core` may use the .NET base
  class library only; it must not reference Unity, PlayerPrefs, Resources,
  PlayFab, ads or UI. Tests for migrated production code reference its asmdef
  directly; reflection is reserved for genuine Unity test boundaries.
- **Scene edits are hand-edited YAML.** Preserve existing fileIDs and serialized
  callbacks. When a retired MonoBehaviour is deleted, remove its serialized
  component from the scene in the same controlled phase so no Missing Script is
  left behind.
- **RuntimeUI is infrastructure, not a theme.** `RuntimeUI/` helpers may create
  RectTransforms, TMP text/inputs, localization bindings and neutral emergency
  fallbacks. Production screen owners must explicitly assign their approved
  sprites before the screen is accepted.
- **All user-facing strings go through `L10n.Get(key)`** with both EN and EL
  entries in `Assets/SCRIPT/Localization/L10n.cs`. Never add hardcoded English
  UI text. Formatted entries take args: `L10n.Get("key", arg)`. Runtime-built
  labels must follow live language switching through `LocalizedText` /
  `LocalizedLegacyText` or the `RuntimeUI.Localize*` helpers.
- **Current UI colors are derived from approved current artwork/reference.**
  Do not reintroduce a global palette contract that recolors approved sprites.
  Text colors may be authored per screen for contrast and fidelity.
- **The duel rules live in two places and must stay in step.**
  `Assets/SCRIPT/Core/DuelRules.cs` (solo) and `playfab/cloudscript.js`
  (PlayFab, server-authoritative) implement the same round/last-licks/Lock
  machine. Change one, change the other, and update both test suites —
  `Assets/Tests/EditMode/DuelRulesTests.cs` and `tools/test/cloudscript.test.mjs`.
- **Signals carry an index, never text.** `Signals.Table` order is protocol and
  the server validates against its length: append only, never reorder or remove.
  Keeping the vocabulary closed is what keeps HOL free of user-generated
  content — do not add a free-text path without revisiting `docs/privacy.html`
  and the Play Data Safety declaration first.
- **A room outlives its match.** Rematch keeps the room and deals a new match in
  place, so anything per-match must be cleared in `resetForRematch`.
- **Every room mutation uses `withRoomMutation`.** Its revision/epoch fence is
  the protection against last-write-wins Shared Group updates. Never add a
  mutating handler that reads and writes room state outside that lock. Keep the
  client retry for the transient `room busy` response.
- **Room expiry stays discoverable.** `writeState` refreshes the sharded room
  registry, and `cleanupExpiredRooms` is scheduled by the deployment script. A
  new deletion path must unregister the room as well as deleting its group.
- **Null-guard optional scene references** (`if (x != null)`) — several Inspector
  fields are intentionally unwired and filled at runtime.
- **PlayFab clients never read or write Shared Group Data directly.** All
  gameplay-impacting room operations go through `ExecuteCloudScript`, and
  `playfab/cloudscript.js` derives the player side from `currentPlayerId`. Keep
  Shared Group state `Private`; production deployment keeps Client Shared Group
  API methods denied as defense in depth.
- **Committed production config stays empty.** Do not put real production values
  into `Assets/Resources/HOLReleaseConfig.json`. The signed release workflow
  injects `PLAYFAB_TITLE_ID`, `PROVISIONING_URL`, and
  `GOOGLE_CLOUD_PROJECT_NUMBER` only in its temporary Actions workspace.
- **Production workflows are manual and fail closed.** Preserve the `main` ref
  checks, typed `BUILD`/`DEPLOY` confirmations, `production` environment and
  serialized concurrency. Never turn merge-to-main into automatic deploy.
- Git: feature branches merged with `--no-ff` into `main`, pushed immediately.
  Never commit tokens, keystores (`*.keystore` is ignored), or `hol.bundle` /
  `_to_delete/`. Watch for `.git/index.lock` — another assistant session may
  work this repo concurrently.
- CI cost and ordering: see `docs/ci-policy.md`. Fast `CI` checks run on every
  PR push; PlayMode follows a green `CI` run; Android preview APK captures
  require an explicit label or manual dispatch and a green `CI` run on the same
  commit.

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
- Ads: the Android LevelPlay App Key lives in `AdsManager.AppKey` (single source
  of truth, from the LevelPlay dashboard — a Unity Ads-shaped game id there
  fails init with 2110); iOS keys are placeholders. Ads are opt-in: declining
  keeps LevelPlay uninitialized on later launches and blocks ad loads/shows.
  Settings → Ads privacy re-opens the choice. Interstitial unit
  `Interstitial_Android` plus rewarded unit `Rewarded_Android` powers the
  save-your-streak offer. Production CMP/mediation compliance is an external
  release setting and must match `docs/privacy.html`.
- Force update: optional PlayFab TitleData key `minVersion` (e.g. `0.2.0`)
  blocks older builds with a store-link dialog. It reuses the PvP PlayFab
  session. Missing key / no authenticated PlayFab session / offline remains
  fail-open.
- Signing: use `.github/workflows/build-release.yml` for production AABs. The
  upload keystore is supplied as `ANDROID_KEYSTORE_BASE64` plus password/alias
  environment secrets; the workflow passes `-holReleaseBuild`, and
  `ReleaseBuildGuard` enables/validates custom signing only for that build.
  Never commit the keystore or its passwords; keep an offline backup.

## Reproducible validation environments

- **Node-only validation must run without an IDE.** Use Node `>=22 <23`
  for the provisioner and the repository's built-in Node test surfaces.
- **Unity verification requires a licensed Unity 2022.3 Editor.** A licensed
  local Editor or the configured GitHub Actions GameCI jobs may run EditMode,
  PlayMode and Android compile checks. Do not infer a Unity pass from Node-only
  checks or cached assemblies.
- **Provisioner dependencies are explicit.** On a fresh checkout, run `npm ci`
  from `services/provisioner/` before its tests. `tools/test/` and
  `playfab/cloudscript.js` use Node built-ins and need no package install.
- The three headless jobs from `.github/workflows/ci.yml` are reproducible from
  the repository root:
  - `static-checks`: run `node --check` on the JavaScript files plus the
    grep/Node integrity guards for server authority, privacy parity, empty
    `HOLReleaseConfig.json`, dependency pinning and `.meta` presence.
  - `rules-tests`: run `node --check playfab/cloudscript.js`, then
    `node --test tools/test/*.test.mjs`. The glob form is required.
  - `provisioner-test`: from `services/provisioner/`, run `npm test`, then
    `npm run check`.
- Unity `check-license`, EditMode, PlayMode and Android build jobs require the
  configured GitHub Actions Unity credentials. Their recorded results are the
  authoritative CI evidence for pull requests.
