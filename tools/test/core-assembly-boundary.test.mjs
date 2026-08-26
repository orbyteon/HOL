import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";

const coreAsmdefPath = "Assets/SCRIPT/Core/HOL.Core.asmdef";
const rulesPath = "Assets/SCRIPT/Core/DuelRules.cs";
const rulesMetaPath = rulesPath + ".meta";
const editModeAsmdefPath = "Assets/Tests/EditMode/HOL.EditModeTests.asmdef";
const editModeTestsPath = "Assets/Tests/EditMode/DuelRulesTests.cs";

const core = JSON.parse(fs.readFileSync(coreAsmdefPath, "utf8"));
const editMode = JSON.parse(fs.readFileSync(editModeAsmdefPath, "utf8"));
const rules = fs.readFileSync(rulesPath, "utf8");
const rulesMeta = fs.readFileSync(rulesMetaPath, "utf8");
const editModeTests = fs.readFileSync(editModeTestsPath, "utf8");

test("HOL.Core is a Unity-free production assembly", () => {
  assert.equal(core.name, "HOL.Core");
  assert.equal(core.noEngineReferences, true);
  assert.equal(core.autoReferenced, true);
  assert.deepEqual(core.references, []);
  assert.doesNotMatch(rules, /^\s*using\s+Unity(?:Engine|Editor)\b/m);
  assert.doesNotMatch(
    rules,
    /\b(?:UnityEngine|UnityEditor|PlayerPrefs|Resources)\s*\./
  );
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
