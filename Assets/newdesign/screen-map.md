# HOL production screen ownership map

This map documents the current one-screen/one-presentation-owner architecture.
Approved references and screen-specific production sprites remain the visual source of truth.

| Production surface | Presentation owner | Functional/state owner |
|---|---|---|
| Splash / loading | `SplashDesign` | `SplashLoader` |
| Home / mode selection | `MainMenuHomeVisuals` | `MenuManager`, runtime entry wiring |
| Solo compatibility preview / PanelPlay | `MainMenuPlayVisuals` | explicit capture/test seams only |
| Private Room landing | `PrivateRoomVisuals` | `PvpGameController`, `PvpRuntimeUI` functional roots |
| Private Room prebattle | `PvpRuntimeUI` screen-local production helpers | `PvpGameController` |
| Solo duel board | `SoloDuelVisuals` | `NumberManager`, `GameManager`, `DuelRules` |
| PvP duel / result / terminal | `PvpRuntimeUI` screen-local production helpers | `PvpGameController` |
| Settings | `SettingsVisuals` | `MenuManager`, localization/settings controllers |
| Daily Hunt | `DailyHuntVisuals` | `DailyHunt` |
| Solo search compatibility capture | `SoloSearchVisuals` | `FakeMatchmaking` capture seam |
| Consent / force update | controller-local production surfaces | `ConsentManager`, `ForceUpdate` |
| Motion / feedback | additive `UIJuice/*` only | existing Button callbacks/controllers |

## Production rules

- One screen has one presentation owner; no late global recolor/reskin passes.
- Approved sprites render visibly at alpha `1`; `_9s` assets use `Image.Type.Sliced`.
- `RuntimeUI` provides neutral construction/localization/safe-area infrastructure only.
- `HolUiStateColors` is limited to dynamic text/state colors; it never selects or recolors artwork.
- All user-facing copy is localization-keyed in `L10n.cs` and validated in EN/EL.
- Final acceptance uses native-resolution captures compared with the approved reference.
