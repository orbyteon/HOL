import { app } from "@azure/functions";
import { GoogleAuth } from "google-auth-library";
import {
  canonicalRequestHash,
  createFixedWindowRateLimiter,
  validateIntegrityPayload,
} from "./security.js";
import { privacyResponse } from "./privacy.js";

const WINDOW_MS = 60_000;
const MAX_REQUESTS_PER_WINDOW = 12;
const MAX_RATE_LIMIT_ENTRIES = 10_000;
const ipRateLimiter = createFixedWindowRateLimiter({
  windowMs: WINDOW_MS,
  maxRequests: MAX_REQUESTS_PER_WINDOW,
  maxEntries: MAX_RATE_LIMIT_ENTRIES,
});

function env(name, fallback = "") {
  const value = (process.env[name] || fallback).trim();
  if (!value) throw new Error(`${name} is required`);
  return value;
}

function optionalEnv(name, fallback = "") {
  return (process.env[name] || fallback).trim();
}

function clientIp(request) {
  const forwarded = request.headers.get("x-forwarded-for") || "";
  return forwarded.split(",")[0].trim() || "unknown";
}

function serviceAccountCredentials() {
  const encoded = optionalEnv("GOOGLE_SERVICE_ACCOUNT_JSON_B64");
  if (!encoded) return undefined; // Azure workload/ADC is also supported.

  try {
    return JSON.parse(Buffer.from(encoded, "base64").toString("utf8"));
  } catch {
    throw new Error("GOOGLE_SERVICE_ACCOUNT_JSON_B64 is not valid base64 JSON");
  }
}

async function decodeIntegrityToken(packageName, integrityToken) {
  const auth = new GoogleAuth({
    credentials: serviceAccountCredentials(),
    scopes: ["https://www.googleapis.com/auth/playintegrity"],
  });
  const client = await auth.getClient();
  const response = await client.request({
    url: `https://playintegrity.googleapis.com/v1/${encodeURIComponent(packageName)}:decodeIntegrityToken`,
    method: "POST",
    data: { integrity_token: integrityToken },
  });
  return response.data?.tokenPayloadExternal;
}

async function provisionPlayFab(titleId, secretKey, customId, appVersion) {
  const response = await fetch(`https://${titleId}.playfabapi.com/Server/LoginWithCustomID`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      "X-SecretKey": secretKey,
    },
    body: JSON.stringify({
      CustomId: customId,
      CreateAccount: true,
      CustomTags: {
        source: "play-integrity-provisioner",
        appVersion,
      },
    }),
  });

  const payload = await response.json().catch(() => ({}));
  if (!response.ok || payload.error) {
    const detail = payload.errorMessage || payload.error || response.statusText;
    throw new Error(`PlayFab provisioning failed (${response.status}): ${detail}`);
  }

  return payload.data || {};
}

function json(status, body) {
  return { status, jsonBody: body, headers: { "Cache-Control": "no-store" } };
}

// Read-only, static, no player data: the policy page Play Console links to.
app.http("privacy", {
  route: "privacy",
  methods: ["GET"],
  authLevel: "anonymous",
  handler: async () => privacyResponse(),
});

app.http("provision", {
  route: "provision",
  methods: ["POST"],
  authLevel: "anonymous",
  handler: async (request, context) => {
    const ip = clientIp(request);
    if (!ipRateLimiter.consume(ip)) return json(429, { ok: false, error: "rate_limited" });

    let body;
    try { body = await request.json(); }
    catch { return json(400, { ok: false, error: "invalid_request" }); }

    const customId = String(body?.customId || "").trim();
    const appVersion = String(body?.appVersion || "").trim();
    const integrityToken = String(body?.integrityToken || "").trim();

    if (!customId || customId.length > 128 || !appVersion || appVersion.length > 64 ||
        !integrityToken || integrityToken.length > 32_768) {
      return json(400, { ok: false, error: "invalid_request" });
    }

    try {
      const packageName = env("GOOGLE_PLAY_PACKAGE_NAME");
      const titleId = env("PLAYFAB_TITLE_ID");
      const secretKey = env("PLAYFAB_DEV_SECRET_KEY");
      const expectedRequestHash = canonicalRequestHash(packageName, customId, appVersion);
      const decoded = await decodeIntegrityToken(packageName, integrityToken);

      const verdict = validateIntegrityPayload(decoded, {
        packageName,
        expectedRequestHash,
        certificateSha256: optionalEnv("GOOGLE_PLAY_CERT_SHA256"),
        requireLicensed: !/^(0|false|no)$/i.test(optionalEnv("REQUIRE_LICENSED", "true")),
        maxAgeMs: Number(optionalEnv("INTEGRITY_MAX_AGE_MS", "120000")),
      });

      if (!verdict.ok) {
        // The observed value stays in the log and out of the response: the
        // caller learns only that it was refused, never which check refused it
        // or what would satisfy it.
        context.warn(
          `Provisioning rejected: ${verdict.reason}` +
          (verdict.observed ? ` (observed: ${verdict.observed})` : "")
        );
        return json(403, { ok: false, error: "integrity_rejected" });
      }

      const result = await provisionPlayFab(titleId, secretKey, customId, appVersion);
      return json(200, { ok: true, newlyCreated: Boolean(result.NewlyCreated) });
    } catch (error) {
      context.error(`Provisioning infrastructure error: ${error?.message || "unknown"}`);
      return json(503, { ok: false, error: "provisioning_unavailable" });
    }
  },
});
