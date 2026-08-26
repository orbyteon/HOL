# HOL CTO Stabilization Roadmap

This document is the execution order for reducing production risk and making
HOL faster and safer to develop through human and AI-assisted workflows.
It complements the mandatory contracts in the root `AGENTS.md`; it does not
replace them.

## Change policy

- Never combine all architecture work into one migration.
- Each phase starts from a green, current baseline and lands through its own PR.
- Behavior-preserving refactors land before screen or feature redesigns.
- A later phase must not bypass a failing gate from an earlier phase.

## Phase 0 — Production safety baseline

Status: implemented by the CTO stabilization branch.

- isolate the PlayMode workflow's sparse action checkout from the Unity project
- preserve diagnostics and test artifacts on every PlayMode failure
- fail closed when required production artwork is missing
- fence Signal, rematch and result acknowledgement commands by `matchIndex`
- remove silent room-state deserialization catches

Exit gate: Static integrity, Node rule tests, EditMode, Android compile,
Production Visual Integrity and PlayMode are green on the exact merge candidate.

## Phase 1 — Compile-time module boundaries

Create a small set of Unity assemblies rather than one monolithic
`Assembly-CSharp` surface:

- `HOL.Core` — pure duel rules, outcomes and value objects
- `HOL.Application` — use cases and service interfaces
- `HOL.Infrastructure.PlayFab` — authentication, transport and DTO mapping
- `HOL.UI.Foundation` — responsive layout and generic UI infrastructure
- `HOL.UI.Screens` — concrete screen views/presenters
- `HOL.Monetization` — ads, consent and rewarded flows
- `HOL.Bootstrap` — the only composition root

Tests must reference the assembly they exercise directly. Reflection is reserved
for deliberate black-box compatibility tests, not ordinary unit coverage.

Exit gate: dependency directions compile, player builds contain no test
assemblies, and the existing gameplay/visual suite remains green.

## Phase 2 — Deterministic bootstrap

Replace frame-count dependency discovery with one explicit composition root.

- no 120/240/300-frame installers
- no production dependency discovery through scene-wide searches
- required dependencies are serialized or constructed in a defined order
- startup fails immediately and diagnostically when a required dependency is
  absent

Migrate Private Room first because it currently has the clearest installer and
runtime-controller chain.

Exit gate: a PlayMode bootstrap test proves every required service/controller
and screen owner is ready without frame-budget polling.

## Phase 3 — Prefab screen pattern and typed asset catalog

- one prefab and one presentation owner per screen
- callback-bearing controls are serialized references
- approved sprites/fonts are direct references through a
  `HOLUiAssetCatalog` ScriptableObject
- `Resources.Load`, `DeepFind` and hierarchy-name wiring are prohibited for
  required production dependencies
- runtime creation remains only for genuinely dynamic rows/items/effects

Exit gate: renamed hierarchy nodes cannot break navigation or artwork loading,
and missing required references fail validation before the screen opens.

## Phase 4 — Controller decomposition

Split orchestration from presentation and infrastructure:

- PvP session, match state, Signals, rematch and navigation have focused owners
- PlayFab authentication, CloudScript transport, polling and DTO mapping are
  separate services
- solo AI/rules are testable without MonoBehaviour or UI dependencies
- controllers publish state; views render state

Exit gate: core flows have direct unit tests and no controller owns networking,
UI text, audio, stats and navigation simultaneously.

## Phase 5 — AI/CI throughput

- keep root `AGENTS.md` short and canonical
- move detailed maps to `docs/architecture/` and `docs/ai/`
- use reusable workflows for Android capture instead of copying shell pipelines
- every failed CI job uploads logs, test XML, relevant captures and commit
  identity
- only one overlapping visual migration PR is active at a time

Exit gate: a new agent can identify the screen owner, controller, assets,
contracts and test command without reconstructing runtime hierarchy from code.

## External repository setting

`main` must be protected with required PR review, required current status checks,
resolved conversations, blocked force-push/deletion and administrator coverage.
This is a GitHub repository setting and remains tracked by issue #58 until the
branch/ruleset API confirms it is active.
