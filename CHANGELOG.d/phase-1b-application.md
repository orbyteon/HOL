# Phase 1B — HOL.Application

## Changed

- Introduced `HOL.Application` as a Unity-free production assembly that depends
  only on `HOL.Core`.
- Moved `MatchOutcome` and `GameEvents` from `SmartHooks/` into the application
  module while preserving their committed Unity GUIDs and all existing
  win/loss/draw event behavior.
- Replaced the reflection-based outcome/event test harness with direct
  compile-time construction and calls through `HOL.EditModeTests`.
- Added structural dependency checks, explicit transitional friend access for
  unmigrated `Assembly-CSharp` callers, architecture documentation and a scoped
  mandatory agent contract.

No gameplay, scene, UI, persistence, PlayFab handler, package, release-version,
deployment or store behavior changed.

This fragment must be folded into the top `[Unreleased]` → `Changed` section of
`CHANGELOG.md` during the next release-note consolidation. Until then it is the
canonical focused note for PR #77 and must not be silently deleted.
