# Phase 1D — fixed PvP Signal protocol

## Changed

- Moved the ordered six Signal localization keys, id validation and per-side cap
  into Unity-free `HOL.Application` as `PvpSignalProtocol`.
- Kept the existing Unity `Signals` API as a thin localization adapter and
  compatibility alias, without changing any wire id, localization key, icon
  resource path or CloudScript limit.
- Replaced reflection-based Signal vocabulary discovery in EditMode coverage
  with direct compile-time application references.
- Added structural parity checks against CloudScript `SIGNAL_COUNT` and
  `SIGNAL_CAP_PER_SIDE`.

No gameplay, networking, moderation, scene, UI, localization copy, persistence,
deployment or release behavior changed.

This fragment must be folded into the top `[Unreleased]` section of
`CHANGELOG.md` during release-note consolidation. Until then it is the canonical
focused note for the Phase 1D pull request and must not be silently deleted.
