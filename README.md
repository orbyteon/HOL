# HOL

[![CI](https://github.com/orbyteon/HOL/actions/workflows/ci.yml/badge.svg)](https://github.com/orbyteon/HOL/actions/workflows/ci.yml)

A mobile **Higher or Lower number-duel game** built in Unity for Android.

You and an opponent each pick a secret number between **1 and 100**, then take turns guessing each other's number. After every guess you're told whether to go *higher* or *lower*. A round gives both sides one guess, so an opening correct guess can still be answered before the result is decided.

HOL includes solo play against a lightweight on-device AI and live PlayFab PvP room-code duels with a friend.

## Features

- Turn-based Higher/Lower guessing against an AI opponent
- **Fair duel rules** with a coin-flip opener, equal turns and a once-per-match **Lock** mechanic
- **Signals** — six fixed PvP quick messages sent by index and localized on each client
- **Server-authoritative PvP** with room-code invites through PlayFab CloudScript
- **Production cartoon UI** — approved production sprites and screen references are the visual source of truth; each screen has one presentation owner, large mobile-first CTAs and readable EN/EL typography
- **English + native Greek** localization with live language switching
- **Difficulty modes** — Easy / Normal / Hard / Adaptive
- **Persistent stats** — wins/losses, current + best streak and best winning guess-count
- Perfect-run celebration and rewarded **save your streak** flow
- Daily-play streak and haptics on win/lose
- Simulated solo matchmaking presentation and randomized opponent names
- Player name entry and persistent music settings
- Interstitial and rewarded ads via Unity LevelPlay
- **Force-update gate** using PlayFab TitleData `minVersion`
- First-launch ads consent and in-game Ads Privacy settings
- Android back-button handling across supported flows

## Gameplay

1. A splash screen leads into the main menu.
2. Choose solo play or a private-room PvP duel.
3. Enter your secret number from 1–100.
4. Take turns guessing while the live range and history help narrow the answer.
5. Each round gives both players an equal chance to answer, so moving first does not decide the match.
6. Once per match you can use the **Lock**: a correct locked guess can break a tied round, while a wrong Lock forfeits your next turn.

For a live PvP duel, create a room and share the five-character invite code, or join with a friend's code. Rematches keep the same room. Hints, turn order, Lock state and results are adjudicated server-side by `playfab/cloudscript.js`.

## Production UI architecture

The approved cartoon HOL references and production sprites are the sole visual source of truth.

- `Assets/SCRIPT/RuntimeUI/` contains theme-agnostic construction and wiring infrastructure.
- `Assets/SCRIPT/Design/` contains screen-specific production presentation owners.
- `Assets/newdesign/Resources/` contains approved art grouped by current screen/family, including `reference`, `phase2a`, `mainmenu`, `settings`, `splash` and the active PvP signal icon set.
- Production sprites stay visibly rendered at alpha `1`; `_9s` sprites use authored borders and `Image.Type.Sliced`.
- One screen has one presentation owner. Global theme passes, stacked reskins and procedural replacements for approved artwork are prohibited by `AGENTS.md` and CI integrity gates.
- Final acceptance is based on native-resolution side-by-side comparison with approved references in both English and Greek.

## Compile-time architecture

Phase 1 is replacing implicit `Assembly-CSharp` coupling with explicit Unity
assembly definitions. The first production module is `HOL.Core`, a pure C#
assembly with `noEngineReferences: true` that owns the shared duel state machine.
Legacy runtime consumers and `HOL.EditModeTests` both compile against it; migrated
core tests no longer locate production APIs through reflection. See
`docs/architecture/assembly-boundaries.md`.

## Tech stack

- **Engine:** Unity `2022.3.62f3` LTS
- **Target platform:** Android (target API 36)
- **UI:** Unity UGUI + TextMesh Pro
- **PvP:** PlayFab Legacy CloudScript, server-authoritative room state
- **Ads:** Unity LevelPlay `9.5.0`
- **Persistence:** `PlayerPrefs`
- **Language:** C# + Node.js tooling/tests

## Getting started

### Requirements

- Unity **2022.3.62f3** or matching 2022.3 LTS environment
- Android Build Support module

### Open the project

1. Clone the repository.
2. Open the project folder in Unity Hub with Unity `2022.3.62f3`.
3. Let Unity resolve packages on first import.

### Run

Open `Assets/Scenes/SplashScene.unity` and press **Play** to start from the beginning, or open `Assets/Scenes/MainMenu.unity` to jump directly to the main scene.

### Build for Android

1. **File → Build Settings → Android**, then **Switch Platform**.
2. Keep the build scenes ordered as `SplashScene`, then `MainMenu`.
3. Build an APK/AAB as appropriate. Production release builds follow `docs/release-checklist.md` and the guarded GitHub workflows.

## Project structure

```text
HOL/
├── Assets/
│   ├── Scenes/                 # SplashScene, MainMenu
│   ├── SCRIPT/
│   │   ├── Core/               # HOL.Core pure duel domain assembly
│   │   ├── Design/             # screen-specific production visual owners
│   │   ├── RuntimeUI/          # neutral runtime UI/wiring infrastructure
│   │   ├── Localization/
│   │   ├── PvP/
│   │   ├── SmartHooks/
│   │   └── UIJuice/
│   ├── newdesign/Resources/    # approved production art
│   ├── MUSIC/
│   └── Plugins/
├── Packages/
├── ProjectSettings/
├── playfab/                    # server-authoritative CloudScript
├── services/provisioner/       # first-install production provisioner
├── tools/                      # tests, deployment and release tooling
└── docs/                       # privacy, release and architecture documentation
```

The game uses two scenes. Main menu, settings, solo, private-room PvP and match panels live in `MainMenu` as controller-owned flows with dedicated presentation owners where required.

## Key scripts

| Script | Responsibility |
|---|---|
| `Core/DuelRules.cs` | Pure duel state machine in `HOL.Core`: rounds, equal turns, Lock and draws; mirrored server-side |
| `GameManager.cs` | Solo game loop, AI, turns, stats and match-end flow |
| `NumberManager.cs` | Secret-number validation and player guess routing |
| `MenuManager.cs` | Main menu/settings/play navigation and Android back handling |
| `AdsManager.cs` | LevelPlay initialization and ad lifecycle |
| `ConsentManager.cs` | Ads-consent flow |
| `ForceUpdate.cs` | PlayFab `minVersion` update gate |
| `Localization/L10n.cs` | EN/EL string table and language persistence |
| `PvP/PvpBackend.cs` | PvP transport abstraction |
| `PvP/PlayFabPvpClient.cs` | PlayFab REST/CloudScript client |
| `PvP/PvpGameController.cs` | PvP orchestration and real callbacks/state |
| `RuntimeUI/RuntimeUI.cs` | Theme-agnostic UI infrastructure |
| `RuntimeUI/PvpRuntimeUI.cs` | PvP functional runtime hierarchy and controller controls |
| `RuntimeUI/ExtrasRuntimeWiring.cs` | Functional runtime bindings and disclosures |
| `Design/MainMenuHomeVisuals.cs` | Main-menu production presentation owner |
| `Design/MainMenuPlayVisuals.cs` | Play-selection production presentation owner |
| `Design/PrivateRoomVisuals.cs` | Private-room production presentation owner |
| `Design/PrivateRoomVisualsInstaller.cs` | Sole lifecycle bridge that attaches the Private Room owner |
| `Design/SettingsVisuals.cs` | Settings production presentation owner |
| `Design/DailyHuntVisuals.cs` | Daily Hunt production presentation owner |
| `Design/SoloSearchVisuals.cs` | Solo search production presentation owner |
| `Design/SplashDesign.cs` | Splash production presentation owner |
| `UIJuice/` | Additive interaction feedback only |

## Testing and CI

The repository enforces production correctness with:

- static integrity and release-boundary checks
- Node.js duel-rule, assembly-boundary and provisioner tests
- Unity EditMode tests with direct references to migrated production assemblies
- Android compile build
- PlayMode tests after green CI on the same commit
- a dedicated production visual-integrity workflow that rejects retired visual architecture, visual graveyard folders, near-zero-alpha sprite hiding and rejected generic surfaces

See `docs/ci-policy.md` for ordering and cost controls.

## Release docs

- `docs/release-checklist.md` — ordered go-live checklist
- `docs/store-listing.md` — Play Store copy and release notes
- `docs/privacy.html` — privacy policy

## Configuration

Production configuration remains fail-closed and secret-free in git. Signed release workflows inject required public production values only in their temporary Actions workspace. Never commit Title Secret Keys, keystores, passwords or other credentials.

### PvP backend

PvP uses PlayFab exclusively. Production room mutations go through CloudScript, and clients do not directly mutate Shared Group state. Deployment and validation steps are documented in `docs/release-checklist.md`.

### Ads consent

The first-launch consent flow runs before LevelPlay initialization. The stored choice can be changed later from **Settings → Ads privacy**. Data-practice documentation must stay aligned with `docs/privacy.html`.

### Release signing

Use a dedicated HOL upload keystore and the guarded signed-release workflow. Keep the keystore backed up offline and never commit it.

## License

Proprietary — see `LICENSE` (© 2026 Orbyteon, all rights reserved). Third-party Unity packages remain under their own licenses.
