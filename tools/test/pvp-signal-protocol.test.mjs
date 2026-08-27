import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";

const protocolPath = "Assets/SCRIPT/Application/PvpSignalProtocol.cs";
const adapterPath = "Assets/SCRIPT/PvP/Signals.cs";
const cloudScriptPath = "playfab/cloudscript.js";
const iconTestsPath = "Assets/Tests/EditMode/SignalIconTests.cs";
const protocolTestsPath = "Assets/Tests/EditMode/PvpSignalProtocolTests.cs";

function read(path) {
  return fs.readFileSync(path, "utf8");
}

function numberConstant(source, name) {
  const match = source.match(new RegExp(
    `(?:var|const|public\\s+const\\s+int)\\s+${name}\\s*=\\s*(\\d+)`
  ));
  assert.ok(match, `${name} not found`);
  return Number(match[1]);
}

test("Signal ids and cap stay aligned with CloudScript", () => {
  const protocol = read(protocolPath);
  const cloud = read(cloudScriptPath);
  const expected = [
    "signal_luck",
    "signal_close",
    "signal_ouch",
    "signal_nice",
    "signal_your_turn",
    "signal_gg",
  ];
  const keys = [...protocol.matchAll(/"(signal_[a-z_]+)"/g)]
    .map((match) => match[1]);

  assert.deepEqual(keys, expected);
  assert.equal(keys.length, numberConstant(cloud, "SIGNAL_COUNT"));
  assert.equal(
    numberConstant(protocol, "CapPerSide"),
    numberConstant(cloud, "SIGNAL_CAP_PER_SIDE")
  );
});

test("Unity Signals is a localization adapter, not a duplicate protocol", () => {
  const adapter = read(adapterPath);

  assert.match(adapter,
    /public static readonly string\[\] Table = PvpSignalProtocol\.Keys;/);
  assert.match(adapter,
    /public const int CapPerSide = PvpSignalProtocol\.CapPerSide;/);
  assert.match(adapter, /PvpSignalProtocol\.IsValid\(id\)/);
  assert.match(adapter, /PvpSignalProtocol\.Key\(id\)/);
  assert.match(adapter, /L10n\.Get\(key\)/);
  assert.doesNotMatch(adapter, /"signal_[a-z_]+"/);
});

test("Signal tests bind the application protocol directly without reflection", () => {
  const icons = read(iconTestsPath);
  const protocolTests = read(protocolTestsPath);

  assert.match(icons, /PvpSignalProtocol\.Keys/);
  assert.doesNotMatch(icons, /System\.Reflection/);
  assert.doesNotMatch(icons, /AppDomain\.CurrentDomain/);

  assert.match(protocolTests, /PvpSignalProtocol\.Keys/);
  assert.match(protocolTests, /PvpSignalProtocol\.CapPerSide/);
  assert.doesNotMatch(protocolTests, /System\.Reflection/);
});
