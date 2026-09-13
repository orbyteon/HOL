# PvP production presentation candidate

## Provenance and reference measurements

Base: integrated main `96f71e2d0da7382ca29e73fba2956c77f2c0b502`.
Current match authority: the approved canonical `SoloDuelVisuals` production
owner and its exact committed sprites, not the old PvP LOCK screenshot or
PR #66. Solo production code is unchanged. Units below are centered 1080x1920
reference coordinates; positive Y is upward. Sprites and live TMP stay separate.
The earlier PR #66-derived result composition remains adapted to the same
production materials; no historical installer or competing owner is restored.

| Region | Center | Size | Adaptation |
| --- | --- | --- | --- |
| Match logo | -46,835 | 390 x 229 | Exact Solo geometry/art |
| Profile chip | 339,860 | 370 x 150 | Shared persisted avatar, circular aperture, actual wins |
| Player/opponent cards | -276,472 / 282,470 | 514 x 620 | Exact Solo shells and decorative characters; real player names |
| VS burst | 1,508 | 338 x 290 | Approved Solo v2 artwork |
| Prompt ribbon | -18,86 | 636 x 181 | Live round, actor/guess, explicit target/result, next turn |
| Number/keypad panel | -189,-488 | 760 x 1004 | Solo v2 board, input and twelve real keypad callbacks |
| Input / keypad | -13,285 / -1,-80 board-local | 520 x 140 / 577 x 440 | Solo number/key art and 76px display type |
| Submit / LOCK | -150,-385 / 150,-385 board-local | 276 x 94 | Real guarded callbacks; glyphs centered in usable artwork faces |
| Right rail | 339,-488 | 362 x 901 | Same Solo placement; PvP signals replace AI speech |
| History | 0,0 rail-local | 374 x 400 | 20px shorter than Solo to separate the extra Signals control; retained events scroll chronologically |
| Tip | 0,-330 rail-local | 390 x 260 | Full existing LOCK guidance; useful range has one owner above input |
| Result title | 0,650 | 900 x 160 | Live localized authoritative outcome |
| Result hero | -220,285 | 520 x 540 | Decorative approved boy, not a fabricated player identity |
| Result opponent card | 330,285 | 330 x 420 | Updated from arriving room state |
| Result statistics | 0,-100 | 900 x 390 | Actual attempts, revealed number and persisted streak only |
| Result actions | 0,-480 | 850 x 270 | Native numeric keyboard retained for new secret |

The Private Room landing now uses the recovered illustrated panels described
below. The shared avatar resolver retains the saved portrait without new
persistence or mapping.
Pre-match background uses an aspect envelope; the match uses Solo's exact
full-bleed background treatment. One safe-area owner per screen protects the
composition. Match uses Solo's approved mascots 7 and 3; Private Room retains
6 and 7. The six unchanged signal callbacks live in a clearly labeled drawer,
not over the history or keypad. Result mascots remain separated from controls.

