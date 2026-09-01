# Settings Teen Polish Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans (recommended) to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Restyle the existing Settings page to the approved portrait Teen Polish reference without adding new interactive settings or changing callbacks.

**Architecture:** Add a presentation-only `SettingsVisuals` runtime component. It creates non-interactive chrome (logo, title, backdrop, row cards, decorative 6/7), discovers the already-wired settings controls by stable names, and repositions/styles them in place. `MenuManager`, `ExtrasRuntimeWiring`, `LanguageSelector`, `MusicSettings`, `SavePlayerName`, consent, and difficulty behavior remain authoritative.

**Tech Stack:** Unity 2022.3, UGUI, TextMesh Pro, existing Resources art, NUnit EditMode tests, mcs stub compile.

## Global Constraints

- Stack on `feature/result-teen-polish-20260818`.
- No new Button, Toggle, InputField, Store, Profile, currency, or settings category.
- All new user-facing labels use EN/EL `L10n`.
- Use exact approved logo, boy/girl-independent 6 and 7 decor, 1080×1920 portrait and safe margins.
- Keep existing callback wiring and PlayerPrefs behavior unchanged.

---

### Task 1: Add failing Settings presentation contract

- [ ] Assert `SettingsVisuals` and `settings_title` exist.
- [ ] Assert the visual builder creates no interactive controls.
- [ ] Run the focused Unity test and observe the expected missing contract.
- [ ] Commit the red contract.

### Task 2: Build presentation-only Settings chrome

- [ ] Add EN/EL `settings_title`.
- [ ] Add `SettingsVisuals.cs` and `.meta`.
- [ ] Build header, row cards, logo, title, backdrop and 6/7 decor.
- [ ] Reposition/style existing name, language, music, difficulty and ads controls.
- [ ] Commit implementation.

### Task 3: Verify and publish

- [ ] Run all-script mcs compile, focused tests, and 28 Node tests.
- [ ] Push and create a draft stacked PR.
- [ ] Require green EditMode, PlayMode and Android compile checks.
