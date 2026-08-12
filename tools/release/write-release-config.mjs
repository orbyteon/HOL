import fs from "node:fs/promises";

function required(name) {
  const value = String(process.env[name] || "").trim();
  if (!value) throw new Error(`${name} is required`);
  return value;
}

const playFabTitleId = required("PLAYFAB_TITLE_ID");
const provisioningUrl = required("PROVISIONING_URL");
const projectText = required("GOOGLE_CLOUD_PROJECT_NUMBER");

if (!/^[A-Za-z0-9]{3,32}$/.test(playFabTitleId))
  throw new Error("PLAYFAB_TITLE_ID must be 3-32 letters/digits");

let url;
try { url = new URL(provisioningUrl); }
catch { throw new Error("PROVISIONING_URL must be a valid absolute URL"); }
if (url.protocol !== "https:" || !url.hostname || url.username || url.password)
  throw new Error("PROVISIONING_URL must be HTTPS and must not contain embedded credentials");

if (!/^\d+$/.test(projectText))
  throw new Error("GOOGLE_CLOUD_PROJECT_NUMBER must contain only digits");
const googleCloudProjectNumber = Number(projectText);
if (!Number.isSafeInteger(googleCloudProjectNumber) || googleCloudProjectNumber <= 0)
  throw new Error("GOOGLE_CLOUD_PROJECT_NUMBER must be a positive safe integer");

const output = {
  playFabTitleId,
  provisioningUrl: url.toString(),
  googleCloudProjectNumber,
};

await fs.mkdir("Assets/Resources", { recursive: true });
await fs.writeFile(
  "Assets/Resources/HOLReleaseConfig.json",
  `${JSON.stringify(output, null, 2)}\n`,
  "utf8",
);

console.log("Wrote validated public release configuration.");
