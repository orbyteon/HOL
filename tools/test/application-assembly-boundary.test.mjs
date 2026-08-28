import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";

const appDir = "Assets/SCRIPT/Application";
const asmdefPath = `${appDir}/HOL.Application.asmdef`;
const testsAsmdefPath = "Assets/Tests/EditMode/HOL.EditModeTests.asmdef";
const playModeTestsAsmdefPath =
  "Assets/Tests/PlayMode/HOL.PlayModeTests.asmdef";
const missionFlowPlayModeTestsPath =
  "Assets/Tests/PlayMode/DailyChallengeMissionFlowPlayModeTests.cs";
const outcomeTestsPath = "Assets/Tests/EditMode/MatchOutcomeTests.cs";
const roomStatePath = `${appDir}/PvpRoomState.cs`;
const roomStateTestsPath = "Assets/Tests/EditMode/PvpRoomStateTests.cs";
const signalProtocolPath = `${appDir}/PvpSignalProtocol.cs`;
const signalProtocolTestsPath =
  "Assets/Tests/EditMode/PvpSignalProtocolTests.cs";
const pvpBackendPath = "Assets/SCRIPT/PvP/PvpBackend.cs";
const scopedAgentPath = `${appDir}/AGENTS.md`;
const phase1bValidationDocPath =
  "docs/architecture/phase-1b-application-validation.md";
const phase1bChangelogFragmentPath =
  "CHANGELOG.d/phase-1b-application.md";
const phase1cValidationDocPath =
  "docs/architecture/phase-1c-pvp-room-state-validation.md";
const phase1cChangelogFragmentPath =
  "CHANGELOG.d/phase-1c-pvp-room-state.md";
const phase1dValidationDocPath =
  "docs/architecture/phase-1d-signal-protocol-validation.md";
const phase1dChangelogFragmentPath =
  "CHANGELOG.d/phase-1d-signal-protocol.md";

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
    /\bL10n\b/,
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
  assert.match(friends, /InternalsVisibleTo\("HOL.PlayModeTests"\)/);
});

