# Main Menu and Avatar Asset Pack Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace PR #31's provisional Main Menu layers and add 40 individual human avatars, eight group avatars, and number mascots 0–9 as validated Unity-ready sprites.

**Architecture:** Artwork remains data-only under `Assets/newdesign/Resources/`. A Node integrity test validates the concrete file roster, PNG headers, importer metadata, borders, and GUID uniqueness without Unity; a reflection-free EditMode test verifies that Unity imports every manifest entry as a Sprite. A JSON manifest is the stable contract for the later profile selector, but this PR adds no profile behavior.

**Tech Stack:** Unity 2022.3 Resources, PNG RGBA sprites, Unity TextureImporter `.meta` files, Node 22 built-in test runner, NUnit EditMode tests, image generation with approved reference assets.

## Global Constraints

- Keep this PR assets-only: no profile persistence, profile creation, Settings controls, avatar selection UI, or runtime wiring.
- Human and group avatars must be 1024×1024 RGBA PNGs with transparent backgrounds.
- Number avatars must be 1024×1024 RGBA PNGs; approved exact 3 and 7 remain canonical and are centered without redrawing.
- No baked words, score values, names, flags, language-specific glyphs, real-world insignia, brands, or third-party characters.
- Use lowercase snake-case filenames and stable two-digit numeric identifiers.
- Every asset and folder needs a committed `.meta`; all repository GUIDs must be unique.
- Main Menu full-screen layers use 1080×1920; resizable frames carry valid L/B/R/T `spriteBorder` values.
- Preserve the empty committed `Assets/Resources/HOLReleaseConfig.json`.
- Update `CHANGELOG.md` under Unreleased.

## File Structure

- `Assets/newdesign/Resources/mainmenu/` — regenerated composition layers and exact approved cast.
- `Assets/newdesign/Resources/avatars/humans/` — 40 individual profile portraits.
- `Assets/newdesign/Resources/avatars/groups/` — eight group portraits.
- `Assets/newdesign/Resources/avatars/numbers/` — ten number-mascot portraits.
- `Assets/newdesign/Resources/avatars/manifest.json` — stable resource paths and display-neutral IDs.
- `Assets/Tests/EditMode/AvatarAssetTests.cs` — Unity import/load contract.
- `Assets/Tests/EditMode/AvatarAssetTests.cs.meta` — fixed Unity identity for the test.
- `tools/test/avatar-assets.test.mjs` — editor-independent asset and meta integrity contract.
- `Assets/newdesign/Resources/mainmenu/README.md` — menu layer inventory and integration notes.
- `Assets/newdesign/Resources/avatars/README.md` — avatar roster, naming, and scope boundary.
- `CHANGELOG.md` — Unreleased asset-pack entry.

---

### Task 1: Asset integrity harness and regenerated Main Menu pack

**Files:**
- Create: `tools/test/avatar-assets.test.mjs`
- Replace: `Assets/newdesign/Resources/mainmenu/*.png`
- Add: `Assets/newdesign/Resources/mainmenu/mainmenu_cta_magenta_9s.png`
- Add: `Assets/newdesign/Resources/mainmenu/mainmenu_deco_horizon_overlay.png`
- Add: `Assets/newdesign/Resources/mainmenu/opponent_purple_exact.png`
- Modify/Create: matching `Assets/newdesign/Resources/mainmenu/*.png.meta`
- Modify: `Assets/newdesign/Resources/mainmenu/README.md`

**Interfaces:**
- Produces: `assertSprite(relativePath, width, height, border)` in the Node test for later roster batches.
- Produces: stable Main Menu resource paths under `mainmenu/<filename-without-extension>`.

- [ ] **Step 1: Write the failing Main Menu contract**

Create `tools/test/avatar-assets.test.mjs` with Node built-ins. The helper must:

