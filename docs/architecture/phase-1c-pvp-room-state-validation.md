# Phase 1C — PvP room-state application contract validation

This slice is behavior-neutral. It may merge only when the exact PR merge
candidate satisfies every gate below.

## Structural contract

- `PvpRoomState` compiles inside Unity-free `HOL.Application`.
- Its public primitive fields are the sole C# source of truth for the PlayFab
  public room view.
- `PvpBackend.RoomState` is serializable, inherits `PvpRoomState`, and contains
  no fields or behavior of its own.
- Existing Unity backend, controller and polling signatures remain unchanged in
  this compatibility slice.
- `PvpRoomStateTests` references `HOL.Application` directly without production
  reflection.
- `tools/test/room-state-contract.test.mjs` compares all CloudScript-emitted keys
  against `Application/PvpRoomState.cs`.

## Behavioral contract

- No public room field is renamed, added, removed or given a different default.
- `LockUsedBy`, `ForfeitPendingFor`, `GuessCountFor` and
  `IsMatchPointAgainst` preserve their existing results.
- PlayFab continues to deserialize and mutate the same runtime
  `PvpBackend.RoomState` object.
- No gameplay rule, signal ordering, rematch generation, polling cadence,
  controller flow, UI, scene, persistence or transport behavior changes.

## Required evidence

1. Node/CloudScript/architecture contracts pass on the exact PR merge candidate.
2. Unity EditMode passes on the same candidate.
3. Android compile passes on the same candidate.
4. Production Visual Integrity passes.
5. Automatic PlayMode checks out and passes the same candidate.
6. No unresolved review thread remains.
7. The focused Phase 1C release-note fragment is retained until release-note
   consolidation folds it into the top `[Unreleased]` section.

No successful gate authorizes PlayFab/Azure deployment, a signed build,
`minVersion` change or store publication.
