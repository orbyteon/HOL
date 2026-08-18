# HOL Splash 6–7 Design Specification

> **Status — Superseded (2026-08-18):** Superseded by the 2026-08-18 cartoon
> stairs/clouds Splash (mascot 6 left / 7 right, boy + girl, no neon-grid).
> The live contract is `SplashDesign` +
> `Resources/splash/splash_bg_stairs_clouds` and related cartoon assets.

**Status:** Approved in conversation on 2026-08-17, with the requested
replacement of mascot 3 by mascot 6 so the pair reads left-to-right as 6–7.

## Goal

Replace the current competing Splash presentation layers with one authoritative,
safe-area-aware branded Splash screen that expresses HOL's neon arcade identity
and the 6–7 mascot pairing while preserving the existing loading behavior
exactly.

This is the first page in a page-by-page product workflow. Main Menu, Profile,
Settings, Store, and PvP page redesigns are explicitly out of scope until this
Splash receives visual approval.

## Product Truth

The current Splash has only two real behaviors:

1. It automatically loads `MainMenu` after the serialized `2.5` seconds.
2. A tap/click anywhere skips immediately to `MainMenu`.

The Splash has no real async progress, login, provisioning, network status,
profile, score, currency, retry, update, or error flow. The redesign must not
suggest or invent any of those systems.

The thin gold line is an elapsed-duration indicator tied to the existing
`SplashLoader.waitTime`; it is not presented as file/network progress and has no
percentage label.

## Brand Interpretation

The supplied references establish a visual system rather than a set of product
features:

- deep indigo arcade depth;
- cyan and magenta edge light;
- scarce gold reserved for the progress destination;
- oversized smiling HOL logo;
- glossy, friendly number mascots;
- energetic stars/confetti/lightning without cluttering the central subject;
- 1080×1920 portrait composition with transparent runtime layers.

The Splash uses mascot **6 on the left** and mascot **7 on the right**, so the
pair reads naturally as **6–7**. This is a visual cultural wink only. The screen
contains no `six seven` text, TikTok reference, music reference, basketball
reference, branded personality, or new gameplay mechanic.

## Recommended Composition

### Full-bleed layer

`SplashBackground` fills the physical display, including cutout insets. It is a
dedicated 1080×1920 opaque text-free PNG:

- darkest indigo in the center for logo readability;
- controlled cyan light from the right and magenta light from the left;
- sparse edge lightning, stars, confetti, and a faint lower arcade-grid depth;
- no logo, characters, words, controls, numbers, or fake UI baked into it.

The background is visually related to Main Menu references but lives under
`Resources/splash/`; Splash must not load from `Resources/mainmenu/`.

### Safe content layer

`SplashSafeAreaRoot` uses normalized anchors derived from `Screen.safeArea`.
Content is authored against a 1080×1920 reference and aspect-fitted inside that
safe rectangle.

Recommended reference layout:

| Element | Center | Size |
|---|---:|---:|
| Logo glow | `(0, +260)` | `960×620` |
| HOL logo | `(0, +260)` | `820×546` |
| Mascot 6 | `(-285, -330)` | `270×350` |
| Mascot 7 | `(+285, -330)` | `250×350` |
| Progress track | `(0, -770)` | `480×8` |

On shorter safe areas, the complete safe-content group scales uniformly. It
must never stretch the logo or mascots non-uniformly.

### Layer order

```text
/Canvas
└── SplashVisualRoot
    ├── SplashBackground
    └── SplashSafeAreaRoot
        ├── SplashLogoGlow
        ├── SplashLogo
        ├── SplashMascotSix
        ├── SplashMascotSeven
        └── SplashProgressTrack
            └── SplashProgressFill
```

All generated `Image` components are non-raycast. The existing whole-screen
tap gesture remains owned by `SplashLoader`; no Button is created.

## Artwork Contract

### Reuse

- `Resources/reference/hol_logo_exact`
- `Resources/reference/mascot_7_exact`
- the already-approved number-six raster from PR #31
  (`avatars/numbers/avatar_number_06`) copied byte-for-byte into this isolated
  feature as `Resources/reference/mascot_6_exact` with a **new Unity GUID** to
  avoid a future collision when the avatar pack is rebased.

The existing number-six raster already matches the user-supplied blue mascot
reference and does not need regeneration.

### Generate

- `Resources/splash/splash_bg_neon_arcade.png`: dedicated 1080×1920 opaque
  text-free background.
- `Resources/splash/splash_logo_glow.png`: transparent cyan/magenta radial
  bloom sized for the approved HOL logo.

