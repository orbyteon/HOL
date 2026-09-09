# Private Room Teen Polish Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reskin the existing PvP menu, create-room, and join-room runtime panels into the approved 1080×1920 Teen Polish portrait design without changing PlayFab behavior or controller callbacks.

**Architecture:** Keep `PvpRuntimeUI` as the sole runtime presentation owner and keep `PvpGameController` as the interaction authority. Add a focused portrait private-room visual builder inside `PvpRuntimeUI`, using the existing panel/button/input objects and wiring them to their current handlers. Add only approved reference art and localized copy needed by the existing create/join flows.

**Tech Stack:** Unity 2022.3, UGUI, TextMesh Pro, Resources sprites/SVGs, NUnit EditMode tests, mcs stub compile.

## Global Constraints

- Use a new `feature/private-room-teen-polish-20260818` branch from `main`.
- Keep PlayFab server authority, room-code normalization, secrets, callbacks, and controller state transitions unchanged.
- Use runtime wiring; do not edit `MainMenu.unity`.
- Every user-facing string must use `L10n` with EN and EL entries.
- Use 1080×1920 portrait coordinates and safe-area clamping.
- The visual cast is 6, boy, girl, 7; do not add fake score, coins, Store, Profile, or matchmaking.
- Every added `.cs` and folder needs a committed `.meta`; no secrets or production config changes.
- Run the C# stub compile for all `Assets/SCRIPT`, then Node/Unity-relevant tests available locally, after the last edit.

---

### Task 1: Lock the visual/resource contract with failing tests

**Files:**
- Modify: `Assets/Tests/EditMode/ExactReferenceAssetsTests.cs`
- Modify: `Assets/Tests/EditMode/L10nIntegrityTests.cs` only if the existing test needs explicit new keys
- Test: EditMode tests above

**Interfaces:**
- Consumes: `Resources/reference/*` and `L10n`
- Produces: release-blocking assertions for logo, 6, 7, boy, girl, friend/join/plus assets and private-room copy

- [ ] Write assertions for the approved private-room sprite paths and required EN/EL localization keys.
- [ ] Run the focused tests and confirm they fail because the new paths/keys do not exist yet.
- [ ] Commit the red test contract.

### Task 2: Add approved private-room art and localized copy

**Files:**
- Create: `Assets/newdesign/Resources/reference/mascot_6_exact.png` and `.meta`
- Create: `Assets/newdesign/Resources/reference/char_girl_exact.png` and `.meta`
- Modify: `Assets/SCRIPT/Localization/L10n.cs`
- Modify: `CHANGELOG.md`

**Interfaces:**
- Consumes: approved reference assets from the existing design work
- Produces: loadable 6/girl art and EN/EL keys for page title, create/join descriptions, share, room-code placeholder, and TIP

- [ ] Copy the already-approved exact 6 and girl assets with stable Unity metadata.
- [ ] Add concise EN/EL entries without baking copy into images.
- [ ] Run the focused asset/localization tests and confirm green.
- [ ] Commit the asset and localization change.

### Task 3: Build the portrait private-room presentation

**Files:**
- Modify: `Assets/SCRIPT/RuntimeUI/PvpRuntimeUI.cs`
- Modify: `Assets/Tests/EditMode/ExactReferenceAssetsTests.cs` if behavior-level names need coverage

**Interfaces:**
- Consumes: `PvpGameController` fields and existing handlers; `RuntimeUI`; exact reference sprites; `L10n`
- Produces: portrait `PvPMenuPanel`, `PvPCreatePanel`, and `PvPJoinPanel` with unchanged controller wiring

- [ ] Write a failing structural test for the expected named visual roots and required existing control names.
- [ ] Run the focused test and confirm it fails before the builder exists.
- [ ] Add small private helpers for portrait background, logo/mascot decoration, sheet ribbon, card framing, and localized labels.
- [ ] Restyle the PvP menu with the title ribbon, two cards, 6/7 corner accents, share/TIP chrome, and back control.
- [ ] Restyle create and join panels with their own card identity while preserving `createGo`, `copyBtn`, `joinGo`, inputs, status text, and back listeners.
- [ ] Keep dynamic room code/status text controller-owned and keep all button/input event wiring unchanged.
- [ ] Run focused tests and the C# stub compile.
- [ ] Commit the presentation change.

### Task 4: Verify and hand off the portrait capture

**Files:**
- No new production files; update `CHANGELOG.md` if the implementation adds a user-visible shipped entry

- [ ] Run all local EditMode/unit checks available without Unity.
- [ ] Run the all-`Assets/SCRIPT` mcs stub compile against the Unity/TMP stubs.
- [ ] Build/run the development Android preview at 1080×1920 GLES3 and capture the private-room menu.
- [ ] Inspect EN and EL layouts for overflow and safe-area clipping.
- [ ] Commit any verification-driven fixes separately.
- [ ] Push branch and create/update the draft PR.
