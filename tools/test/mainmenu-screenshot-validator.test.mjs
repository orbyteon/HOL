import assert from "node:assert/strict";
import test from "node:test";
import zlib from "node:zlib";

import {
  expectedDimensionsFromPath,
  validateMainMenuPng,
} from "../mainmenu/validate-mainmenu-screenshot.mjs";

const crcTable = (() => {
  const table = new Uint32Array(256);
  for (let n = 0; n < table.length; n++) {
    let value = n;
    for (let bit = 0; bit < 8; bit++) {
      value = value & 1 ? 0xedb88320 ^ (value >>> 1) : value >>> 1;
    }
    table[n] = value >>> 0;
  }
  return table;
})();

const crc32 = data => {
  let crc = 0xffffffff;
  for (const byte of data) crc = crcTable[(crc ^ byte) & 0xff] ^ (crc >>> 8);
  return (crc ^ 0xffffffff) >>> 0;
};

const chunk = (type, data) => {
  const name = Buffer.from(type, "ascii");
  const result = Buffer.alloc(12 + data.length);
  result.writeUInt32BE(data.length, 0);
  name.copy(result, 4);
  data.copy(result, 8);
  result.writeUInt32BE(crc32(Buffer.concat([name, data])), 8 + data.length);
  return result;
};

const rgbaPng = (width, height, compressed) => {
  const ihdr = Buffer.alloc(13);
  ihdr.writeUInt32BE(width, 0);
  ihdr.writeUInt32BE(height, 4);
  ihdr.set([8, 6, 0, 0, 0], 8);
  return Buffer.concat([
    Buffer.from("89504e470d0a1a0a", "hex"),
    chunk("IHDR", ihdr),
    chunk("IDAT", compressed),
    chunk("IEND", Buffer.alloc(0)),
  ]);
};

const syntheticRgbaPng = (pixel, width = 1080, height = 1920) => {
  const stride = width * 4;
  const raw = Buffer.allocUnsafe((stride + 1) * height);
  for (let y = 0; y < height; y++) {
    const row = y * (stride + 1);
    raw[row] = 0;
    for (let x = 0; x < width; x++) {
      const [r, g, b, a] = pixel(x, y, width, height);
      const offset = row + 1 + x * 4;
      raw[offset] = r;
      raw[offset + 1] = g;
      raw[offset + 2] = b;
      raw[offset + 3] = a;
    }
  }
  return rgbaPng(width, height, zlib.deflateSync(raw));
};

test("dimensions fail before malformed image data is inflated", () => {
  const png = rgbaPng(1, 1, Buffer.from("not a deflate stream"));

  assert.throws(
    () => validateMainMenuPng(png),
    /Expected 1080x1920, got 1x1/);
});

test("named portrait screenshots provide their exact expected viewport", () => {
  assert.deepEqual(
    expectedDimensionsFromPath("daily-hunt-en-1080x2400.png"),
    { expectedWidth: 1080, expectedHeight: 2400 });
  assert.deepEqual(
    expectedDimensionsFromPath("/tmp/daily-hunt-el-1179x2556.png"),
    { expectedWidth: 1179, expectedHeight: 2556 });
  assert.deepEqual(
    expectedDimensionsFromPath("mainmenu.png"),
    { expectedWidth: 1080, expectedHeight: 1920 });
});

test("uniform gray Home screenshot fails closed", () => {
  const png = syntheticRgbaPng(() => [127, 127, 127, 255]);

  assert.throws(
    () => validateMainMenuPng(png),
    /uniform|range/i);
});

test("near-uniform Home screenshot fails range thresholds", () => {
  const png = syntheticRgbaPng((x, y, width) =>
    x < width / 2
      ? [127, 127, 127, 255]
      : [130, 130, 130, 255]);

  assert.throws(
    () => validateMainMenuPng(png),
    /luminance range|color range/i);
});

test("varied 1080x1920 Home screenshot passes content validation", () => {
  const png = syntheticRgbaPng((x, y, width, height) =>
    x < width / 2 && y < height / 2
      ? [16, 8, 48, 255]
      : [240, 196, 72, 255]);

  const result = validateMainMenuPng(png);

  assert.equal(result.width, 1080);
  assert.equal(result.height, 1920);
  assert.ok(result.sampledColors > 1);
  assert.ok(result.luminanceRange > 0);
  assert.ok(result.channelRange > 0);
});

test("varied alternate portrait screenshot passes exact viewport validation", () => {
  const png = syntheticRgbaPng((x, y, width, height) =>
    x < width / 2 && y < height / 2
      ? [12, 18, 64, 255]
      : [238, 74, 160, 255], 360, 800);

  const result = validateMainMenuPng(
    png,
    { expectedWidth: 360, expectedHeight: 800 });

  assert.equal(result.width, 360);
  assert.equal(result.height, 800);
  assert.ok(result.sampledColors > 1);
  assert.ok(result.luminanceRange > 0);
  assert.ok(result.channelRange > 0);
});
