// Regression contract for the generated avatar roster, PNGs, and Unity importers.
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

function avatarRoster(directory, idPrefix, resourcePrefix, first, last) {
  return Array.from({ length: last - first + 1 }, (_, offset) => {
    const suffix = String(first + offset).padStart(2, "0");
    return {
      id: `${idPrefix}_${suffix}`,
      resource: `avatars/${directory}/${resourcePrefix}_${suffix}`,
    };
  });
}

function expectedAvatarPngs() {
  return [
    ...avatarRoster("humans", "human", "avatar_human", 1, 40),
    ...avatarRoster("groups", "group", "avatar_group", 1, 8),
    ...avatarRoster("numbers", "number", "avatar_number", 0, 9),
  ].map((entry) => `Assets/newdesign/Resources/${entry.resource}.png`);
}

function walk(directory) {
  return readdirSync(directory).flatMap((name) => {
    const path = join(directory, name);
    return statSync(path).isDirectory() ? walk(path) : [path];
  });
}

test("main menu sprite contract", () => {
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
  assertSprite("Assets/newdesign/Resources/mainmenu/mainmenu_icon_1v1.png", 192, 192);
  assertSprite("Assets/newdesign/Resources/mainmenu/mainmenu_icon_private_room.png", 192, 192);
  assertSprite("Assets/newdesign/Resources/mainmenu/mainmenu_icon_solo.png", 192, 192);
  assertSprite("Assets/newdesign/Resources/mainmenu/mainmenu_icon_streak.png", 128, 128);
  assertSprite("Assets/newdesign/Resources/mainmenu/mainmenu_icon_tip_bulb.png", 160, 160);
  assertSprite("Assets/newdesign/Resources/mainmenu/mainmenu_player_chip_frame_9s.png", 420, 136, [48, 40, 48, 40]);
  assertSprite("Assets/newdesign/Resources/mainmenu/mainmenu_tip_frame_9s.png", 960, 300, [130, 140, 130, 155]);
  assertSprite("Assets/newdesign/Resources/mainmenu/mascot_3_exact.png", 987, 1019);
  assertSprite("Assets/newdesign/Resources/mainmenu/mascot_7_exact.png", 973, 1034);
  assertSprite("Assets/newdesign/Resources/mainmenu/opponent_purple_exact.png",
    965, 1043);
  assertSprite("Assets/newdesign/Resources/mainmenu/player_cyan_exact.png", 1037, 970);
});

test("human avatars 01-10", () => {
  assertAvatarRange("humans", "avatar_human", 1, 10);
});

test("human avatars 11-20", () => {
  assertAvatarRange("humans", "avatar_human", 11, 20);
});

test("human avatars 21-30", () => {
  assertAvatarRange("humans", "avatar_human", 21, 30);
});

test("human avatars 31-40", () => {
  assertAvatarRange("humans", "avatar_human", 31, 40);
});

test("group avatars 01-08", () => {
  assertAvatarRange("groups", "avatar_group", 1, 8);
});

test("number avatars 0-9", () => {
  assertAvatarRange("numbers", "avatar_number", 0, 9);
});

test("all and only avatar sprites use the Android mipmap override", () => {
  const expectedMetas = expectedAvatarPngs().map((path) => file(path + ".meta").pathname).sort();
  const actualMetas = walk(file("Assets/newdesign/Resources/avatars").pathname)
    .filter((path) => path.endsWith(".png.meta"))
    .sort();
  assert.deepEqual(actualMetas, expectedMetas,
    "Android importer contract must cover exactly the 58 manifest avatar sprites");

  for (const path of actualMetas) {
    const meta = readFileSync(path, "utf8");
    assert.match(meta, /^\s*enableMipMap: 1$/m, `${path} must enable mipmaps`);

    const marker = "  - serializedVersion: 3\n    buildTarget: Android\n";
    assert.equal(meta.split(marker).length - 1, 1,
      `${path} must contain exactly one Android platform override`);
    const start = meta.indexOf(marker);
    const end = meta.indexOf("  spriteSheet:", start);
    assert.ok(end > start, `${path} Android platform override must precede spriteSheet`);
    const android = meta.slice(start, end);
    assert.match(android, /^\s*maxTextureSize: 256$/m,
      `${path} Android maxTextureSize must be 256`);
    assert.match(android, /^\s*overridden: 1$/m,
      `${path} Android override must be enabled`);
  }
});

test("avatar manifest contract", () => {
  const manifest = JSON.parse(readFileSync(
    file("Assets/newdesign/Resources/avatars/manifest.json"),
    "utf8"));
  const expected = {
    humans: avatarRoster("humans", "human", "avatar_human", 1, 40),
    groups: avatarRoster("groups", "group", "avatar_group", 1, 8),
    numbers: avatarRoster("numbers", "number", "avatar_number", 0, 9),
  };

  assert.deepEqual(Object.keys(manifest), ["humans", "groups", "numbers"]);
  assert.equal(manifest.humans.length, 40, "manifest humans must contain 40 entries");
  assert.equal(manifest.groups.length, 8, "manifest groups must contain 8 entries");
  assert.equal(manifest.numbers.length, 10, "manifest numbers must contain 10 entries");

  const entries = [...manifest.humans, ...manifest.groups, ...manifest.numbers];
  assert.equal(new Set(entries.map((entry) => entry.id)).size, entries.length,
    "manifest IDs must be unique");
  assert.equal(new Set(entries.map((entry) => entry.resource)).size, entries.length,
    "manifest resource paths must be unique");

  for (const [category, expectedEntries] of Object.entries(expected)) {
    assert.deepEqual(manifest[category], expectedEntries);
    for (const entry of manifest[category]) {
      const png = `Assets/newdesign/Resources/${entry.resource}.png`;
      pngHeader(png);
    }
  }
});

test("every Unity GUID is unique", () => {
  const seen = new Map();
  for (const path of walk(file("Assets").pathname).filter((name) => name.endsWith(".meta"))) {
    const match = readFileSync(path, "utf8").match(/^guid: ([0-9a-f]{32})$/m);
    assert.ok(match, `${path} has no valid GUID`);
    assert.ok(!seen.has(match[1]), `${path} duplicates GUID from ${seen.get(match[1])}`);
    seen.set(match[1], path);
  }
});
