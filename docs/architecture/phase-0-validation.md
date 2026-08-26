# Phase 0 Validation Matrix

The CTO stabilization PR is mergeable only when the exact merge candidate
passes every applicable gate below.

| Area | Required evidence |
|---|---|
| JavaScript | `node --check playfab/cloudscript.js` |
| PvP authority | `node --test tools/test/*.test.mjs` |
| Production art failure | `ProductionAssetFailClosedTests` in EditMode |
| Unity compilation | EditMode plus Android compile job |
| Visual architecture | Production Visual Integrity workflow |
| Runtime presentation | Exact visuals PlayMode workflow |
| Diagnostics | PlayMode artifact includes preflight data and Unity outputs |
| Repository integration | Branch is current with `main`; no unresolved review thread |

## Specific regression scenarios

- A missing or empty production sprite path clears, disables and makes the
  affected `Image` non-raycastable.
- A valid production sprite restores the image and approved rendering state.
- Match 0 Signal, rematch and result acknowledgement commands remain valid.
- After a room advances to match 1, explicit or omitted match-0 commands fail
  with `stale match` and cannot mutate the room.
- Current match acknowledgements remain idempotent and delete a room only after
  both sides acknowledge.
- PlayMode starts from a complete Unity checkout containing
  `ProjectSettings/ProjectVersion.txt`.

No production deployment, PlayFab publish or Google Play action is authorized by
this validation document or by merging the PR.
