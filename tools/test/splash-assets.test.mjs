import assert from "node:assert/strict";
import crypto from "node:crypto";
import fs from "node:fs";
import test from "node:test";

const root = new URL("../../", import.meta.url);
const read = relative => fs.readFileSync(new URL(relative, root));
const dimensions = png => ({
  width: png.readUInt32BE(16),
  height: png.readUInt32BE(20),
});

test("approved number six stays byte-exact", () => {
  const png = read("Assets/newdesign/Resources/reference/mascot_6_exact.png");
  assert.equal(crypto.createHash("sha256").update(png).digest("hex"),
    "067beafc207aea302e0993a3bacdb2b69478429aa3685f275bb6705bd902ac4b");
});

test("Splash background is exact Android portrait size", () => {
  assert.deepEqual(
    dimensions(read("Assets/newdesign/Resources/splash/splash_bg_neon_arcade.png")),
    { width: 1080, height: 1920 });
});

test("every Splash asset has a Unity meta", () => {
  for (const path of [
    "Assets/newdesign/Resources/reference/mascot_6_exact.png",
    "Assets/newdesign/Resources/splash/splash_bg_neon_arcade.png",
    "Assets/newdesign/Resources/splash/splash_logo_glow.png",
  ]) assert.equal(fs.existsSync(new URL(path + ".meta", root)), true, path);
});
