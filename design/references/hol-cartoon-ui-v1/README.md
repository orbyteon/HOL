# HOL Cartoon UI v1 — approved visual reference set

These images are the immutable visual-direction references supplied and approved by the product owner on 2026-08-24. They are design evidence, not production UI textures and not a source of runtime copy, scores, room codes, rewards, or gameplay state.

All six files are 941 x 1672 PNG screenshots. The seventh supplied image was not copied because it is byte-identical to `05-results-approved.png` (SHA-256 `F1747D0C8C425F93DA2AC7410DC8CC716808D346208F89F51626D931B9136EFD`).

`06-daily-hunt-approved.png` and the pre-existing `../2026-08-24-daily-hunt-approved.png` use different PNG encoding/metadata but decode to identical RGBA pixels (decoded pixel SHA-256 `bb6c0cef69fb272a74aecec41c9d2dd9365c24e0836cc25cb49fd9a0f9bdc7c1`). They are one visual gold, not competing Daily Hunt references.

| Order | File | Screen role | SHA-256 |
|---|---|---|---|
| 01 | `01-home-approved.png` | Main menu / mode selection | `8FAEFBAB84B67D7936B5C408A8AB3D69F79EB389FC31C5622566196C925479C4` |
| 02 | `02-private-room-approved.png` | Create or join a private room | `9A8139989CD346F34219254208DFAF29F4F63DE66232035D155EB50E34AABEE5` |
| 03 | `03-opponent-search-approved.png` | Opponent-search presentation | `AC10048127D35C9508686D5F10E9C61B64C34E8AEDED4122E9EAA7159BDF4FC0` |
| 04 | `04-duel-gameplay-approved.png` | Number-duel gameplay board | `C9F73B5664FB2E9E406E2D20C2763612659E9029AAA6CE32DB61B5F9BF7B2F26` |
| 05 | `05-results-approved.png` | Duel result presentation | `F1747D0C8C425F93DA2AC7410DC8CC716808D346208F89F51626D931B9136EFD` |
| 06 | `06-daily-hunt-approved.png` | Daily Hunt | `5D9A0167D2C9EF90C3FC03F95C1CBA73F97B629A0D31668EC80E36D5DAE36D13` |

## Shared visual grammar

- Deep navy neon-arena background, confetti and focused cyan/magenta/gold light.
- Purple rounded outer frame, HOL logo/header, compact player chip and curved purple title ribbon.
- Cyan and magenta content surfaces, gold primary CTA, cyan or purple secondary CTA.
- Bold white cartoon display typography with dark depth; cyan, magenta and gold semantic accents.
- Approved characters and number mascots used as composition anchors, not as interactive controls.
- Dense vertical composition with deliberate tall-phone reflow; no uniform top-pinned 9:16 scaling.

## Production boundary

- Artwork is composed from reusable sprites; dynamic strings, player names, scores, codes, timers, mission progress and controls remain live TMP/UI elements.
- The screenshots do not authorize fake networking, invented rewards, hard-coded player data, or changed gameplay rules.
- Each screen has one presentation owner. Shared assets and layout helpers are allowed; competing late visual passes are not.
- Missing artwork must be reported or generated as a versioned asset. Do not replace it with procedural placeholder art.
- Geometry assertions are rebaselined only after the six required local screenshots have received human visual approval.
