# HOL Cartoon Gameplay Flow Reference Contract — 2026-08-24

Status: **User-supplied approved visual direction; implementation source of truth after the corrections below.**

This contract records the three references supplied by Marinos for:

1. PvP duel gameplay / number entry
2. Opponent search / matchmaking
3. Private Room create-and-join landing

The references define the **composition, hierarchy, color roles, character energy, panel language, glow treatment and cartoon identity**. They are not flat production backgrounds and their baked sample text/data are not runtime truth.

## Global production rules

- Rebuild every screen from modular production sprites and real Unity controls. Never ship a screenshot as the interactive UI.
- Keep all names, ratings, room codes, rounds, numbers, history rows, state messages and tips as localized TMP text or live UI state.
- Preserve the real callbacks, hit targets, controller ownership, networking flow and accessibility behavior.
- One screen has exactly one presentation owner. No global reskin pass, hidden approved sprite, procedural lookalike or competing `LateUpdate` theme writer.
- Approved sprites render at alpha `1`; `_9s` frames use `Image.Type.Sliced`.
- Author the final layout at `1080×1920`. The supplied references have different aspect ratios, so adapt by controlled reflow and spacing. Do not non-uniformly stretch them.
- Validate English and Greek at `1080×1920`, tall Android and representative iPhone portrait viewports.
- Text must remain large, bold and readable. Greek expansion is solved through deliberate bounds and line breaks, not tiny autosizing.

## Typography and copy authority

The image text is visual placement guidance only. Correct runtime copy comes from `L10n`.

Required Greek production copy includes:

- `ΜΑΡΙΝΟΣ`
- `ΑΝΔΡΕΑΣ`
- `ΕΣΥ`
- `ΑΝΤΙΠΑΛΟΣ`
- `ΓΥΡΟΣ {0}/{1}` or the actual live round format
- `ΜΑΝΤΕΨΕ ΤΟΝ ΑΡΙΘΜΟ!`
- `ΤΡΕΧΩΝ ΑΡΙΘΜΟΣ`
- `ΙΣΤΟΡΙΚΟ`
- `ΠΙΟ ΨΗΛΑ`
- `ΠΙΟ ΧΑΜΗΛΑ`
- `ΣΩΣΤΟ`
- `ΣΤΕΙΛΕ!`
- `ΒΡΕΣ ΑΝΤΙΠΑΛΟ`
- `ΑΝΑΖΗΤΗΣΗ ΑΝΤΙΠΑΛΟΥ…`
- `ΑΚΥΡΩΣΗ`
- `ΠΑΙΞΕ ΜΕ ΦΙΛΟ`
- `ΔΗΜΙΟΥΡΓΗΣΕ ΔΩΜΑΤΙΟ`
- `ΔΗΜΙΟΥΡΓΙΑ`
- `ΣΥΜΜΕΤΟΧΗ ΣΕ ΔΩΜΑΤΙΟ`
- `ΚΩΔΙΚΟΣ ΔΩΜΑΤΙΟΥ`
- `ΜΠΕΣ!`
- `ΜΟΙΡΑΣΟΥ`

Do not bake example values such as `2,450`, `2,210`, `3/10`, `68`, `27`, `42` or `MTW8H` into art.

## Reference A — PvP duel gameplay

### Canonical hierarchy

1. Safe top bar: Back, HOL logo, player currency/rating chip and avatar.
2. Two large player cards: cyan `YOU` card and magenta `OPPONENT` card.
3. Central `VS` burst and a strong round/instruction ribbon.
4. Main interaction body:
   - left: current-number display, numeric keypad and gold submit CTA;
   - right: opponent reaction/signal, history stack and contextual tip.
5. Bottom-safe spacing with no control under the system gesture area.

### Dynamic behavior

- Player names, avatars, ratings and streak/currency are live values.
- Round label is driven by the actual duel rules; no fixed `3/10` assumption.
- Current number is bound to the active turn state.
- Keypad reuses the real number-entry controller and has explicit `backspace` and `clear` semantics.
- Submit is disabled until the value is valid and legal for the current range.
- History rows are generated from real guesses and outcomes; color roles are magenta/higher, cyan/lower and green/correct.
- The opponent bubble comes from the closed Signals vocabulary or deterministic bot copy; never free-text UGC.
- Tips are context-aware. Do not ship the sample claim that the correct number is “always higher”.
- Secret numbers remain hidden. The visual redesign must not leak either player’s secret.

