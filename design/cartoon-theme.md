# HOL Cartoon Theme — Canonical Product Visual Identity

Status: **Mandatory / project-wide**

This document is the visual source of truth for the HOL product identity. It applies to Splash, Onboarding, Main Menu, Settings, Solo, PvP, Private Room, Daily Hunt, result screens, profile surfaces, ads/consent surfaces, and every future player-facing screen.

The Production UI Asset Fidelity Contract in `AGENTS.md` remains mandatory. This theme document defines **what HOL should look and feel like**; the Asset Fidelity Contract defines **how approved artwork must be implemented without degradation**.

## 1. Product identity

HOL is a friendly competitive **cartoon number/brain game**, not a sterile instrument UI and not a racing product.

The target presentation is:

- polished 2.5D cartoon / mobile arcade
- expressive, age-neutral characters and number mascots
- thick dark outlines and clear silhouettes
- large glossy controls with obvious hierarchy
- playful competitive energy
- deep night-plum / indigo depth behind vivid character art
- small number, chevron, spark, star and confetti motifs used as visual seasoning
- strong readability in both Greek and English

Do not import RideCore visual language: no cars, tracks, garages, motorsport HUD language, racing badges, or vehicle progression motifs.

## 2. Theme hierarchy and precedence

When sources disagree, use this precedence:

1. User-approved screen/reference capture for the current phase.
2. Approved production sprite/PNG/SVG referenced by that screen.
3. This `design/cartoon-theme.md` contract.
4. `Assets/newdesign/design-tokens.json`.
5. Legacy Converging Light rules and procedural fallbacks.

Converging Light is retained only as a **secondary atmospheric subsystem**: deep indigo depth, subtle number fields, restrained cyan/magenta glow, faint interval/chevron motifs. It must never override the cartoon characters, chunky controls, production sprites, typography hierarchy, or screen composition.

## 3. Canon palette

Use the token file as the machine-readable source. The intended family is:

- deep outline / near-black plum: `#140A22`
- deep background: `#12071F`
- HOL plum family: `#241645`
- raised purple surface: `#3A2068`
- cyan: `#40D9FF`
- blue: `#2A8DFF`
- magenta: `#E84791`
- violet: `#8B6CFF`
- gold: `#FFCA55`
- success green: `#8AD64F`
- primary text: warm near-white `#F5F0FF`

### Color roles

- **Gold**: primary CTA and highest-value positive action.
- **Cyan / blue**: secondary actions, navigation, player-side accents.
- **Magenta**: opponent / contrast state and selective competitive emphasis.
- **Violet**: supporting competitive / premium depth, never a substitute for hierarchy.
- **Green**: success / confirmation only.

Do not scatter every accent on every component. The screen should feel colorful because of character art and controlled focal accents, not because every label has a different color.

## 4. Character and mascot language

Production character art is a first-class part of the HOL identity.

Current approved families include:

- `Assets/newdesign/Resources/reference/hol_logo_exact.png`
- `Assets/newdesign/Resources/reference/mascot_3_exact.png`
- `Assets/newdesign/Resources/reference/mascot_6_exact.png`
- `Assets/newdesign/Resources/reference/mascot_7_exact.png`
- `Assets/newdesign/Resources/reference/player_cyan_exact.png`
- `Assets/newdesign/Resources/reference/opponent_purple_exact.png`
- `Assets/newdesign/Resources/phase2a/hol_menu_boy_arms_crossed_r3.png`
- `Assets/newdesign/Resources/phase2a/hol_menu_girl_forward_fist_r3.png`

Characters should feel expressive, playful and competitive. Poses, faces and silhouettes may carry emotion more strongly than decorative UI glyphs.

A robot is **not** a production-standard HOL mascot until an approved robot asset is committed to the repository and added to the asset authority map. Mockup-only robot artwork must not be recreated procedurally by Codex.

## 5. UI surface language

### Buttons

- large, readable, thumb-friendly
- glossy / 2.5D / arcade depth
- thick dark edge or silhouette where the approved asset contains it
- clear pressed/disabled feedback
- primary CTA visually dominant
- approved `_9s` artwork must remain visible and use `Image.Type.Sliced`
- glow/pulse may be additive, never a replacement for the production sprite

### Cards and panels

