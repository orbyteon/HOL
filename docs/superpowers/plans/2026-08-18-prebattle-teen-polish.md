# Pre-battle Teen Polish Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Apply the approved v5 portrait pre-battle/waiting visual to the existing PvP create/join waiting flow without adding a new gameplay action.

**Architecture:** Reuse `PvpCreatePanel` and `PvpJoinPanel` as the waiting/pre-battle presentation surfaces. Keep `PvpGameController` authoritative: its existing status text, room code, cancellation, and automatic transition to `matchPanel` remain the only state transitions. `PvpRuntimeUI` only creates the new visual hierarchy and preserves the existing field/listener wiring.

**Tech Stack:** Unity 2022.3, UGUI, TextMesh Pro, Resources sprites/SVGs, NUnit EditMode tests, mcs stub compile.

## Global Constraints

- This branch stacks on `feature/private-room-teen-polish-20260818` and does not modify PlayFab or duel rules.
- Use 1080×1920 portrait coordinates and safe-area clamping.
- Every user-facing string uses EN/EL `L10n`; tests access game types through reflection only.
- Use exact approved art: HOL logo, boy, girl, 6, 7, VS burst, rocket.
- No fake trophy/score, coins, names, Store/Profile, number 3, or clickable Start action.
- Preserve existing create/join/cancel/copy/room-code callbacks and automatic match transition.

---

### Task 1: Add failing pre-battle resource/localization contract tests

- [ ] Assert `reference/board_vs_burst`, `reference/board_rocket`, and pre-battle localization keys exist.
- [ ] Run the focused test and observe the expected failure for missing keys.
- [ ] Commit the red contract.

### Task 2: Add localized pre-battle copy

- [ ] Add EN/EL title, role, rule, and waiting-state entries.
- [ ] Run focused localization/resource checks.
- [ ] Commit the copy change.

### Task 3: Replace create/join waiting presentation

- [ ] Build the v5 portrait header, versus cards, rule panel, room/share row, waiting plate, mascots, and cancel control.
- [ ] Preserve all controller field assignments and listener targets.
- [ ] Keep dynamic status and room code controller-owned.
- [ ] Run script and focused test compiles.
- [ ] Commit the UI change.

### Task 4: Verify and publish

- [ ] Run all local script, Node, and focused test gates.
- [ ] Push branch and create a draft stacked PR.
- [ ] Run CI and inspect PlayMode/Android results.
