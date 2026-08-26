# HOL Architecture Summary

HOL currently ships as one Unity application with two scenes. The production
runtime is largely in `Assembly-CSharp`; the staged migration toward compile-time
boundaries is defined in `docs/architecture/cto-stabilization-roadmap.md`.

## Runtime layers

- **Rules/domain:** `DuelRules`, result/value objects and stats behavior
- **Application orchestration:** `GameManager`, `DailyHunt`, `PvpGameController`
- **Infrastructure:** `PlayFabPvpClient`, provisioning, ads and release config
- **UI foundation:** `RuntimeUI`, responsive layout, localization bindings
- **Presentation:** screen-specific owners in `Assets/SCRIPT/Design/` and focused
  presentation components in `RuntimeUI/`
- **Server authority:** `playfab/cloudscript.js`

## Dependency direction for new work

- Pure rule/state code must not depend on Unity UI, `Resources`, PlayerPrefs,
  PlayFab or ads.
- Presentation reads state and invokes explicit actions; it does not adjudicate
  gameplay or networking.
- Infrastructure implements interfaces used by orchestration; controllers must
  not parse raw HTTP payloads.
- Bootstrap is the only place that constructs and connects required services.

## Known transition constraints

Some current screens are built or re-seated at runtime and some tests use
reflection because production code is still in `Assembly-CSharp`. Do not extend
those patterns. Refactors must remove them one focused surface at a time while
preserving callbacks, scene GUIDs and production visual contracts.
