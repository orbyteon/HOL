# Approved exact reference assets

These assets reproduce the user-approved attached designs. They are the literal
visual source for the live HOL skin, not a new concept or a loose reinterpretation.

- `hol_logo_exact.png`: the approved smiling HOL wordmark, isolated for runtime use.
- Runtime path: `Resources.Load<Sprite>("reference/hol_logo_exact")`.
- `ExactReferenceAssetsTests` keeps the import path release-blocking in Unity CI.

Gameplay and real data remain authoritative. Reference-only coins, ranks, or
matchmaking claims are not enabled by the visual skin.