```js
import assert from "node:assert/strict";
import { readFileSync, readdirSync, statSync } from "node:fs";
import { join } from "node:path";
import test from "node:test";

const root = new URL("../../", import.meta.url);

function file(relativePath) {
  return new URL(relativePath, root);
}

function pngHeader(relativePath) {
  const bytes = readFileSync(file(relativePath));
  assert.deepEqual([...bytes.subarray(0, 8)], [137, 80, 78, 71, 13, 10, 26, 10]);
  return {
    width: bytes.readUInt32BE(16),
    height: bytes.readUInt32BE(20),
    bitDepth: bytes[24],
    colorType: bytes[25],
  };
}

function assertSprite(relativePath, width, height, border = [0, 0, 0, 0]) {
  const header = pngHeader(relativePath);
  assert.equal(header.width, width, relativePath);
  assert.equal(header.height, height, relativePath);
  assert.equal(header.bitDepth, 8, relativePath);
  assert.ok(header.colorType === 4 || header.colorType === 6,
    `${relativePath} must contain an alpha channel`);

  const meta = readFileSync(file(relativePath + ".meta"), "utf8");
  assert.match(meta, /^guid: [0-9a-f]{32}$/m, relativePath);
  assert.match(meta, /^\s*textureType: 8$/m, relativePath);
  assert.match(meta, /^\s*spriteMode: 1$/m, relativePath);
  const expected = `{x: ${border[0]}, y: ${border[1]}, z: ${border[2]}, w: ${border[3]}}`;
  assert.ok(meta.includes("spriteBorder: " + expected), relativePath);
}

function assertAvatarRange(directory, prefix, first, last) {
  for (let index = first; index <= last; index += 1) {
    const suffix = String(index).padStart(2, "0");
    assertSprite(
      `Assets/newdesign/Resources/avatars/${directory}/${prefix}_${suffix}.png`,
      1024,
      1024);
  }
}

function walk(directory) {
  return readdirSync(directory).flatMap((name) => {
    const path = join(directory, name);
    return statSync(path).isDirectory() ? walk(path) : [path];
  });
}
```

Add a `main menu sprite contract` test with this complete inventory:

```js
assertSprite("Assets/newdesign/Resources/mainmenu/hol_logo_exact.png", 1229, 819);
assertSprite("Assets/newdesign/Resources/mainmenu/mainmenu_bg_night_arcade.png", 1080, 1920);
assertSprite("Assets/newdesign/Resources/mainmenu/mainmenu_cta_blue_9s.png", 480, 220, [72, 64, 72, 64]);
assertSprite("Assets/newdesign/Resources/mainmenu/mainmenu_cta_gold_9s.png", 900, 280, [112, 80, 112, 80]);
assertSprite("Assets/newdesign/Resources/mainmenu/mainmenu_cta_violet_9s.png", 480, 220, [72, 64, 72, 64]);
assertSprite("Assets/newdesign/Resources/mainmenu/mainmenu_cta_magenta_9s.png",
  480, 220, [72, 64, 72, 64]);
assertSprite("Assets/newdesign/Resources/mainmenu/mainmenu_daily_hunt_frame_9s.png", 900, 190, [72, 56, 72, 56]);
assertSprite("Assets/newdesign/Resources/mainmenu/mainmenu_deco_confetti_overlay.png", 1080, 1920);
assertSprite("Assets/newdesign/Resources/mainmenu/mainmenu_deco_horizon_overlay.png",
  1080, 1920);
assertSprite("Assets/newdesign/Resources/mainmenu/mainmenu_deco_lightning_overlay.png", 1080, 1920);
assertSprite("Assets/newdesign/Resources/mainmenu/mainmenu_deco_numbers_overlay.png", 1080, 1920);
assertSprite("Assets/newdesign/Resources/mainmenu/mainmenu_deco_stars_overlay.png", 1080, 1920);
assertSprite("Assets/newdesign/Resources/mainmenu/mainmenu_gear_glossy.png", 192, 192);
assertSprite("Assets/newdesign/Resources/mainmenu/mainmenu_gloss_primary_row.png", 1000, 320);
assertSprite("Assets/newdesign/Resources/mainmenu/mainmenu_gloss_secondary_row.png", 1000, 320);
assertSprite("Assets/newdesign/Resources/mainmenu/mainmenu_glow_logo.png", 900, 480);
assertSprite("Assets/newdesign/Resources/mainmenu/mainmenu_glow_primary.png", 980, 380);
assertSprite("Assets/newdesign/Resources/mainmenu/mainmenu_glow_secondary_row.png", 1000, 320);
assertSprite("Assets/newdesign/Resources/mainmenu/mainmenu_icon_daily_hunt.png", 192, 192);
assertSprite("Assets/newdesign/Resources/mainmenu/mainmenu_icon_private_room.png", 192, 192);
assertSprite("Assets/newdesign/Resources/mainmenu/mainmenu_icon_solo.png", 192, 192);
assertSprite("Assets/newdesign/Resources/mainmenu/mainmenu_icon_streak.png", 128, 128);
assertSprite("Assets/newdesign/Resources/mainmenu/mainmenu_icon_tip_bulb.png", 160, 160);
assertSprite("Assets/newdesign/Resources/mainmenu/mainmenu_player_chip_frame_9s.png", 420, 136, [48, 40, 48, 40]);
assertSprite("Assets/newdesign/Resources/mainmenu/mainmenu_tip_frame_9s.png", 960, 300, [80, 72, 80, 72]);
assertSprite("Assets/newdesign/Resources/mainmenu/mascot_3_exact.png", 987, 1019);
assertSprite("Assets/newdesign/Resources/mainmenu/mascot_7_exact.png", 973, 1034);
assertSprite("Assets/newdesign/Resources/mainmenu/opponent_purple_exact.png",
  965, 1043);
assertSprite("Assets/newdesign/Resources/mainmenu/player_cyan_exact.png", 1037, 970);
```

