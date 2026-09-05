import assert from "node:assert/strict";
import crypto from "node:crypto";
import fs from "node:fs";
import test from "node:test";

const root = new URL("../../", import.meta.url);
const read = relative => fs.readFileSync(new URL(relative, root));
const exists = relative => fs.existsSync(new URL(relative, root));
const dimensions = png => ({
  width: png.readUInt32BE(16),
  height: png.readUInt32BE(20),
});

const approvedBackground =
  "Assets/newdesign/Resources/solo/production/solo_background_v1.png";
const rejectedCloudBackground =
  "Assets/newdesign/Resources/mainmenu/mainmenu_bg_stairs_clouds.png";

const playPngs = [
  approvedBackground,
  "Assets/newdesign/Resources/mainmenu/mainmenu_deco_stars.png",
  "Assets/newdesign/Resources/mainmenu/mainmenu_cta_gold_9s.png",
  "Assets/newdesign/Resources/mainmenu/mainmenu_cta_blue_9s.png",
  "Assets/newdesign/Resources/mainmenu/mainmenu_tip_frame_9s.png",
  "Assets/newdesign/Resources/mainmenu/mainmenu_icon_solo.png",
  "Assets/newdesign/Resources/mainmenu/mainmenu_icon_tip_bulb.png",
  "Assets/newdesign/Resources/reference/hol_logo_exact.png",
];

test("approved number six stays byte-exact", () => {
  const png = read("Assets/newdesign/Resources/reference/mascot_6_exact.png");
  assert.equal(crypto.createHash("sha256").update(png).digest("hex"),
    "067beafc207aea302e0993a3bacdb2b69478429aa3685f275bb6705bd902ac4b");
});

test("Play reuses approved Solo background with canonical 941x1672 portrait geometry", () => {
  const png = read(approvedBackground);
  const { width, height } = dimensions(png);
  assert.equal(png.subarray(0, 8).toString("hex"), "89504e470d0a1a0a",
    "Play background must be a PNG");
  assert.equal(png[24], 8, "Play background must stay 8-bit");
  // 04-solo-duel-approved-contract.md fixes the source at 941x1672.
  assert.deepEqual({ width, height }, { width: 941, height: 1672 });
  assert.ok(width >= 900,
    `Play background width must stay production-resolution; got ${width}`);
  assert.ok(height >= 1600,
    `Play background height must stay production-resolution; got ${height}`);
  assert.ok(Math.abs(width / height - 9 / 16) <= 0.002,
    `Play background must stay approximately 9:16; got ${width}x${height}`);
  assert.ok(png[25] === 2 || png[25] === 6,
    "Play background must be RGB or RGBA");
});

test("every current cartoon Play asset has a Unity meta", () => {
  for (const path of playPngs) {
    assert.equal(exists(path), true, path);
    assert.equal(exists(path + ".meta"), true, path + ".meta");
  }
});

test("rejected Play backgrounds and obsolete CTA stay deleted", () => {
  assert.equal(exists(rejectedCloudBackground), false, rejectedCloudBackground);
  assert.equal(exists(rejectedCloudBackground + ".meta"), false,
    rejectedCloudBackground + ".meta");
  assert.equal(
    exists("Assets/newdesign/Resources/mainmenu/mainmenu_bg_night_arcade.png"),
    false);
  assert.equal(
    exists("Assets/newdesign/Resources/mainmenu/mainmenu_cta_violet_9s.png"),
    false);
});

test("Play owner uses approved Solo art and never loads Splash or retired backgrounds", () => {
  const owner = "Assets/SCRIPT/Design/MainMenuPlayVisuals.cs";
  assert.equal(exists(owner), true, owner);
  const source = read(owner).toString("utf8");
  assert.equal(source.includes('Resources.Load("splash/'), false);
  assert.equal(source.includes("splash/"), false);
  assert.match(source,
    /const string BackgroundResource\s*=\s*"solo\/production\/solo_background_v1";/);
  assert.equal(source.includes("phase2a/hol_neon_reference_bg_r3"), false);
  assert.equal(source.includes("settings/hol_settings_bg_r1"), false);
  assert.equal(source.includes("mainmenu_bg_stairs_clouds"), false);
  assert.match(source, /reference\/hol_logo_exact/);
});
