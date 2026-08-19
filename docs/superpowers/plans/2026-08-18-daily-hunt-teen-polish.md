# Daily Hunt Teen Polish Implementation Plan

> **For agentic workers:** Use the TDD cycle for each task. Steps use checkbox syntax.

**Goal:** Apply the approved portrait Teen Polish Daily Hunt presentation to the existing seeded daily-number flow.

**Architecture:** Add a presentation-only `DailyHuntVisuals` owner invoked after `DailyHunt.Build()`. It creates only non-interactive chrome and repositions existing input/buttons/status/trail/streak controls. `DailyHunt` remains authoritative for the secret, guess budget, revive ad, persistence, and share callback.

**Constraints:** 1080×1920 portrait; EN/EL localization; no new gameplay action, social feed, percentile, coins, or free text; approved logo and 6/7 art only; no scene surgery.

### Task 1: Contract
- [ ] Add reflection-only contract for `DailyHuntVisuals` and approved resources.
- [ ] Observe the expected red test.
- [ ] Commit.

### Task 2: Presentation
- [ ] Build title/logo/card/progress/trail chrome.
- [ ] Reposition existing DailyHunt controls without replacing callbacks.
- [ ] Commit.

### Task 3: Verification
- [ ] Run all-script compile, focused tests, 28 Node tests, and CI.
- [ ] Push/update draft PR.
