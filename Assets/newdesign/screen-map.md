# HOL screen map

This map keeps the new consumer-first visual direction aligned with the existing runtime architecture.

| New design surface | Existing owner |
|---|---|
| Splash / loading | `SplashLoader`, `SplashDesign` |
| Home / mode selection | `MenuManager`, `RuntimeUI`, `ExtrasRuntimeWiring` |
| Create/join room | `PvpRuntimeUI`, `PvpGameController` |
| Matchmaking | `FakeMatchmaking` |
| Secret number / duel | `NumberManager`, `GameManager`, `DuelRules` |
| Signals / reactions | `PvP/Signals.cs`, `PvpGameController` |
| Result / rematch | `GameManager`, `PvpGameController`, `GameEvents` |
| Profile / stats | `GameStats`, `SavePlayerName`, `DailyStreak` |
| Ads consent / rewarded flow | `ConsentManager`, `AdsManager` |
| Language switching | `Localization/L10n.cs`, `LocalizedText` |
| Motion / feedback | `UIJuice/*`, `Haptics`, `DesignRuntimeWiring` |

## Rules

- Keep the central `ΠΑΙΞΕ` action visually dominant.
- Use gold only for the primary CTA.
- Use cyan/blue for secondary actions and navigation.
- Use magenta for opponent/negative states, never for ordinary destructive confirmation without clear copy.
- Keep interstitial ads post-match only.
- Keep rewarded ad dialogs explicit about the reward before the player watches.
- All copy must be localization-keyed in `L10n.cs`.
