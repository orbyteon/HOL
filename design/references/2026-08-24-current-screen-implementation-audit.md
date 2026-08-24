# HOL Existing Screen Implementation Audit — 2026-08-24

Status: **runtime audit against the three user-supplied cartoon references**

This audit answers a specific implementation question: which current HOL screens
already exist, which parts are controller-authoritative and must be preserved,
and which presentation owners must be corrected or replaced.

## Decision matrix

| Reference | Existing runtime implementation | Decision |
| --- | --- | --- |
| Opponent Search | `MainMenuPlayVisuals`, `SoloSearchVisuals`, `FakeMatchmaking` | **Do not polish the dormant fake-search fallback. Replace only when attached to a truthful lifecycle.** |
| Private Room | `PrivateRoomVisuals`, `PvpRuntimeUI.ReplacePrivateRoomPanels`, `PvpGameController` | **Keep controller/callback architecture; recompose the presentation owner to the approved reference.** |
| Solo Duel | `HolDuelBoardLayout`, `GameManager`, `NumberManager`, `DuelRules` | **Replace visual composition while preserving all gameplay state and callbacks.** |
| PvP Duel | `PvpRuntimeUI.BuildMatchPanel`, `PvpGameController`, `GuessHistoryRail`, PlayFab backend | **Replace match presentation while preserving server authority and typed event flow.** |

## 1. Opponent Search — current truth

### Existing files

- `Assets/SCRIPT/Design/MainMenuPlayVisuals.cs`
- `Assets/SCRIPT/Design/SoloSearchVisuals.cs`
- `Assets/SCRIPT/FakeMatchmaking.cs`

### What is currently live

`MainMenuPlayVisuals` owns the idle Solo entry surface. It presents the real
`ButtonChallenger`, Back control and the disclosure that Solo uses a simulated
opponent.

`FakeMatchmaking.StartSearch()` does not run matchmaking or a delayed search. It
immediately opens the local AI game panel. This is intentional product truth:
Solo is local AI play and must not pretend to discover a network opponent.

`SoloSearchVisuals` still exists as a fallback presentation for the historical
`searchingPanel`, but its current composition is also not the supplied reference:
it renders two characters, a VS burst and a rocket instead of the approved
single-player + radar composition.

### Decision

- Do not spend production time polishing the dormant fallback as if it were real
  matchmaking.
- Do not reintroduce artificial waiting or random failure into Solo.
- The supplied search reference becomes production runtime only when one of these
  truthful lifecycles exists:
  1. real public matchmaking, or
  2. an explicitly labelled, deterministic `PREPARING AI CHALLENGER` transition
     that does not claim a human/network opponent and does not add fake delay.
- Until then, correct the idle PanelPlay screen and keep immediate Solo entry.

## 2. Private Room — current truth

### Existing files

- `Assets/SCRIPT/Design/PrivateRoomVisuals.cs`
- `Assets/SCRIPT/RuntimeUI/PvpRuntimeUI.cs`
- `Assets/SCRIPT/PvP/PvpGameController.cs`
- `Assets/Tests/PlayMode/PrivateRoomVisualsPlayModeTests.cs`

### What is already correct

- one production presentation owner (`PrivateRoomVisuals`);
- real Create, Join and Back controls are reused rather than cloned;
- five-character uppercase room-code entry;
- landing code transfers into the real Join flow;
- Create/Join transitions remain owned by `PvpGameController`;
- approved production sprites are visible at alpha `1`;
- the current PlayMode test captures a 1080×1920 render and verifies callbacks.

### What must change

- remeasure and recompose logo, ribbon, Create card, Join card, Share action,
  mascots and tip to the supplied reference;
- increase title/heading visual weight without shrinking Greek copy;
- make cyan Create and magenta Join blocks read as two strong, distinct actions;
- preserve inline validation and the existing secret-number/pre-battle phases;
- add tall Android and iPhone portrait capture gates, not only 1080×1920.

### Decision

**Correct/recompose `PrivateRoomVisuals`; do not replace `PvpGameController`,
room networking or callback-bearing controls.**

## 3. Solo Duel — current truth

### Existing files

- `Assets/SCRIPT/RuntimeUI/HolDuelBoardLayout.cs`
- `Assets/SCRIPT/GameManager.cs`
- `Assets/SCRIPT/NumberManager.cs`
- `Assets/SCRIPT/DuelRules.cs`
- `Assets/Tests/PlayMode/SoloBoardPresenterPlayModeTests.cs`

### What is already correct

- typed presentation state for phases, prompts, rounds, range and histories;
- one visible submit control;
- on-screen keypad with clear/backspace semantics;
- no soft keyboard over the board;
- truthful phase gating of input, submit and answer controls;
- Back confirmation, rematch reset, history reset and round/range tests.

### What must change

The current visual composition is a functional production-sprite layout, but it
is not the supplied cartoon board. It uses simple text-based player cards,
text-only VS, a generic history card and no approved character/mascot/speech
bubble composition.

The replacement presentation must add the approved hierarchy while retaining the
same state model and controller references:

- cyan player and magenta AI cards with character art;
- production VS burst;
- strong round/instruction ribbon;
- large current-number display and keypad column;
- structured history rows and contextual tip rail;
- mascots/decorative reaction elements that never cover controls;
- all existing answer, Lock/range and result truth.

### Decision

**Replace the `HolDuelBoardLayout` presentation, but retain `GameManager`,
`NumberManager`, `DuelRules` and the tested presentation-state contract.**

## 4. PvP Duel — current truth

### Existing files

- `Assets/SCRIPT/RuntimeUI/PvpRuntimeUI.cs`
- `Assets/SCRIPT/PvP/PvpGameController.cs`
- `Assets/SCRIPT/RuntimeUI/GuessHistoryRail.cs`
- PlayFab client/server authority and duel-rule tests

### What is already correct

- server-authoritative room and guess flow;
- real player/opponent identity fields;
- current-number input and keypad;
- Lock, Signals, range, history, result, rematch and terminal state wiring;
- `GuessHistoryRail` retains typed guess identity independently from localized
  display text;
- no free-text opponent communication.

### What must change

`PvpRuntimeUI.BuildMatchPanel` currently builds a generic production-frame layout.
It does not yet match the supplied board's character-led header, VS burst,
round ribbon, large keypad hierarchy, coloured history rows, opponent speech
bubble and mascot-led tip composition.

### Decision

**Replace only the live match presentation and view bindings. Preserve
`PvpGameController`, PlayFab authority, `GuessHistoryRail` event identity,
Lock/last-licks rules, Signals vocabulary, terminal states and rematch flow.**

## Required implementation order

1. **Private Room correction** — the feature exists, the controller contract is
   mature and the supplied reference maps directly onto the current flow.
2. **Solo/PvP duel visual foundation** — build modular shared art, then apply it
   through separate screen owners without combining gameplay controllers.
3. **Opponent Search** — implement only against a truthful real-matchmaking or
   explicitly AI-preparation lifecycle; never restore deceptive fake search.

Each runtime phase remains a separate feature branch and PR. No direct edits to
`main`, no oversized all-screen migration, and no merge before native EN/EL
capture review.