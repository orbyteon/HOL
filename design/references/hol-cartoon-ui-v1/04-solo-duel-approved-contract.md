# HOL Solo Duel approved-reference contract

## Canonical source

- File: `04-solo-duel-approved.png`
- Source dimensions: `941 x 1672`
- SHA-256: `C9F73B5664FB2E9E406E2D20C2763612659E9029AAA6CE32DB61B5F9BF7B2F26`
- The PNG is the sole screen-level visual reference for the Solo Duel gameplay
  board. Existing runtime captures, stale exact-coordinate tests, old helper
  passes and resource names containing `exact` are not visual authorities.

The source aspect ratio differs from `1080 x 1920` by less than `0.05%`.
Adapt it with one uniform scale (`1080 / 941`) and sub-pixel vertical centering.
Do not stretch either axis independently.

## Measured canonical composition

Bounds use top-left `x, y, width, height`. Target values are the normalized
`1080 x 1920` composition. They are measurement anchors, not permission to bake
copy or gameplay values into sprites.

| Element | Source bounds | 1080 x 1920 bounds | Unity centre |
| --- | ---: | ---: | ---: |
| Reference edge treatment (not a production overlay) | `9, 10, 923, 1652` | `10, 12, 1059, 1896` | `0, 0` |
| Back button | `18, 30, 115, 112` | `21, 35, 132, 129` | `-453, 860` |
| HOL logo | `264, 30, 333, 170` | `303, 35, 382, 195` | `-46, 828` |
| Player identity chip | `606, 29, 319, 116` | `696, 34, 366, 133` | `339, 860` |
| Player card | `25, 208, 385, 459` | `29, 239, 442, 527` | `-290, 458` |
| Opponent card | `530, 209, 385, 459` | `608, 240, 442, 527` | `289, 456` |
| VS burst | `357, 286, 228, 214` | `410, 329, 262, 246` | `1, 508` |
| Round/prompt ribbon | `209, 682, 522, 158` | `240, 783, 599, 181` | `0, 86` |
| Numeric interaction board | `20, 869, 572, 785` | `23, 998, 656, 901` | `-189, -488` |
| Opponent speech card | `608, 871, 315, 190` | `698, 1000, 362, 218` | `339, -149` |
| History board | `608, 1060, 315, 371` | `698, 1217, 362, 426` | `339, -470` |
| Tip/context board | `608, 1437, 315, 217` | `698, 1650, 362, 249` | `339, -814` |
| Current-number heading | `60, 885, 475, 52` | `69, 1016, 545, 60` | `-198, -86` |
| Real numeric input | `58, 942, 490, 160` | `67, 1082, 562, 184` | `-192, -214` |
| Numeric keypad | `53, 1118, 503, 416` | `61, 1284, 577, 477` | `-190, -562` |
| Primary submit action | `54, 1554, 501, 82` | `62, 1784, 575, 94` | `-190, -871` |

Major visible bounds should remain within the repository contract's `2-3%`
tolerance. Internal text and control padding must be measured independently.

## Locked visual language

- Premium 2.5D cartoon competitive mobile-game UI.
- Deep navy/purple arcade background with controlled cyan/magenta/gold light.
- Chunky rounded panels, strong dark outlines, glossy bevels and clear depth.
- Cyan identifies the player and higher/range-positive states.
- Magenta identifies the opponent and lower/range-negative states.
- Gold identifies VS, correct/success and the primary submit action.
- Purple owns the prompt ribbon, neutral containers and secondary surfaces.
- TMP text remains live, localized and separate from artwork.

## Truthful runtime mapping

The reference controls composition and visual hierarchy. Production gameplay
controls content and visibility.

- Player chip: canonical persisted Onboarding avatar plus `GameStats.Wins`; the
  approved cyan Solo portrait is the safe fallback for missing/invalid profile
  data. No fake currency.
