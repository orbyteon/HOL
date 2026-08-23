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
const colorType = png => png[25];

const approvedBackground =
  "Assets/newdesign/Resources/phase2a/hol_neon_reference_bg_r3.png";
const rejectedCloudBackground =
  "Assets/newdesign/Resources/mainmenu/mainmenu_bg_stairs_clouds.png";

const homeMainmenuPngs = [
  "mainmenu_deco_stars.png",
  "mainmenu_deco_lightning.png",
  "mainmenu_deco_confetti.png",
  "mainmenu_deco_numbers.png",
  "mainmenu_cta_gold_9s.png",
  "mainmenu_cta_blue_9s.png",
  "mainmenu_cta_magenta_9s.png",
  "mainmenu_player_chip_frame_9s.png",
  "mainmenu_tip_frame_9s.png",
  "mainmenu_gear_glossy.png",
  "mainmenu_icon_solo.png",
  "mainmenu_icon_private_room.png",
  "mainmenu_icon_daily_hunt.png",
  "mainmenu_icon_streak.png",
  "mainmenu_icon_tip_bulb.png",
];

const sharedPngs = [
  "reference/hol_logo_exact.png",
  "reference/mascot_6_exact.png",
  "reference/mascot_7_exact.png",
  "reference/char_boy_exact.png",
  "reference/char_girl_exact.png",
];

test("approved number six stays byte-exact", () => {
  const png = read("Assets/newdesign/Resources/reference/mascot_6_exact.png");
  assert.equal(crypto.createHash("sha256").update(png).digest("hex"),
    "067beafc207aea302e0993a3bacdb2b69478429aa3685f275bb6705bd902ac4b");
});

test("approved Revision 3 Home background is exact Android portrait size", () => {
  const png = read(approvedBackground);
  assert.deepEqual(dimensions(png), { width: 1080, height: 1920 });
  assert.ok(colorType(png) === 2 || colorType(png) === 6,
    "Home background must be RGB or RGBA");
  assert.equal(exists(approvedBackground + ".meta"), true,
    approvedBackground + ".meta");
});

test("rejected stairs/clouds Home background is deleted", () => {
  assert.equal(exists(rejectedCloudBackground), false, rejectedCloudBackground);
  assert.equal(exists(rejectedCloudBackground + ".meta"), false,
    rejectedCloudBackground + ".meta");
});

test("every current cartoon Home asset has a Unity meta", () => {
  for (const filename of homeMainmenuPngs) {
    const path = "Assets/newdesign/Resources/mainmenu/" + filename;
    assert.equal(exists(path), true, path);
    assert.equal(exists(path + ".meta"), true, path + ".meta");
  }
  for (const path of sharedPngs) {
    const full = "Assets/newdesign/Resources/" + path;
    assert.equal(exists(full), true, full);
    assert.equal(exists(full + ".meta"), true, full + ".meta");
  }
});

test("cartoon Home art stays isolated from Splash resources", () => {
  for (const filename of homeMainmenuPngs) {
    const path = "Assets/newdesign/Resources/splash/" + filename;
    assert.equal(exists(path), false, path);
  }
  assert.equal(
    exists("Assets/newdesign/Resources/splash/char_boy_exact.png"),
    false);
  assert.equal(
    exists("Assets/newdesign/Resources/mainmenu/splash_bg_stairs_clouds.png"),
    false);
});

test("rejected neon arcade Home background is absent", () => {
  for (const suffix of ["", ".meta"]) {
    const path = "Assets/newdesign/Resources/mainmenu/mainmenu_bg_night_arcade.png"
      + suffix;
    assert.equal(exists(path), false, path);
  }
});

test("Home does not ship a 1v1 CTA", () => {
  for (const name of [
    "mainmenu_cta_violet_9s.png",
    "mainmenu_icon_1v1.png",
  ]) {
    const path = "Assets/newdesign/Resources/mainmenu/" + name;
    assert.equal(exists(path), false, path);
    assert.equal(exists(path + ".meta"), false, path + ".meta");
  }
});

test("Home chrome PNGs stay text-free RGBA overlays or 9-slices", () => {
  for (const filename of homeMainmenuPngs) {
    const png = read("Assets/newdesign/Resources/mainmenu/" + filename);
    assert.equal(colorType(png), 6, filename + " must be RGBA");
  }
});

test("Home owner uses approved background and cannot restore rejected one", () => {
  const owner = "Assets/SCRIPT/Design/MainMenuHomeVisuals.cs";
  const source = read(owner).toString("utf8");
  assert.match(source, /phase2a\/hol_neon_reference_bg_r3/);
  assert.equal(source.includes("mainmenu_bg_stairs_clouds"), false);
});