- dark plum / indigo surfaces that let character art and buttons pop
- clear rounded/chamfered silhouette based on the approved production asset
- restrained outer glow
- generous internal padding
- no thin scientific-instrument treatment as the final product surface

### Icons and decoration

Prefer HOL-specific motifs:

- numbers
- range / interval marks
- chevrons
- lightning / energy
- trophy / challenge
- stars and sparse confetti
- character expressions / mascots

Do not use generic hearts as recurring filler decoration. A heart is acceptable only when it has explicit semantic meaning in a future feature and is approved for that feature.

## 6. Typography and readability

Typography is part of the cartoon theme, not an afterthought.

- Display/CTA text should feel bold, friendly and arcade-readable.
- Body text must remain clean and highly legible.
- Greek and English must fit the same authored region without tiny fallback text.
- Use the approved Phase 2A TMP font assets when the screen contract calls for them.
- Text remains live TMP/localized content and must not be baked into production artwork.
- Avoid thin, tiny, widely tracked scientific labels as the dominant product typography.

The Typography, Readability & Layout Fidelity Contract in `AGENTS.md` remains authoritative for placement, sizing, safe areas, wrapping and responsive behavior.

## 7. Background language

Backgrounds should support the characters and CTA hierarchy.

Preferred ingredients:

- deep plum / indigo night depth
- subtle arcade/neon environment
- faint numbers or interval motifs
- restrained stars/sparks/confetti
- localized cyan/magenta/violet glow

Avoid visually busy full-screen decoration behind important copy. Decorative layers must not reduce contrast or readability.

## 8. Main Menu asset authority

The current Main Menu production direction is the Phase 2A + exact reference family.

Keep as production sources unless a newer user-approved reference explicitly replaces them:

- background: `phase2a/hol_neon_reference_bg_r3`
- logo: `reference/hol_logo_exact`
- boy: `phase2a/hol_menu_boy_arms_crossed_r3`
- girl: `phase2a/hol_menu_girl_forward_fist_r3`
- number mascots: `reference/mascot_6_exact`, `reference/mascot_7_exact`
- primary CTA: `phase2a/hol_cta_gold_r2_9s`
- secondary CTA: `phase2a/hol_cta_blue_r2_9s`
- competitive/opponent CTA where required: `phase2a/hol_cta_magenta_r2_9s`
- player chip: `phase2a/hol_player_chip_r2_9s`
- settings: `phase2a/hol_settings_gear_r2`
- mode icons: `phase2a/hol_mode_solo_r2`, `phase2a/hol_mode_private_r2`, `phase2a/hol_mode_daily_r2`
- chevron: `phase2a/hol_chevron_r2`

The older `mainmenu/mainmenu_bg_stairs_clouds` family is not the Home background source of truth when the Phase 2A Revision 3 background is available.

## 9. Onboarding rule

Onboarding must look like the same game the player reaches after completion.

Every onboarding page must inherit:

- the HOL logo/character language
- cartoon arcade surfaces
- the same palette roles
- the same typography system
- numbers/sparks/chevrons as controlled accents
- large, obvious progression CTA
- readable, uncluttered data-entry controls

Do not build explanatory marketing boards as onboarding screens. Onboarding is the **actual interactive player setup flow**: nickname, age, identity/preferences where product requirements call for them, confirmation, then entry into the real Main Menu.

Until onboarding production assets are approved and committed, Codex must not invent substitute cartoon art or a procedural robot.

## 10. Presentation ownership

Target architecture: **one screen, one authoritative presentation owner**.

Legacy/global restyle layers may remain as compatibility fallbacks, but they must defer to a screen-specific production visual root and must not overwrite it in `LateUpdate`, delayed coroutines, or repeated generic restyle passes.

For Main Menu Home, `MainMenuHomeVisuals` is the intended screen-specific owner. Its job is to expose the approved production sprites on the existing real controls while preserving callbacks, not to repaint those sprites with procedural substitutes.

## 11. Completion gate

A visual phase is not complete until all of the following are true:

- approved production sprites are visibly used at alpha `1`
- `_9s` sprites use `Image.Type.Sliced`
- no procedural graphic visually replaces an approved production asset
- Greek and English are readable
- safe-area/responsive layouts are verified
- callbacks/navigation remain unchanged
- native-resolution Unity/device capture is compared side-by-side with the approved reference
- EditMode/PlayMode regression is green for the affected scope

The final judgement is the player's screen, not the existence of assets in the Project window.