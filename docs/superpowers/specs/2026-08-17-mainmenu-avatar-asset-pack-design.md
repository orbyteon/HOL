# Main Menu and Avatar Asset Pack Design

## Goal

Replace the provisional Main Menu art in PR #31 with a coherent, text-free
Unity asset pack based on the supplied neon arcade references. Extend the pack
with a broad avatar library that can support a future profile-picture selector.
This change delivers assets only; profile creation and Settings behavior remain
outside this PR.

## Visual Direction

The pack uses a polished, friendly cartoon style over deep indigo night-arcade
backgrounds. Cyan, magenta, violet, and muted gold provide the primary accents,
with glow used deliberately around silhouettes and controls. Assets must remain
readable at mobile sizes and must not contain baked English or Greek copy.

Existing approved exact reference art is retained where available. Newly
generated art must match its approachable proportions, strong silhouettes,
soft dimensional shading, and saturated neon palette without reproducing
third-party characters, brands, or logos.

## Deliverables

### Main Menu layers

- One portrait night-arcade background.
- Separate transparent overlays for numbers, stars, confetti, lightning,
  horizon light, and related decorative effects.
- The approved HOL logo and existing approved exact characters.
- Text-free gold, cyan/blue, violet, and magenta controls and panels.
- Sliced button, profile-chip, Daily Hunt, and tip-panel frames.
- Settings, solo, private-room, Daily Hunt, tip, streak, and supporting icons.
- Separate glows and highlights where runtime composition benefits from them.

### Number mascots

Create one square transparent portrait for each digit from 0 through 9. Each
digit is a recognizable character with a distinct pose or personality while
sharing the same rendering style and framing. Existing approved exact number
characters remain the canonical versions of those digits.

### Individual human avatars

Provide 40 square transparent avatars spanning:

- children, teenagers, adults, and seniors;
- varied skin tones, hair, facial features, body presentation, and mobility;
- a balanced range of masculine, feminine, and neutral presentation;
- everyday identities plus recognizable generic roles such as student,
  athlete, musician, artist, teacher, scientist, engineer, healthcare worker,
  firefighter, chef, builder, farmer, pilot, astronaut, and gardener.

Children are represented through age-appropriate interests and activities,
not occupations. Professional clothing and props remain generic and contain no
real-world insignia or trademarks.

### Group avatars

Provide eight additional square transparent portraits containing groups of two
to five people. Groups vary in age and composition and read clearly within the
same profile-picture crop as individual avatars.

## Asset Contract

- Avatar source images are 1024 by 1024 RGBA PNGs with transparent backgrounds.
- Main Menu layers use dimensions appropriate to their runtime purpose; the
  background and full-screen overlays share the same portrait aspect ratio.
- Filenames use lowercase snake case and stable numeric identifiers.
- No user-facing words, score values, names, flags, or language-specific glyphs
  are baked into images.
- Every asset and new folder has a committed Unity `.meta` file.
- Every GUID is unique across the repository.
- Resizable frames import as Sprites with explicit L/B/R/T `spriteBorder`
  values; other sprites use zero borders.
- Alpha edges must not contain opaque black mattes.
- The committed production configuration remains untouched.

Resource paths:

- `Assets/newdesign/Resources/mainmenu/` for menu composition layers.
- `Assets/newdesign/Resources/avatars/humans/` for 40 individual avatars.
- `Assets/newdesign/Resources/avatars/groups/` for eight group avatars.
- `Assets/newdesign/Resources/avatars/numbers/` for number mascots 0–9.

## Scope Boundary

This PR does not add profile data, persistence, network fields, profile
creation, Settings controls, avatar selection UI, or runtime wiring. A later
feature PR can consume the stable resource paths defined here.

## Validation

- Verify the expected roster and all required Main Menu files exist.
- Verify every source asset has a matching `.meta`.
- Verify all GUIDs are unique.
- Verify images decode as RGBA PNGs, required avatar dimensions are exact, and
  transparent assets contain usable alpha.
- Verify 9-slice borders are nonzero and fit inside their source dimensions.
- Add a reflection-compatible Unity EditMode integrity test that loads the
  resource roster as Sprites.
- Run the full `Assets/SCRIPT` stub compile after the last C# edit.
- Run repository static integrity, EditMode tests, and Android compile CI.

## Acceptance Criteria

The pack is complete when all 58 avatar choices (40 people, eight groups, ten
number mascots) and all documented Main Menu layers are present, importable,
text-free, visually coherent, and validated by the asset contract. The PR
remains assets-only and is not considered mergeable until every required CI job
is green.