Tall match geometry follows Solo's same interpolation: cards 526x606 at .90
scale, input board 730x1220, keypad 577x600, 186x132 keys, 276x120 actions.
The right history is 340x570 (20px shorter than Solo's tall history) for Signals.
No difficulty, AI timing or AI strategy is imported into PvP.

## Ownership and authority

`PvpRuntimeUI` wires controls and the existing PlayFab client.
`PvpDuelCartoonVisuals` constructs final match/result controls directly.
`PrivateRoomVisuals` owns the illustrated landing composition and retains its
existing callbacks.
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

## Illustrated landing panels recovered from Git history

The earlier missing-door audit was incomplete: it inspected the current assets,
PR #60 and the outline SVG used by PR #66, but missed the illustrated card PNGs
on the historical cartoon UI branch. The user explicitly selected those panels
on 2026-09-08. They are now the landing-card visual authority, not the former
two narrow Solo shells or white door/arrow substitute.

Source commit: `6c6e37bd1a4e15ca80db7a689e9cc548cf305a8a`.
Only `hol_private_create_card_v1.png` and `hol_private_join_card_v1.png`, plus
their original metadata, are reused from `cartoonui/v1/private`. Current assets
live in `Assets/newdesign/Resources/reference`; no old installer, theme or
architecture is restored. Original PNG bytes and GUIDs remain unchanged.
Only five trailing spaces on empty YAML scalars in each recovered `.meta` were
removed to satisfy staged whitespace validation; every import value is identical.

The blue 1616x745 panel already contains the high-five boy/girl. The magenta
1635x778 panel contains the illustrated pink door. Both have real alpha and
empty right-hand content regions; no screenshot crop, generated replacement,
localized text, room code or gameplay value is baked into them.

At the 1080x1920 design viewport, the upper card is centered at (0,173),
960x442.5743; the lower at (0,-297), 960x456.8073. These preserve native aspect,
60px side clearance, the existing logo/ribbon/profile and one presentation
owner. Live headings, descriptions, input and actions occupy measured right-hand
safe regions. The existing cyan 9-sliced Create face and gold Join face retain
real callbacks. Only the landing tip/mascots move down to clear the stacked cards;
The subsequent bounded correction reduces the waiting player status panels
to 340x252 so they remain inside their owning board. Networking is unchanged.

## Illustrated-panel focused validation (2026-09-08)

Unity 2022.3.62f3 interactive Editor; no batchmode or new APK:

- Asset contracts: 9 passed, 0 failed/skipped, including exact PNG SHA-256,
  native dimensions, original GUID and import requirements for both panels.
- Pre-match regressions: 7 passed, 0 failed/skipped after increasing only the
  Greek landing tip's available height. The earlier failed XML remains intact;
  no font reduction, assertion tolerance change or warning suppression was used.
- Presentation regressions: 6 passed, 0 failed/skipped, including actual
  illustrated-resource ownership and rendered EN/EL text containment.

These are local focused results for the uncommitted illustrated-panel revision,
not remote CI or human approval. Earlier APK/CI evidence below does not contain
the recovered panels. Real two-client gameplay acceptance remains pending.

## Earlier candidate validation record (2026-09-08)

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

## Complete Solo-aligned match validation (2026-09-09)

These final local results supersede the earlier presentation counts above;
they do not predeclare the new remote CI, APK or human acceptance:

| Focused gate | Passed | Failed / skipped |
| --- | ---: | ---: |
| Illustrated asset contracts | 9 | 0 / 0 |
| Final match presentation, EN/EL four-viewport glyphs and six signals | 8 | 0 / 0 |
| Private Room / pre-match | 7 | 0 / 0 |
| Home route, controller and terminal regressions | 7 | 0 / 0 |
| Responsive integration across existing safe-area cases | 1 | 0 / 0 |
| Solo reference capture / PvP flow capture / pre-match capture | 1 / 1 / 1 | 0 / 0 |
| Repository Node regression | 129 | 0 / 0 |
| Affected Node presentation contracts after final signal inset | 6 | 0 / 0 |

Licensed interactive Unity 2022.3.62f3 completed import/compilation with zero
C# errors or warnings. Known TMP underline and diagnostic touch-target warning
families remain unsuppressed. No broad Unity suite was manually rerun.

Final external review package: `REVIEW-20260909-final`. It records SHA-256 for
56 untouched original PNGs: four real Solo reference frames and 52 PvP fixture
frames. Four native-pixel side-by-side sheets compare EN/EL at 1080x1920 and
1080x2400. Sixteen shared screen-space bounds (safe root, logo, player card,
input board) match within 0.01 px. PvP additionally covers representative
1179x2556 states. A separate settled 22-frame pre-match run records the stacked
illustrated landing, secret/loading/error and waiting screens.

Accepted local XML identifiers: assets `203251_754`, match `205418_721`,
pre-match `203600_562`, routes `204120_794`, responsive `210119_889`, Solo
reference `204838_065`, final PvP captures `210252_106`, pre-match captures
`205051_845` (all date prefix `20260908`). Raw XML and complete Editor logs,
including earlier failed attempts, remain preserved externally.

Native review corrected full Greek active-glyph containment and inset the
six signal buttons inside their visible panel rim. Approved typography,
assertion tolerances, callback indices, server rules and Solo production remain
unchanged. The exact Solo PNG import blobs are preserved, including four
historical mipmap-enabled imports; their explicit hash contracts prevent an
unrequested reimport from changing the approved source.

Two capture-only environmental issues were diagnosed rather than hidden:
Device Simulator supplied safe-area dimensions outside the requested PNG,
and resolution changes settled across multiple CanvasScaler frames. Final
captures use the native Game View and require three consecutive stable viewport
and safe-root observations. No production pacing or capture fixture state was
changed. Invalid earlier visual evidence is retained but not used for approval.

The generic responsive integration test now applies its requested viewport to
the sole owner and measures all nontransparent pixels of the exact Solo
interaction PNG, rather than counting its intentional transparent overscan as
visible panel overflow. Existing 0.05 px containment tolerance and all other
geometry/lifecycle assertions remain intact.

Unity closed normally after validation. The complete 39-value PlayerPrefs
snapshot matches the pre-test snapshot with zero differences. The three
pre-existing Daily Hunt SDF / ProjectSettings modifications and two generated
test-scene residue files are preserved and excluded from the commit. Native
fixture images demonstrate presentation, not authenticated two-client gameplay.
