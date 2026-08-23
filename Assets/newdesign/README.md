# HOL Production Design

Unity-ready production design foundation for the current cartoon HOL UI.

## Current direction

The approved screen references and production sprites are the visual source of
truth. HOL is a friendly, expressive social number game with a polished 2.5D
cartoon presentation, strong readable typography and large mobile-first CTAs.

Production presentation uses:

- approved illustrated HOL logo and number mascots
- expressive boy/girl character art
- deep purple/blue arcade backgrounds from approved screen families
- cyan/blue secondary actions
- warm gold primary actions
- magenta/pink competitive or join-room accents where the reference uses them
- large readable EN/EL typography
- screen-specific composition measured against approved references

## Mandatory implementation rules

- `AGENTS.md` is authoritative for asset fidelity, typography/layout fidelity
  and one-screen/one-owner visual architecture.
- Approved PNG/SVG/sprites stay visibly rendered at alpha `1`.
- Do not replace approved artwork with procedural lookalikes, generated
  gradients, generic rounded rectangles or global palette passes.
- `RuntimeUI` is infrastructure only; it must not select the product theme.
- Every production screen has one presentation owner.
- Historical theme systems and reskin chains are retired and must not return.
- All user-facing copy remains localized through `L10n.Get` / `LocalizedText`.

## Production resource families

Current approved Resources are grouped by the screen/family that owns them,
including:

- `reference/`
- `phase2a/`
- `mainmenu/`
- `settings/`
- `splash/`

`Resources/design/` contains only the current localized PvP signal icon set.
The retired generic background/panel/button theme surfaces have been deleted.

## Integration targets

- `Assets/SCRIPT/RuntimeUI` — neutral runtime infrastructure
- `Assets/SCRIPT/Design` — screen-specific production presentation owners
- `Assets/SCRIPT/UIJuice` — additive interaction feedback only
- `Assets/SCRIPT/Localization`
- `Assets/SCRIPT/AdsManager.cs`
- `Assets/Scenes/MainMenu.unity`

Before any visual task is marked complete, validate a native-resolution capture
side-by-side with its approved reference in both English and Greek.
