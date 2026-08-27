# PlayMode test-runner scene isolation

## Incident

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

## Invariant

A PlayMode fixture that needs an empty component context must:

1. create one uniquely named non-production scene;
2. retain its exact `Scene` handle;
3. make only that scene active for objects created by the fixture; and
4. unload only that retained scene during teardown.

Example:

```csharp
Scene testScene = SceneManager.CreateScene(
    "FeatureTests_" + Guid.NewGuid().ToString("N"));
SceneManager.SetActiveScene(testScene);
// ... test ...
yield return SceneManager.UnloadSceneAsync(testScene);
```

The fixture must not enumerate `SceneManager.sceneCount`, call `GetSceneAt` to
claim other scenes, use `LoadSceneMode.Single`, or start a production scene just
to obtain an empty hierarchy.

## Workflow fail-safe

The `Exact visuals PlayMode` job has a hard 25-minute timeout. A timeout is a
failed validation gate, not a reason to merge. The existing `always()`
diagnostic and artifact steps remain mandatory so a terminating failure keeps
checkout and Unity evidence.

## Acceptance

This repair is complete only when the exact pull-request candidate passes:

1. Node workflow/isolation contracts.
2. Unity EditMode.
3. Android compile.
4. Production Visual Integrity.
5. Exact-target PlayMode with checkout SHA evidence and an uploaded artifact.
6. Zero unresolved review threads.

After merge, the resulting protected `main` SHA must pass the same CI, visual
integrity and PlayMode baseline before architecture work resumes.

No successful gate authorizes PlayFab/Azure deployment, a signed build,
`minVersion` change or store publication.