Every PNG and folder receives a committed `.meta`; Sprite importers use
`textureType: Sprite`, single mode, alpha transparency where applicable,
mipmaps off, clamp, and Android max size 2048.

No Main Menu CTA, player chip, profile, currency, avatar selector, or 1V1 asset
is loaded on Splash.

## Runtime Ownership

`SplashDesign` becomes the sole Splash presentation owner:

- builds/reuses exactly one `SplashVisualRoot`;
- hides the serialized legacy `Panel` Image and `Image` logo without
  destroying them;
- loads only the approved shared reference and `splash/` resources;
- animates and updates only presentation;
- does not load scenes or handle taps.

The global installers for `ExactReferenceVisuals`,
`AttachmentReskinVisuals`, `AttachmentReskinPolish`, and
`AttachmentReskinCanvasBindings` explicitly skip `SplashScene`. They continue
their existing behavior in `MainMenu`.

No new Canvas is created. The scene-authored root Canvas, CanvasScaler,
GraphicRaycaster, EventSystem, and `SplashLoader` remain.

## Animation

- Logo entrance: `0.65s`, alpha `0→1`, uniform scale `0.86→1.0`, ease-out.
- Mascots: enter with the same fade and a smaller `0.92→1.0` scale; mascot 6
  begins with the logo and mascot 7 begins `0.08s` later.
- After entrance, logo breathing is limited to ±1% uniform scale.
- Background and mascots remain otherwise static to avoid visual noise during
  a short 2.5-second screen.
- Progress fill is monotonic from 0 to 1 over `SplashLoader.waitTime`.

Animations use unscaled delta time. The scene transition remains controlled by
the existing `SplashLoader`.

## Navigation and Failure Behavior

- Automatic route remains `SplashScene → MainMenu`.
- Tap/click skip remains available over the entire screen.
- The existing duplicate-load guard remains.
- Android Back has no Splash-specific action.
- Missing optional art logs an error but never blocks the transition.
- No retry or error dialog is added because no such product flow exists.

## Localization and Accessibility

The approved Splash contains no user-facing text, so no new L10n keys are
needed. It is visually identical in EN and EL.

There are no interactive controls to label. Decorative images are
non-raycast, and high-contrast logo/progress treatment remains legible without
depending on text.

## QA-only Android Capture

The page checkpoint needs deterministic evidence without changing production
behavior:

- the exact Android intent extra `hol_capture_screen=splash` activates only in
  a Development Android player and changes that run's Splash timeout from
  `2.5s` to `30s`;
- this path is compiled out or inert in non-Development builds;
- it emits the exact marker `HOL_SPLASH_CAPTURE_READY` after the entrance
  animation settles;
- it does not change bundle version/versionCode, production config, signing,
  release workflows, or the normal 2.5-second path.

The PR must produce:

- one 1080×1920 Android screenshot;
- an ARM64-capable debug APK built from the exact checkpoint SHA;
- APK and screenshot SHA-256 values;
- package metadata and logcat.

## Automated Acceptance

Tests must prove:

1. the approved logo, mascot 6, mascot 7, background, and glow load as Sprites;
2. exactly one active Splash visual root/backdrop/logo/progress track exists;
3. no global Main Menu/exact/attachment presenter owns Splash;
4. no Button, profile, currency, score, login, retry, or 1V1 object is created;
5. 6 is left of 7 and both remain inside the safe content bounds;
6. logo and mascots preserve aspect ratio;
7. all decorative images are non-raycast;
8. progress is monotonic and tracks the existing 2.5-second duration;
9. tap skip and automatic transition both reach `MainMenu` once;
10. missing optional art does not block navigation;
11. the normal MainMenu presentation behavior remains unchanged;
12. the Android screenshot is exactly 1080×1920 and unobstructed.

Unity PlayMode tests continue to reach game types through reflection only.

## Scope Boundaries

Allowed changes are limited to Splash presentation, Splash resource assets,
tests, QA capture tooling/workflow, and the Unreleased changelog.

Explicitly prohibited:

- Main Menu redesign or asset changes;
- Profile, Settings, Store, or PvP page work;
- currency, score, rank, inventory, account, or backend features;
- scene-flow changes beyond preserving the existing Splash transition;
- `ProjectSettings.asset`, versionCode, release, PlayFab, provisioner, ads,
  minVersion, or Play Console changes;
- merge, deploy, release, or Google Play upload.

## Approval Gate

After local gates and green CI, work stops with the Splash branch and PR still
unmerged. The owner receives the exact SHA, changed files, CI links, ARM64 APK,
and Android screenshot. No second page begins until the Splash receives
explicit visual approval.
