# HOL

A mobile **"Higher or Lower" number-guessing duel** built in Unity for Android.

You and an opponent each pick a secret number between **1 and 100**, then take turns guessing each other's number. After every guess you're told whether to go *higher* or *lower*. The first to correctly guess the other's number wins.

Under the hood the game is single-player against a lightweight AI, but it's presented as an online match — complete with a "searching for opponent" screen and a randomly assigned opponent name.

## Features

- Turn-based Higher/Lower guessing against an AI opponent
- **Real PvP duels** with room-code invites (Firebase RTDB or PlayFab backend, both REST, no SDK)
- **Converging Light design** (`design/philosophy.md`) — indigo depth gradients, drifting number fields, a cyan→magenta seam, gold reserved for primary actions; animated splash with logo bloom and a loading hairline
- **English + native Greek** localization with live language switching; first launch follows the device language
- **Difficulty modes** — Easy / Normal / Hard / Adaptive (the AI tunes itself to your recent win rate), selectable in Settings
- **Persistent stats** — wins/losses, current + best streak, best winning guess-count (`PlayerPrefs`); solo and PvP matches both count
- **Perfect-run celebration** — win in 7 guesses or fewer and the game calls it out
- **Rewarded "save your streak"** — lose with a streak of 2+ and you can watch a rewarded ad to keep it alive (`Rewarded_Android` LevelPlay unit)
- Daily-play streak, haptics on win/lose, and a `GameEvents` hub for analytics/notifications
- Simulated online matchmaking (searching screen, occasional "opponent not found")
- Randomized opponent names for a multiplayer feel
- Player name entry and a music on/off toggle, saved between sessions
- Full sound coverage: looping music, click on every button, opponent-found stinger, win/lose stingers (solo and PvP)
- Interstitial ads via Unity LevelPlay (ironSource), shown at match end with a frequency cap
- **Force-update gate** — PlayFab TitleData `minVersion` blocks outdated builds with a store link; fails open when the backend isn't configured
- First-launch ads-consent dialog (zero setup — builds itself from code)
- Android back button handled everywhere — including mid-match exit (solo)

## Gameplay

1. A splash screen leads into the main menu (tap to skip).
2. Press **Play** — matchmaking begins (ads show at match end, not here).
3. Once an "opponent" is found, enter your secret number (1–100).
4. Take turns: when the opponent guesses, answer **Higher**, **Lower**, or **Correct** relative to your secret number. When it's your turn, guess theirs — a live range label and guess history help you play optimally.
5. First to guess the other's number correctly wins.

For a **live PvP duel**, create a room and share the 5-letter invite code, or join with a friend's code. Hints are computed automatically and honestly by the room state.

The AI narrows its range with a midpoint (binary-search) strategy, guessing randomly at a difficulty-dependent rate to feel less mechanical. Who goes first is decided by a coin flip each round.

## Tech stack

- **Engine:** Unity `2022.3.62f3` (LTS)
- **Target platform:** Android
- **UI:** Unity UGUI + TextMesh Pro
- **Ads:** Unity LevelPlay (ironSource) `9.5.0` — interstitial ads
- **Persistence:** `PlayerPrefs` (player name, music setting)
- **Language:** C#

## Getting started

### Requirements

- Unity **2022.3.62f3** (or a matching 2022.3 LTS release)
- Android build support module (for building to device)

### Open the project

1. Clone the repository:
   ```bash
   git clone https://github.com/orbyteon/HOL.git
   ```
2. Open the project folder in Unity Hub using Unity `2022.3.62f3`.
3. Let Unity resolve packages on first import (LevelPlay and the mobile dependency resolver may run additional setup).

### Run

- Open `Assets/Scenes/SplashScene.unity` and press **Play** in the editor to start from the beginning, or open `Assets/Scenes/MainMenu.unity` to jump straight to the menu.

### Build for Android

1. **File → Build Settings → Android**, then **Switch Platform**.
2. Ensure both scenes are in the build list, in order: `SplashScene`, then `MainMenu`.
3. Build (APK or App Bundle). The mobile dependency resolver handles the required Android libraries for the ads SDK.

## Project structure

```
HOL/
├── Assets/
│   ├── SCRIPT/        # Gameplay C# scripts
│   ├── Scenes/        # SplashScene, MainMenu
│   ├── Photos/        # Image assets
│   ├── MUSIC/         # Audio
│   ├── Plugins/       # Native / plugin libraries
│   ├── LevelPlay/     # Ads SDK integration
│   ├── MobileDependencyResolver/
│   └── TextMesh Pro/
├── Packages/          # Unity package manifest
└── ProjectSettings/   # Unity project configuration
```

The game uses only two scenes. All gameplay (menu, settings, matchmaking, and the match itself) lives inside `MainMenu` as toggled UI panels.

## Scripts