test("EditMode outcome tests bind HOL.Application without reflection", () => {
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

test("PlayMode mission flow binds HOL.Application semantic events directly", () => {
  const testAsmdef = JSON.parse(read(playModeTestsAsmdefPath));
  assert.ok(
    testAsmdef.references.includes("HOL.Application"),
    "PlayMode tests must reference HOL.Application directly"
  );

  const source = read(missionFlowPlayModeTestsPath);
  assert.match(source, /GameEvents\.CorrectGuess\(\)/);
  assert.match(source, /GameEvents\.RoomShared\(\)/);
  assert.match(
    source,
    /GameEvents\.MatchCompleted\(WinningOutcome\(\)\)/
  );
  assert.match(source, /new MatchOutcome/);
  assert.doesNotMatch(source, /RuntimeType\("GameEvents"\)/);
  assert.doesNotMatch(source, /RuntimeType\("MatchOutcome"\)/);
  assert.doesNotMatch(source, /InvokeStatic\("GameEvents"/);
});

test("PvP room state is an application contract with a fieldless runtime shim", () => {
  const state = read(roomStatePath);
  assert.match(state, /\[Serializable\]/);
  assert.match(state, /public\s+class\s+PvpRoomState/);
  assert.match(state, /public\s+string\s+hostName\s*=/);
  assert.match(state, /public\s+int\s+matchIndex\s*;/);
  assert.match(state, /public\s+bool\s+IsMatchPointAgainst\s*\(/);

  assert.match(read(`${roomStatePath}.meta`),
    /guid:\s*cfd673ea22ec47f0b612e95424861e0b/);

  const backend = read(pvpBackendPath);
  assert.match(
    backend,
    /public\s+class\s+RoomState\s*:\s*PvpRoomState\s*\{\s*\}/s
  );
  assert.doesNotMatch(backend, /public\s+string\s+hostName\s*=/);
  assert.doesNotMatch(backend, /public\s+int\s+matchIndex\s*;/);

  const tests = read(roomStateTestsPath);
  assert.doesNotMatch(tests, /System\.Reflection/);
  assert.doesNotMatch(tests, /Activator\.CreateInstance/);
  assert.match(tests, /new PvpRoomState/);
  assert.match(tests, /IsMatchPointAgainst/);
  assert.match(
    tests,
    /sealed\s+class\s+DerivedRoomState\s*:\s*PvpRoomState\s*\{\s*\}/s
  );
  assert.match(tests, /JsonUtility\.FromJson<DerivedRoomState>/);
});

test("fixed Signal protocol belongs to HOL.Application with direct tests", () => {
  const protocol = read(signalProtocolPath);
  assert.match(protocol, /public\s+static\s+class\s+PvpSignalProtocol/);
  assert.match(protocol, /public\s+static\s+readonly\s+string\[\]\s+Keys/);
  assert.match(protocol, /public\s+const\s+int\s+CapPerSide\s*=\s*12/);
  assert.match(protocol, /public\s+static\s+bool\s+IsValid\s*\(/);
  assert.match(protocol, /public\s+static\s+string\s+Key\s*\(/);
  assert.doesNotMatch(protocol, /\bL10n\b/);
  assert.doesNotMatch(protocol, /\bResources\b/);

  assert.match(read(`${signalProtocolPath}.meta`),
    /guid:\s*0a3a957b710043878bc8d3e41c2c75f0/);

  const tests = read(signalProtocolTestsPath);
  assert.match(tests, /PvpSignalProtocol\.Keys/);
  assert.match(tests, /PvpSignalProtocol\.CapPerSide/);
  assert.doesNotMatch(tests, /System\.Reflection/);
  assert.doesNotMatch(tests, /AppDomain\.CurrentDomain/);
});

test("Phase 1B keeps scoped agent, validation and release-note contracts", () => {
  const scopedAgent = read(scopedAgentPath);
  assert.match(scopedAgent, /HOL\.Application — Mandatory Agent Contract/);
  assert.match(scopedAgent, /Do not reference `UnityEngine`/);
  assert.match(scopedAgent, /legacy JSON formatting methods/);

  const validation = read(phase1bValidationDocPath);
  assert.match(validation, /exact PR merge\s+candidate/);
  assert.match(validation, /Android compile/);
  assert.match(validation, /Automatic PlayMode/);
  assert.match(validation, /focused release-note fragment/);

  const fragment = read(phase1bChangelogFragmentPath);
  assert.match(fragment, /Introduced `HOL\.Application`/);
  assert.match(fragment, /preserving their committed Unity GUIDs/);
  assert.match(fragment, /No gameplay, scene, UI, persistence/);
});

test("Phase 1C keeps room-state guidance, validation and release notes", () => {
  const scopedAgent = read(scopedAgentPath);
  assert.match(scopedAgent, /`PvpRoomState`/);
  assert.match(scopedAgent, /wire field names/);
  assert.match(scopedAgent, /fieldless compatibility shim/);

  const validation = read(phase1cValidationDocPath);
  assert.match(validation, /exact PR merge candidate/);
  assert.match(validation, /CloudScript-emitted keys/);
  assert.match(validation, /Automatic PlayMode/);

  const fragment = read(phase1cChangelogFragmentPath);
  assert.match(fragment, /Moved the PvP public room-state contract/);
  assert.match(fragment, /fieldless compatibility shim/);
  assert.match(fragment, /No gameplay, transport, scene, UI or deployment/);
});

test("Phase 1D keeps Signal guidance, validation and release notes", () => {
  const scopedAgent = read(scopedAgentPath);
  assert.match(scopedAgent, /`PvpSignalProtocol`/);
  assert.match(scopedAgent, /append-only protocol/);
  assert.match(scopedAgent, /Unity `Signals` adapter/);

  const validation = read(phase1dValidationDocPath);
  assert.match(validation, /exact PR merge\s+candidate/);
  assert.match(validation, /SIGNAL_COUNT/);
  assert.match(validation, /Automatic PlayMode/);

  const fragment = read(phase1dChangelogFragmentPath);
  assert.match(fragment, /Moved the ordered six Signal localization keys/);
  assert.match(fragment, /thin localization adapter/);
  assert.match(fragment, /No gameplay, networking, moderation/);
});
