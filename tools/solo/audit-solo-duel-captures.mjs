import crypto from "node:crypto";
import fs from "node:fs";
import path from "node:path";
import { pathToFileURL } from "node:url";
import zlib from "node:zlib";

const baseStates = [
  "preparation",
  "active-input",
  "ai-feedback",
  "history",
  "result",
  "rematch",
];
const primaryExtras = [
  "difficulty-easy",
  "difficulty-normal",
  "difficulty-hard",
  "difficulty-adaptive",
  "outcome-win",
  "outcome-loss",
  "outcome-draw",
  "outcome-lock",
];
const resolutions = [
  [720, 1280],
  [1080, 1920],
  [1080, 2400],
  [1179, 2556],
];
const languages = ["en", "el"];
const requiredElements = [
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
const minimumLuminanceRange = 12;
const minimumChannelRange = 16;
const minimumVisibleAlpha = 16;
const minimumVisibleSampleFraction = 0.95;
const minimumStaticPixelFraction = 0.25;
const targetSampleCount = 4096;
const geometryTolerance = 1.5;
const dynamicPadding = 8;

const requireCondition = (condition, message) => {
  if (!condition) throw new Error(message);
};

const sha256 = data => crypto.createHash("sha256").update(data).digest("hex");

const paeth = (left, up, upperLeft) => {
  const prediction = left + up - upperLeft;
  const leftDistance = Math.abs(prediction - left);
  const upDistance = Math.abs(prediction - up);
  const upperLeftDistance = Math.abs(prediction - upperLeft);
  if (leftDistance <= upDistance && leftDistance <= upperLeftDistance) return left;
  return upDistance <= upperLeftDistance ? up : upperLeft;
};

export const decodeSoloPng = (png, options = {}) => {
  requireCondition(Buffer.isBuffer(png), "Solo screenshot must be a PNG Buffer");
  requireCondition(
    png.length >= 33 &&
      png.subarray(0, 8).toString("hex") === "89504e470d0a1a0a",
    "Solo screenshot has an invalid PNG signature");

  let ihdr = null;
  const idat = [];
  for (let offset = 8; offset < png.length;) {
    requireCondition(offset + 12 <= png.length, "PNG chunk header is truncated");
    const length = png.readUInt32BE(offset);
    const dataStart = offset + 8;
    const dataEnd = dataStart + length;
    requireCondition(dataEnd + 4 <= png.length, "PNG chunk data is truncated");
    const type = png.subarray(offset + 4, dataStart).toString("ascii");
    const data = png.subarray(dataStart, dataEnd);
    if (type === "IHDR") ihdr = data;
    if (type === "IDAT") idat.push(data);
    offset = dataEnd + 4;
  }

  requireCondition(ihdr?.length === 13, "Solo screenshot PNG has no valid IHDR");
  requireCondition(idat.length > 0, "Solo screenshot PNG has no IDAT data");
  const width = ihdr.readUInt32BE(0);
  const height = ihdr.readUInt32BE(4);
  if (options.expectedWidth !== undefined) {
    requireCondition(
      width === options.expectedWidth && height === options.expectedHeight,
      `Expected ${options.expectedWidth}x${options.expectedHeight}, ` +
        `got ${width}x${height}`);
  }

  const bitDepth = ihdr[8];
  const colorType = ihdr[9];
  const compressionMethod = ihdr[10];
  const filterMethod = ihdr[11];
  const interlaceMethod = ihdr[12];
  requireCondition(bitDepth === 8, "Solo PNG must use 8-bit channels");
  requireCondition(
    colorType === 2 || colorType === 6,
    "Solo PNG must use RGB (2) or RGBA (6) color");
  requireCondition(compressionMethod === 0, "Solo PNG compression method must be 0");
  requireCondition(filterMethod === 0, "Solo PNG filter method must be 0");
  requireCondition(interlaceMethod === 0, "Solo PNG must be non-interlaced");

  const sourceBytesPerPixel = colorType === 2 ? 3 : 4;
  const sourceStride = width * sourceBytesPerPixel;
  const expectedBytes = (sourceStride + 1) * height;
  const filtered = zlib.inflateSync(Buffer.concat(idat), {
    maxOutputLength: expectedBytes,
  });
  requireCondition(
    filtered.length === expectedBytes,
    "Solo PNG decompressed to an unexpected size");

  const decoded = Buffer.allocUnsafe(sourceStride * height);
  for (let y = 0; y < height; y++) {
    const filteredRow = y * (sourceStride + 1);
    const outputRow = y * sourceStride;
    const filter = filtered[filteredRow];
    requireCondition(filter <= 4, "Unsupported PNG row filter " + filter);
    for (let x = 0; x < sourceStride; x++) {
      const encoded = filtered[filteredRow + x + 1];
      const output = outputRow + x;
      const left = x >= sourceBytesPerPixel
        ? decoded[output - sourceBytesPerPixel]
        : 0;
      const up = y > 0 ? decoded[output - sourceStride] : 0;
      const upperLeft = y > 0 && x >= sourceBytesPerPixel
        ? decoded[output - sourceStride - sourceBytesPerPixel]
        : 0;
      let predictor = 0;
      if (filter === 1) predictor = left;
      else if (filter === 2) predictor = up;
      else if (filter === 3) predictor = Math.floor((left + up) / 2);
      else if (filter === 4) predictor = paeth(left, up, upperLeft);
      decoded[output] = (encoded + predictor) & 0xff;
    }
  }

  if (colorType === 6) {
    return { width, height, colorType, rgba: decoded };
  }
  const rgba = Buffer.allocUnsafe(width * height * 4);
  for (let pixel = 0; pixel < width * height; pixel++) {
    const source = pixel * 3;
    const target = pixel * 4;
    rgba[target] = decoded[source];
    rgba[target + 1] = decoded[source + 1];
    rgba[target + 2] = decoded[source + 2];
    rgba[target + 3] = 255;
  }
  return { width, height, colorType, rgba };
};

export const validateSoloPng = (png, options = {}) => {
  const decoded = decodeSoloPng(png, options);
  const pixelCount = decoded.width * decoded.height;
  const sampleStep = Math.max(1, Math.floor(pixelCount / targetSampleCount));
  const colors = new Set();
  let minimumLuminance = Number.POSITIVE_INFINITY;
  let maximumLuminance = Number.NEGATIVE_INFINITY;
  let minimumRed = 255;
  let minimumGreen = 255;
  let minimumBlue = 255;
  let maximumRed = 0;
  let maximumGreen = 0;
  let maximumBlue = 0;
  let sampledPixels = 0;
  let visibleSampledPixels = 0;

  for (let pixel = 0; pixel < pixelCount; pixel += sampleStep) {
    const offset = pixel * 4;
    sampledPixels++;
    if (decoded.rgba[offset + 3] < minimumVisibleAlpha)
      continue;
    visibleSampledPixels++;
    const red = decoded.rgba[offset];
    const green = decoded.rgba[offset + 1];
    const blue = decoded.rgba[offset + 2];
    colors.add((red << 16) | (green << 8) | blue);
    const luminance = (2126 * red + 7152 * green + 722 * blue) / 10000;
    minimumLuminance = Math.min(minimumLuminance, luminance);
    maximumLuminance = Math.max(maximumLuminance, luminance);
    minimumRed = Math.min(minimumRed, red);
    minimumGreen = Math.min(minimumGreen, green);
    minimumBlue = Math.min(minimumBlue, blue);
    maximumRed = Math.max(maximumRed, red);
    maximumGreen = Math.max(maximumGreen, green);
    maximumBlue = Math.max(maximumBlue, blue);
  }

  requireCondition(
    visibleSampledPixels >= Math.ceil(
      sampledPixels * minimumVisibleSampleFraction),
    "Solo screenshot is mostly transparent");
  const luminanceRange = maximumLuminance - minimumLuminance;
  const channelRange = Math.max(
    maximumRed - minimumRed,
    maximumGreen - minimumGreen,
    maximumBlue - minimumBlue);
  requireCondition(colors.size > 1, "Solo screenshot is uniform");
  requireCondition(
    luminanceRange >= minimumLuminanceRange,
    `Solo screenshot luminance range ${luminanceRange.toFixed(2)} is too small`);
  requireCondition(
    channelRange >= minimumChannelRange,
    `Solo screenshot channel range ${channelRange} is too small`);

  return {
    ...decoded,
    sampledPixels,
    visibleSampledPixels,
    sampledColors: colors.size,
    luminanceRange,
    channelRange,
  };
};

export const captureDescriptorFromPath = filePath => {
  const name = path.basename(filePath);
  const match = name.match(/^solo-(.+)-(en|el)-(\d+)x(\d+)\.png$/i);
  requireCondition(match, "Unexpected Solo capture filename: " + name);
  return {
    state: match[1].toLowerCase(),
    language: match[2].toLowerCase(),
    width: Number(match[3]),
    height: Number(match[4]),
    key: `${match[1].toLowerCase()}|${match[2].toLowerCase()}|` +
      `${Number(match[3])}x${Number(match[4])}`,
  };
};

export const expectedCaptureMatrix = () => {
  const expected = [];
  for (const [width, height] of resolutions) {
    for (const language of languages) {
      for (const state of baseStates) {
        expected.push({
          state,
          language,
          width,
          height,
          key: `${state}|${language}|${width}x${height}`,
        });
      }
    }
  }
  for (const language of languages) {
    for (const state of primaryExtras) {
      expected.push({
        state,
        language,
        width: 1080,
        height: 1920,
        key: `${state}|${language}|1080x1920`,
      });
    }
  }
  return expected;
};

const validNumber = value => Number.isFinite(value);
const validateRect = (rect, context) => {
  requireCondition(rect && typeof rect === "object", context + " rect is missing");
  for (const key of ["x", "y", "width", "height"]) {
    requireCondition(validNumber(rect[key]), `${context} rect.${key} is invalid`);
  }
  requireCondition(
    rect.width >= 0 && rect.height >= 0,
    context + " rect has a negative size");
};

const contains = (outer, inner, tolerance = geometryTolerance) =>
  inner.x >= outer.x - tolerance &&
  inner.y >= outer.y - tolerance &&
  inner.x + inner.width <= outer.x + outer.width + tolerance &&
  inner.y + inner.height <= outer.y + outer.height + tolerance;

const normalizedRect = (rect, width, height) => ({
  x: rect.x / width,
  y: rect.y / height,
  width: rect.width / width,
  height: rect.height / height,
});

export const validateLayoutSidecar = (layout, descriptor) => {
  requireCondition(layout?.schemaVersion === 1, "Layout schemaVersion must be 1");
  requireCondition(
    layout.coordinateSystem === "bottom-left",
    "Layout coordinate system must be bottom-left");
  requireCondition(layout.state === descriptor.state, "Layout state does not match PNG");
  requireCondition(
    layout.language === descriptor.language,
    "Layout language does not match PNG");
  requireCondition(
    layout.requestedWidth === descriptor.width &&
      layout.requestedHeight === descriptor.height,
    "Layout viewport does not match PNG");
  requireCondition(
    Number.isSafeInteger(layout.screenWidth) && layout.screenWidth > 0 &&
      Number.isSafeInteger(layout.screenHeight) && layout.screenHeight > 0 &&
      Number.isSafeInteger(layout.captureScale) && layout.captureScale > 0,
    "Layout screen dimensions/capture scale are invalid");
  requireCondition(
    layout.screenWidth * layout.captureScale === descriptor.width &&
      layout.screenHeight * layout.captureScale === descriptor.height,
    "Layout screen dimensions do not scale to the capture viewport");

  validateRect(layout.safeArea, "safeArea");
  const viewport = { x: 0, y: 0, width: descriptor.width, height: descriptor.height };
  requireCondition(contains(viewport, layout.safeArea, 0.5), "SafeArea leaves viewport");
  requireCondition(
    layout.safeArea.width > 0 && layout.safeArea.height > 0,
    "SafeArea is empty");

  requireCondition(Array.isArray(layout.elements), "Layout elements are missing");
  const elements = new Map();
  for (const element of layout.elements) {
    requireCondition(
      typeof element.name === "string" && !elements.has(element.name),
      "Layout has a missing or duplicate element name");
    validateRect(element.rect, "element " + element.name);
    elements.set(element.name, element);
  }
  for (const name of requiredElements) {
    requireCondition(elements.has(name), "Required Solo element is missing: " + name);
  }
  for (const element of elements.values()) {
    if (!element.active || element.name === "SoloDuelSafeRoot") continue;
    requireCondition(
      contains(layout.safeArea, element.rect, 3),
      "Active Solo element leaves SafeArea: " + element.name);
  }

  requireCondition(Array.isArray(layout.texts), "Layout text records are missing");
  let activeGlyphRecords = 0;
  for (const text of layout.texts) {
    validateRect(text.rect, "text " + text.name);
    validateRect(text.glyph, "glyph " + text.name);
    if (!text.active || !text.value) continue;
    requireCondition(!text.overflowing, "TMP overflow: " + text.name);
    requireCondition(
      contains(layout.safeArea, text.rect, 3),
      "Active TMP rect leaves SafeArea: " + text.name);
    if (text.hasGlyphs) {
      activeGlyphRecords++;
      requireCondition(
        contains(text.rect, text.glyph, 2.5),
        "Rendered TMP glyphs leave their rect: " + text.name);
    }
  }
  requireCondition(activeGlyphRecords > 0, "Layout has no rendered TMP glyph records");

  requireCondition(
    Array.isArray(layout.touchTargets),
    "Layout touch-target records are missing");
  let interactableTargets = 0;
  for (const target of layout.touchTargets) {
    validateRect(target.rect, "touch target " + target.name);
    if (!target.active || !target.interactable) continue;
    interactableTargets++;
    requireCondition(target.raycastTarget, "Button does not receive raycasts: " + target.name);
    requireCondition(
      target.rect.width >= 44 && target.rect.height >= 44,
      "Button is smaller than 44x44 output pixels: " + target.name);
    requireCondition(
      contains(layout.safeArea, target.rect, 3),
      "Button leaves SafeArea: " + target.name);
  }
  requireCondition(interactableTargets > 0, "Layout has no interactable buttons");

  requireCondition(
    Array.isArray(layout.dynamicRegions) && layout.dynamicRegions.length > 0,
    "Layout dynamic regions are missing");
  for (let index = 0; index < layout.dynamicRegions.length; index++) {
    validateRect(layout.dynamicRegions[index], "dynamic region " + index);
    requireCondition(
      layout.dynamicRegions[index].width * layout.dynamicRegions[index].height <
        descriptor.width * descriptor.height * 0.25,
      "A dynamic region masks too much of the capture viewport");
  }

  return { elements, activeGlyphRecords, interactableTargets };
};

const expandRect = (rect, width, height, padding = dynamicPadding) => ({
  x: Math.max(0, Math.floor(rect.x - padding)),
  y: Math.max(0, Math.floor(rect.y - padding)),
  width: Math.min(width, Math.ceil(rect.x + rect.width + padding)) -
    Math.max(0, Math.floor(rect.x - padding)),
  height: Math.min(height, Math.ceil(rect.y + rect.height + padding)) -
    Math.max(0, Math.floor(rect.y - padding)),
});

export const compareStaticPixels = (
  enDecoded,
  elDecoded,
  enLayout,
  elLayout,
) => {
  requireCondition(
    enDecoded.width === elDecoded.width && enDecoded.height === elDecoded.height,
    "EN/EL static comparison dimensions differ");
  const width = enDecoded.width;
  const height = enDecoded.height;
  const regions = [
    ...enLayout.dynamicRegions,
    ...elLayout.dynamicRegions,
  ].map(rect => expandRect(rect, width, height));
  const rowIntervals = Array.from({ length: height }, () => []);
  for (const rect of regions) {
    const xMin = Math.max(0, Math.floor(rect.x));
    const xMax = Math.min(width, Math.ceil(rect.x + rect.width));
    const yBottomMin = Math.max(0, Math.floor(rect.y));
    const yBottomMax = Math.min(height, Math.ceil(rect.y + rect.height));
    const yTopMin = Math.max(0, height - yBottomMax);
    const yTopMax = Math.min(height, height - yBottomMin);
    for (let y = yTopMin; y < yTopMax; y++)
      rowIntervals[y].push([xMin, xMax]);
  }
  for (const row of rowIntervals) {
    row.sort((left, right) => left[0] - right[0]);
    for (let index = 1; index < row.length;) {
      const previous = row[index - 1];
      const current = row[index];
      if (current[0] <= previous[1]) {
        previous[1] = Math.max(previous[1], current[1]);
        row.splice(index, 1);
      } else {
        index++;
      }
    }
  }

  const masked = Buffer.from(enDecoded.rgba);
  let outsideDifferences = 0;
  let comparedPixels = 0;
  for (let y = 0; y < height; y++) {
    const intervals = rowIntervals[y];
    let intervalIndex = 0;
    for (let x = 0; x < width; x++) {
      while (intervalIndex < intervals.length && x >= intervals[intervalIndex][1])
        intervalIndex++;
      const dynamic = intervalIndex < intervals.length &&
        x >= intervals[intervalIndex][0] && x < intervals[intervalIndex][1];
      const offset = (y * width + x) * 4;
      if (dynamic) {
        masked.fill(0, offset, offset + 4);
        continue;
      }
      comparedPixels++;
      if (!enDecoded.rgba.subarray(offset, offset + 4)
        .equals(elDecoded.rgba.subarray(offset, offset + 4))) {
        outsideDifferences++;
      }
    }
  }
  requireCondition(
    comparedPixels >= Math.ceil(width * height * minimumStaticPixelFraction),
    `Only ${comparedPixels} static pixel(s) remained after dynamic masking`);
  requireCondition(
    outsideDifferences === 0,
    `EN/EL differ at ${outsideDifferences} pixel(s) outside dynamic UI regions`);
  return {
    comparedPixels,
    outsideDifferences,
    staticSha256: sha256(masked),
  };
};

const compareNormalizedGeometry = (enLayout, elLayout, descriptor) => {
  const enElements = new Map(enLayout.elements.map(value => [value.name, value]));
  const elElements = new Map(elLayout.elements.map(value => [value.name, value]));
  for (const name of requiredElements) {
    const en = enElements.get(name);
    const el = elElements.get(name);
    requireCondition(en && el, "EN/EL element pair is missing: " + name);
    const enRect = normalizedRect(en.rect, descriptor.width, descriptor.height);
    const elRect = normalizedRect(el.rect, descriptor.width, descriptor.height);
    for (const key of ["x", "y", "width", "height"]) {
      requireCondition(
        Math.abs(enRect[key] - elRect[key]) <= 0.0005,
        `EN/EL normalized ${name}.${key} drifted`);
    }
  }
};

const walkPngs = root => {
  const result = [];
  for (const entry of fs.readdirSync(root, { withFileTypes: true })) {
    const full = path.join(root, entry.name);
    if (entry.isDirectory()) result.push(...walkPngs(full));
    else if (entry.isFile() && entry.name.toLowerCase().endsWith(".png"))
      result.push(full);
  }
  return result.sort((left, right) => left.localeCompare(right));
};

export const auditCaptureDirectory = root => {
  const captureRoot = path.resolve(root);
  requireCondition(fs.statSync(captureRoot).isDirectory(), "Capture root is not a directory");
  const files = walkPngs(captureRoot);
  const expected = expectedCaptureMatrix();
  requireCondition(
    files.length === expected.length,
    `Expected ${expected.length} Solo PNGs, found ${files.length}`);

  const expectedKeys = new Set(expected.map(value => value.key));
  const records = new Map();
  for (const file of files) {
    const descriptor = captureDescriptorFromPath(file);
    requireCondition(expectedKeys.has(descriptor.key), "Unexpected Solo lane: " + descriptor.key);
    requireCondition(!records.has(descriptor.key), "Duplicate Solo lane: " + descriptor.key);
    const png = fs.readFileSync(file);
    const decoded = validateSoloPng(png, {
      expectedWidth: descriptor.width,
      expectedHeight: descriptor.height,
    });
    const layoutFile = file + ".layout.json";
    requireCondition(fs.existsSync(layoutFile), "Missing layout sidecar: " + layoutFile);
    const layoutBytes = fs.readFileSync(layoutFile);
    const layout = JSON.parse(layoutBytes.toString("utf8"));
    validateLayoutSidecar(layout, descriptor);
    records.set(descriptor.key, {
      descriptor,
      file,
      png,
      decoded,
      layout,
      layoutFile,
      layoutBytes,
    });
  }
  for (const expectedLane of expected)
    requireCondition(records.has(expectedLane.key), "Missing Solo lane: " + expectedLane.key);

  const staticPairs = [];
  for (const expectedLane of expected.filter(value => value.language === "en")) {
    const en = records.get(expectedLane.key);
    const elKey = `${expectedLane.state}|el|${expectedLane.width}x${expectedLane.height}`;
    const el = records.get(elKey);
    requireCondition(el, "Missing EL pair for " + expectedLane.key);
    compareNormalizedGeometry(en.layout, el.layout, expectedLane);
    const comparison = compareStaticPixels(
      en.decoded, el.decoded, en.layout, el.layout);
    staticPairs.push({
      state: expectedLane.state,
      width: expectedLane.width,
      height: expectedLane.height,
      ...comparison,
    });
  }

  const captures = [...records.values()]
    .sort((left, right) => left.descriptor.key.localeCompare(right.descriptor.key))
    .map(record => ({
      state: record.descriptor.state,
      language: record.descriptor.language,
      width: record.descriptor.width,
      height: record.descriptor.height,
      png: path.relative(captureRoot, record.file).replaceAll("\\", "/"),
      pngSha256: sha256(record.png),
      layout: path.relative(captureRoot, record.layoutFile).replaceAll("\\", "/"),
      layoutSha256: sha256(record.layoutBytes),
      pngColorType: record.decoded.colorType,
      sampledColors: record.decoded.sampledColors,
      luminanceRange: record.decoded.luminanceRange,
      channelRange: record.decoded.channelRange,
    }));

  return {
    schemaVersion: 1,
    captureCount: captures.length,
    captures,
    staticPairs,
  };
};

const isMain = process.argv[1] &&
  import.meta.url === pathToFileURL(process.argv[1]).href;
if (isMain) {
  try {
    const root = process.argv[2];
    requireCondition(
      root,
      "Usage: audit-solo-duel-captures.mjs <capture-root> " +
        "[--inventory <output.json>]");
    const inventoryFlag = process.argv.indexOf("--inventory");
    const inventoryPath = inventoryFlag >= 0
      ? process.argv[inventoryFlag + 1]
      : null;
    if (inventoryFlag >= 0)
      requireCondition(inventoryPath, "--inventory requires an output path");

    const result = auditCaptureDirectory(root);
    if (inventoryPath) {
      fs.writeFileSync(
        inventoryPath,
        JSON.stringify(result, null, 2) + "\n",
        { flag: "wx" });
    }
    console.log(
      `Validated ${result.captureCount} Solo captures and ` +
      `${result.staticPairs.length} EN/EL static-art pairs.`);
  } catch (error) {
    console.error("Solo capture audit failed: " + error.message);
    process.exitCode = 1;
  }
}
