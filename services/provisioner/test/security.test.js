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

const wrongCertBase64Url = Buffer.from("22".repeat(32), "hex").toString("base64url");

// The fourth column is the value the rejection reports having seen. Checks
// whose reason is already unambiguous do not carry one; the verdict-derived
// checks do, because "refused" alone cannot tell an app Play does not know
// from a device that failed the integrity bar — and only one of those two is
// fixable by changing how the build is distributed.
for (const [name, mutate, reason, observed] of [
  ["package mismatch", p => { p.requestDetails.requestPackageName = "evil.pkg"; }, "package_mismatch"],
  ["request tampering", p => { p.requestDetails.requestHash = "bad"; }, "request_hash_mismatch"],
  ["stale token", p => { p.requestDetails.timestampMillis = String(now - 121_000); }, "stale_token"],
  ["unrecognized app", p => { p.appIntegrity.appRecognitionVerdict = "UNRECOGNIZED_VERSION"; },
    "app_not_recognized", "UNRECOGNIZED_VERSION"],
  ["wrong signing cert", p => {
    p.appIntegrity.certificateSha256Digest = [wrongCertBase64Url];
  }, "certificate_mismatch", wrongCertBase64Url],
  ["weak device", p => { p.deviceIntegrity.deviceRecognitionVerdict = ["MEETS_BASIC_INTEGRITY"]; },
    "device_integrity_failed", "MEETS_BASIC_INTEGRITY"],
  ["unlicensed app", p => { p.accountDetails.appLicensingVerdict = "UNLICENSED"; },
    "app_not_licensed", "UNLICENSED"],
]) {
  test(`rejects ${name}`, () => {
    const payload = goodPayload();
    mutate(payload);
    assert.deepEqual(
      validateIntegrityPayload(payload, options),
      observed === undefined ? { ok: false, reason } : { ok: false, reason, observed },
    );
  });
}

// The distinction the rejection log exists to make: both refuse the same way,
// but only UNRECOGNIZED_VERSION is answered by shipping through a Play track.
test("separates an unevaluated app from an unrecognized one", () => {
  const payload = goodPayload();
  payload.appIntegrity.appRecognitionVerdict = "UNEVALUATED";

  assert.deepEqual(validateIntegrityPayload(payload, options), {
    ok: false,
    reason: "app_not_recognized",
    observed: "UNEVALUATED",
  });
});

test("reports a missing verdict as absent rather than as an empty string", () => {
  const payload = goodPayload();
  delete payload.appIntegrity.appRecognitionVerdict;

  assert.deepEqual(validateIntegrityPayload(payload, options), {
    ok: false,
    reason: "app_not_recognized",
    observed: "(absent)",
  });
});

test("an accepted payload carries no observed value", () => {
  assert.deepEqual(validateIntegrityPayload(goodPayload(), options), { ok: true, reason: "ok" });
});

test("licensing can be disabled explicitly for controlled non-Play testing", () => {
  const payload = goodPayload();
  payload.accountDetails.appLicensingVerdict = "UNEVALUATED";
  assert.equal(validateIntegrityPayload(payload, { ...options, requireLicensed: false }).ok, true);
});