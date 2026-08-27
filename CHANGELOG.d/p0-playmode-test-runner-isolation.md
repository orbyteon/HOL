# P0 — PlayMode test-runner isolation

## Fixed

- Replaced a Daily Challenge PlayMode fixture that manually created a
  `SplashScene` and unloaded every other loaded scene with Unity's supported
  `LoadSceneAsync(..., LoadSceneMode.Single)` transition.
- Added a hard 25-minute timeout to the PlayMode job so a stranded Unity fixture
  fails closed instead of consuming a runner indefinitely.
- Added structural contracts preventing all-scene ownership and timeout removal
  from returning.

No gameplay, Daily Hunt progression, visual composition, localization,
networking, persistence, deployment or release behavior changed.
