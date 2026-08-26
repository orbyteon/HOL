# HOL — CI cost and reliability policy

These rules keep Actions spend predictable, separate real code failures from
billing or infrastructure failures, and ensure what CI validates matches what
merges.

## 1. One feature per PR

Keep pull requests small and focused. Split unrelated work (visual restyle,
gameplay, backend, CI) into separate branches and PRs so path filters and review
stay honest.

## 2. Draft until locally verified

Open PRs as **draft** until the branch passes the local gates in `CLAUDE.md`
(stub compile, provisioner tests, CloudScript tests). Mark ready for review only
when you expect CI to pass. Draft PRs still run the fast `CI` workflow; they do
not auto-run expensive Android preview captures.

## 3. Fast checks first

The `CI` workflow always runs cheap jobs before Unity work:

1. **Static integrity** — JS syntax, PlayFab guards, privacy drift, meta/GUID
   checks, production workflow pins
2. **Provisioner tests** and **Duel rule tests** (parallel, after static)
3. **Require Unity credentials** (parallel gate)
4. **EditMode tests** and **Build Android (compile check)** (after 1–3)
5. **PlayMode tests** (after `CI` completes successfully — see below)

Never start PlayMode or Android preview work before static integrity is green.

## 4. Expensive jobs only after fast checks pass

- **PlayMode tests** trigger from a successful `CI` workflow run (or manual
  dispatch), not from every push in parallel with `CI`. A path-filtered
  `preview-mainmenu` label event may self-test changes to the PlayMode workflow
  itself before merge, but only after `CI` is green on that exact PR head.
- **Android preview workflows** (Main Menu, PanelPlay, Splash) require a green
  `CI` run on the same commit **and** an explicit label or manual dispatch.

## 5. Android previews are opt-in

Preview workflows do **not** run automatically on every PR push. Request a
capture by either:

- adding the label **`preview-mainmenu`**, **`preview-panelplay`**, or
  **`preview-splash`** to the PR, or
- using **Run workflow** on the matching workflow in the Actions tab.

GitHub sends a `labeled` pull-request event to every preview workflow before
job-level `if` expressions decide which screen was requested. Therefore every
label-triggered preview workflow must use a **screen-scoped workflow concurrency
group**. Never reuse one workflow-level concurrency group across Main Menu,
PanelPlay, Splash, or future screen previews: a non-matching run can otherwise
cancel the requested capture before its own job is skipped.

Remove the label after the capture succeeds to avoid accidental re-runs on later
pushes.

## 6. Re-run with intent

Do not use **Re-run all jobs** blindly after a failure.

1. Read the failing job log or Checks annotation.
2. If the message is *Actions budget* / *job was not started*, restore billing
   quota first — rerunning will fail again.
3. If the failure is a single flaky job, re-run **that job only**.
4. If the failure is code, fix locally, push, and let CI start fresh.

## 7. Validate the merge ref

For pull requests, `CI` checks out `refs/pull/<number>/merge` — the same merge
commit GitHub creates when merging into the base branch. PlayMode and preview
workflows resolve that merge ref when the run is tied to an open PR.

Push-to-`main` runs use the pushed commit directly.

## 8. Short artifact retention

CI and preview workflows retain build/test artifacts for **3 days** (intermediate
APK staging artifacts for **1 day**). Download anything you need before expiry.
Production release artifacts keep their longer retention in `build-release.yml`.

## 9. Actions budget alerts

Configure org/repo billing before minutes run out:

- GitHub **Settings → Billing & plans → Budgets and alerts**
- Set an email alert well below the hard limit (for example 75% and 90%)
- When budget is nearly exhausted, **stop adding preview labels** and finish
  in-flight PRs after quota resets

Workflows cannot raise the spending limit; only an org owner can.

## 10. One clean green merge build before merge

Do not merge until **all required checks** on the PR head (including PlayMode
when visuals changed) are green on the latest commit. After merge, confirm the
`push` to `main` `CI` run is green before any production dispatch
(CloudScript, provisioner, release build, minVersion).

Recommended branch protection on `main`:

- Require status checks: `Static integrity`, `Provisioner tests`, `Duel rule
  tests`, `EditMode tests`, `Build Android (compile check)`
- Require PlayMode when PlayMode paths changed (team discretion)
- Require branches to be up to date before merging
