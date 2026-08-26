# Phase 1B — HOL.Application validation

This slice is behavior-neutral. It may merge only when the exact PR merge
candidate satisfies every gate below.

## Structural contract

- `HOL.Application` has `noEngineReferences: true`.
- Its only production assembly dependency is `HOL.Core`.
- `MatchOutcome` and `GameEvents` no longer compile in `Assembly-CSharp`.
- Their existing Unity `.meta` GUIDs remain unchanged.
- `HOL.EditModeTests` references `HOL.Application` directly.
- `MatchOutcomeTests` contains no reflection-based production lookup.
- `Assembly-CSharp` is a temporary friend only for unmigrated internal callers.

## Behavioral contract

- Match JSON keys and escaping remain byte-compatible.
- A win reaches the legacy event with its guess count.
- A loss reaches the legacy event with zero guesses, as before.
- A draw never reaches the legacy win/loss event.
- Every completed result raises the typed outcome and stats event exactly once.

## Required evidence

1. Node/architecture contracts pass on the PR merge ref.
2. Unity EditMode passes on the same merge ref.
3. Android compile passes on the same merge ref.
4. Production Visual Integrity passes.
5. Automatic PlayMode checks out and passes the same exact candidate.
6. No unresolved review thread remains.
7. The canonical changelog records the boundary before merge.

No successful gate authorizes PlayFab/Azure deployment, a signed build,
`minVersion` change or store publication.