- Player card: live player name and real win count.
- Opponent card: live simulated-opponent name plus real AI difficulty label;
  do not invent an opponent score.
- Ribbon: live round and phase prompt. Do not bake or promise a fixed `3/10`.
- Current number: the real secret-number display owned by `NumberManager`.
- Input/keypad/primary CTA: the real `TMP_InputField`, numeric keypad and submit
  callback. No duplicate functional controls.
- Opponent speech: live AI guess/answer feedback from `GameManager`.
- History board: real accepted player guesses with their authoritative
  Higher/Lower/Correct hints, matching the approved three-row composition. AI
  guesses remain visible in the live speech/state beat and are retained in the
  typed presentation/outcome history; no fake extra rows are invented.
- Tip/context: live valid range and state guidance.
- AI feedback: `DuelRules` computes the authoritative Higher/Lower/Correct
  result from the player's secret. Production presents that truthful feedback
  beat and resolves it automatically; the retired manual answer controls stay
  hidden so they cannot contradict the known secret or deadlock the match.
- Lock: real callback-bearing optional control, visible only when the rules
  expose it. It must occupy a deliberate secondary action slot.
- Match result/rematch and optional rewarded Save Streak remain truthful live
  controls. They must never be hidden behind the production root.

## Architecture lock

- One screen, one presentation owner under `Assets/SCRIPT/Design/`.
- Runtime controllers (`GameManager`, `NumberManager`, `DuelRules`) remain
  gameplay-authoritative.
- No `Hardener`, `Guard`, `Fix`, `Pass`, `Overlay` or competing `LateUpdate`
  writer may mutate Solo Duel presentation.
- Reparent callback-bearing scene controls while preserving active state.
- Retired legacy guess panels must not render above the production owner.
- Missing approved artwork fails explicitly; generic fallback art must not
  silently become the production look.

## Current asset classification

Existing approved/reusable production assets:

- HOL logo, player/opponent characters, VS burst, mascots.
- Current shared navy arcade background and decorative particles.
- Current approved sliced cyan, magenta, purple and gold button surfaces.
- Current production display/body TMP font chain.
- `solo/production/solo_opponent_speech_bubble_v2`: transparent, text-free
  rasterized Solo speech surface derived from this canonical reference.
- `solo/production/solo_player_card_shell_v1`: text-free cyan card shell.
- `solo/production/solo_opponent_card_shell_v1`: text-free magenta card shell.
- `solo/production/solo_prompt_ribbon_v1`: text-free purple prompt ribbon.
- `solo/production/solo_interaction_board_v2`: text-free numeric board shell.
- `solo/production/solo_player_chip_v1`: text-free right-avatar identity chip.
- `solo/production/solo_tip_board_v1`: text-free contextual tip surface.
- `solo/production/solo_history_board_v1` plus the approved live-state row
  surfaces/icons: dedicated text-free history presentation.
- `solo/production/solo_vs_burst_v2`: final approved VS treatment.

The superseded v1 speech, interaction, outer-frame and VS pairs are not part of
the final production owner and must not be reintroduced.

Every new Solo raster above was generated from this exact canonical PNG, kept
text-free, and post-processed only to replace the generator preview checkerboard
with real alpha. The source silhouettes were not redrawn by gameplay code.

`Assets/newdesign/Resources/cartoon/cartoon_speech_bubble.svg` is a generated
approximation and has been removed. It must not be restored merely because the
old branch referenced it.

## Visual gates

1. Fresh real-runtime EN/EL captures at `720 x 1280`, `1080 x 1920`,
   `1080 x 2400` and `1179 x 2556`.
2. Preparation, active turn, number entry, truthful history, round progression,
   result transition, all four difficulties and win/loss/draw/Lock evidence.
3. Automated safe-area, rendered TMP glyph, callback and state-containment audit.
4. Full regression and Android build smoke.
5. Human gameplay, visual and responsive approval before any push or PR.