Add a repository-wide GUID test:

```js
test("every Unity GUID is unique", () => {
  const seen = new Map();
  for (const path of walk(file("Assets").pathname).filter((name) => name.endsWith(".meta"))) {
    const match = readFileSync(path, "utf8").match(/^guid: ([0-9a-f]{32})$/m);
    assert.ok(match, `${path} has no valid GUID`);
    assert.ok(!seen.has(match[1]), `${path} duplicates GUID from ${seen.get(match[1])}`);
    seen.set(match[1], path);
  }
});
```

- [ ] **Step 2: Run the contract and verify RED**

Run: `node --test --test-name-pattern="main menu|GUID" tools/test/avatar-assets.test.mjs`

Expected: FAIL because the magenta CTA, horizon overlay, and copied opponent are absent.

- [ ] **Step 3: Generate and normalize the Main Menu layers**

Use the supplied screenshots as style direction and
`Assets/newdesign/Resources/reference/{hol_logo_exact,mascot_3_exact,mascot_7_exact,player_cyan_exact,opponent_purple_exact}.png`
as image references. Generate each chrome layer separately with this fixed prompt:

```text
HOL mobile number-duel game UI asset, polished friendly 2.5D cartoon,
deep indigo night arcade, saturated cyan and magenta neon, muted gold only
for primary action, strong mobile silhouette, clean alpha edge, no text,
no letters, no logo, no score, no watermark, no mockup, isolated asset.
```

Append the exact subject for each existing filename: portrait background,
confetti overlay, lightning overlay, floating-number overlay, star overlay,
horizon-light overlay, gold/cyan/violet/magenta pill control, Daily Hunt panel,
tip panel, player chip, settings gear, solo play, private room, target,
lightbulb, streak, and the three glow/highlight rows. Keep overlays transparent
and resize full-screen outputs to 1080×1920. Preserve the approved exact cast
by byte-copying it from `Resources/reference/`; do not redraw it.

Generate `.meta` files by cloning the current TextureImporter template, replacing
both `guid` and `spriteID` with fresh 32-character lowercase hex values, and
setting these borders:

