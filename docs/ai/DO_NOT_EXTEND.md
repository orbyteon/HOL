# Transitional Patterns — Do Not Extend

The following patterns exist during migration but are not approved for new
required dependencies:

- frame-count polling for controller/view discovery
- scene-wide `Find*` scans in per-frame methods
- hierarchy-name strings as required dependency identifiers
- required production sprites supplied only by `Resources.Load` strings
- procedural UI placeholders surviving a required-art failure
- ordinary unit tests using reflection because the tested code has no assembly
  boundary
- controllers that own networking, navigation, UI copy, audio, stats and
  analytics simultaneously
- copied Android capture workflow bodies instead of reusable workflow inputs

A focused refactor may temporarily touch one of these patterns only to remove it,
with regression coverage and no behavior/visual authority change in the same PR.
