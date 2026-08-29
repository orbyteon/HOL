# Main Menu approved visual contract

## Source of truth

`01-main-menu-approved.png` is the sole Main Menu layout, hierarchy, color,
surface, spacing, and visual-density reference at its native 941 x 1672
portrait resolution. Runtime geometry is authored on the equivalent 1080 x
1920 canvas.

The only approved visual substitution is the hero pair:

- `Resources/phase2a/hol_menu_boy_arms_crossed_r3`
- `Resources/phase2a/hol_menu_girl_forward_fist_r3`

These two production sprites replace the reference's single pointing boy. The
second supplied Unity screenshot is not a layout reference; it identifies only
the approved boy and girl artwork.

## Runtime truth

- `MainMenuHomeVisuals` is the sole Home presentation and responsive-layout
  authority.
- The real `ButtonPlay`, `ButtonPvP`, `ButtonPrivateRoom`, `DailyHuntButton`,
  and `Buttonsettings` controls retain their production callbacks.
- All titles, subtitles, player values, and speech copy remain live TMP and
  localized. No language is baked into a bitmap.
- The background, frame, logo, player chip, four large mode cards, promo card,
  mascots, and portal-like lower glow follow the approved composition.
- Tall screens deliberately reflow vertical positions; they do not uniformly
  shrink the 1080 x 1920 composition.

## Canonical 1080 x 1920 geometry

The visual owner uses these reference-space blocks:

| Block | Center | Size |
| --- | ---: | ---: |
| Outer frame | (0, 0) | 1080 x 1920 |
| Settings | (-468, 838) | 124 x 124 |
| Player chip | (344, 838) | 395 x 132 |
| HOL logo | (0, 690) | 585 x 310 |
| Hero boy | (-195, 410) | 420 x 420 |
| Hero girl | (82, 405) | 430 x 430 |
| Live speech bubble | (366, 405) | 286 x 178 |
| Solo | (0, 105) | 930 x 190 |
| PvP | (0, -92) | 930 x 180 |
| Friend | (0, -288) | 930 x 180 |
| Daily Hunt | (0, -478) | 930 x 168 |
| Daily promo | (0, -716) | 500 x 190 |
| Bottom portal | (0, -868) | 560 x 130 |
| Mascot 6 | (-414, -765) | 278 x 318 |
| Mascot 7 | (414, -765) | 278 x 318 |

## Acceptance gate

1. Unity compile succeeds in 2022.3.62f3.
2. Main Menu focused PlayMode coverage proves one presentation owner,
   production sprites, callbacks, localization, and measured geometry.
3. Full relevant EditMode/PlayMode/Node regressions remain green.
4. Fresh runtime captures are inspected against the reference before merge.
5. Human visual approval remains required before this branch is merged.
