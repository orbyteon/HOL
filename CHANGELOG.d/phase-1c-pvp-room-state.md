# Phase 1C — PvP room-state application contract

## Changed

- Moved the PvP public room-state contract and its pure side/match-point helpers
  into Unity-free `HOL.Application` as `PvpRoomState`.
- Kept `PvpBackend.RoomState` as a serializable, fieldless compatibility shim so
  existing Unity transport, controller and polling signatures remain unchanged.
- Redirected the CloudScript field-parity contract to the application source of
  truth and added direct compile-time EditMode coverage.

No gameplay, transport, scene, UI or deployment behavior changed.

This fragment must be folded into the top `[Unreleased]` section of
`CHANGELOG.md` during release-note consolidation. Until then it is the canonical
focused note for the Phase 1C pull request and must not be silently deleted.
