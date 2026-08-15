import { readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";

// Serves the privacy policy from the Function App, because Play Console
// requires a stable public HTTPS URL for the policy before the app can be
// published, and this is the one public HTTPS host the project already runs.
//
// The HTML is a committed byte-copy of docs/privacy.html — CI fails if the two
// ever differ — and is read once at cold start: the file is inside the
// deployment package and cannot change while the process lives.
const here = dirname(fileURLToPath(import.meta.url));
const PRIVACY_PATH = join(here, "..", "static", "privacy.html");

let cached = null;

export function privacyHtml() {
  if (cached === null) cached = readFileSync(PRIVACY_PATH, "utf8");
  return cached;
}

export function privacyResponse() {
  return {
    status: 200,
    body: privacyHtml(),
    headers: {
      "Content-Type": "text/html; charset=utf-8",
      // Cacheable, briefly: reviewers and players read it; it changes only on
      // deploy, and an hour of staleness on a policy page is acceptable.
      "Cache-Control": "public, max-age=3600",
      "X-Content-Type-Options": "nosniff",
    },
  };
}
