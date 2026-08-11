# HOL

A mobile **"Higher or Lower" number-guessing duel** built in Unity for Android.

You and an opponent each pick a secret number between **1 and 100**, then take turns guessing each other's number. After every guess you're told whether to go *higher* or *lower*. The first to correctly guess the other's number wins.

Under the hood the game is single-player against a lightweight AI, but it's presented as an online match — complete with a "searching for opponent" screen and a randomly assigned opponent name.

## Features

- Turn-based Higher/Lower guessing against an AI opponent
- Simulated online matchmaking (searching screen, occasional "opponent not found")
- Randomized opponent names for a multiplayer feel
- Player name entry and a music on/off toggle, saved between sessions
- Interstitial ads via Unity LevelPlay (ironSource)
- Win/lose sound feedback

## Gameplay

1. A splash screen leads into the main menu.
2. Press **Play** — an interstitial ad may show, then matchmaking begins.
3. Once an "opponent" is found, enter your secret number (1–100).
4. Take turns: when the opponent guesses, answer **Higher**, **Lower**, or **Correct** relative to your secret number. When it's your turn, guess theirs.
5. First to guess the other's number correctly wins.

The AI narrows its range with a midpoint (binary-search) strategy, guessing randomly about 20% of the time to feel less mechanical. Who goes first is decided by a coin flip each round.

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
| `GameManager.cs` | Core game loop: secret numbers, AI guessing logic, turns, win/lose handling |
| `NumberManager.cs` | Validates the player's number, starts the round, routes player guesses |
| `AdsManager.cs` | LevelPlay initialization and interstitial ads |
| `FakeMatchmaking.cs` | Simulated opponent search |
| `MenuManager.cs` | Menu / settings / play panel switching; triggers the ad on Play |
| `MusicSettings.cs` | Music on/off toggle, persisted via `PlayerPrefs` |
| `SavePlayerName.cs` | Saves the player name to `PlayerPrefs` |
| `SplashLoader.cs` | Splash timer → loads `MainMenu` |
| `BlinkText.cs` | Blinking-text UI helper |
| `QuitGame.cs` | Quits the application |

## Configuration

Ad settings are constants at the top of `Assets/SCRIPT/AdsManager.cs`:

- **LevelPlay Game ID:** `6076495`
- **Interstitial ad unit:** `Interstitial_Android` (an `Interstitial_iOS` unit is selected automatically on iOS builds via `#if UNITY_IOS` — create it in the LevelPlay dashboard before shipping iOS)

Replace these with your own LevelPlay credentials before publishing.

### Ads consent

On first launch the game shows a consent dialog (`ConsentManager`) before initializing the ads SDK; the choice is stored in `PlayerPrefs` under `AdsConsent` and passed to LevelPlay via `LevelPlayPrivacySettings.SetGDPRConsent` (requires `com.unity.services.levelplay` ≥ 9.5.0, set in `Packages/manifest.json`). The privacy policy lives at `docs/privacy.html` — enable GitHub Pages on this repo to host it and link that URL in the Play Console.

## License

No license file is currently included in this repository. Add one to clarify how others may use the code.