| Script | Responsibility |
|---|---|
| `GameManager.cs` | Core game loop: secret numbers, AI guessing (difficulty + adaptive), turns, stats, win/lose, match-end ads |
| `NumberManager.cs` | Validates the player's number, starts the round, routes player guesses |
| `AdsManager.cs` | LevelPlay init (consent-gated), interstitial lifecycle, frequency cap, init retry |
| `ConsentManager.cs` | First-launch ads-consent dialog; builds its own UI from code if unwired |
| `FakeMatchmaking.cs` | Simulated opponent search (cancellable, animated) |
| `MenuManager.cs` | Menu/settings/play panel switching; Android back-button handling |
| `MusicSettings.cs` | Music on/off toggle, persisted via `PlayerPrefs` |
| `SavePlayerName.cs` | Saves the player name to `PlayerPrefs` |
| `SplashLoader.cs` | Splash timer → loads `MainMenu`; tap to skip |
| `BlinkText.cs` | Blinking-text UI helper |
| `QuitGame.cs` | Quits the application |
| `GameStats.cs` | Persistent W/L, streaks, best guess-count, rolling win-rate window |
| `ForceUpdate.cs` | PlayFab TitleData `minVersion` gate with blocking store-link dialog (fail-open) |
| `Localization/L10n.cs` | EN/EL string table + language persistence |
| `Localization/LocalizedText.cs` | Drop-in component: TMP_Text follows the selected language |
| `Localization/LanguageSelector.cs` | Settings-language picker hooks |
| `SmartHooks/GameEvents.cs` | Static event hub (match ended, daily streak) |
| `SmartHooks/DailyStreak.cs` | Daily-play streak counter |
| `SmartHooks/Haptics.cs` | Win/lose haptic feedback |
| `PvP/PvpBackend.cs` | Abstract PvP room transport |
| `PvP/PvpClient.cs` | Firebase RTDB REST backend |
| `PvP/PlayFabPvpClient.cs` | PlayFab REST + CloudScript backend |
| `PvP/PvpGameController.cs` | PvP UI orchestration on top of `PvpBackend` |
| `RuntimeUI/RuntimeUI.cs` | Code-only UI factory (labels, buttons, inputs) |
| `RuntimeUI/PvpRuntimeUI.cs` | Builds the whole PvP interface at runtime + entry button |
| `RuntimeUI/ExtrasRuntimeWiring.cs` | Runtime wiring: rematch, search cancel, language buttons, ads-privacy, stats, disclosures, scene-label localization |
| `RuntimeUI/JuiceRuntimeWiring.cs` | Attaches UIJuice components at runtime (buttons, panels, confetti) |
| `UIJuice/` | `ButtonJuice` (press squash), `PanelAnimator` (fade+rise), `ConfettiBurst` (win celebration), `PulseText`, `AnimatedEllipsis` |
| `Design/ConvergingLight.cs` | Palette + gradient/texture canon (indigo depth, cyan/gold accents) |
| `Design/SplashDesign.cs` | Builds the animated splash (logo bloom, loading hairline) from code |
| `Design/DesignRuntimeWiring.cs` | Applies the Converging Light layer to runtime-built panels |
| `Design/NumberDrift.cs` | Drifting background number fields |

## Release docs

- `docs/release-checklist.md` — ordered go-live checklist (Unity smoke test → PlayFab → keystore → dashboards → privacy hosting → token rotation)
- `docs/store-listing.md` — paste-ready Play Store listing copy (EN + EL) and release notes
- `docs/privacy.html` — privacy policy, ready for static hosting

## Configuration

Ad settings are constants at the top of `Assets/SCRIPT/AdsManager.cs`:

- **LevelPlay Game ID:** `6076495`
- **Interstitial ad unit:** `Interstitial_Android` (an `Interstitial_iOS` unit is selected automatically on iOS builds via `#if UNITY_IOS` — create it in the LevelPlay dashboard before shipping iOS)
- **Rewarded ad unit:** `Rewarded_Android` (same `#if UNITY_IOS` pattern with `Rewarded_iOS`) — powers the save-your-streak offer; create it in the dashboard or the offer silently never appears

Replace these with your own LevelPlay credentials before publishing.

### PvP backend setup

PvP uses **PlayFab** by default (`usePlayFab` on the `PvpRuntimeUI` object in `MainMenu`). One-time setup (free):

1. developer.playfab.com → create a Studio + Title, copy the **Title ID** (4–6 hex chars).
2. Game Manager → **Automation → CloudScript (Legacy) → Revisions**: paste `playfab/cloudscript.js`, Save, **Deploy**. The client expects the current revision (atomic `joinRoom`) — an old deployed revision breaks joining.
3. Paste the Title ID into `PvpRuntimeUI.playFabTitleId` (Inspector on the `PvpRuntimeUI` object — it's copied onto the backend component created at startup).

The **Firebase RTDB** backend (`PvpClient`) is the fallback: untick `usePlayFab` and set `PvpRuntimeUI.firebaseDatabaseUrl` (setup steps in the `PvpClient.cs` header). Firebase joins are last-write-wins under a two-guest race; PlayFab joins are atomic via CloudScript.

### Ads consent

On first launch the game shows a consent dialog (`ConsentManager`) before initializing the ads SDK; the choice is stored in `PlayerPrefs` under `AdsConsent` and passed to LevelPlay via `LevelPlayPrivacySettings.SetGDPRConsent` (requires `com.unity.services.levelplay` ≥ 9.5.0, set in `Packages/manifest.json`). The choice can be changed any time in-game via **Settings → Ads privacy**. The privacy policy lives at `docs/privacy.html` — enable GitHub Pages on this repo to host it and link that URL in the Play Console.

### Release signing

The project builds with the debug key by default. Before a Play Console upload, generate a **dedicated HOL release keystore** (Player Settings → Keystore Manager → Create New), back it up offline, and never commit it (`*.keystore` is gitignored). Do not reuse a keystore from another title.

## License

Proprietary — see `LICENSE` (© 2026 Orbyteon, all rights reserved). Third-party Unity packages remain under their own licenses.
