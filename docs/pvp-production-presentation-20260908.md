# PvP production presentation candidate

## Provenance and reference measurements

Base: integrated main `96f71e2d0da7382ca29e73fba2956c77f2c0b502`.
Match/result composition: PR #66, `140a1f5badf801d34f3300963f28660b19376cc3`.
Its construction measurements, rather than its installer or retired underlying
screen, are the source below. Units are centered 1080 x 1920 reference pixels;
positive Y is upward. Sprites remain separate from localized TMP text.

| Region | Center | Size | Adaptation |
| --- | --- | --- | --- |
| Match logo | 0,835 | 310 x 155 | Proportional artwork |
| Profile chip | 350,842 | 365 x 118 | Shared persisted avatar, circular aperture |
| Player/opponent cards | -270/270,610 | 470 x 345 | Live names; opponent illustration is not claimed to be their saved avatar |
| VS burst | 0,605 | 190 x 190 | Original modular art |
| Prompt ribbon | 0,370 | 900 x 150 | Live round and turn |
| Number/keypad panel | -225,-255 | 610 x 940 | Native input and twelve real keypad callbacks |
| History/signal/range column | 330,-255 | 350 x 940 | Older history scrolls; newest event stays outside the scroller |
| Result title | 0,650 | 900 x 160 | Live localized authoritative outcome |
| Result hero | -220,285 | 520 x 540 | Decorative approved boy, not a fabricated player identity |
| Result opponent card | 330,285 | 330 x 420 | Updated from arriving room state |
| Result statistics | 0,-100 | 900 x 390 | Actual attempts, revealed number and persisted streak only |
| Result actions | 0,-480 | 850 x 270 | Native numeric keyboard retained for new secret |

The existing Private Room landing geometry remains unchanged. The shared avatar
resolver replaces its fixed cyan portrait without new persistence or mapping.
Background art uses an aspect envelope. A single safe-area root per screen
scales the authored portrait composition without nonuniform artwork stretching.
Only mascots 6 and 7 are used. Enlarged lower controls provide usable targets;
result mascots are separated from the six signal buttons.

## Ownership and authority

`PvpRuntimeUI` wires controls and the existing PlayFab client.
`PvpDuelCartoonVisuals` constructs final match/result/preparation controls directly.
`PrivateRoomVisuals` retains the existing landing composition and callbacks.
`PvpGameController`, `PvpResultPresentation`, `PvpTerminalPresentation` and
`GuessHistoryRail` retain their authoritative state responsibilities. No PR #66
installer is transferred. No screen is constructed and then destroyed/reskinned.

The server protocol currently supplies an opponent name, not an avatar ID.
The approved generic opponent drawing is decorative; no rank, wallet, reward,
profile image or other unsupported opponent data is invented.

## Evidence boundaries

The explicitly instantiated fixture transport is Editor/Windows-Development-only,
has no runtime initializer, and never contacts PlayFab. Tests supply snapshots
and assert the real controller callbacks. Fixture captures are not proof of a
two-device account, provisioning, room or connectivity test.

Unity/CI results, native capture paths and APK hashes are recorded at delivery;
this document does not predeclare those gates successful.

## Unresolved visual-source discrepancy

The supplied approved Private Room screenshot contains a detailed illustrated
pink door. The current owner and PR #66 instead reference
`reference/board_join_exact.svg`, a white outline door/arrow. The bounded asset
inventory did not locate the illustrated modular door. The other private-room
icons are chat bubbles, not that illustration. The screenshot is not a modular
production asset: it must not be flattened, cropped into controls or recreated
as a substitute. Exact pink-door fidelity remains blocked pending the original
asset or its Git path; existing landing art is retained, not claimed equivalent.
PR #60 was also checked: its five-file change added no illustrated door asset.

## Local validation record (2026-09-08)

Unity 2022.3.62f3, licensed interactive canonical Editor, not batchmode:

- New focused production presentation regressions: 6 passed, 0 failed/skipped.
  Includes EN/EL glyph bounds at 720x1280, 1080x1920, 1080x2400 and 1179x2556,
  touch-rematch configuration, real keypad/submit/Lock guards, arriving identity,
  localized results, repeated room entry and disconnected-room recovery.
- Existing Home/Private Room route plus terminal regressions: 7 passed,
  0 failed/skipped on the final controller. The earlier Simulator-only Home
  attempt failed its genuine end-of-frame barrier; the unchanged test passed
  with a visible Game View. The failed XML is retained, not replaced.
- Existing focused PvP EditMode evidence: 11 passed, 0 failed/skipped.
- Node repository regression: 129 passed, 0 failed/skipped. JavaScript syntax
  checks passed. Local provisioner execution is not green: npm is unavailable,
  and direct Node 24 execution cannot resolve google-auth-library. Required
  CI uses its declared Node 22 environment and dependency installation.
- Native fixture matrix: 48 original PNGs, 32 across the complete EN/EL
  1080x1920 flow plus 16 representative tall Android/iPhone frames. The capture
  gate verifies runtime and PNG dimensions. A retained failed attempt detected
  a prior test's 1440-wide runtime size; capture now sets the runtime resolution
  as well as the Game View index, without changing any production setting.

Native review caught a real repeated-entry defect: connection loss hid Exit,
but a later normal result did not restore it. ShowRematchOffer now restores Exit
for a legitimate completed match. Regression coverage enters another room,
finishes it, and invokes the real Exit callback. Server authority is unchanged.

The baseline TMP underline warning family remains documented and unsuppressed;
there are no C# compiler errors or warnings. Unity-generated Daily Hunt SDF and
ProjectSettings changes are excluded and preserved outside the candidate.

This is a draft technical candidate, not human visual or two-device acceptance.
Android keyboard interaction, real authenticated create/join, signals, recovery
and repeated rematches require two real clients. No rewards, avatars or balances
are fabricated; fixture statistics are temporary and restored by test teardown.
No APK is claimed until the exact candidate passes CI and its one authorized
development preview workflow completes.
