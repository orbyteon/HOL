# HOL Screen Ownership Map

This is the current production ownership map. It describes the repository as it
exists today; the deterministic-bootstrap/prefab migration is tracked separately.

| Surface | Functional owner | Presentation owner |
|---|---|---|
| Splash | `SplashLoader` | `SplashDesign` |
| Home | `MenuManager` plus existing callback-bearing buttons | `MainMenuHomeVisuals` |
| Play selection | `MenuManager` | `MainMenuPlayVisuals` |
| Solo search | `FakeMatchmaking` / menu callbacks | `SoloSearchVisuals` |
| Solo duel | `GameManager`, `NumberManager`, `DuelRules` | `HolDuelBoardLayout` and focused presentation state |
| Private Room landing | `PvpGameController` | `PrivateRoomVisuals` |
| PvP live/result | `PvpGameController` | current runtime/presentation components under `RuntimeUI/` |
| Daily Hunt | `DailyHunt` | `DailyHuntVisuals` |
| Settings | `MenuManager`, language/music/name controls | `SettingsVisuals` |

## Rules

- A surface may have only one production presentation owner.
- A presentation owner may re-seat real callback-bearing controls but must not
  create disconnected duplicates.
- New required dependencies must be explicit; do not introduce another
  frame-wait installer or scene-wide string lookup.
- When a surface migrates to a prefab, update this map in the same PR.
