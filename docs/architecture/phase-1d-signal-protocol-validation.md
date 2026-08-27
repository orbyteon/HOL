# Phase 1D — fixed PvP Signal protocol validation

This slice is behavior-neutral. It may merge only when the exact PR merge
candidate satisfies every gate below.

## Structural contract

- `PvpSignalProtocol` compiles in Unity-free `HOL.Application`.
- The ordered localization keys appear in one production source only.
- Existing Unity callers continue to use the `Signals` compatibility adapter.
- `Signals.Table` aliases `PvpSignalProtocol.Keys`.
- `Signals` delegates count, cap, id validation and key lookup.
- `L10n` and `Resources` remain outside `HOL.Application`.
- EditMode Signal tests reference `PvpSignalProtocol` directly without
  reflection.

## Wire and behavior contract

- Existing ids `0..5` retain their exact ordered keys.
- `Count` remains six and matches CloudScript `SIGNAL_COUNT`.
- `CapPerSide` remains twelve and matches CloudScript
  `SIGNAL_CAP_PER_SIDE`.
- Invalid ids still fail closed with an empty key.
- Existing icon resource paths remain `design/<signal-key>`.
- No free-text or user-generated-content path is introduced.

## Required evidence

1. Node/CloudScript/architecture contracts pass on the exact PR merge ref.
2. Unity EditMode passes on the same merge ref.
3. Android compile passes on the same merge ref.
4. Production Visual Integrity passes.
5. Automatic PlayMode checks out and passes the same exact candidate.
6. No unresolved review thread remains.
7. The focused release-note fragment remains committed until release-note
   consolidation.

No successful gate authorizes PlayFab/Azure deployment, a signed build,
`minVersion` change or store publication.
