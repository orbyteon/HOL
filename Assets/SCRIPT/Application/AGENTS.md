# HOL.Application — Mandatory Agent Contract

This file applies to every current and future file under `Assets/SCRIPT/Application/`.

## Purpose

`HOL.Application` owns Unity-free application contracts, semantic events and
orchestration use cases. It may coordinate `HOL.Core`; it must not become a
second UI, persistence, transport or service layer.

## Mandatory dependency rules

- Depend only on `HOL.Core` and .NET base-class-library types.
- Do not reference `UnityEngine`, `UnityEditor`, scenes, `MonoBehaviour`,
  `ScriptableObject`, TextMesh Pro or Unity UI.
- Do not call `PlayerPrefs`, `Resources`, `UnityWebRequest`, PlayFab clients,
  ads, consent, release configuration or platform APIs.
- Do not perform HTTP, JSON transport-envelope parsing, polling, retries,
  authentication, persistence or screen navigation here.
- Do not introduce dependencies from `HOL.Core` back to `HOL.Application`.

## Current transitional seams

- `MatchOutcome` and `GameEvents` are the first application contracts.
- Their event behavior, analytics field names and win/loss/draw semantics are
  compatibility contracts; preserve them unless a separately approved product
  migration changes the wire contract and its tests.
- `MatchOutcome` still contains legacy JSON formatting methods. They are frozen
  transitional behavior and belong in `HOL.Infrastructure.PlayFab` in a later
  behavior-preserving slice. Do not add new transport concerns to them.
- `AssemblyInfo.cs` grants `Assembly-CSharp` temporary internal access while
  unmigrated callers remain in Unity's predefined assembly. Do not add more
  friend assemblies without an explicit architecture reason. Remove the
  `Assembly-CSharp` friend after typed application entry points replace those
  callers.

## Testing and migration

- Tests for this module reference `HOL.Application` directly at compile time.
  Reflection is not an acceptable substitute for ordinary C# contracts.
- Preserve committed `.meta` GUIDs when moving an existing Unity asset.
- Every new invariant requires focused tests plus an update to
  `tools/test/application-assembly-boundary.test.mjs` when structurally
  enforceable.
- Keep each PR behavior-neutral unless the owner explicitly authorizes a product
  or gameplay change.
- No module change authorizes PlayFab/Azure deployment, signed builds,
  `minVersion` changes or store publication.
