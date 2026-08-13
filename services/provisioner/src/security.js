import { createHash, timingSafeEqual } from "node:crypto";

export function canonicalRequestHash(packageName, customId, appVersion) {
  return createHash("sha256")
    .update(`${packageName}\n${customId}\n${appVersion}`, "utf8")
    .digest("hex");
}

function safeEqual(a, b) {
  const left = Buffer.from(String(a || ""), "utf8");
  const right = Buffer.from(String(b || ""), "utf8");
  return left.length === right.length && timingSafeEqual(left, right);
}

function allowedCertificates(value) {
  return String(value || "")
    .split(",")
    .map(v => v.trim())
    .filter(Boolean);
}

export function validateIntegrityPayload(payload, options) {
  const {
    packageName,
    expectedRequestHash,
    certificateSha256 = "",
    requireLicensed = true,
    maxAgeMs = 120_000,
    futureSkewMs = 30_000,
    nowMs = Date.now(),
  } = options;

  const request = payload?.requestDetails || {};
  if (request.requestPackageName !== packageName)
    return { ok: false, reason: "package_mismatch" };
  if (!safeEqual(request.requestHash, expectedRequestHash))
    return { ok: false, reason: "request_hash_mismatch" };

  const timestamp = Number(request.timestampMillis);
  if (!Number.isFinite(timestamp)) return { ok: false, reason: "bad_timestamp" };
  if (timestamp > nowMs + futureSkewMs) return { ok: false, reason: "future_timestamp" };
  if (nowMs - timestamp > maxAgeMs) return { ok: false, reason: "stale_token" };

  const app = payload?.appIntegrity || {};
  if (app.appRecognitionVerdict !== "PLAY_RECOGNIZED")
    return { ok: false, reason: "app_not_recognized" };
  if (app.packageName && app.packageName !== packageName)
    return { ok: false, reason: "app_package_mismatch" };

  const allowCerts = allowedCertificates(certificateSha256);
  if (allowCerts.length > 0) {
    const verdictCerts = Array.isArray(app.certificateSha256Digest)
      ? app.certificateSha256Digest
      : [];
    if (!verdictCerts.some(cert => allowCerts.includes(cert)))
      return { ok: false, reason: "certificate_mismatch" };
  }

  const deviceVerdicts = payload?.deviceIntegrity?.deviceRecognitionVerdict;
  if (!Array.isArray(deviceVerdicts) || !deviceVerdicts.includes("MEETS_DEVICE_INTEGRITY"))
    return { ok: false, reason: "device_integrity_failed" };

  if (requireLicensed && payload?.accountDetails?.appLicensingVerdict !== "LICENSED")
    return { ok: false, reason: "app_not_licensed" };

  return { ok: true, reason: "ok" };
}
