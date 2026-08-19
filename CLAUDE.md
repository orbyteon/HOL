# CLAUDE.md

HOL — Unity 2022.3 Android number-duel game. Solo vs adaptive AI plus live
PvP over PlayFab CloudScript, EN/EL localization, LevelPlay ads, Consumer
First design built at runtime from code.

## Working loop (owner's standing rule — follow for every change)

1. **Branch** off fresh `main`, named `claude/<topic>`. One feature per
   branch, nothing else riding along.
2. **Work** that single feature.
3. **Test locally before pushing.** Open the PR as **draft** until local gates
   pass. The strongest local gate per area:
   - C# — stub-compile *all* of `Assets/SCRIPT` together against Unity/TMP
     stubs (mcs; keep legacy `Text` int/`TextAnchor` vs TMP float/
     `TextAlignmentOptions` shapes honest so misuse fails like Unity).
     Run the stub compile again *after the last edit*, not before it.
   - Provisioner — `npm test` in `services/provisioner`.
   - CloudScript / duel rules — `node --test tools/test`.
   - Real Unity CI (EditMode + Android compile) is always the authority.
4. **PR and merge** (merge commit) only when every CI job is green. Mark the PR
   ready for review only after local verification; expensive Android preview
   captures are label-triggered (`preview-mainmenu`, `preview-panelplay`,
   `preview-splash`) after CI is green — see `docs/ci-policy.md`.
5. **Delete the merged branch.** The repo auto-deletes head branches on
   merge; delete the local branch too. The CI credential cannot delete
   remote refs directly — do not fight the 403.

## Hard rules

- **Never upload to Google Play.** The owner does all Play Console uploads
  and forms.
- **Production workflows** (PlayFab CloudScript deploy, provisioner deploy,
  signed release build, minVersion gate) dispatch from `main` only, with
  their typed confirmations, and only on the owner's word.
- **minVersion** locks older clients out of PvP. Raise it only on the
  owner's explicit word, only after their devices carry the new build.
- `docs/privacy.html` and `services/provisioner/static/privacy.html` are
  byte-identical twins (CI enforces both directions). The live
  `/api/privacy` page changes only on a provisioner deploy, not on merge.
- Every `.cs` under `Assets/` has a committed `.meta`. The SVGs under
  `Assets/newdesign/Resources/design/` are load-bearing at runtime
  (`Resources.Load`, held green by an EditMode test).
- Every user-facing string goes through `L10n`, with EN **and** EL entries.
- PlayFab clients call only `ExecuteCloudScript` — the server is the sole
  PvP authority. `Signals.Table` is append-only and must match
  `SIGNAL_COUNT` in `playfab/cloudscript.js`.
- The committed `HOLReleaseConfig.json` stays empty. No secrets, keys, or
  keystores in git — CI gates enforce this.
- Tests in `Assets/Tests/EditMode` reach game types **via reflection only**
  (the test asmdef deliberately does not reference Assembly-CSharp).

## Where things live

- Release order and gates: `docs/release-checklist.md` (order is
  load-bearing — client, then minVersion, then CloudScript).
- Play Console answers (Data safety, release notes): `docs/store-listing.md`.
- Keep `CHANGELOG.md`'s Unreleased section current with every merged change.
- Rules/fairness harness: `tools/test/` (seeded simulations reproduce the
  documented win-rate figures exactly).
