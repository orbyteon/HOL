import fs from 'node:fs/promises';
import { pathToFileURL } from 'node:url';
import { GoogleAuth } from 'google-auth-library';

const ANDROID_PUBLISHER_SCOPE = 'https://www.googleapis.com/auth/androidpublisher';
const TRACK = 'internal';

function required(name) {
  const value = (process.env[name] || '').trim();
  if (!value) throw new Error(`${name} is required`);
  return value;
}

function parseServiceAccount(encoded) {
  let parsed;
  try {
    parsed = JSON.parse(Buffer.from(encoded, 'base64').toString('utf8'));
  } catch (error) {
    throw new Error(`GOOGLE_SERVICE_ACCOUNT_JSON_B64 is not valid base64 JSON: ${error.message}`);
  }
  if (!parsed || typeof parsed !== 'object' || !parsed.client_email || !parsed.private_key) {
    throw new Error('GOOGLE_SERVICE_ACCOUNT_JSON_B64 does not contain a Google service account credential');
  }
  return parsed;
}

async function responseBody(response) {
  const text = await response.text();
  if (!text) return {};
  try {
    return JSON.parse(text);
  } catch {
    return { raw: text.slice(0, 4000) };
  }
}

async function apiRequest(token, url, options, label) {
  const response = await fetch(url, {
    ...options,
    headers: {
      Authorization: `Bearer ${token}`,
      ...(options.headers || {})
    }
  });
  const body = await responseBody(response);
  if (!response.ok) {
    const detail = typeof body === 'object' ? JSON.stringify(body).slice(0, 4000) : String(body).slice(0, 4000);
    throw new Error(`${label} failed with HTTP ${response.status}: ${detail}`);
  }
  return body;
}

export function buildInternalTrack(version, versionCode) {
  return {
    track: TRACK,
    releases: [
      {
        name: `HOL ${version} (${versionCode}) internal`,
        versionCodes: [String(versionCode)],
        status: 'completed',
        releaseNotes: [
          {
            language: 'en-US',
            text: `HOL ${version} internal test build ${versionCode}.`
          }
        ]
      }
    ]
  };
}

export async function publishInternal() {
  const packageName = required('GOOGLE_PLAY_PACKAGE_NAME');
  const aabPath = required('AAB_PATH');
  const version = required('PUBLIC_VERSION');
  const expectedVersionCode = required('EXPECTED_VERSION_CODE');
  const serviceAccount = parseServiceAccount(required('GOOGLE_SERVICE_ACCOUNT_JSON_B64'));

  if (packageName !== 'com.Orbyteon.HOL') {
    throw new Error(`GOOGLE_PLAY_PACKAGE_NAME must be com.Orbyteon.HOL, got ${packageName}`);
  }
  if (!/^\d+$/.test(expectedVersionCode) || Number(expectedVersionCode) < 1) {
    throw new Error('EXPECTED_VERSION_CODE must be a positive integer');
  }

  const auth = new GoogleAuth({
    credentials: serviceAccount,
    scopes: [ANDROID_PUBLISHER_SCOPE]
  });
  const client = await auth.getClient();
  const access = await client.getAccessToken();
  const token = access && access.token;
  if (!token) throw new Error('Google service account did not return an Android Publisher access token');

  const encodedPackage = encodeURIComponent(packageName);
  const base = `https://androidpublisher.googleapis.com/androidpublisher/v3/applications/${encodedPackage}`;

  const edit = await apiRequest(token, `${base}/edits`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: '{}'
  }, 'Create Google Play edit');
  const editId = edit.id;
  if (!editId) throw new Error('Google Play edit response did not include an edit id');

  const bundleBytes = await fs.readFile(aabPath);
  const uploadUrl = `https://androidpublisher.googleapis.com/upload/androidpublisher/v3/applications/${encodedPackage}/edits/${encodeURIComponent(editId)}/bundles?uploadType=media`;
  const bundle = await apiRequest(token, uploadUrl, {
    method: 'POST',
    headers: { 'Content-Type': 'application/octet-stream' },
    body: bundleBytes
  }, 'Upload Android App Bundle');

  const uploadedVersionCode = String(bundle.versionCode || '');
  if (uploadedVersionCode !== expectedVersionCode) {
    throw new Error(`Uploaded bundle versionCode ${uploadedVersionCode || '(missing)'} does not match expected ${expectedVersionCode}`);
  }

  const trackPayload = buildInternalTrack(version, expectedVersionCode);
  const trackUrl = `${base}/edits/${encodeURIComponent(editId)}/tracks/${TRACK}`;
  const track = await apiRequest(token, trackUrl, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(trackPayload)
  }, 'Update Google Play internal track');

  const releaseContainsVersion = Array.isArray(track.releases) && track.releases.some(release =>
    Array.isArray(release.versionCodes) && release.versionCodes.map(String).includes(expectedVersionCode)
  );
  if (!releaseContainsVersion) {
    throw new Error(`Google Play internal track response did not contain versionCode ${expectedVersionCode}`);
  }

  await apiRequest(token, `${base}/edits/${encodeURIComponent(editId)}:commit`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: '{}'
  }, 'Commit Google Play edit');

  console.log(`Published ${packageName} ${version} (${expectedVersionCode}) to the Google Play internal track.`);
}

const entrypoint = process.argv[1] ? pathToFileURL(process.argv[1]).href : '';
if (import.meta.url === entrypoint) {
  publishInternal().catch(error => {
    console.error(`::error::${error.message}`);
    process.exit(1);
  });
}
