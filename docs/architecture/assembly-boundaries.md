# HOL assembly boundaries

Issue #73 is the ordered authority for the architecture program. This document
records the compile-time dependency rules introduced during Phase 1. Each slice
must preserve gameplay and production behavior while replacing implicit
Assembly-CSharp coupling with explicit Unity assembly definitions.

## Current graph

Arrows point from consumers to their compile-time dependency:

```text
Assembly-CSharp ───────► HOL.Application ───────► HOL.Core
       │                                           ▲
       └───────────────────────────────────────────┘

HOL.EditModeTests ─────► HOL.Application
HOL.EditModeTests ─────► HOL.Core
```

`Assembly-CSharp` still owns the unmigrated runtime. Because both production
modules are `autoReferenced`, existing callers continue to compile while later
slices extract their responsibilities.

Tests for migrated contracts reference the corresponding production asmdef
directly. They do not route ordinary C# API checks through reflection.

## `HOL.Core`

Location: `Assets/SCRIPT/Core/HOL.Core.asmdef`

Allowed:

- pure C# duel rules and core value types;
- deterministic calculations with no scene, service or persistence dependency;
- .NET base-class-library types.

Forbidden:

- `UnityEngine` or `UnityEditor`;
- UI, scenes, `MonoBehaviour`, `ScriptableObject` or `Resources`;
- `PlayerPrefs`;
- PlayFab or other transport code;
- ads, consent or release configuration.

The asmdef enforces the first rule with `noEngineReferences: true` and declares
no assembly or precompiled references. `tools/test/core-assembly-boundary.test.mjs`
scans every C# source under `Assets/SCRIPT/Core/` for forbidden framework
imports/calls and locks the remaining path, identity and direct-test-reference
invariants.

## `HOL.Application`

Location: `Assets/SCRIPT/Application/HOL.Application.asmdef`

Allowed:

- Unity-free match/application value contracts;
- application-level events and orchestration contracts;
- use cases that depend only on `HOL.Core` and .NET base types.

Forbidden:

- `UnityEngine`, scenes, UI, `MonoBehaviour` or `ScriptableObject`;
- `PlayerPrefs` or `Resources`;
- `UnityWebRequest`, PlayFab clients or transport scheduling;
- ads, consent and release configuration.

The initial slice moved `MatchOutcome` and `GameEvents` with their existing
Unity GUIDs. `MatchOutcomeTests` binds both types directly at compile time.

`PvpRoomState` is the Unity-free public-room value contract shared by the
application boundary and the existing PlayFab runtime. Its public primitive
field names are the JsonUtility wire contract and stay mechanically compared
with every key emitted by `playfab/cloudscript.js`.

`PvpBackend.RoomState` remains temporarily as a serializable, fieldless subclass
of `PvpRoomState`. That source-compatible shim lets existing Unity callers keep
their current signatures while the state fields and helper behavior compile in
`HOL.Application`. A later typed-backend slice will update those signatures and
remove the shim.

`AssemblyInfo.cs` temporarily grants internal access to `Assembly-CSharp` while
callers remain in the predefined Unity assembly, plus `HOL.EditModeTests` for
direct internal behavior tests. Remove the `Assembly-CSharp` friend when those
callers move behind typed application entry points.

`MatchOutcome` still contains its existing analytics JSON methods in this first
behavior-neutral move. Their eventual extraction belongs to
`HOL.Infrastructure.PlayFab`; this temporary placement is not permission to add
new transport concerns to `HOL.Application`.

`tools/test/application-assembly-boundary.test.mjs` locks the asmdef direction,
forbidden dependencies, stable GUIDs, direct tests and transitional shims.
`tools/test/room-state-contract.test.mjs` locks the complete CloudScript-to-C#
field contract.

## Completed migrations

### Phase 1A — Core

`DuelRules.cs` moved from `Assets/SCRIPT/` to `Assets/SCRIPT/Core/` with its
existing `.meta` file unchanged. `HOL.EditModeTests` references `HOL.Core`
directly and its duel tests use normal construction and method calls.

The server-authoritative mirror remains `playfab/cloudscript.js`; any rule
change must still update both the C# and CloudScript test suites.

### Phase 1B — application outcome/events

`MatchOutcome.cs` and `GameEvents.cs` moved from `SmartHooks/` to
`Application/`. The event behavior, JSON field names and legacy win/loss
compatibility stay unchanged. The only production source edit removed an unused
`UnityEngine` import so the module can enforce `noEngineReferences: true`.

### Phase 1C — PvP room-state contract

The public room fields and pure side/match-point helpers moved from the nested
`PvpBackend.RoomState` implementation into `Application/PvpRoomState.cs`.
Existing callers still compile through the empty inherited compatibility shim;
there is no transport, polling, controller, scene or gameplay change.

`PvpRoomStateTests` now exercises the helpers through a direct
`HOL.Application` reference. The Node room-state contract reads the application
source of truth instead of scraping the Unity backend.

## Migration discipline

For every later Phase 1 module:

1. Move one coherent dependency seam at a time.
2. Preserve `.meta` GUIDs for moved Unity assets.
3. Make dependency direction explicit in asmdefs; do not create cycles.
4. Add a cheap Node contract where structure can be proven without Unity.
5. Keep Unity EditMode and Android compile as the authoritative validation for
   `Assets/` changes.
6. Merge only through an active `main` protection rule and after the exact PR
   merge candidate is green.

No assembly slice authorizes gameplay changes, PlayFab/Azure deployment, signed
builds, store publication, package changes or `minVersion` changes.
