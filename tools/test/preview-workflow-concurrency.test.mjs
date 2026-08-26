import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";

const previews = [
  {
    id: "mainmenu",
    label: "preview-mainmenu",
    path: ".github/workflows/mainmenu-android-preview.yml",
  },
  {
    id: "panelplay",
    label: "preview-panelplay",
    path: ".github/workflows/panelplay-android-preview.yml",
  },
  {
    id: "splash",
    label: "preview-splash",
    path: ".github/workflows/splash-android-preview.yml",
  },
];

function concurrencyGroup(source, path) {
  const match = source.match(/^concurrency:\s*\n\s+group:\s*(.+)$/m);
  assert.ok(match, `${path} must declare an explicit concurrency group`);
  return match[1].trim();
}

test("label-triggered Android previews use screen-scoped concurrency", () => {
  const groups = [];

  for (const preview of previews) {
    const source = fs.readFileSync(preview.path, "utf8");
    const group = concurrencyGroup(source, preview.path);
    groups.push(group);

    assert.match(source, /pull_request:\s*\n\s*types:\s*\[labeled\]/);
    assert.match(source, new RegExp(`github\\.event\\.label\\.name == '${preview.label}'`));
    assert.match(group, new RegExp(preview.id));
    assert.doesNotMatch(
      group,
      /^hol-android-preview-/,
      `${preview.path} must not share the legacy cross-workflow group`,
    );
    assert.match(source, /cancel-in-progress:\s*true/);
  }

  assert.equal(
    new Set(groups).size,
    groups.length,
    "a non-matching labeled workflow must never cancel the requested screen capture",
  );
});
