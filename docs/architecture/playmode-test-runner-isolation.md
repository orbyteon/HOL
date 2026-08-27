# PlayMode test-runner scene isolation

## Incident

After the Daily Hunt merge, PlayMode workflow runs `#456` and `#457` remained
inside the GameCI PlayMode step instead of producing a test result artifact.
The newly added daily-mission fixture isolated itself by creating a scene named
`SplashScene`, enumerating every loaded scene and unloading everything else.

That pattern is unsafe in PlayMode tests because Unity Test Framework owns
runner lifecycle objects and may own a loaded runner scene. A fixture must not
assume that every scene it did not create belongs to production code.

## Invariant

PlayMode fixtures that need an empty production context must use Unity's normal
scene transition:

```csharp
yield return SceneManager.LoadSceneAsync(
    "SplashScene", LoadSceneMode.Single);
```

They must not enumerate `SceneManager.sceneCount`, create a synthetic scene with
a production scene name, or call `UnloadSceneAsync` over all other loaded
scenes.

## Workflow fail-safe

The `Exact visuals PlayMode` job has a hard 25-minute timeout. A timeout is a
failed validation gate, not a reason to merge. The workflow must still execute
its `always()` diagnostic and artifact steps so the next change can address the
specific Unity failure.

## Acceptance

This repair is complete only when the exact pull-request candidate passes:

1. Node workflow/isolation contracts.
2. Unity EditMode.
3. Android compile.
4. Production Visual Integrity.
5. Exact-target PlayMode with checkout SHA evidence and an uploaded artifact.
6. Zero unresolved review threads.

No successful gate authorizes PlayFab/Azure deployment, a signed build,
`minVersion` change or store publication.
