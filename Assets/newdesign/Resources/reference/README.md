# Approved exact reference assets

These assets reproduce the user-approved attached designs. They are the literal
visual source for the live HOL skin, not a new concept or a loose reinterpretation.

- `hol_logo_exact.png`: the approved smiling HOL wordmark, isolated for runtime use.
- `player_cyan_exact.png` and `opponent_purple_exact.png`: the approved versus cast.
- `mascot_7_exact.png` and `mascot_3_exact.png`: the approved number mascots.
- Runtime paths use `Resources.Load<Sprite>("reference/<asset>")`.
- `ExactReferenceAssetsTests` keeps every runtime import path release-blocking in Unity CI.

Gameplay and real data remain authoritative. Reference-only coins, ranks, or
matchmaking claims are not enabled by the visual skin.