### Readability gates

- Player names and ratings remain readable without competing with character art.
- Current number is the strongest numeric value on the screen.
- Keypad digits have consistent touch targets and visual weight.
- History labels remain legible on a narrow side rail in both EN and EL.
- No keypad/history overlap at any production viewport.

## Reference B — Opponent search

### Canonical hierarchy

1. Back button and compact player chip.
2. Large HOL logo.
3. Purple title ribbon: `FIND OPPONENT` / `ΒΡΕΣ ΑΝΤΙΠΑΛΟ`.
4. Cyan search card containing the player character, animated radar and live search status.
5. Large cyan Cancel CTA.
6. Mascot 6 and 7 as bottom decorative anchors around the search platform.

### Dynamic behavior

- Radar sweep and three-dot state animate; they are not baked into one image.
- Status changes through localized states such as searching, opponent found and retry/error.
- Cancel invokes the real cancellation/navigation path exactly once.
- Back and Cancel cannot leave matchmaking running in the background.
- Player avatar/name/streak chip is live.

### Aspect-ratio adaptation

The supplied search reference is 2:3 rather than 9:16. The production version must use the extra vertical space for controlled breathing room between title, radar card, CTA and mascots. It must not vertically stretch the artwork.

## Reference C — Private Room

### Canonical hierarchy

1. Back/breadcrumb area and player chip.
2. Large HOL logo and purple `PLAY WITH A FRIEND` ribbon.
3. Cyan Create Room card with two characters, create icon, concise explanation and CTA.
4. Magenta Join Room card with door art, room-code input and gold Join CTA.
5. Purple Share action.
6. Bottom tip card framed by mascots 6 and 7.

### Dynamic behavior

- Reuse the real Create, Join, Back and Share buttons/callbacks.
- Room code is a real uppercase five-character input; `MTW8H` is example copy only.
- Create/Join status and validation errors remain inline and localized.
- Successful create/join transitions to the existing pre-battle flow; the visual pass must not change server-authoritative room behavior.
- The secret-number step remains in the correct existing gameplay phase and is not silently removed because the concept image omits it.
- Share uses the real invite string and current room code.
- The top-left breadcrumb does not replace a usable Back control.

### Readability gates

- Create and Join are visually distinct: cyan creation, magenta joining, gold decisive Join CTA.
- Greek headings may wrap deliberately but cannot shrink below the approved display weight.
- Input text, validation errors and room code remain readable above the keyboard and safe area.

## Required asset decomposition

Each implementation phase must use modular approved assets for at least:

- full-screen background / outer frame;
- HOL logo;
- title ribbons;
- cyan, magenta, purple and gold sliced frames;
- boy, girl and opponent characters;
- mascots 6, 7 and 3 where applicable;
- player/avatar chips;
- VS burst;
- radar and radar glow;
- door/create/share/history/tip icons;
- keypad normal/pressed/disabled states;
- history row frames;
- speech bubble;
- decorative confetti, spotlights and stage platform.

Text, numbers and user-specific information remain separate from artwork.

## Implementation phases

### Phase A — Opponent Search

- dedicated search presentation owner;
- real Cancel/Back lifecycle;
- animated radar and search states;
- EN/EL and viewport tests;
- native Android captures.

### Phase B — Private Room

- migrate current `PrivateRoomVisuals` to this approved composition;
- preserve Create/Join/Back/Share callbacks and room-code transfer;
- test create/join/pre-battle transitions;
- EN/EL and native viewport captures.

### Phase C — PvP Duel Board

- migrate duel HUD, current-number input, keypad, history, signal and tip surfaces;
- preserve `DuelRules`, PlayFab authority and all controller references;
- functional turn/range/Lock/last-licks regression tests;
- EN/EL and native viewport captures.

Do not combine all three runtime migrations into one oversized PR. Each phase follows feature branch → audit → implementation → EditMode/PlayMode/Android checks → native screenshot review → PR merge.

## Acceptance definition

A phase is complete only when:

- the rendered screen matches the approved composition at production portrait resolution;
- all approved base sprites are visible at alpha `1`;
- no screenshot/flat mockup is used as the interactive production screen;
- every real callback and gameplay/network state still works;
- EN and EL have no overflow, clipping or unreadable autosizing;
- safe-area and tall-phone captures pass;
- there are no Missing Script, Missing Sprite, dangling GUID or `.meta` regressions;
- the user approves the native side-by-side capture.
