import assert from "node:assert/strict";
import fs from "node:fs";
import test from "node:test";

const workflowPath = ".github/workflows/daily-hunt-android-preview.yml";
const workflow = fs.readFileSync(workflowPath, "utf8");

test("Daily Hunt native preview captures the complete EN and EL viewport matrix", () => {
  const captures = workflow.match(
    /^\s*capture_viewport (?:en|el) HOL_DAILYHUNT_CAPTURE_READY_(?:EN|EL) \d+ \d+ \d+ \S+\.png$/gm,
  ) ?? [];

  assert.deepEqual(captures.map((line) => line.trim()), [
    "capture_viewport en HOL_DAILYHUNT_CAPTURE_READY_EN 1080 1920 420 daily-hunt-en-1080x1920.png",
    "capture_viewport el HOL_DAILYHUNT_CAPTURE_READY_EL 1080 1920 420 daily-hunt-el-1080x1920.png",
    "capture_viewport en HOL_DAILYHUNT_CAPTURE_READY_EN 1080 2400 420 daily-hunt-en-1080x2400.png",
    "capture_viewport el HOL_DAILYHUNT_CAPTURE_READY_EL 1080 2400 420 daily-hunt-el-1080x2400.png",
    "capture_viewport en HOL_DAILYHUNT_CAPTURE_READY_EN 1179 2556 460 daily-hunt-en-1179x2556.png",
    "capture_viewport el HOL_DAILYHUNT_CAPTURE_READY_EL 1179 2556 460 daily-hunt-el-1179x2556.png",
  ]);
});

test("Daily Hunt preview settles PackageManager before launching the capture app", () => {
  assert.match(workflow, /cmd package wait-for-handler --timeout 60000/);
  assert.match(workflow, /cmd package wait-for-background-handler --timeout 60000/);
  assert.match(workflow, /adb shell pm path "\$package_name"/);
  assert.match(
    workflow,
    /adb install -r "\$apk"[\s\S]*?wait_for_package_manager[\s\S]*?capture_viewport\(\)/,
  );
});

test("Daily Hunt preview retries only a verified early process exit", () => {
  assert.match(workflow, /for launch_attempt in 1 2; do/);
  assert.match(workflow, /adb shell pidof "\$package_name"/);
  assert.match(workflow, /if \[ "\$process_exited" -ne 1 \]; then/);
  assert.match(workflow, /Daily Hunt exited before capture readiness/);
  assert.doesNotMatch(workflow, /for launch_attempt in 1 2 3/);
});