```text
cta_gold        112,80,112,80
cta_blue         72,64,72,64
cta_violet       72,64,72,64
cta_magenta      72,64,72,64
daily_hunt       72,56,72,56
player_chip      48,40,48,40
tip_frame        80,72,80,72
all others        0, 0, 0, 0
```

Update the README inventory and explicitly state that labels remain live TMP.

- [ ] **Step 4: Verify GREEN and visually inspect**

Run:

```bash
node --test --test-name-pattern="main menu|GUID" tools/test/avatar-assets.test.mjs
git diff --check
```

Expected: PASS. Build a contact sheet and inspect for opaque mattes, baked copy,
cropped silhouettes, inconsistent lighting, and illegible mobile-scale icons.

- [ ] **Step 5: Commit**

```bash
git add tools/test/avatar-assets.test.mjs Assets/newdesign/Resources/mainmenu
git commit -m "assets(mainmenu): rebuild neon arcade layer pack"
```

### Task 2: Human avatars 01–10

**Files:**
- Modify: `tools/test/avatar-assets.test.mjs`
- Create: `Assets/newdesign/Resources/avatars.meta`
- Create: `Assets/newdesign/Resources/avatars/humans.meta`
- Create: `Assets/newdesign/Resources/avatars/humans/avatar_human_01.png` through `avatar_human_10.png`
- Create: matching `.png.meta` files

**Interfaces:**
- Produces resource paths `avatars/humans/avatar_human_01` through `_10`.

- [ ] **Step 1: Add the failing batch test**

Add:

```js
test("human avatars 01-10", () => {
  assertAvatarRange("humans", "avatar_human", 1, 10);
});
```

- [ ] **Step 2: Verify RED**

Run: `node --test --test-name-pattern="human avatars 01-10" tools/test/avatar-assets.test.mjs`

Expected: FAIL at `avatar_human_01.png`.

- [ ] **Step 3: Generate the first ten individual portraits**

Generate one transparent square portrait per subject, using
`player_cyan_exact.png` and `opponent_purple_exact.png` as style references:

```text
01 child boy, enthusiastic gamer, handheld controller
02 child girl, curious science learner, safe toy telescope
03 child boy, cheerful painter, brush and palette
04 child girl, happy reader, closed storybook
05 child girl, football player, ball under arm
06 child boy, beginner musician, small drum
07 child girl using a wheelchair, bright explorer outfit
08 child boy, nature enthusiast, binoculars
09 teenage girl, skateboarder with helmet
10 teenage boy, coder with generic tablet
```

Use head-and-shoulders or waist-up framing, transparent background, no text or
logos. Normalize every output to 1024×1024 RGBA and create unique Sprite metas.

- [ ] **Step 4: Verify GREEN**

