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

const playPngs = [
  "Assets/newdesign/Resources/mainmenu/mainmenu_bg_stairs_clouds.png",
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

test("Play reuses the Home stairs/clouds background", () => {
  const png = read(
    "Assets/newdesign/Resources/mainmenu/mainmenu_bg_stairs_clouds.png");
  assert.deepEqual(dimensions(png), { width: 1080, height: 1920 });
});

test("every cartoon Play asset has a Unity meta", () => {
  for (const path of playPngs) {
    assert.equal(exists(path), true, path);
    assert.equal(exists(path + ".meta"), true, path + ".meta");
  }
});

test("Play art stays out of splash/ and neon arcade", () => {
  assert.equal(
    exists("Assets/newdesign/Resources/splash/mainmenu_bg_stairs_clouds.png"),
    false);
  assert.equal(
    exists("Assets/newdesign/Resources/mainmenu/mainmenu_bg_night_arcade.png"),
    false);
  assert.equal(
    exists("Assets/newdesign/Resources/mainmenu/mainmenu_cta_violet_9s.png"),
    false);
});

test("Play owner source never loads splash resources", () => {
  const owner = "Assets/SCRIPT/Design/MainMenuPlayVisuals.cs";
  if (!exists(owner)) return;
  const source = read(owner).toString("utf8");
  assert.equal(source.includes('Resources.Load("splash/'), false);
  assert.equal(source.includes("splash/"), false);
  assert.match(source, /mainmenu\/mainmenu_bg_stairs_clouds/);
  assert.match(source, /reference\/hol_logo_exact/);
});
