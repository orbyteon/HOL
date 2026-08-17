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

test("every Unity GUID is unique", () => {
  const seen = new Map();
  for (const path of walk(file("Assets").pathname).filter((name) => name.endsWith(".meta"))) {
    const match = readFileSync(path, "utf8").match(/^guid: ([0-9a-f]{32})$/m);
    assert.ok(match, `${path} has no valid GUID`);
    assert.ok(!seen.has(match[1]), `${path} duplicates GUID from ${seen.get(match[1])}`);
    seen.set(match[1], path);
  }
});
