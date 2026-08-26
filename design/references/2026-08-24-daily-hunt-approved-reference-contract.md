# Daily Hunt Approved Visual Reference Contract — 2026-08-24

Status: **User-approved gold visual reference.**

Canonical reference:

- `design/references/2026-08-24-daily-hunt-approved.png`
- source dimensions: `941×1672`
- source aspect ratio: `0.562799`, effectively the production `9:16` target
- Git blob hash at handoff: `658c480bed6eba10f44da28c931f115173e08cc4`

The PNG is the sole geometry, hierarchy, density, color-role and decorative
composition source for the Daily Hunt screen. It is a design reference, not a
flat runtime background.

## Verified relationship to the HOL cartoon system

The repository does **not** currently contain a single approved six-screen
composite image. No global geometry authority is inferred from prose or from
an unavailable conversation attachment. The reusable, locally verifiable
system is limited to these production resources and conventions:

| Shared role | Verified production source |
| --- | --- |
| neon background | `phase2a/hol_neon_reference_bg_r3` |
| HOL logo | `reference/hol_logo_exact` |
| player avatar and mascots | `reference/player_cyan_exact`, `reference/mascot_6_exact`, `reference/mascot_7_exact` |
| cyan, magenta and gold surfaces | `phase2a/hol_cta_*_r2_9s` |
| dark neon panel | `phase2a/hol_tip_frame_r2_9s` |
| display/body hierarchy | `phase2a/fonts/HOL Menu Display SDF`, `phase2a/fonts/HOL Menu Body SDF` |

Reuse means shared art, typography and responsive rules. It does not create a
global runtime visual writer: `DailyHuntVisuals` remains the sole presentation
owner for this screen. PR #70 must not refactor #65, #66 or another screen's
owner.

The repo has no dedicated transparent outer-border sprite, Daily Hunt title
ribbon, or reward-chest sprite matching the approved reference. Those are
explicit production-art gaps; code must not procedurally paint substitutes or
claim exact parity for them.

## Truthful runtime mapping

The current HOL Daily Hunt remains the real UTC-date-seeded number challenge:
one shared secret number, seven guesses, persisted progress, one optional
rewarded revive, result sharing and a completion streak.

The reference-only mission rows, reset clock, trophy payout and currency are
not product behavior and must not be invented. Their visual hierarchy maps to
the real flow as follows:

| Approved visual role | Live HOL control/state |
| --- | --- |
| Daily challenge header | localized Daily Hunt day number |
| Cyan mission board | target art, live status, trail and number input |
| Magenta reward board | real daily streak/progress state |
| Bottom gold CTA | whichever real action is active: Guess, Revive or Share |
| Top player chip | live player name and wins |
| Bottom mascots | approved decorative sprites; never interaction owners |

All text and values remain localized TMP/live state. The approved PNG must
never be shipped as the interactive screen.

## Measured 1080×1920 composition anchors

These are pre-approval implementation measurements, not automated-test
baselines. Exact geometry assertions must not be updated until the user
approves the six local screenshots.

| Element | Center | Size |
| --- | ---: | ---: |
| Back control | `(-455, 842)` | `(118, 108)` |
| Player chip | `(338, 842)` | `(390, 146)` |
| HOL logo | `(-10, 710)` | `(410, 220)` |
| Title ribbon | `(0, 560)` | `(960, 160)` |
| Cyan challenge card | `(0, 125)` | `(980, 650)` |
| Magenta streak card | `(0, -465)` | `(940, 310)` |
| Active bottom CTA | `(0, -795)` | `(580, 154)` |
| Mascot 6 | `(-415, -800)` | `(265, 300)` |
| Mascot 7 | `(415, -800)` | `(265, 300)` |

At tall aspects the top shell moves upward, the challenge board gains a small
upward offset, and the reward/CTA/mascot group moves downward. This is
deliberate reflow inside the same owner, not uniform scale-and-center.

## Approval gate

Produce EN and EL captures at `1080×1920`, `1080×2400` and `1179×2556`.
Stop for human visual approval before changing exact geometry assertions,
pushing the final visual commit, triggering GitHub Actions or requesting native
Android acceptance.
