import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";

const appDir = "Assets/SCRIPT/Application";
const asmdefPath = `${appDir}/HOL.Application.asmdef`;
const testsAsmdefPath = "Assets/Tests/EditMode/HOL.EditModeTests.asmdef";
const outcomeTestsPath = "Assets/Tests/EditMode/MatchOutcomeTests.cs";
const scopedAgentPath = `${appDir}/AGENTS.md`;
const validationDocPath = "docs/architecture/phase-1b-application-validation.md";
const changelogFragmentPath = "CHANGELOG.d/phase-1b-application.md";

function read(file) {
  return fs.readFileSync(file, "utf8");
}

function csharpFiles(dir) {
  return fs.readdirSync(dir, { withFileTypes: true }).flatMap((entry) => {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) return csharpFiles(full);
    return entry.isFile() && entry.name.endsWith(".cs") ? [full] : [];
  });
}

test("HOL.Application is a Unity-free module that depends only on HOL.Core", () => {
  const asmdef = JSON.parse(read(asmdefPath));
  assert.equal(asmdef.name, "HOL.Application");
  assert.equal(asmdef.noEngineReferences, true);
  assert.equal(asmdef.autoReferenced, true);
  assert.deepEqual(asmdef.references, ["HOL.Core"]);
  assert.deepEqual(asmdef.precompiledReferences, []);

  const forbidden = [
    /\busing\s+UnityEngine\b/,
    /\busing\s+UnityEditor\b/,
    /\bPlayerPrefs\b/,
    /\bResources\s*\./,
    /\bMonoBehaviour\b/,
    /\bScriptableObject\b/,
    /\bUnityWebRequest\b/,
    /\bPlayFabPvpClient\b/,
    /\bAdsManager\b/,
    /\bTMP_/,
  ];

  for (const file of csharpFiles(appDir)) {
    const source = read(file);
    for (const pattern of forbidden)
      assert.doesNotMatch(source, pattern, `${file} violates HOL.Application`);
  }
});

test("outcome and event contracts moved with stable Unity identities", () => {
  assert.equal(fs.existsSync("Assets/SCRIPT/SmartHooks/MatchOutcome.cs"), false);
  assert.equal(fs.existsSync("Assets/SCRIPT/SmartHooks/GameEvents.cs"), false);
  assert.equal(fs.existsSync(`${appDir}/MatchOutcome.cs`), true);
  assert.equal(fs.existsSync(`${appDir}/GameEvents.cs`), true);

  assert.match(read(`${appDir}/MatchOutcome.cs.meta`),
    /guid:\s*e5009990eddaa9d5fdcb1d7740837266/);
  assert.match(read(`${appDir}/GameEvents.cs.meta`),
    /guid:\s*ed3199f0ddb04587b14bd7948f1bc7c0/);

  const friends = read(`${appDir}/AssemblyInfo.cs`);
  assert.match(friends, /InternalsVisibleTo\("Assembly-CSharp"\)/);
  assert.match(friends, /InternalsVisibleTo\("HOL.EditModeTests"\)/);
});

test("EditMode tests bind HOL.Application at compile time without reflection", () => {
  const testAsmdef = JSON.parse(read(testsAsmdefPath));
  assert.ok(testAsmdef.references.includes("HOL.Application"));

  const source = read(outcomeTestsPath);
  assert.doesNotMatch(source, /System\.Reflection/);
  assert.doesNotMatch(source, /Activator\.CreateInstance/);
  assert.doesNotMatch(source, /FindGameType/);
  assert.doesNotMatch(source, /GetMethod\s*\(/);
  assert.match(source, /new MatchOutcome/);
  assert.match(source, /GameEvents\.MatchCompleted/);
});

test("Phase 1B keeps scoped agent, validation and release-note contracts", () => {
  const scopedAgent = read(scopedAgentPath);
  assert.match(scopedAgent, /HOL\.Application — Mandatory Agent Contract/);
  assert.match(scopedAgent, /Do not reference `UnityEngine`/);
  assert.match(scopedAgent, /legacy JSON formatting methods/);

  const validation = read(validationDocPath);
  assert.match(validation, /exact PR merge candidate/);
  assert.match(validation, /Android compile/);
  assert.match(validation, /Automatic PlayMode/);

  const fragment = read(changelogFragmentPath);
  assert.match(fragment, /Introduced `HOL\.Application`/);
  assert.match(fragment, /preserving their committed Unity GUIDs/);
  assert.match(fragment, /No gameplay, scene, UI, persistence/);
});
