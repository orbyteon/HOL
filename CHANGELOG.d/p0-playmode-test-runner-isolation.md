# P0 — PlayMode runner and exact-target isolation

## Fixed

- Replaced a Daily Challenge PlayMode fixture that claimed every loaded scene
  with one uniquely named test-owned scene, made active only for fixture-created
  objects and unloaded by its exact retained handle.
- Prevented component isolation from loading the real `SplashScene`, whose
  `SplashLoader` automatically starts the `MainMenu` transition lifecycle.
- Made chained PlayMode runs re-read their completed source CI run so a PR head
  is recovered even when GitHub's `workflow_run` event reports `main` and an
  empty pull-request array.
- Isolated automatic PlayMode concurrency by PR number or unique source CI run
  id so unrelated PRs cannot cancel each other through `playmode-main`.
- Added requested branch to checkout diagnostics and structurally locked source
  run recovery, exact merge-ref selection and SHA evidence.
- Added a hard 25-minute timeout so a stranded Unity fixture fails closed
  instead of consuming a runner indefinitely.

No gameplay, Daily Hunt progression, visual composition, localization,
networking, persistence, deployment or release behavior changed.
