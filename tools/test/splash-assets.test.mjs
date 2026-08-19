import assert from "node:assert/strict";
import crypto from "node:crypto";
import fs from "node:fs";
import test from "node:test";
import zlib from "node:zlib";

const root = new URL("../../", import.meta.url);
const read = relative => fs.readFileSync(new URL(relative, root));
const dimensions = png => ({
  width: png.readUInt32BE(16),
  height: png.readUInt32BE(20),
});

const paeth = (left, up, upperLeft) => {
  const prediction = left + up - upperLeft;
  const leftDistance = Math.abs(prediction - left);
  const upDistance = Math.abs(prediction - up);
  const upperLeftDistance = Math.abs(prediction - upperLeft);
  if (leftDistance <= upDistance && leftDistance <= upperLeftDistance) return left;
  return upDistance <= upperLeftDistance ? up : upperLeft;
};

const decodeRgba = png => {
  assert.equal(png.subarray(0, 8).toString("hex"), "89504e470d0a1a0a");
  const idat = [];
  let ihdr;
  for (let offset = 8; offset < png.length;) {
    const length = png.readUInt32BE(offset);
    const type = png.subarray(offset + 4, offset + 8).toString("ascii");
    const data = png.subarray(offset + 8, offset + 8 + length);
    if (type === "IHDR") ihdr = data;
    if (type === "IDAT") idat.push(data);
    offset += 12 + length;
  }

  assert.ok(ihdr);
  const width = ihdr.readUInt32BE(0);
  const height = ihdr.readUInt32BE(4);
  assert.deepEqual([...ihdr.subarray(8, 13)], [8, 6, 0, 0, 0],
    "Expected non-interlaced 8-bit RGBA PNG");

  const bytesPerPixel = 4;
  const stride = width * bytesPerPixel;
  const filtered = zlib.inflateSync(Buffer.concat(idat));
  assert.equal(filtered.length, (stride + 1) * height);
  const rgba = Buffer.alloc(stride * height);

  for (let y = 0; y < height; y++) {
    const filter = filtered[y * (stride + 1)];
    assert.ok(filter <= 4, "Unsupported PNG filter " + filter);
    for (let x = 0; x < stride; x++) {
      const encoded = filtered[y * (stride + 1) + x + 1];
      const output = y * stride + x;
      const left = x >= bytesPerPixel ? rgba[output - bytesPerPixel] : 0;
      const up = y > 0 ? rgba[output - stride] : 0;
      const upperLeft = y > 0 && x >= bytesPerPixel
        ? rgba[output - stride - bytesPerPixel] : 0;
      const predictor = [0, left, up, Math.floor((left + up) / 2),
        paeth(left, up, upperLeft)][filter];
      rgba[output] = (encoded + predictor) & 0xff;
    }
  }
  return { width, height, rgba };
};

test("approved number six stays byte-exact", () => {
  const png = read("Assets/newdesign/Resources/reference/mascot_6_exact.png");
  assert.equal(crypto.createHash("sha256").update(png).digest("hex"),
    "067beafc207aea302e0993a3bacdb2b69478429aa3685f275bb6705bd902ac4b");
});

test("Splash background is exact Android portrait size", () => {
  assert.deepEqual(
    dimensions(read("Assets/newdesign/Resources/splash/splash_bg_stairs_clouds.png")),
    { width: 1080, height: 1920 });
});

test("Splash logo glow is exact overlay size", () => {
  assert.deepEqual(
    dimensions(read("Assets/newdesign/Resources/splash/splash_logo_glow.png")),
    { width: 960, height: 620 });
});

test("Splash logo glow has a fully transparent outer edge", () => {
  const { width, height, rgba } = decodeRgba(
    read("Assets/newdesign/Resources/splash/splash_logo_glow.png"));
  const alpha = (x, y) => rgba[(y * width + x) * 4 + 3];

  for (let x = 0; x < width; x++) {
    assert.equal(alpha(x, 0), 0, "nonzero top alpha at x=" + x);
    assert.equal(alpha(x, height - 1), 0, "nonzero bottom alpha at x=" + x);
  }
  for (let y = 0; y < height; y++) {
    assert.equal(alpha(0, y), 0, "nonzero left alpha at y=" + y);
    assert.equal(alpha(width - 1, y), 0, "nonzero right alpha at y=" + y);
  }
  assert.ok(alpha(Math.floor(width / 2), Math.floor(height / 2)) < 160);
});

const newSplashPngs = [
  "splash_bg_stairs_clouds.png",
  "splash_logo_glow.png",
  "splash_deco_stars.png",
  "splash_deco_lightning.png",
  "splash_deco_confetti.png",
  "splash_deco_numbers.png",
  "splash_char_boy.png",
  "splash_char_girl.png",
];

test("every cartoon Splash asset has a Unity meta", () => {
  for (const filename of newSplashPngs) {
    const path = "Assets/newdesign/Resources/splash/" + filename;
    assert.equal(fs.existsSync(new URL(path, root)), true, path);
    assert.equal(fs.existsSync(new URL(path + ".meta", root)), true, path + ".meta");
  }
});

test("cartoon Splash art stays isolated from Main Menu resources", () => {
  for (const filename of newSplashPngs) {
    const path = "Assets/newdesign/Resources/mainmenu/" + filename;
    assert.equal(fs.existsSync(new URL(path, root)), false, path);
  }
});

test("rejected neon arcade Splash background is absent", () => {
  for (const suffix of ["", ".meta"]) {
    const path = "Assets/newdesign/Resources/splash/splash_bg_neon_arcade.png" + suffix;
    assert.equal(fs.existsSync(new URL(path, root)), false, path);
  }
});
