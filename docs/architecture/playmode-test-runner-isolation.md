# PlayMode test-runner and exact-target isolation

## Incident 1 — fixture scene ownership

After the Daily Hunt merge, PlayMode workflow runs `#456` and `#457` remained
inside the GameCI PlayMode step instead of producing a test result artifact.
The newly added daily-mission fixture created a scene named `SplashScene`,
enumerated every loaded scene and unloaded everything else.

That pattern is unsafe because Unity Test Framework owns runner lifecycle
objects and may own another loaded scene. A fixture must never assume that every
scene it did not create belongs to production code.

The real `SplashScene` is also not a neutral test sandbox. `SplashLoader`
automatically transitions it to `MainMenu` after its configured delay or a tap,
so loading it can start unrelated production lifecycle code during a component
fixture.

### Fixture invariant

A PlayMode fixture that needs an empty component context must create one uniquely
named non-production scene, retain its exact `Scene` handle, make only that scene
active for fixture-created objects, and unload only that retained scene during
teardown. It must not enumerate `SceneManager.sceneCount`, call `GetSceneAt`, use
`LoadSceneMode.Single`, or start a production scene just to obtain an empty
hierarchy.

## Incident 2 — chained workflow target drift

After exact PR CI #629 succeeded, automatic PlayMode #459 was created with
`head_branch: main`, the current `main` SHA and an empty `pull_requests` array.
The completed source CI run itself still contained PR #80 and its real head.
Trusting only the lossy `workflow_run` event would therefore validate `main`
instead of the candidate that produced the green CI result.

The same lossy branch value also made unrelated automatic PR runs share
`playmode-main`, allowing one PR to cancel another.

### Exact-target invariant

For a `workflow_run` trigger, PlayMode uses the source run id to re-read:

```text
repos/<owner>/<repo>/actions/runs/<source-run-id>
```

If that source run contains a pull request, its `head.sha` and `head.ref` are the
target. Only a source run with no PR association may fall back to the event's
push SHA and branch. The resolved branch is passed to the open-PR merge-ref
resolver, and diagnostics record requested SHA, requested branch, checkout ref
and actual checkout SHA.

Automatic concurrency uses the event PR number when available and otherwise the
unique source CI run id. It must never use the lossy `workflow_run.head_branch`
as the sole group identity.

## Incident 3 — stale Unity compilation and application binding

PlayMode #462 terminated and uploaded results, but it executed the removed
`DailyHuntPanel_StaysInsideSafeAreaAcrossCommonPortraits` test. Its source was
absent from the exact checked-out candidate, proving that a restored
`Library/ScriptAssemblies` or Bee product had outlived the C# source that built
it.

The same run's Daily Challenge fixture failed to find `GameEvents` because its
runtime resolver assumed every type lived in `Assembly-CSharp`. `GameEvents` and
`MatchOutcome` are now contracts in `HOL.Application`, where module tests are
required to bind them directly at compile time.

### Compilation-cache invariant

PlayMode Library reuse is valid only for the exact Unity and C# assembly graph.
Its cache key therefore fingerprints `ProjectSettings/ProjectVersion.txt`, both
package manifests, every `Assets/**/*.cs`, every `Assets/**/*.asmdef` and every
`Assets/**/*.asmref`. It has no broad `restore-keys` prefix.

Even on an exact cache hit, the workflow removes `Library/ScriptAssemblies`,
`Library/Bee`, `Library/BuildCache` and `Library/BuildPlayerData` before GameCI.
Imported asset state may be reused; compiled C# and player-build products must be
regenerated from the checked-out candidate.

### Application-binding invariant

`HOL.PlayModeTests` references `HOL.Application` directly. The mission-flow
fixture invokes typed `GameEvents` methods and constructs a typed
`MatchOutcome`; a narrowly documented `InternalsVisibleTo` keeps those semantic
raise points internal to production callers while making the integration
contract testable. Reflection remains only at genuine Unity component/default-
assembly boundaries that have not yet migrated.

## Workflow fail-safe

The `Exact visuals PlayMode` job has a hard 25-minute timeout. A timeout is a
failed validation gate, not a reason to merge. The existing `always()`
diagnostic and artifact steps remain mandatory so a terminating failure keeps
checkout and Unity evidence.

## Acceptance

This repair is complete only when the exact pull-request candidate passes Node
workflow/isolation contracts, Unity EditMode, Android compile, Production Visual
Integrity, exact-target PlayMode with SHA artifact evidence, and zero unresolved
review threads. After merge, the resulting protected `main` SHA must pass the
same baseline before architecture work resumes.

No successful gate authorizes PlayFab/Azure deployment, a signed build,
`minVersion` change or store publication.
