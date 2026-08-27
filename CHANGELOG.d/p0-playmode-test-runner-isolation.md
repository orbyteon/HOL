# P0 — PlayMode test-runner isolation

## Fixed

- Replaced a Daily Challenge PlayMode fixture that claimed every loaded scene
  with one uniquely named test-owned scene, made active only for fixture-created
  objects and unloaded by its exact retained handle.
- Prevented component isolation from loading the real `SplashScene`, whose
  `SplashLoader` automatically starts the `MainMenu` transition lifecycle.
- Added a hard 25-minute timeout to the PlayMode job so a stranded Unity fixture
  fails closed instead of consuming a runner indefinitely.
- Added structural contracts preventing all-scene ownership, production-scene
  isolation and timeout removal from returning.

No gameplay, Daily Hunt progression, visual composition, localization,
networking, persistence, deployment or release behavior changed.
