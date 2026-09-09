# Result Teen Polish Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the approved v3 portrait result overlay and one-shot animated victory pop over the existing server-authoritative PvP result/rematch flow.

**Architecture:** Add a focused `PvpResultPresentation` component that owns only result visuals and pop animation. `PvpGameController` remains the state authority and supplies win/loss/draw title, authoritative guess counts, revealed number, rematch state, and Signals callbacks. `PvpRuntimeUI` builds the overlay and wires existing controller handlers; no new matchmaking or gameplay actions are introduced.

**Tech Stack:** Unity 2022.3, UGUI, TextMesh Pro, Resources sprites/SVGs, existing `ConfettiBurst`, NUnit EditMode/PlayMode tests, mcs stub compile.

## Global Constraints

- Stack on `feature/prebattle-teen-polish-20260818`; do not modify CloudScript or duel rules.
- Use 1080×1920 portrait geometry and exact approved logo/boy/girl/6/7/trophy art.
- Dynamic result supports win, loss, and draw without geometry changes.
- Display only authoritative host/guest guess counts and revealed secret.
- Keep rematch’s required fresh secret, Rematch, Exit, and six index-only Signals.
- No new-opponent action, free text, fake score/trophy total, coins, Store, Profile, or number-3 mascot.
- Animated confetti is one-shot, unscaled-time, raycast-free, and only for a win.
- All copy uses EN/EL `L10n`; tests reach game types via reflection only.

---

### Task 1: Add failing result presentation contracts

- [ ] Add reflection-only assertions for result localization keys and `PvpResultPresentation`.
- [ ] Assert the upgraded `ConfettiBurst` exposes a configurable pop target/radial mode.
- [ ] Run Unity EditMode CI and observe the expected missing-contract failure.
- [ ] Commit the red contract.

### Task 2: Add result presentation and pop animation

- [ ] Add EN/EL result headings and labels.
- [ ] Add `PvpResultPresentation.cs` plus `.meta`.
- [ ] Extend `ConfettiBurst` with configurable radial + secondary burst and target overshoot while preserving existing solo defaults.
- [ ] Build the portrait result overlay in `PvpRuntimeUI`.
- [ ] Commit implementation.

### Task 3: Wire authoritative state and existing actions

- [ ] Populate title, both authoritative guess counts, and revealed number in `PvpGameController`.
- [ ] Hide overlay on live match/rematch and show it exactly once on done state.
- [ ] Wire fresh-secret input, Rematch, Exit, and six duplicate result Signal buttons to existing handlers.
- [ ] Keep live-match Signals unchanged.
- [ ] Commit controller wiring.

### Task 4: Verify and publish

- [ ] Run all-script stub compile, focused test compile, and 28 Node tests.
- [ ] Push and create a stacked draft PR.
- [ ] Require green EditMode, PlayMode, and Android compile before moving to another screen.