Run: `node --test --test-name-pattern="human avatars 01-10|GUID" tools/test/avatar-assets.test.mjs`

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add tools/test/avatar-assets.test.mjs Assets/newdesign/Resources/avatars.meta Assets/newdesign/Resources/avatars/humans.meta Assets/newdesign/Resources/avatars/humans
git commit -m "assets(avatars): add first human portrait set"
```

### Task 3: Human avatars 11–20

**Files:**
- Modify: `tools/test/avatar-assets.test.mjs`
- Create: `Assets/newdesign/Resources/avatars/humans/avatar_human_11.png` through `avatar_human_20.png`
- Create: matching `.png.meta` files

**Interfaces:**
- Produces resource paths `avatars/humans/avatar_human_11` through `_20`.

- [ ] **Step 1: Add and run the failing `human avatars 11-20` test**

Add:

```js
test("human avatars 11-20", () => {
  assertAvatarRange("humans", "avatar_human", 11, 20);
});
```

Then run:

`node --test --test-name-pattern="human avatars 11-20" tools/test/avatar-assets.test.mjs`

Expected: FAIL at `avatar_human_11.png`.

- [ ] **Step 2: Generate and normalize portraits**

Use the same prompt, references, framing, and meta contract for:

```text
11 teenage girl, energetic dancer
12 teenage boy, guitarist with unbranded acoustic guitar
13 teenage boy, basketball player
14 teenage girl, photographer with generic camera
15 teenager with neutral presentation, student with backpack
16 teenage girl, community volunteer carrying a small plant
17 adult woman, doctor with generic stethoscope
18 adult man, nurse in unbranded scrubs
19 adult woman, teacher holding notebooks
20 adult man, scientist wearing generic safety goggles
```

- [ ] **Step 3: Verify and commit**

```bash
node --test --test-name-pattern="human avatars 11-20|GUID" tools/test/avatar-assets.test.mjs
git add tools/test/avatar-assets.test.mjs Assets/newdesign/Resources/avatars/humans
git commit -m "assets(avatars): add teen and healthcare portraits"
```

Expected: PASS before commit.

### Task 4: Human avatars 21–30

**Files:**
- Modify: `tools/test/avatar-assets.test.mjs`
- Create: `Assets/newdesign/Resources/avatars/humans/avatar_human_21.png` through `avatar_human_30.png`
- Create: matching `.png.meta` files

**Interfaces:**
- Produces resource paths `avatars/humans/avatar_human_21` through `_30`.

- [ ] **Step 1: Add and run the failing `human avatars 21-30` test**

Add `assertAvatarRange("humans", "avatar_human", 21, 30)` in a test named
`human avatars 21-30`.

Expected: FAIL at `avatar_human_21.png`.

- [ ] **Step 2: Generate and normalize portraits**

```text
21 adult woman, engineer with generic hard hat and rolled blueprint
22 adult man, firefighter in generic protective gear
23 adult woman, chef with plain apron
24 adult man, builder with generic tool belt
25 adult woman, farmer holding vegetables
26 adult man, pilot in unbranded uniform
27 adult woman, astronaut in unbranded suit
28 adult man, visual artist holding sketchbook
29 adult woman, musician holding violin
30 adult man, runner wearing unbranded sportswear
```

- [ ] **Step 3: Verify and commit**

```bash
node --test --test-name-pattern="human avatars 21-30|GUID" tools/test/avatar-assets.test.mjs
git add tools/test/avatar-assets.test.mjs Assets/newdesign/Resources/avatars/humans
git commit -m "assets(avatars): add professional portrait set"
```

Expected: PASS before commit.

### Task 5: Human avatars 31–40

**Files:**
- Modify: `tools/test/avatar-assets.test.mjs`
- Create: `Assets/newdesign/Resources/avatars/humans/avatar_human_31.png` through `avatar_human_40.png`
- Create: matching `.png.meta` files

**Interfaces:**
- Produces resource paths `avatars/humans/avatar_human_31` through `_40`.

- [ ] **Step 1: Add and run the failing `human avatars 31-40` test**

Add `assertAvatarRange("humans", "avatar_human", 31, 40)` in a test named
`human avatars 31-40`.

Expected: FAIL at `avatar_human_31.png`.

- [ ] **Step 2: Generate and normalize portraits**

```text
31 adult woman, paramedic with generic first-aid bag and no emblem
32 adult man, gardener holding small watering can
33 senior woman, professor with glasses and closed book
34 senior man, craftsperson holding a carved wooden toy
35 senior woman, baker with plain apron and bread
36 senior man, sailor in generic weather jacket
37 senior woman, gardener holding flowers
38 senior man, chess enthusiast holding an unmarked piece
39 senior woman, musician holding a small ukulele
40 senior man, cheerful traveler with plain camera and hat
```

- [ ] **Step 3: Verify and commit**

```bash
node --test --test-name-pattern="human avatars 31-40|GUID" tools/test/avatar-assets.test.mjs
git add tools/test/avatar-assets.test.mjs Assets/newdesign/Resources/avatars/humans
git commit -m "assets(avatars): complete individual portrait roster"
```

Expected: PASS before commit.

### Task 6: Eight group avatars

**Files:**
- Modify: `tools/test/avatar-assets.test.mjs`
- Create: `Assets/newdesign/Resources/avatars/groups.meta`
- Create: `Assets/newdesign/Resources/avatars/groups/avatar_group_01.png` through `avatar_group_08.png`
- Create: matching `.png.meta` files

**Interfaces:**
- Produces resource paths `avatars/groups/avatar_group_01` through `_08`.

- [ ] **Step 1: Add and run the failing `group avatars 01-08` test**

Add `assertAvatarRange("groups", "avatar_group", 1, 8)` in a test named
`group avatars 01-08`. Expected: FAIL at `avatar_group_01.png`.

- [ ] **Step 2: Generate and normalize group portraits**

Keep every face readable inside a circular profile crop:

```text
01 warm three-generation family, four people
02 diverse group of four school-age friends
03 diverse group of five teenage friends
04 healthcare coworkers, three adults
05 science and engineering team, four adults
06 cheerful mixed-age music and art group, four people
07 inclusive amateur sports team, five people including wheelchair athlete
08 senior community friends, four people
```

- [ ] **Step 3: Verify and commit**

```bash
node --test --test-name-pattern="group avatars 01-08|GUID" tools/test/avatar-assets.test.mjs
git add tools/test/avatar-assets.test.mjs Assets/newdesign/Resources/avatars/groups.meta Assets/newdesign/Resources/avatars/groups
git commit -m "assets(avatars): add group portrait roster"
```

Expected: PASS before commit.

### Task 7: Number mascots 0–9

**Files:**
- Modify: `tools/test/avatar-assets.test.mjs`
- Create: `Assets/newdesign/Resources/avatars/numbers.meta`
- Create: `Assets/newdesign/Resources/avatars/numbers/avatar_number_00.png` through `avatar_number_09.png`
- Create: matching `.png.meta` files

**Interfaces:**
- Produces resource paths `avatars/numbers/avatar_number_00` through `_09`.

- [ ] **Step 1: Add and run the failing `number avatars 0-9` test**

Add `assertAvatarRange("numbers", "avatar_number", 0, 9)` in a test named
`number avatars 0-9`. Expected: FAIL at `avatar_number_00.png`.

- [ ] **Step 2: Generate number characters**

Generate 0, 1, 2, 4, 5, 6, 8, and 9 as friendly personified digits with eyes,
hands, feet, distinct poses, and no accessory that obscures the digit. Use the
approved 3 and 7 as style references. For 3 and 7, proportionally fit and center
the approved artwork from `Resources/reference/mascot_3_exact.png` and
`mascot_7_exact.png` on transparent 1024×1024 canvases without redrawing.

- [ ] **Step 3: Verify and commit**

```bash
node --test --test-name-pattern="number avatars 0-9|GUID" tools/test/avatar-assets.test.mjs
git add tools/test/avatar-assets.test.mjs Assets/newdesign/Resources/avatars/numbers.meta Assets/newdesign/Resources/avatars/numbers
git commit -m "assets(avatars): add complete number mascot roster"
```

Expected: PASS before commit.

### Task 8: Manifest, Unity import test, documentation, and final verification

**Files:**
- Create: `Assets/newdesign/Resources/avatars/manifest.json`
- Create: `Assets/newdesign/Resources/avatars/manifest.json.meta`
- Create: `Assets/newdesign/Resources/avatars/README.md`
- Create: `Assets/newdesign/Resources/avatars/README.md.meta`
- Create: `Assets/Tests/EditMode/AvatarAssetTests.cs`
- Create: `Assets/Tests/EditMode/AvatarAssetTests.cs.meta`
- Modify: `tools/test/avatar-assets.test.mjs`
- Modify: `CHANGELOG.md`

**Interfaces:**
- Produces manifest schema:
  `{"humans":[{"id":"human_01","resource":"avatars/humans/avatar_human_01"}],"groups":[...],"numbers":[...]}`.

- [ ] **Step 1: Add the failing manifest contract**

Add a Node test that loads `manifest.json`, asserts counts 40/8/10, IDs unique,
resource paths unique, and every resource maps to a tested `.png`.

Run: `node --test --test-name-pattern="manifest" tools/test/avatar-assets.test.mjs`

Expected: FAIL because `manifest.json` does not exist.

- [ ] **Step 2: Create the manifest and make the Node contract pass**

List every resource path in numeric order using the exact schema above.

Run: `node --test tools/test/avatar-assets.test.mjs`

Expected: all asset contract tests PASS.

- [ ] **Step 3: Write the Unity load test before its first Unity run**

Create `AvatarAssetTests.cs`:

```csharp
using System;
using NUnit.Framework;
using UnityEngine;

