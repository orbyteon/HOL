import test from "node:test";
import assert from "node:assert/strict";
import {
  canonicalRequestHash,
  normalizeCertificateDigest,
  parseCertificateConfig,
  validateIntegrityPayload,
} from "../src/security.js";

const packageName = "com.Orbyteon.HOL";
const customId = "device-123";
const appVersion = "0.2.0";
const now = 1_800_000_000_000;
const requestHash = canonicalRequestHash(packageName, customId, appVersion);
const certHex = "11".repeat(32);
const certHexFingerprint = certHex.match(/../g).join(":").toUpperCase();
const certBase64Url = Buffer.from(certHex, "hex").toString("base64url");

function goodPayload() {
  return {
    requestDetails: {
      requestPackageName: packageName,
      requestHash,
      timestampMillis: String(now - 5_000),
    },
    appIntegrity: {
      appRecognitionVerdict: "PLAY_RECOGNIZED",
      packageName,
      certificateSha256Digest: [certBase64Url],
    },
    deviceIntegrity: {
      deviceRecognitionVerdict: ["MEETS_BASIC_INTEGRITY", "MEETS_DEVICE_INTEGRITY"],
    },
    accountDetails: {
      appLicensingVerdict: "LICENSED",
    },
  };
}

const options = {
  packageName,
  expectedRequestHash: requestHash,
  // Exercise the common Play Console / keytool colon-separated hex form.
  certificateSha256: certHexFingerprint,
  nowMs: now,
};

test("canonical hash is stable SHA-256 hex", () => {
  assert.equal(requestHash.length, 64);
  assert.equal(requestHash, canonicalRequestHash(packageName, customId, appVersion));
  assert.notEqual(requestHash, canonicalRequestHash(packageName, customId + "x", appVersion));
});

test("normalizes certificate hex and base64 to Play Integrity base64url form", () => {
  assert.equal(normalizeCertificateDigest(certHexFingerprint), certBase64Url);
  assert.equal(
    normalizeCertificateDigest(Buffer.from(certHex, "hex").toString("base64")),
    certBase64Url,
  );
  assert.equal(normalizeCertificateDigest(certBase64Url), certBase64Url);
});

test("certificate config fails closed on malformed entries", () => {
  assert.deepEqual(parseCertificateConfig("not-a-sha256-digest"), {
    configured: true,
    valid: false,
    certificates: [],
  });
  assert.deepEqual(
    validateIntegrityPayload(goodPayload(), {
      ...options,
      certificateSha256: "not-a-sha256-digest",
    }),
    { ok: false, reason: "certificate_config_invalid" },
  );
});

test("accepts a recognized licensed app on a device-integrity device", () => {
  assert.deepEqual(validateIntegrityPayload(goodPayload(), options), { ok: true, reason: "ok" });
});

for (const [name, mutate, reason] of [
  ["package mismatch", p => { p.requestDetails.requestPackageName = "evil.pkg"; }, "package_mismatch"],
  ["request tampering", p => { p.requestDetails.requestHash = "bad"; }, "request_hash_mismatch"],
  ["stale token", p => { p.requestDetails.timestampMillis = String(now - 121_000); }, "stale_token"],
  ["unrecognized app", p => { p.appIntegrity.appRecognitionVerdict = "UNRECOGNIZED_VERSION"; }, "app_not_recognized"],
  ["wrong signing cert", p => {
    p.appIntegrity.certificateSha256Digest = [
      Buffer.from("22".repeat(32), "hex").toString("base64url"),
    ];
  }, "certificate_mismatch"],
  ["weak device", p => { p.deviceIntegrity.deviceRecognitionVerdict = ["MEETS_BASIC_INTEGRITY"]; }, "device_integrity_failed"],
  ["unlicensed app", p => { p.accountDetails.appLicensingVerdict = "UNLICENSED"; }, "app_not_licensed"],
]) {
  test(`rejects ${name}`, () => {
    const payload = goodPayload();
    mutate(payload);
    assert.deepEqual(validateIntegrityPayload(payload, options), { ok: false, reason });
  });
}

test("licensing can be disabled explicitly for controlled non-Play testing", () => {
  const payload = goodPayload();
  payload.accountDetails.appLicensingVerdict = "UNEVALUATED";
  assert.equal(validateIntegrityPayload(payload, { ...options, requireLicensed: false }).ok, true);
});