import fs from "node:fs/promises";

const titleId = required("PLAYFAB_TITLE_ID");
const secretKey = required("PLAYFAB_DEV_SECRET_KEY");
const hardenPolicy = /^(1|true|yes)$/i.test(process.env.HARDEN_SHARED_GROUP_POLICY || "");
const commit = process.env.GITHUB_SHA || "local";
const cleanupTaskName = "HOL expired PvP room cleanup";
const cleanupTaskSchedule = "*/5 * * * *";

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

async function verifyPublishedCloudScript(source, deployment) {
  if (!Number.isInteger(deployment?.Revision) || !Number.isInteger(deployment?.Version))
    throw new Error("UpdateCloudScript did not return a version/revision to verify");

  const revision = await playFab("/Admin/GetCloudScriptRevision", {
    Revision: deployment.Revision,
    Version: deployment.Version,
  });

  if (revision.IsPublished !== true)
    throw new Error(`CloudScript revision ${deployment.Revision} is not published`);
  if (revision.Revision !== deployment.Revision || revision.Version !== deployment.Version)
    throw new Error("Published CloudScript version/revision does not match deployment response");

  const files = Array.isArray(revision.Files) ? revision.Files : [];
  const cloudScript = files.find(file => file?.Filename === "cloudscript.js");
  if (!cloudScript)
    throw new Error("Published CloudScript revision does not contain cloudscript.js");
  if (cloudScript.FileContents !== source)
    throw new Error("Published CloudScript source differs from the repository source");

  console.log(`Verified published Legacy CloudScript version ${revision.Version}, revision ${revision.Revision}.`);
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
  await verifyPublishedCloudScript(source, result);
  return result;
}

function cleanupTaskDefinition() {
  return {
    Name: cleanupTaskName,
    Description: "Delete expired server-authoritative PvP room state and stale registry entries.",
    IsActive: true,
    Schedule: cleanupTaskSchedule,
    Parameter: {
      FunctionName: "cleanupExpiredRooms",
      Argument: {},
    },
  };
}

async function ensureCleanupTask() {
  const found = await playFab("/Admin/GetTasks", {
    Identifier: { Name: cleanupTaskName },
  });
  const existing = Array.isArray(found.Tasks) ? found.Tasks[0] : undefined;
  const desired = cleanupTaskDefinition();

  if (existing) {
    await playFab("/Admin/UpdateTask", {
      ...desired,
      Type: "CloudScript",
      Identifier: { Id: existing.TaskId },
    });
    console.log(`Updated scheduled cleanup task ${existing.TaskId}.`);
  } else {
    const created = await playFab("/Admin/CreateCloudScriptTask", desired);
    if (!created.TaskId) throw new Error("CreateCloudScriptTask returned no TaskId");
    console.log(`Created scheduled cleanup task ${created.TaskId}.`);
  }

  const verified = await playFab("/Admin/GetTasks", {
    Identifier: { Name: cleanupTaskName },
  });
  const task = Array.isArray(verified.Tasks) ? verified.Tasks[0] : undefined;
  if (!task || task.Type !== "CloudScript" || task.IsActive !== true ||
      task.Schedule !== cleanupTaskSchedule ||
      task.Parameter?.FunctionName !== "cleanupExpiredRooms") {
    throw new Error("Scheduled cleanup task verification failed");
  }
  console.log(`Verified scheduled room cleanup (${cleanupTaskSchedule} UTC).`);
}

const sharedGroupClientResources = [
  "pfrn:api--/Client/CreateSharedGroup",
  "pfrn:api--/Client/GetSharedGroupData",
  "pfrn:api--/Client/UpdateSharedGroupData",
  "pfrn:api--/Client/AddSharedGroupMembers",
  "pfrn:api--/Client/RemoveSharedGroupMembers",
];

function isGlobalDeny(statement, resource) {
  return statement?.Resource === resource &&
    String(statement?.Effect).toLowerCase() === "deny" &&
    (statement?.Principal ?? "*") === "*";
}

async function verifySharedGroupPolicy() {
  const policy = await playFab("/Admin/GetPolicy", { PolicyName: "ApiPolicy" });
  const current = Array.isArray(policy.Statements) ? policy.Statements : [];
  const missing = sharedGroupClientResources.filter(resource =>
    !current.some(statement => isGlobalDeny(statement, resource))
  );

  if (missing.length > 0)
    throw new Error(`PlayFab API policy verification failed; missing deny statements: ${missing.join(", ")}`);

  console.log("Verified direct Client Shared Group APIs are denied by PlayFab API policy.");
  return policy;
}

async function hardenSharedGroupPolicy() {
  const policy = await playFab("/Admin/GetPolicy", { PolicyName: "ApiPolicy" });
  const current = Array.isArray(policy.Statements) ? policy.Statements : [];

  const missing = sharedGroupClientResources.filter(resource =>
    !current.some(statement => isGlobalDeny(statement, resource))
  );

  if (missing.length > 0) {
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
  } else {
    console.log("Client Shared Group APIs are already denied by explicit policy statements.");
  }

  await verifySharedGroupPolicy();
}

// Fail closed: production sets HARDEN_SHARED_GROUP_POLICY=true, so the client
// access policy must be hardened and verified before any new CloudScript is
// published. If policy hardening fails, production code remains untouched. If
// CloudScript publishing then fails, the safer deny policy remains in effect.
if (hardenPolicy) await hardenSharedGroupPolicy();
await deployCloudScript();
await ensureCleanupTask();
