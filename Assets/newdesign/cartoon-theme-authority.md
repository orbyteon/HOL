# HOL Cartoon Theme Asset Authority Map

This file prevents older asset generations and generic runtime fallbacks from silently replacing the current cartoon production direction.

## HOME / Main Menu

**Authoritative production family**

- `Resources/phase2a/hol_neon_reference_bg_r3.png`
- `Resources/reference/hol_logo_exact.png`
- `Resources/phase2a/hol_menu_boy_arms_crossed_r3.png`
- `Resources/phase2a/hol_menu_girl_forward_fist_r3.png`
- `Resources/reference/mascot_6_exact.png`
- `Resources/reference/mascot_7_exact.png`
- `Resources/reference/player_cyan_exact.png`
- `Resources/phase2a/hol_cta_gold_r2_9s.png`
- `Resources/phase2a/hol_cta_blue_r2_9s.png`
- `Resources/phase2a/hol_cta_magenta_r2_9s.png`
- `Resources/phase2a/hol_player_chip_r2_9s.png`
- `Resources/phase2a/hol_tip_frame_r2_9s.png`
- `Resources/phase2a/hol_settings_gear_r2.png`
- `Resources/phase2a/hol_mode_solo_r2.png`
- `Resources/phase2a/hol_mode_private_r2.png`
- `Resources/phase2a/hol_mode_daily_r2.png`
- `Resources/phase2a/hol_chevron_r2.png`
- `Resources/phase2a/fonts/HOL Menu Display SDF.asset`
- `Resources/phase2a/fonts/HOL Menu Body SDF.asset`

**Legacy / fallback only for Home**

- `Resources/mainmenu/mainmenu_bg_stairs_clouds.png`
- `Resources/mainmenu/mainmenu_tip_frame_9s.png`
- generic `Assets/UI/bg_menu_1080x1920.png`
- procedural CTA/chip/icon geometry when the authoritative production asset above is available

## SPLASH

Keep the dedicated `Resources/splash/` production family plus `Resources/reference/hol_logo_exact.png` and exact/approved character or mascot art referenced by `SplashDesign`.

Splash-specific assets remain authoritative for Splash and must not automatically become Home assets.

## SETTINGS

Keep the dedicated `Resources/settings/` production family and the current `SettingsVisuals` owner. Generic rounded-rect or global palette passes must defer once `SettingsVisualRoot` exists.

## GAMEPLAY / PvP / Daily Hunt

Use the most recent screen-specific approved reference/art family for the target screen. `Resources/reference/` contains approved common identity art such as player/opponent portraits, number mascots and board companion icons.

Do not infer that an older reference-board reskin is globally authoritative merely because it still exists in the repository.

## ONBOARDING

Status: **production asset family not yet committed**.

Mockups define direction but are not Unity production assets by themselves. Before implementation, commit approved assets under a dedicated, clearly named production resource family (recommended: `Resources/onboarding/`) and then add the exact paths to this map.

No procedural robot or invented cartoon character may be used as a substitute for missing onboarding production art.

## RULE

Where this map names an authoritative production asset, the `AGENTS.md` Production UI Asset Fidelity Contract applies: normal-state alpha `1`, `_9s` uses `Image.Type.Sliced`, text remains separate/localized, and procedural rendering may only be additive rather than a replacement.