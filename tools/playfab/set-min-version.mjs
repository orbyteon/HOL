// Sets the PlayFab Title Data `minVersion` force-update gate.
//
// This is step 2 of the ordering in docs/release-checklist.md section 3: the
// client ships, `minVersion` gates the clients that predate it, and only then
// does CloudScript publish. Doing it by hand in the PlayFab console is a
// release step with no record and no read-back, on the one setting that decides
// whether an out-of-date client is shown a wrong match result or an update
// screen. This makes it repeatable and auditable instead.
//
// It writes exactly one key. The key name is not an input.

const KEY = "minVersion";

const titleId = required("PLAYFAB_TITLE_ID");
const secretKey = required("PLAYFAB_DEV_SECRET_KEY");
const minVersion = required("MIN_VERSION");

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

// ForceUpdate.IsOutdated compares segment by segment and reads the leading
// digits of each, so anything it cannot parse silently becomes 0 and the gate
// stops gating. Refuse the input here rather than discover that on a device.
function assertComparable(value) {
  if (!/^\d+(\.\d+)*$/.test(value))
    throw new Error(
      `MIN_VERSION must be numeric segments such as 0.3.0 (got "${value}"). ` +
      "ForceUpdate parses only leading digits per segment; anything else " +
      "compares as zero and disables the gate."
    );
}

async function main() {
  assertComparable(minVersion);

  await playFab("/Admin/SetTitleData", { Key: KEY, Value: minVersion });

  const read = await playFab("/Admin/GetTitleData", { Keys: [KEY] });
  const stored = read?.Data?.[KEY];

  if (stored !== minVersion)
    throw new Error(
      `Read-back mismatch: ${KEY} is "${stored ?? "(unset)"}" after writing "${minVersion}"`
    );

  console.log(`Verified Title Data ${KEY} = ${stored}.`);
  console.log(
    `Clients reporting a version below ${stored} now receive the update screen ` +
    "instead of a match."
  );
}

main().catch((error) => {
  console.error(`::error::${error.message}`);
  process.exit(1);
});
