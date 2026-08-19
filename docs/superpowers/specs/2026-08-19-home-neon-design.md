# Neon Solo-First Home Design

## Goal

Replace the current stairs/clouds Home presentation with the supplied dark
neon reference: a solo-first gold CTA, two compact secondary mode cards, the
HOL hero and character group, the player chip, settings gear, and a compact
tip card.

## Scope

In scope:

- `MainMenuHomeVisuals` presentation and layout.
- Existing Home button surfaces and their localization/callbacks.
- Procedural neon backdrop decoration built from the existing Converging Light
  helpers and approved reference sprites.
- Regression coverage for visual ownership, resource use, button hierarchy,
  and portrait-safe layout.
- The Unreleased changelog entry.

Out of scope:

- `MenuManager`, solo gameplay, PvP, Daily Hunt, Settings, or backend logic.
- Store/Profile screens, which are not part of the live HOL product.
- Baking the supplied screenshot into a single image. Text remains live
  localized UI and interactive controls remain scene-owned.

## Design

`MainMenuHomeVisuals` remains the sole Home presentation owner. It will keep
the existing `ButtonPlay`, `ButtonPvP`, `DailyHuntButton`, and
`Buttonsettings` objects and their callbacks, but will reparent and restyle
them inside the existing safe-area root:

1. `HomeBackground` becomes a deep indigo procedural surface rather than the
   stairs/clouds bitmap. `HomeNeonBackdrop` adds non-interactive cyan and
   magenta light seams, faint number texture, and approved decorative stars
   without introducing a new binary asset.
2. The logo remains near the top center. The boy and girl form the central
   hero group beneath it, with mascots 6 and 7 framing the lower sides.
3. `ButtonPlay` becomes the large gold primary CTA with the localized
   `play_solo` label.
4. `ButtonPvP` and `DailyHuntButton` become two equal-width secondary cards
   in one row. They retain cyan and gold/magenta contrast, their existing
   localized labels, and their live callbacks.
5. The settings gear and player chip remain top-left/top-right. The compact
   tip card is moved below the mode cards and keeps live localized copy.
6. All decorative Images keep `raycastTarget = false`; only the four existing
   controls remain interactive. All positions remain in the 1080x1920
   reference space and are clamped by the existing safe-area logic.

## Testing

The PlayMode Home contract will assert:

- `HomeNeonBackdrop` exists, fills the visual root, and does not block
  raycasts.
- The Home background no longer uses the stairs/clouds sprite.
- `ButtonPlay` is the largest mode control and uses the gold sprite.
- Private Room and Daily Hunt are side-by-side, smaller than Play Solo, and
  retain their blue/magenta surfaces and localized labels.
- The logo, hero art, mascots, chip, gear, and tip remain present with
  decorative raycasts disabled.
- No `Home*` object invents a new Button and no Store/Profile panel appears.

The existing Node asset, workflow, CloudScript, provisioner, and Unity CI
contracts remain green. No gameplay or server tests require changes because
the callbacks and feature owners are unchanged.
