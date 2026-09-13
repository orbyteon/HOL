# Release and merge gates

All production dispatches and the internal-test publisher now verify successful CI and visual-integrity push runs for the exact main SHA before side effects. Provisioner deployment also retains its own tests. An API error, missing evidence, newer pending run or failure stops the operation. Dispatch again only after the corresponding revision is validated. This change does not deploy anything.

The internal-test request file remains disabled. An armed request still represents an explicit publication request, but it cannot bypass the new validation gate. If triggered before main CI finishes it fails closed; use the existing manual dispatch once checks complete.

## Required-check configuration to apply with repository administration access

The connector used for these changes exposes no branch-protection update endpoint. The live setting has therefore not been changed by this PR.

The exact payload in `required-status-checks.json` is for:

`PATCH /repos/orbyteon/HOL/branches/main/protection/required_status_checks`

Apply only this subresource, preserving review requirements, deletion protection, force-push settings and bypass policy. If the required-check subresource does not yet exist, use repository Settings to enable it with the listed contexts. Confirm the applied configuration by reading it back. Do not replace the complete protection document with defaults.

`Visual system integrity` is the actual check name. `Production Visual Integrity` is its workflow title. PlayMode is still a separate evidence gate for affected visual changes; do not configure a permanently missing PR-head context while its workflow_run association remains unresolved. Require its verified tested revision as part of review.

The production environment exists. A required-reviewer rule was not visible during the audit. Its owner should confirm the intended reviewer and administrator-bypass policy. No reviewer identity is invented here.
