# HOL avatar resource roster

This directory contains 58 text-free profile-avatar assets. Every avatar is a
1024×1024 RGBA PNG with a transparent background and imports as a single Unity
Sprite. Resource paths below omit the file extension, as required by
`Resources.Load`.

`manifest.json` is the canonical machine-readable roster. IDs and resource
paths are stable contracts for future consumers. Runtime consumers load it as
`Resources.Load<TextAsset>("avatars/manifest")`.

## Human avatars

| ID | Resource path |
|---|---|
| `human_01` | `avatars/humans/avatar_human_01` |
| `human_02` | `avatars/humans/avatar_human_02` |
| `human_03` | `avatars/humans/avatar_human_03` |
| `human_04` | `avatars/humans/avatar_human_04` |
| `human_05` | `avatars/humans/avatar_human_05` |
| `human_06` | `avatars/humans/avatar_human_06` |
| `human_07` | `avatars/humans/avatar_human_07` |
| `human_08` | `avatars/humans/avatar_human_08` |
| `human_09` | `avatars/humans/avatar_human_09` |
| `human_10` | `avatars/humans/avatar_human_10` |
| `human_11` | `avatars/humans/avatar_human_11` |
| `human_12` | `avatars/humans/avatar_human_12` |
| `human_13` | `avatars/humans/avatar_human_13` |
| `human_14` | `avatars/humans/avatar_human_14` |
| `human_15` | `avatars/humans/avatar_human_15` |
| `human_16` | `avatars/humans/avatar_human_16` |
| `human_17` | `avatars/humans/avatar_human_17` |
| `human_18` | `avatars/humans/avatar_human_18` |
| `human_19` | `avatars/humans/avatar_human_19` |
| `human_20` | `avatars/humans/avatar_human_20` |
| `human_21` | `avatars/humans/avatar_human_21` |
| `human_22` | `avatars/humans/avatar_human_22` |
| `human_23` | `avatars/humans/avatar_human_23` |
| `human_24` | `avatars/humans/avatar_human_24` |
| `human_25` | `avatars/humans/avatar_human_25` |
| `human_26` | `avatars/humans/avatar_human_26` |
| `human_27` | `avatars/humans/avatar_human_27` |
| `human_28` | `avatars/humans/avatar_human_28` |
| `human_29` | `avatars/humans/avatar_human_29` |
| `human_30` | `avatars/humans/avatar_human_30` |
| `human_31` | `avatars/humans/avatar_human_31` |
| `human_32` | `avatars/humans/avatar_human_32` |
| `human_33` | `avatars/humans/avatar_human_33` |
| `human_34` | `avatars/humans/avatar_human_34` |
| `human_35` | `avatars/humans/avatar_human_35` |
| `human_36` | `avatars/humans/avatar_human_36` |
| `human_37` | `avatars/humans/avatar_human_37` |
| `human_38` | `avatars/humans/avatar_human_38` |
| `human_39` | `avatars/humans/avatar_human_39` |
| `human_40` | `avatars/humans/avatar_human_40` |

## Group avatars

| ID | Resource path |
|---|---|
| `group_01` | `avatars/groups/avatar_group_01` |
| `group_02` | `avatars/groups/avatar_group_02` |
| `group_03` | `avatars/groups/avatar_group_03` |
| `group_04` | `avatars/groups/avatar_group_04` |
| `group_05` | `avatars/groups/avatar_group_05` |
| `group_06` | `avatars/groups/avatar_group_06` |
| `group_07` | `avatars/groups/avatar_group_07` |
| `group_08` | `avatars/groups/avatar_group_08` |

## Number mascots

| ID | Resource path |
|---|---|
| `number_00` | `avatars/numbers/avatar_number_00` |
| `number_01` | `avatars/numbers/avatar_number_01` |
| `number_02` | `avatars/numbers/avatar_number_02` |
| `number_03` | `avatars/numbers/avatar_number_03` |
| `number_04` | `avatars/numbers/avatar_number_04` |
| `number_05` | `avatars/numbers/avatar_number_05` |
| `number_06` | `avatars/numbers/avatar_number_06` |
| `number_07` | `avatars/numbers/avatar_number_07` |
| `number_08` | `avatars/numbers/avatar_number_08` |
| `number_09` | `avatars/numbers/avatar_number_09` |

The approved number 3 and number 7 references remain canonical:

- Number 3 uses the complete 987×1019 approved source unscaled at `(18, 2)` on
  the transparent 1024×1024 canvas. Its source pixels, including RGB values in
  transparent pixels, are unchanged.
- Number 7 uses the complete 973×1034 approved source, proportionally fitted to
  964×1024 with Pillow LANCZOS resampling and centered at `(30, 0)`. It is not
  redrawn, cropped, generatively extended, or given new accessories.

## Scope boundary

This pack defines resources only. It does not add or alter profile data,
persistence, networking fields, profile creation, Settings controls, avatar
selection UI, or runtime wiring. A later feature may consume the stable
manifest and resource paths.
