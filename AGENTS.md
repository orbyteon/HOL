# AGENTS.md — HOL (Higher or Lower)

Unity 2022.3 Android game. Repo: `orbyteon/HOL` (private). Bundle id
`com.Orbyteon.HOL`. No test suite; verification is by code review + in-editor
smoke test (there is no CLI build/verify loop on this machine).

## Layout

- `Assets/Scenes/` — `SplashScene.unity` (loader) and `MainMenu.unity`
  (everything: menu, solo game, PvP UI hooks).
- `Assets/SCRIPT/` — gameplay (`GameManager`, `NumberManager`,
  `FakeMatchmaking`, `MenuManager`), ads (`AdsManager`, `ConsentManager`),
  `PvP/` (backend abstraction + Firebase/PlayFab clients + controller),
  `Localization/` (`L10n` table, `LocalizedText`, `LanguageSelector`),
  `SmartHooks/` (`GameEvents`, `DailyStreak`, `Haptics`),
  `UIJuice/` (`ButtonJuice`, `PanelAnimator`, `ConfettiBurst`, …),
  `RuntimeUI/` (`RuntimeUI` factory + runtime wiring components),
  `Design/` (Converging Light layer: `ConvergingLight` palette/textures,
  `SplashDesign`, `DesignRuntimeWiring`, `NumberDrift`).
- `playfab/cloudscript.js` — deploy to PlayFab → Automation → CloudScript.
- `docs/privacy.html` — privacy policy; keep it truthful when data
  practices change.

## Conventions (follow these)

- **Every `.cs` and folder needs a committed `.meta`.** Scripts without
  metas got random GUIDs per machine in the past; don't let it happen
  again. MonoImporter metas use the standard block, new GUID per file.
- **Scene edits are hand-edited YAML.** Use fileIDs in the `20000001xx`
  range for new documents on the `PvpRuntimeUI` root GameObject (next free:
  2000000110), register new roots in the `SceneRoots` block at file end,
  and reuse the ConsentManager block as the template.
- **Prefer runtime wiring over scene surgery** for new UI: the
  `RuntimeUI/` components (`PvpRuntimeUI`, `ExtrasRuntimeWiring`,
  `JuiceRuntimeWiring`) and `Design/DesignRuntimeWiring` build/attach UI
  one frame after `Start`, so runtime-built panels are covered too.
- **All user-facing strings go through `L10n.Get(key)`** with both EN and
  EL entries in `Assets/SCRIPT/Localization/L10n.cs`. Never add hardcoded
  English UI text. Formatted entries take args: `L10n.Get("key", arg)`.
- **UI colors follow Converging Light** (`design/philosophy.md`): indigo
  depth backgrounds, cyan `(0.25, 0.85, 1)` for secondary actions, muted
  gold `(1, 0.78, 0.34)` reserved for the primary CTA, text near-white
  `(0.91, 0.93, 1)` — never pure white or pure black. Gold/cyan buttons
  use dark indigo labels for contrast.
- **Null-guard optional scene references** (`if (x != null)`) — several
  Inspector fields are intentionally unwired and filled at runtime.
- Git: feature branches merged with `--no-ff` into `main`, pushed
  immediately. Never commit tokens, keystores (`*.keystore` is ignored),
  or `hol.bundle` / `_to_delete/`. Watch for `.git/index.lock` — another
  assistant session sometimes works this repo concurrently.

## Backend setup state

- PvP backend is **PlayFab** (`usePlayFab: 1` in scene); Firebase client
  (`PvpClient`) is the fallback and needs its RTDB URL in the Inspector.
- PlayFab requires the **Title ID** in `PlayFabPvpClient.titleId` and
  `playfab/cloudscript.js` deployed, or PvP fails gracefully at login.
- Ads: LevelPlay app key `6076495` (Android) in `AdsManager`; iOS keys
  are placeholders. Consent gates init; Settings → Ads privacy re-opens it.
  The pinned unityads-adapter 5.6.0 is catalog-verified compatible with
  LevelPlay 9.5.0 (`Assets/LevelPlay/Editor/LevelPlayVersions.json` →
  adapters → UnityAds → ironSourceSdkVersion [9.0.0, 10.0[).
- Signing: debug only. No release keystore exists yet — generate one on a
  machine with Unity/JDK, keep it out of git, back it up offline. Never
  sign with another title's key (the project once pointed at RideCore's).
