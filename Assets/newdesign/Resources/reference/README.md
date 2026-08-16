# Approved exact reference assets

These assets reproduce the user-approved attached designs. They are the literal
visual source for the live HOL skin, not a new concept or a loose reinterpretation.

- `hol_logo_exact.png`: the approved smiling HOL wordmark, isolated for runtime use.
- `player_cyan_exact.png` and `opponent_purple_exact.png`: the approved versus cast.
- `mascot_7_exact.png` and `mascot_3_exact.png`: the approved number mascots.
- Runtime paths use `Resources.Load<Sprite>("reference/<asset>")`.
- `ExactReferenceAssetsTests` keeps every runtime import path release-blocking in Unity CI.
- `SplashScene` replaces its legacy logo and effects with the same approved
  wordmark, deep-purple field, and confetti used by the live visual layer.
- English and Greek copy share the same approved geometry. TextMesh Pro
  auto-sizing prevents longer localized labels from escaping their controls.

The exact square app icon from the approved reference set lives at
`Assets/newdesign/branding/hol_app_icon_exact.png` and is wired into every
configured application and Android icon slot in `ProjectSettings.asset`.

Gameplay and real data remain authoritative. Reference-only coins, ranks, or
matchmaking claims are not enabled by the visual skin.
