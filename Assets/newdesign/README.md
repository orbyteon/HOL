# HOL New Design

Unity-ready design foundation for the consumer-first HOL UI.

## Canonical direction

HOL now follows the mandatory **HOL Cartoon Theme** defined in:

- `design/cartoon-theme.md`
- `Assets/newdesign/cartoon-theme-authority.md`
- `Assets/newdesign/design-tokens.json`

The intended product presentation is a polished 2.5D cartoon competitive number/brain game: expressive characters and number mascots, deep plum/indigo depth, large glossy arcade controls, bold readable typography, cyan/blue secondary actions, warm gold primary actions, restrained magenta opponent emphasis, and controlled number/chevron/lightning/star/confetti motifs.

Legacy Converging Light remains available only as secondary atmospheric language. It must not override approved cartoon production artwork or owned screen layouts.

## Integration targets

- `Assets/SCRIPT/RuntimeUI`
- `Assets/SCRIPT/Design`
- `Assets/SCRIPT/UIJuice`
- `Assets/SCRIPT/Localization`
- `Assets/SCRIPT/AdsManager.cs`
- `Assets/Scenes/MainMenu.unity`

## Main Menu fidelity

`MainMenuHomeVisuals` owns Home composition and existing navigation. `MainMenuHomeFidelityEnforcer` runs after the Home build and restores the authoritative Phase 2A / exact-reference sprites as the visible base artwork, preserving alpha `1`, `_9s` slicing and existing button callbacks while disabling only procedural replacement graphics that cover approved assets.

## Asset rules

- approved production artwork is the visual source of truth
- `_9s` assets use `Image.Type.Sliced`
- production sprites remain visible at alpha `1`
- text stays separate and localized through `L10n`
- procedural rendering is fallback/additive only when approved artwork exists
- onboarding production artwork must be approved and committed before implementation
- no procedural/mockup-only robot may be treated as a production mascot
