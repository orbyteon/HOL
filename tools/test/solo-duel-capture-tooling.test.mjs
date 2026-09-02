import assert from "node:assert/strict";
import test from "node:test";
import zlib from "node:zlib";

import {
  captureDescriptorFromPath,
  compareStaticPixels,
  decodeSoloPng,
  expectedCaptureMatrix,
  validateLayoutSidecar,
  validateSoloPng,
} from "../solo/audit-solo-duel-captures.mjs";

const crcTable = (() => {
  const table = new Uint32Array(256);
  for (let index = 0; index < table.length; index++) {
    let value = index;
    for (let bit = 0; bit < 8; bit++)
      value = value & 1 ? 0xedb88320 ^ (value >>> 1) : value >>> 1;
    table[index] = value >>> 0;
  }
  return table;
})();

const crc32 = data => {
  let crc = 0xffffffff;
  for (const byte of data)
    crc = crcTable[(crc ^ byte) & 0xff] ^ (crc >>> 8);
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

const colorPng = (width, height, colorType, raw, interlace = 0) => {
  const ihdr = Buffer.alloc(13);
  ihdr.writeUInt32BE(width, 0);
  ihdr.writeUInt32BE(height, 4);
  ihdr.set([8, colorType, 0, 0, interlace], 8);
  return Buffer.concat([
    Buffer.from("89504e470d0a1a0a", "hex"),
    chunk("IHDR", ihdr),
    chunk("IDAT", zlib.deflateSync(raw)),
    chunk("IEND", Buffer.alloc(0)),
  ]);
};

const syntheticPng = (colorType, width = 12, height = 20, alpha = 255) => {
  const bytesPerPixel = colorType === 2 ? 3 : 4;
  const stride = width * bytesPerPixel;
  const raw = Buffer.alloc((stride + 1) * height);
  for (let y = 0; y < height; y++) {
    const row = y * (stride + 1);
    raw[row] = 0;
    for (let x = 0; x < width; x++) {
      const offset = row + 1 + x * bytesPerPixel;
      const bright = x >= width / 2 || y >= height / 2;
      raw[offset] = bright ? 240 : 12;
      raw[offset + 1] = bright ? 188 : 18;
      raw[offset + 2] = bright ? 72 : 64;
      if (bytesPerPixel === 4) raw[offset + 3] = alpha;
    }
  }
  return colorPng(width, height, colorType, raw);
};

const rect = (x, y, width, height) => ({ x, y, width, height });

const requiredNames = [
  "SoloDuelSafeRoot",
  "DuelBack",
  "SoloDuelLogo",
  "SoloDuelPlayerChip",
  "PlayerCard",
  "OpponentCard",
  "SoloVsBurst",
  "SoloPromptRibbon",
  "SoloInteractionCard",
  "SoloOpponentRail",
  "HistoryCard",
  "SoloTipCard",
  "NumberKeypad",
  "ButtonConfirm",
];

const validLayout = () => ({
  schemaVersion: 1,
  coordinateSystem: "bottom-left",
  state: "preparation",
  language: "en",
  requestedWidth: 720,
  requestedHeight: 1280,
  screenWidth: 360,
  screenHeight: 640,
  captureScale: 2,
  safeArea: rect(0, 0, 720, 1280),
  elements: requiredNames.map((name, index) => ({
    name,
    active: true,
    rect: name === "SoloDuelSafeRoot"
      ? rect(0, 0, 720, 1280)
      : rect(20 + index, 20 + index, 100, 80),
  })),
  texts: [{
    name: "PhasePrompt",
    active: true,
    value: "CHOOSE YOUR NUMBER",
    fontSize: 42,
    overflowing: false,
    hasGlyphs: true,
    rect: rect(80, 100, 300, 80),
    glyph: rect(90, 115, 280, 50),
  }],
  touchTargets: [{
    name: "ButtonConfirm",
    active: true,
    interactable: true,
    raycastTarget: true,
    rect: rect(100, 50, 300, 96),
  }],
  dynamicRegions: [rect(80, 100, 300, 80)],
});

test("varied 24-bit RGB Solo screenshot passes", () => {
  const png = syntheticPng(2);
  const result = validateSoloPng(png, {
    expectedWidth: 12,
    expectedHeight: 20,
  });
  assert.equal(result.colorType, 2);
  assert.equal(result.rgba.length, 12 * 20 * 4);
  assert.ok(result.luminanceRange >= 12);
});

test("varied 32-bit RGBA Solo screenshot passes", () => {
  const png = syntheticPng(6);
  const result = validateSoloPng(png, {
    expectedWidth: 12,
    expectedHeight: 20,
  });
  assert.equal(result.colorType, 6);
  assert.equal(result.rgba.length, 12 * 20 * 4);
  assert.ok(result.channelRange >= 16);
  assert.equal(result.visibleSampledPixels, result.sampledPixels);
});

test("varied hidden RGB in a transparent RGBA screenshot fails", () => {
  const png = syntheticPng(6, 12, 20, 0);
  assert.throws(
    () => validateSoloPng(png, {
      expectedWidth: 12,
      expectedHeight: 20,
    }),
    /mostly transparent/);
});

test("unexpected PNG dimensions fail before acceptance", () => {
  const png = syntheticPng(6);
  assert.throws(
    () => decodeSoloPng(png, { expectedWidth: 720, expectedHeight: 1280 }),
    /Expected 720x1280, got 12x20/);
});

test("grayscale and interlaced PNGs fail closed", () => {
  const grayStride = 12;
  const grayRaw = Buffer.alloc((grayStride + 1) * 20);
  assert.throws(
    () => decodeSoloPng(colorPng(12, 20, 0, grayRaw)),
    /RGB \(2\) or RGBA \(6\)/);
  const rgba = syntheticPng(6);
  const decoded = decodeSoloPng(rgba);
  const stride = decoded.width * 4;
  const raw = Buffer.alloc((stride + 1) * decoded.height);
  assert.throws(
    () => decodeSoloPng(colorPng(12, 20, 6, raw, 1)),
    /non-interlaced/);
});

test("approved Solo matrix is exactly 64 unique lanes", () => {
  const matrix = expectedCaptureMatrix();
  assert.equal(matrix.length, 64);
  assert.equal(new Set(matrix.map(value => value.key)).size, 64);
  assert.equal(
    matrix.filter(value => value.state.startsWith("difficulty-")).length,
    8);
  assert.equal(
    matrix.filter(value => value.state.startsWith("outcome-")).length,
    8);
  assert.ok(matrix.every(value =>
    !value.state.startsWith("difficulty-") ||
      (value.width === 1080 && value.height === 1920)));
});

test("hyphenated state filename parses without ambiguity", () => {
  assert.deepEqual(
    captureDescriptorFromPath(
      "C:/evidence/solo-ai-feedback-el-1179x2556.png"),
    {
      state: "ai-feedback",
      language: "el",
      width: 1179,
      height: 2556,
      key: "ai-feedback|el|1179x2556",
    });
});

test("valid layout proves glyph containment and touch readiness", () => {
  const result = validateLayoutSidecar(validLayout(), {
    state: "preparation",
    language: "en",
    width: 720,
    height: 1280,
  });
  assert.equal(result.activeGlyphRecords, 1);
  assert.equal(result.interactableTargets, 1);
});

test("rendered TMP glyph escape fails even when RectTransform is contained", () => {
  const layout = validLayout();
  layout.texts[0].glyph = rect(70, 115, 310, 50);
  assert.throws(
    () => validateLayoutSidecar(layout, {
      state: "preparation",
      language: "en",
      width: 720,
      height: 1280,
    }),
    /Rendered TMP glyphs leave their rect/);
});

test("small or raycast-transparent interactable buttons fail", () => {
  const small = validLayout();
  small.touchTargets[0].rect = rect(10, 10, 43, 44);
  assert.throws(
    () => validateLayoutSidecar(small, {
      state: "preparation",
      language: "en",
      width: 720,
      height: 1280,
    }),
    /smaller than 44x44/);

  const transparent = validLayout();
  transparent.touchTargets[0].raycastTarget = false;
  assert.throws(
    () => validateLayoutSidecar(transparent, {
      state: "preparation",
      language: "en",
      width: 720,
      height: 1280,
    }),
    /does not receive raycasts/);
});

test("a full-frame dynamic region cannot make EN EL equality vacuous", () => {
  const layout = validLayout();
  layout.dynamicRegions = [rect(0, 0, 720, 1280)];
  assert.throws(
    () => validateLayoutSidecar(layout, {
      state: "preparation",
      language: "en",
      width: 720,
      height: 1280,
    }),
    /masks too much/);

  const rgba = Buffer.alloc(32 * 32 * 4, 255);
  assert.throws(
    () => compareStaticPixels(
      { width: 32, height: 32, rgba },
      { width: 32, height: 32, rgba: Buffer.from(rgba) },
      { dynamicRegions: [rect(0, 0, 32, 32)] },
      { dynamicRegions: [rect(0, 0, 32, 32)] }),
    /static pixel/);
});

test("EN and EL may differ inside dynamic text but nowhere else", () => {
  const width = 32;
  const height = 32;
  const base = Buffer.alloc(width * height * 4, 32);
  for (let pixel = 0; pixel < width * height; pixel++)
    base[pixel * 4 + 3] = 255;
  const el = Buffer.from(base);
  const offsetInside = ((height - 1 - 12) * width + 12) * 4;
  el[offsetInside] = 200;
  const enLayout = { dynamicRegions: [rect(12, 12, 2, 2)] };
  const elLayout = { dynamicRegions: [rect(12, 12, 2, 2)] };
  const accepted = compareStaticPixels(
    { width, height, rgba: base },
    { width, height, rgba: el },
    enLayout,
    elLayout);
  assert.equal(accepted.outsideDifferences, 0);

  // Use a larger image so the mandatory eight-pixel dynamic padding does not
  // mask a deliberately distant static-art change.
  const wide = 32;
  const tall = 32;
  const wideBase = Buffer.alloc(wide * tall * 4, 48);
  for (let pixel = 0; pixel < wide * tall; pixel++)
    wideBase[pixel * 4 + 3] = 255;
  const wideEl = Buffer.from(wideBase);
  wideEl[((tall - 1 - 30) * wide + 30) * 4] = 201;
  assert.throws(
    () => compareStaticPixels(
      { width: wide, height: tall, rgba: wideBase },
      { width: wide, height: tall, rgba: wideEl },
      { dynamicRegions: [rect(2, 2, 2, 2)] },
      { dynamicRegions: [rect(2, 2, 2, 2)] }),
    /outside dynamic UI regions/);
});
