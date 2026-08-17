# Task 7 report: number mascots 0–9

## Status

Complete on `cursor/mainmenu-assets-regenerate-7447`. The roster supplies
`avatars/numbers/avatar_number_00` through `avatar_number_09` as transparent
1024×1024 RGBA Unity sprites.

## TDD evidence

- RED commit `95a41fe` added `number avatars 0-9`.
- RED command:
  `node --test --test-name-pattern="number avatars 0-9" tools/test/avatar-assets.test.mjs`
- RED result: failed at the intended missing
  `Assets/newdesign/Resources/avatars/numbers/avatar_number_00.png`.
- GREEN command:
  `node --test --test-name-pattern="number avatars 0-9|GUID" tools/test/avatar-assets.test.mjs`
- GREEN result: 2 tests passed, 0 failed.
- Full `node --test tools/test/avatar-assets.test.mjs`: 8 tests passed,
  0 failed.

## Exact-art composition evidence

- Approved 3 source: `Resources/reference/mascot_3_exact.png`, 987×1019.
  It is placed unscaled at `(18, 2)` on the 1024×1024 transparent canvas.
  An RGBA array comparison confirms the entire source crop, including
  transparent-pixel RGB bytes, is byte-identical.
- Approved 7 source: `Resources/reference/mascot_7_exact.png`, 973×1034.
  Its complete artwork is proportionally fitted to 964×1024 and centered at
  `(30, 0)` using Pillow LANCZOS resampling. An independent deterministic
  reconstruction matches the complete output RGBA canvas.
- No redrawing, generative fill, cropping, or accessory changes were applied
  to either approved reference.

## Visual and alpha QA

- Visual inspection covered each final PNG at full canvas size. Digits
  0, 1, 2, 4, 5, 6, 8, and 9 are distinct friendly characters with clear
  eyes, hands, feet, poses, glossy dimensional bodies, navy edging, and
  tone-on-tone spots matching the approved 3/7 rendering language.
- Each digit silhouette remains immediately readable. Hands and feet remain
  outside defining strokes; there are no props, labels, text, logos, brands,
  or silhouette-obscuring accessories.
- Number 6 received focused inspection: its upper hook and circular lower bowl
  are unobstructed, the wave and hand-on-hip pose stay exterior, and its
  enclosed transparent counter measures 26,834 fully transparent pixels.
- Every image is RGBA 1024×1024, all four corner alpha samples are zero, and
  every nonzero-alpha bounding box remains inside the canvas:

| Digit | Nonzero-alpha bounds `(left, top)–(right, bottom)` | Enclosed transparent regions ≥4,000 px |
|---|---|---|
| 0 | `(57, 37)–(970, 984)` | 42,390 |
| 1 | `(158, 47)–(866, 979)` | none |
| 2 | `(63, 26)–(944, 996)` | none |
| 3 | `(44, 9)–(939, 1020)` | none |
| 4 | `(91, 25)–(931, 992)` | 4,016 |
| 5 | `(107, 27)–(929, 994)` | none |
| 6 | `(88, 16)–(902, 989)` | 26,834 |
| 7 | `(30, 19)–(982, 996)` | none |
| 8 | `(55, 38)–(957, 979)` | 19,890 and 6,024 |
| 9 | `(71, 8)–(972, 981)` | 13,843 |

- SHA-256 checks confirm 10 unique PNG payloads.
- The folder meta and ten PNG metas contain 11 valid, unique GUIDs; the
  repository-wide GUID test also passes.

## Concerns

None.
