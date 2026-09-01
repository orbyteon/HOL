import fs from "node:fs";
import { pathToFileURL } from "node:url";
import zlib from "node:zlib";

const defaultExpectedWidth = 1080;
const defaultExpectedHeight = 1920;
const minimumLuminanceRange = 12;
const minimumChannelRange = 16;
const targetSampleCount = 4096;

const requireCondition = (condition, message) => {
  if (!condition) throw new Error(message);
};

const paeth = (left, up, upperLeft) => {
  const prediction = left + up - upperLeft;
  const leftDistance = Math.abs(prediction - left);
  const upDistance = Math.abs(prediction - up);
  const upperLeftDistance = Math.abs(prediction - upperLeft);
  if (leftDistance <= upDistance && leftDistance <= upperLeftDistance) return left;
  return upDistance <= upperLeftDistance ? up : upperLeft;
};

const normalizedExpectedDimensions = options => {
  const width = Number(options?.expectedWidth ?? defaultExpectedWidth);
  const height = Number(options?.expectedHeight ?? defaultExpectedHeight);
  requireCondition(
    Number.isSafeInteger(width) && width > 0,
    "Expected screenshot width must be a positive integer");
  requireCondition(
    Number.isSafeInteger(height) && height > 0,
    "Expected screenshot height must be a positive integer");
  return { width, height };
};

export const expectedDimensionsFromPath = path => {
  const match = String(path ?? "").match(/(?:^|[-_])(\d+)x(\d+)\.png$/i);
  if (!match) {
    return {
      expectedWidth: defaultExpectedWidth,
      expectedHeight: defaultExpectedHeight,
    };
  }

  return {
    expectedWidth: Number(match[1]),
    expectedHeight: Number(match[2]),
  };
};

export const decodeRgbaPng = (png, options = {}) => {
  requireCondition(Buffer.isBuffer(png), "Screenshot must be a PNG Buffer");
  requireCondition(
    png.length >= 33 &&
      png.subarray(0, 8).toString("hex") === "89504e470d0a1a0a",
    "Screenshot has an invalid PNG signature");

  const idat = [];
  let ihdr = null;
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

  requireCondition(ihdr?.length === 13, "Screenshot PNG has no valid IHDR");
  requireCondition(idat.length > 0, "Screenshot PNG has no IDAT data");
  const width = ihdr.readUInt32BE(0);
  const height = ihdr.readUInt32BE(4);
  const expected = normalizedExpectedDimensions(options);
  requireCondition(
    width === expected.width && height === expected.height,
    `Expected ${expected.width}x${expected.height}, got ${width}x${height}`);
  const bitDepth = ihdr[8];
  const colorType = ihdr[9];
  const compressionMethod = ihdr[10];
  const filterMethod = ihdr[11];
  const interlaceMethod = ihdr[12];
  requireCondition(
    bitDepth === 8 &&
      (colorType === 2 || colorType === 6) &&
      compressionMethod === 0 &&
      filterMethod === 0 &&
      interlaceMethod === 0,
    "Expected a non-interlaced 8-bit RGB or RGBA PNG");

  const sourceBytesPerPixel = colorType === 2 ? 3 : 4;
  const sourceStride = width * sourceBytesPerPixel;
  const expectedScanlineBytes = (sourceStride + 1) * height;
  const filtered = zlib.inflateSync(
    Buffer.concat(idat),
    { maxOutputLength: expectedScanlineBytes });
  requireCondition(
    filtered.length === expectedScanlineBytes,
    "Screenshot PNG decompressed to an unexpected size");
  const decoded = Buffer.allocUnsafe(sourceStride * height);

  for (let y = 0; y < height; y++) {
    const filteredRow = y * (sourceStride + 1);
    const outputRow = y * sourceStride;
    const filter = filtered[filteredRow];
    requireCondition(filter <= 4, "Unsupported PNG filter " + filter);
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

  let rgba = decoded;
  if (colorType === 2) {
    const pixelCount = width * height;
    rgba = Buffer.allocUnsafe(pixelCount * 4);
    for (let pixel = 0; pixel < pixelCount; pixel++) {
      const source = pixel * 3;
      const target = pixel * 4;
      rgba[target] = decoded[source];
      rgba[target + 1] = decoded[source + 1];
      rgba[target + 2] = decoded[source + 2];
      rgba[target + 3] = 255;
    }
  }

  return { width, height, rgba };
};

export const validateMainMenuPng = (png, options = {}) => {
  const { width, height, rgba } = decodeRgbaPng(png, options);

  const pixelCount = width * height;
  const sampleStep = Math.max(1, Math.floor(pixelCount / targetSampleCount));
  const colors = new Set();
  let sampledPixels = 0;
  let minimumLuminance = Number.POSITIVE_INFINITY;
  let maximumLuminance = Number.NEGATIVE_INFINITY;
  let minimumRed = 255;
  let minimumGreen = 255;
  let minimumBlue = 255;
  let maximumRed = 0;
  let maximumGreen = 0;
  let maximumBlue = 0;

  for (let pixel = 0; pixel < pixelCount; pixel += sampleStep) {
    const offset = pixel * 4;
    const red = rgba[offset];
    const green = rgba[offset + 1];
    const blue = rgba[offset + 2];
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
    sampledPixels++;
  }

  const luminanceRange = maximumLuminance - minimumLuminance;
  const channelRange = Math.max(
    maximumRed - minimumRed,
    maximumGreen - minimumGreen,
    maximumBlue - minimumBlue);
  requireCondition(
    colors.size > 1,
    "Home screenshot is uniform: sampled only one RGB color");
  requireCondition(
    luminanceRange >= minimumLuminanceRange,
    `Home screenshot luminance range ${luminanceRange.toFixed(2)} is too small`);
  requireCondition(
    channelRange >= minimumChannelRange,
    `Home screenshot color range ${channelRange} is too small`);

  return {
    width,
    height,
    sampledPixels,
    sampledColors: colors.size,
    luminanceRange,
    channelRange,
  };
};

const isMain = process.argv[1] &&
  import.meta.url === pathToFileURL(process.argv[1]).href;
if (isMain) {
  try {
    const path = process.argv[2];
    requireCondition(
      path,
      "Usage: validate-mainmenu-screenshot.mjs <screenshot.png>");
    const result = validateMainMenuPng(
      fs.readFileSync(path),
      expectedDimensionsFromPath(path));
    console.log(
      `Validated ${result.width}x${result.height} Home PNG: ` +
      `${result.sampledColors} sampled colors, ` +
      `${result.luminanceRange.toFixed(2)} luminance range, ` +
      `${result.channelRange} channel range`);
  } catch (error) {
    console.error("Home screenshot validation failed: " + error.message);
    process.exitCode = 1;
  }
}
