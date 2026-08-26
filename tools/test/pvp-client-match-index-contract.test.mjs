import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";

const clientPath = "Assets/SCRIPT/PvP/PlayFabPvpClient.cs";
const backendPath = "Assets/SCRIPT/PvP/PvpBackend.cs";

function methodSlice(source, signature, nextSignature) {
  const start = source.indexOf(signature);
  assert.notEqual(start, -1, `${signature} not found`);
  const end = nextSignature ? source.indexOf(nextSignature, start + signature.length) : -1;
  return source.slice(start, end >= 0 ? end : source.length);
}

test("PlayFab match-scoped commands carry the authoritative match index", () => {
  const client = fs.readFileSync(clientPath, "utf8");
  const backend = fs.readFileSync(backendPath, "utf8");

  assert.match(backend,
    /RequestRematch\(int secret, int matchIndex,[\s\S]*?Action<bool> done\)/,
    "PvpBackend must expose an explicit match-index rematch overload");
  assert.match(backend,
    /AcknowledgeResult\(int matchIndex\)/,
    "PvpBackend must expose an explicit match-index acknowledgement overload");

  const signal = methodSlice(
    client,
    "public override void SendSignal(int signalId, int matchIndex",
    "public override void RequestRematch(int secret, Action<bool> done)");
  assert.match(signal, /\\\"matchIndex\\\":/,
    "Signals must send matchIndex");

  const rematch = methodSlice(
    client,
    "public override void RequestRematch(int secret, int matchIndex",
    "public override void StartPolling");
  assert.match(rematch, /\\\"matchIndex\\\":/,
    "Rematch commitments must send matchIndex");
  assert.match(rematch, /ObserveReturnedMatchIndex\(resp\)/,
    "A successful rematch response must immediately advance the observed index");

  const leave = methodSlice(
    client,
    "void LeaveExactRoom(string code, int matchIndex)",
    "void ClearPendingRoomRequest");
  assert.match(leave, /\\\"matchIndex\\\":/,
    "Leave commands must send matchIndex");
  assert.match(leave, /ExecuteCloudScript\("leaveRoom"/,
    "Leave must remain server-authoritative");

  const acknowledgement = methodSlice(
    client,
    "public override void AcknowledgeResult(int matchIndex)",
    "IEnumerator Poll");
  assert.match(acknowledgement, /\\\"matchIndex\\\":/,
    "Result acknowledgements must send matchIndex");
});
