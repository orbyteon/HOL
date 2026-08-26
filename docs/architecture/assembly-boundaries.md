# HOL assembly boundaries

Issue #73 is the ordered authority for the architecture program. This document
records the compile-time dependency rules introduced during Phase 1. Each slice
must preserve gameplay and production behavior while replacing implicit
Assembly-CSharp coupling with explicit Unity assembly definitions.

## Current graph

Phase 1A introduces the first production module:

```text
HOL.Core
  ^
  |
Assembly-CSharp (legacy runtime consumers)
  ^
  |
HOL.EditModeTests
```

`Assembly-CSharp` still owns the unmigrated runtime. Because `HOL.Core` is
`autoReferenced`, those existing consumers can continue using `DuelRules`
without source changes while later phases extract additional modules.

## `HOL.Core`

Location: `Assets/SCRIPT/Core/HOL.Core.asmdef`

Allowed:

- pure C# duel rules, outcomes and value types;
- deterministic calculations with no scene, service or persistence dependency;
- .NET base-class-library types.

Forbidden:

- `UnityEngine` or `UnityEditor`;
- UI, scenes, `MonoBehaviour`, `ScriptableObject` or `Resources`;
- `PlayerPrefs`;
- PlayFab or other transport code;
- ads, consent or release configuration.

The asmdef enforces the first rule with `noEngineReferences: true` and declares
no assembly references. `tools/test/core-assembly-boundary.test.mjs` locks the
remaining path, identity and test-reference invariants.

## Phase 1A migration

`DuelRules.cs` moved from `Assets/SCRIPT/` to `Assets/SCRIPT/Core/` with its
existing `.meta` file unchanged. Preserving the GUID keeps every serialized
Unity reference stable.

`HOL.EditModeTests` now references `HOL.Core` directly. Its duel tests instantiate
and call `DuelRules` through the public API rather than locating
`Assembly-CSharp` types with reflection. A renamed or broken public API therefore
fails compilation instead of a runtime lookup.

The server-authoritative mirror remains `playfab/cloudscript.js`; any rule
change must still update both the C# and CloudScript test suites.

## Migration discipline

For every later Phase 1 module:

1. Move one coherent dependency seam at a time.
2. Preserve `.meta` GUIDs for moved Unity assets.
3. Make dependency direction explicit in asmdefs; do not create cycles.
4. Add a cheap Node contract where structure can be proven without Unity.
5. Keep Unity EditMode and Android compile as the authoritative validation for
   `Assets/` changes.
6. Do not merge while issue #58 or exact-head CI is unresolved.

No assembly slice authorizes gameplay changes, PlayFab/Azure deployment, signed
builds, store publication, package changes or `minVersion` changes.
