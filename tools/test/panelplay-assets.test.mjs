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
  "Assets/newdesign/Resources/phase2a/hol_neon_reference_bg_r3.png";
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

test("Play reuses the approved Revision 3 portrait background", () => {
  const png = read(approvedBackground);
  assert.deepEqual(dimensions(png), { width: 1080, height: 1920 });
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

test("Play owner uses current art and never loads splash or rejected background", () => {
  const owner = "Assets/SCRIPT/Design/MainMenuPlayVisuals.cs";
  assert.equal(exists(owner), true, owner);
  const source = read(owner).toString("utf8");
  assert.equal(source.includes('Resources.Load("splash/'), false);
  assert.equal(source.includes("splash/"), false);
  assert.match(source, /phase2a\/hol_neon_reference_bg_r3/);
  assert.equal(source.includes("mainmenu_bg_stairs_clouds"), false);
  assert.match(source, /reference\/hol_logo_exact/);
});
