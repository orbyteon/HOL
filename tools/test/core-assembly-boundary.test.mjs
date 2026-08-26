import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";

const coreDirectory = "Assets/SCRIPT/Core";
const coreAsmdefPath = path.join(coreDirectory, "HOL.Core.asmdef");
const rulesPath = path.join(coreDirectory, "DuelRules.cs");
const rulesMetaPath = rulesPath + ".meta";
const editModeAsmdefPath = "Assets/Tests/EditMode/HOL.EditModeTests.asmdef";
const editModeTestsPath = "Assets/Tests/EditMode/DuelRulesTests.cs";

function listSourceFiles(directory) {
  const files = [];
  for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
    const entryPath = path.join(directory, entry.name);
    if (entry.isDirectory()) files.push(...listSourceFiles(entryPath));
    else if (entry.isFile() && entry.name.endsWith(".cs")) files.push(entryPath);
  }
  return files.sort();
}

const core = JSON.parse(fs.readFileSync(coreAsmdefPath, "utf8"));
const editMode = JSON.parse(fs.readFileSync(editModeAsmdefPath, "utf8"));
const rulesMeta = fs.readFileSync(rulesMetaPath, "utf8");
const editModeTests = fs.readFileSync(editModeTestsPath, "utf8");
const coreSources = listSourceFiles(coreDirectory);

test("HOL.Core is a Unity-free production assembly", () => {
  assert.equal(core.name, "HOL.Core");
  assert.equal(core.rootNamespace, "HOL.Core");
  assert.equal(core.noEngineReferences, true);
  assert.equal(core.autoReferenced, true);
  assert.equal(core.allowUnsafeCode, false);
  assert.equal(core.overrideReferences, false);
  assert.deepEqual(core.references, []);
  assert.deepEqual(core.precompiledReferences, []);
  assert.deepEqual(core.includePlatforms, []);
  assert.deepEqual(core.excludePlatforms, []);
  assert.ok(coreSources.length > 0, "HOL.Core must contain production source");

  for (const sourcePath of coreSources) {
    const source = fs.readFileSync(sourcePath, "utf8");
    assert.doesNotMatch(
      source,
      /^\s*using(?:\s+[A-Za-z_]\w*\s*=)?\s+(?:UnityEngine|UnityEditor|TMPro|PlayFab|Unity\.Services|Unity\.LevelPlay|IronSource)\b/m,
      `${sourcePath} imports a forbidden framework`
    );
    assert.doesNotMatch(
      source,
      /\b(?:UnityEngine|UnityEditor|PlayerPrefs|Resources|PlayFabClientAPI|PlayFabSettings|IronSource|LevelPlay)\s*\./,
      `${sourcePath} calls a forbidden framework`
    );
    assert.doesNotMatch(
      source,
      /:\s*(?:MonoBehaviour|ScriptableObject)\b/,
      `${sourcePath} derives from a Unity object type`
    );
  }
});

test("DuelRules moved into HOL.Core without changing its Unity identity", () => {
  assert.equal(fs.existsSync("Assets/SCRIPT/DuelRules.cs"), false);
  assert.equal(fs.existsSync(rulesPath), true);
  assert.match(rulesMeta, /^guid: 903e7af94b4b40c39c0f079d78b7120a$/m);
});

test("EditMode duel tests compile against HOL.Core instead of reflection", () => {
  assert.ok(editMode.references.includes("HOL.Core"));
  assert.match(editModeTests, /new\s+DuelRules\s*\(\s*\)/);
  assert.match(editModeTests, /rules\.Submit\s*\(/);
  assert.doesNotMatch(
    editModeTests,
    /\b(?:System\.Reflection|Activator\.CreateInstance|GetMethod|GetProperty|GetField)\b/
  );
});
