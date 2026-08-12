import fs from "node:fs/promises";

const titleId = required("PLAYFAB_TITLE_ID");
const secretKey = required("PLAYFAB_DEV_SECRET_KEY");
const hardenPolicy = /^(1|true|yes)$/i.test(process.env.HARDEN_SHARED_GROUP_POLICY || "");
const commit = process.env.GITHUB_SHA || "local";

function required(name) {
  const value = (process.env[name] || "").trim();
  if (!value) throw new Error(`${name} is required`);
  return value;
}

async function playFab(path, body) {
  const response = await fetch(`https://${titleId}.playfabapi.com${path}`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      "X-SecretKey": secretKey,
    },
    body: JSON.stringify(body),
  });

  const text = await response.text();
  let json = {};
  try { json = text ? JSON.parse(text) : {}; }
  catch { throw new Error(`${path} returned non-JSON (${response.status}): ${text.slice(0, 500)}`); }

  if (!response.ok || json.error) {
    const detail = json.errorMessage || json.error || text || response.statusText;
    throw new Error(`${path} failed (${response.status}): ${detail}`);
  }

  return json.data ?? json;
}

async function deployCloudScript() {
  const source = await fs.readFile("playfab/cloudscript.js", "utf8");
  const result = await playFab("/Admin/UpdateCloudScript", {
    Files: [{ Filename: "cloudscript.js", FileContents: source }],
    Publish: true,
    CustomTags: {
      source: "github-actions",
      repository: process.env.GITHUB_REPOSITORY || "orbyteon/HOL",
      commit: commit.slice(0, 40),
    },
  });

  console.log(`Published Legacy CloudScript version ${result.Version}, revision ${result.Revision}.`);
  return result;
}

const sharedGroupClientResources = [
  "pfrn:api--/Client/CreateSharedGroup",
  "pfrn:api--/Client/GetSharedGroupData",
  "pfrn:api--/Client/UpdateSharedGroupData",
  "pfrn:api--/Client/AddSharedGroupMembers",
  "pfrn:api--/Client/RemoveSharedGroupMembers",
];

async function hardenSharedGroupPolicy() {
  const policy = await playFab("/Admin/GetPolicy", { PolicyName: "ApiPolicy" });
  const current = Array.isArray(policy.Statements) ? policy.Statements : [];

  const missing = sharedGroupClientResources.filter(resource =>
    !current.some(statement =>
      statement?.Resource === resource &&
      String(statement?.Effect).toLowerCase() === "deny" &&
      (statement?.Principal ?? "*") === "*"
    )
  );

  if (missing.length === 0) {
    console.log("Client Shared Group APIs are already denied by explicit policy statements.");
    return;
  }

  const statements = missing.map(Resource => ({
    Resource,
    Action: "*",
    Effect: "Deny",
    Principal: "*",
    Comment: "HOL server-authoritative PvP: deny direct Client Shared Group access",
  }));

  const update = {
    PolicyName: "ApiPolicy",
    PolicyVersion: policy.PolicyVersion,
    OverwritePolicy: false,
    Statements: statements,
  };

  const validation = await playFab("/Admin/ValidateApiPolicy", update);
  if (!validation.IsValid) {
    throw new Error(`PlayFab rejected the proposed API policy: ${(validation.ValidationErrors || []).join("; ")}`);
  }
  for (const warning of validation.Warnings || []) console.warn(`Policy warning: ${warning}`);

  const result = await playFab("/Admin/UpdatePolicy", update);
  for (const warning of result.Warnings || []) console.warn(`Policy warning: ${warning}`);
  console.log(`Added ${missing.length} Client Shared Group deny statement(s).`);
}

await deployCloudScript();
if (hardenPolicy) await hardenSharedGroupPolicy();
