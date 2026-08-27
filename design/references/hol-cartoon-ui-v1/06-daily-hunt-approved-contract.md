# Daily Hunt approved production contract

Status: locked for PR #70

Canonical visual reference:
`design/references/hol-cartoon-ui-v1/06-daily-hunt-approved.png`

## Scope

PR #70 owns Daily Hunt only. It must not redesign or migrate Home, Private
Room, Opponent Search, Duel, Results, or their production assets/tests.

`DailyHuntVisuals` is the sole Daily Hunt presentation and layout owner. It
constructs the live hierarchy once. `DailyHunt` owns gameplay state and binds
callbacks to the controls returned by that presentation owner. No legacy
screen, late fidelity pass, overlay, or second responsive writer is allowed.

## Approved real functionality

The entry dashboard is real product functionality, not illustrative copy:

- win one game;
- make three correct guesses;
- share one room;
- reset on the shared UTC Daily Challenge day;
- award 500 persisted Daily Challenge points exactly once;
- show live progress toward the 1,500-point player milestone.

START opens the existing deterministic Daily number hunt. That hunt keeps:

- one UTC date-seeded number in the range 1-100;
- seven base guesses;
- two bonus guesses only after a successful rewarded Revive;
- persisted guesses, range, completion, revive, share, and streak state;
- live localized status/range/input/action text;
- mutually exclusive Guess, Revive, and Share actions.

No screenshot-only currency, timer promise, networking state, room code,
player identity, or reward may be fabricated. Every displayed value comes from
the real state above or from an explicit deterministic capture fixture compiled
only for development builds.

## Visual and localization boundary

- The PNG is the layout and art-direction source of truth, not a runtime UI
  texture.
- Approved production sprites provide artwork; all dynamic copy and values are
  live TMP/UI controls.
- English and Greek use static Roboto Condensed production TMP atlases with
  complete required glyph coverage. Runtime fallback submeshes are a failure.
- Seven base attempt slots keep their normal breathing room. Slots eight and
  nine appear only after Revive and use deliberate adaptive reflow.
- Geometry assertions remain provisional until the required runtime captures
  receive human approval.

## Acceptance order

1. Local compile and Daily behavioral tests.
2. Fresh real-runtime 1080x1920 EN and EL captures.
3. Fresh 1080x2400 and 1179x2556 EN/EL captures.
4. Human visual approval.
5. Only then geometry rebaseline, full regression, commit/push, GitHub CI, and
   native acceptance.
