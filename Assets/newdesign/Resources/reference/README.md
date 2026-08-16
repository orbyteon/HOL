# Approved exact reference assets

These assets reproduce the user-approved attached designs. They are the literal
visual source for the live HOL skin, not a new concept or a loose reinterpretation.

- `hol_logo_exact.png`: the approved smiling HOL wordmark, isolated for runtime use.
- `player_cyan_exact.png` and `opponent_purple_exact.png`: the approved versus cast.
- `mascot_7_exact.png` and `mascot_3_exact.png`: the approved number mascots.
- `board_vs_burst_exact.svg`, `board_trophy_exact.svg`, and `board_rocket_exact.svg`:
  dedicated companion artwork reconstructed from the approved six-screen board
  for the existing versus/search/result surfaces.
- `board_friend_exact.svg`, `board_lightning_exact.svg`, `board_plus_exact.svg`,
  and `board_join_exact.svg`: dedicated board icons replacing temporary text
  glyphs in the existing Home and private-room controls.
- Runtime paths use `Resources.Load<Sprite>("reference/<asset>")`.
- PlayMode coverage keeps the board companion sprite paths release-blocking in
  Unity CI and verifies that the reskin creates no new interactive controls.
- `SplashScene` replaces its legacy logo and effects with the same approved
  wordmark, deep-purple field, and confetti used by the live visual layer.
- The older Converging Light scene pass is disabled before its `Start` method,
  and the splash fallback builds only its loading line when the approved layer
  is active. Legacy drifting numbers, seam, tagline, and surface sprites cannot
  flash underneath the approved presentation.
- English and Greek copy share the same approved geometry. TextMesh Pro
  auto-sizing prevents longer localized labels from escaping their controls.

The six-screen board is applied strictly as a reskin over functions that already
exist in HOL. It does not add the reference-only Store, Profile, currencies,
divisions, missions, purchases, or any new matchmaking/gameplay action. Existing
Home, Daily Hunt, solo, search, private-room create/join, PvP, result, rematch,
settings, consent, and update behavior remains controller-authoritative.

The exact square app icon from the approved reference set lives at
`Assets/newdesign/branding/hol_app_icon_exact.png` and is wired into every
configured application and Android icon slot in `ProjectSettings.asset`.

Gameplay and real data remain authoritative. Reference-only coins, ranks, or
matchmaking claims are not enabled by the visual skin.
