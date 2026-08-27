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
- fixed wire/application protocols expressed with .NET primitive types;
- use cases that depend only on `HOL.Core` and .NET base types.

Forbidden:

- `UnityEngine`, scenes, UI, `MonoBehaviour` or `ScriptableObject`;
- `PlayerPrefs`, `Resources` or `L10n`;
- `UnityWebRequest`, PlayFab clients or transport scheduling;
- ads, consent and release configuration.

`MatchOutcome` and `GameEvents` moved with their existing Unity GUIDs.
`MatchOutcomeTests` binds both types directly at compile time.
`AssemblyInfo.cs` temporarily grants internal access to `Assembly-CSharp` while
callers remain in the predefined Unity assembly, plus `HOL.EditModeTests` for
direct internal behavior tests. Remove the `Assembly-CSharp` friend when those
callers move behind typed application entry points.

`MatchOutcome` still contains its existing analytics JSON methods in this first
behavior-neutral migration. Their eventual extraction belongs to
`HOL.Infrastructure.PlayFab`; this temporary placement is not permission to add
new transport concerns to `HOL.Application`.

`PvpRoomState` is the Unity-free public-room value contract shared by the
application boundary and the existing PlayFab runtime. Its public primitive
field names are the JsonUtility wire contract and stay mechanically compared
with every key emitted by `playfab/cloudscript.js`.

`PvpBackend.RoomState` remains temporarily as a serializable, fieldless subclass
of `PvpRoomState`. That source-compatible shim lets existing Unity callers keep
their current signatures while the state fields and helper behavior compile in
`HOL.Application`. A later typed-backend slice will update those signatures and
remove the shim.

`PvpSignalProtocol` owns the ordered six Signal localization keys and the
per-side cap. Its ids are append-only wire protocol shared with CloudScript.
The Unity `Signals` class remains a thin adapter for `L10n` and icon-facing
runtime code; it must not carry a second copy of the vocabulary.

`tools/test/application-assembly-boundary.test.mjs` locks the asmdef direction,
forbidden dependencies, stable GUIDs and direct-test contracts.
`tools/test/room-state-contract.test.mjs` locks CloudScript public-view parity.
`tools/test/pvp-signal-protocol.test.mjs` locks Signal ordering/count/cap parity
and the thin Unity adapter.

## Completed migrations

### Phase 1A — Core

`DuelRules.cs` moved from `Assets/SCRIPT/` to `Assets/SCRIPT/Core/` with its
existing `.meta` file unchanged. `HOL.EditModeTests` references `HOL.Core`
directly and its duel tests use normal construction and method calls.

The server-authoritative mirror remains `playfab/cloudscript.js`; any rule
change must still update both the C# and CloudScript test suites.

### Phase 1B — application outcome/events

`MatchOutcome.cs` and `GameEvents.cs` moved from `SmartHooks/` to
`Application/`. Event behavior, JSON field names and legacy win/loss
compatibility stayed unchanged.

### Phase 1C — PvP public room state

`PvpRoomState` became the Unity-free source of truth for the public PlayFab room
view. The existing nested `PvpBackend.RoomState` remains a fieldless derived
compatibility type while runtime signatures are migrated later.

### Phase 1D — fixed Signal protocol

The ordered Signal keys, count, validation and per-side cap move into
`PvpSignalProtocol`. `Signals` delegates protocol queries and owns only
localization-facing behavior. Existing ids, keys, icon paths and CloudScript
constants remain unchanged.

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
