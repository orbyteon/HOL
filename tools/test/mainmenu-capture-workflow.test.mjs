import assert from "node:assert/strict";
import fs from "node:fs";
import test from "node:test";

const workflow = fs.readFileSync(
  new URL("../../.github/workflows/mainmenu-android-preview.yml", import.meta.url),
  "utf8");

test("Home preview workflow captures Main Menu, not Splash", () => {
  assert.match(workflow, /hol_capture_screen mainmenu/);
  assert.match(workflow, /HOL_MAINMENU_CAPTURE_READY/);
  assert.match(workflow, /mainmenu\.png/);
  assert.match(workflow, /HOL-mainmenu-debug\.apk/);
  assert.doesNotMatch(workflow, /hol_capture_screen splash/);
  assert.doesNotMatch(workflow, /HOL_SPLASH_CAPTURE_READY/);
  assert.doesNotMatch(workflow, /adb logcat -c/);
});

test("Home preview APK stays development ARM64+x86_64 GLES3", () => {
  assert.match(workflow, /MainMenuPreviewBuild\.Build/);
  assert.match(workflow, /versioning: None/);
  assert.match(workflow, /ram-size: 4096M/);
  assert.match(workflow, /-feature -Vulkan/);
  assert.doesNotMatch(workflow, /production:/);
});
