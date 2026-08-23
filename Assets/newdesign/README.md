# HOL New Design — Cartoon Theme

Unity-ready design foundation for the canonical HOL player-facing visual identity.

## Direction

HOL is a friendly competitive **cartoon number/brain game** with a polished 2.5D mobile-arcade presentation:

- deep indigo/plum depth
- expressive age-neutral cartoon characters and number mascots
- chunky glossy controls with clear hierarchy
- cyan-blue secondary actions
- warm gold primary actions
- restrained magenta/violet competitive emphasis
- numbers, chevrons, lightning, stars and sparse confetti as supporting motifs
- strong Greek/English readability
- no ads during active gameplay
- rewarded ads always disclose the reward first

The complete identity contract is `../../design/cartoon-theme.md` and the production asset authority map is `cartoon-theme-authority.md`.

Converging Light is retained only as a **secondary atmospheric subsystem**. It may contribute deep indigo depth, subtle number fields, interval/chevron motifs and restrained neon glow, but it must not override approved cartoon artwork, chunky controls, typography hierarchy or user-approved compositions.

## Integration targets

- `Assets/SCRIPT/RuntimeUI`
- `Assets/SCRIPT/Design`
- `Assets/SCRIPT/UIJuice`
- `Assets/SCRIPT/Localization`
- `Assets/SCRIPT/AdsManager.cs`
- `Assets/Scenes/MainMenu.unity`

## Asset set

- `design-tokens.json`: canonical palette, spacing, typography and visual-role rules
- `cartoon-theme-authority.md`: screen-by-screen production asset authority
- `Resources/reference/`: shared exact HOL identity art
- `Resources/phase2a/`: current production Main Menu cartoon family
- screen-specific `Resources/<screen>/` families for approved production surfaces

All user-facing copy remains localized through `L10n.Get` / `LocalizedText`. Approved sprites stay visible at alpha `1`; `_9s` artwork uses `Image.Type.Sliced`; procedural rendering is fallback/additive only when no approved production asset exists.