public class AvatarAssetTests
{
    [Serializable]
    class Entry { public string id; public string resource; }
    [Serializable]
    class Manifest
    {
        public Entry[] humans;
        public Entry[] groups;
        public Entry[] numbers;
    }

    [Test]
    public void EveryProfileAvatarImportsAsSprite()
    {
        var text = Resources.Load<TextAsset>("avatars/manifest");
        Assert.IsNotNull(text);
        var manifest = JsonUtility.FromJson<Manifest>(text.text);
        AssertEntries(manifest.humans, 40);
        AssertEntries(manifest.groups, 8);
        AssertEntries(manifest.numbers, 10);
    }

    static void AssertEntries(Entry[] entries, int expectedCount)
    {
        Assert.AreEqual(expectedCount, entries.Length);
        foreach (var entry in entries)
            Assert.IsNotNull(Resources.Load<Sprite>(entry.resource),
                entry.resource + " did not import as a Sprite.");
    }
}
```

Create a standard MonoImporter `.meta` with a fresh unique GUID.

- [ ] **Step 4: Complete docs and changelog**

Document all IDs, resource paths, 1024×1024 contract, exact 3/7 treatment, and
the explicit no-runtime-wiring boundary in `avatars/README.md`. Add an
Unreleased/Added changelog entry for the 58-avatar and Main Menu asset pack.

- [ ] **Step 5: Run the strongest local gates**

Run:

```bash
node --test tools/test/*.test.mjs
git diff --check
git status --short
```

Expected: all Node tests PASS and no whitespace errors.

Stub-compile every file under `Assets/SCRIPT` together against the established
Unity/TMP stubs using `mcs`, after the final C# edit. Expected: zero compiler
errors. The new EditMode test itself is validated by Unity CI because the local
stub gate intentionally covers `Assets/SCRIPT`, not the test assembly.

- [ ] **Step 6: Final visual review**

Create contact sheets for:

- Main Menu layers;
- humans 01–20;
- humans 21–40;
- groups and number mascots.

Reject and regenerate any asset with baked text, unwanted backgrounds, black
mattes, duplicated faces, malformed hands, cropped silhouettes, brand marks,
style drift, or poor readability at a 128×128 preview.

- [ ] **Step 7: Commit and push**

```bash
git add Assets/newdesign/Resources/avatars Assets/Tests/EditMode/AvatarAssetTests.cs Assets/Tests/EditMode/AvatarAssetTests.cs.meta tools/test/avatar-assets.test.mjs CHANGELOG.md
git commit -m "test(assets): lock avatar roster and Unity imports"
git push -u origin cursor/mainmenu-assets-regenerate-7447
```

- [ ] **Step 8: Verify CI authority**

Check PR #31 and require green results from Static integrity, Duel rule tests,
Provisioner tests, EditMode tests, Exact visuals PlayMode, and Android compile.
If any job fails, inspect its log, reproduce locally where possible, fix in a
new commit, rerun the strongest local gates, push, and recheck. Do not merge or
mark ready for review without the owner's explicit instruction.
