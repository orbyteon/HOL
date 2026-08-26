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

  const defaultSignal = methodSlice(
    client,
    "public override void SendSignal(int signalId, Action<bool> done)",
    "public override void SendSignal(int signalId, int matchIndex");
  assert.match(defaultSignal,
    /SendSignal\(signalId,\s*lastObservedMatchIndex,\s*done\)/,
    "The compatibility Signal entry point must use the last authoritative index");

  const signal = methodSlice(
    client,
    "public override void SendSignal(int signalId, int matchIndex",
    "public override void RequestRematch(int secret, Action<bool> done)");
  assert.match(signal, /\\\"matchIndex\\\":/,
    "Signals must send matchIndex");

  const defaultRematch = methodSlice(
    client,
    "public override void RequestRematch(int secret, Action<bool> done)",
    "public override void RequestRematch(int secret, int matchIndex");
  assert.match(defaultRematch,
    /RequestRematch\(secret,\s*lastObservedMatchIndex,\s*done\)/,
    "The controller-facing Rematch entry point must use the last authoritative index");

  const rematch = methodSlice(
    client,
    "public override void RequestRematch(int secret, int matchIndex",
    "public override void StartPolling");
  assert.match(rematch, /\\\"matchIndex\\\":/,
    "Rematch commitments must send matchIndex");
  assert.match(rematch, /ObserveReturnedMatchIndex\(resp\)/,
    "A successful rematch response must immediately advance the observed index");

  const deleteRoom = methodSlice(
    client,
    "public override void DeleteRoom()",
    "void LeaveExactRoom(string code, int matchIndex)");
  assert.match(deleteRoom, /lastObservedMatchIndex/,
    "Room release must be fenced by the last authoritative index");
  assert.match(deleteRoom, /LeaveExactRoom\(code,\s*matchIndex\)/,
    "DeleteRoom must pass its captured generation into leaveRoom");

  const leave = methodSlice(
    client,
    "void LeaveExactRoom(string code, int matchIndex)",
    "void ClearPendingRoomRequest");
  assert.match(leave, /\\\"matchIndex\\\":/,
    "Leave commands must send matchIndex");
  assert.match(leave, /ExecuteCloudScript\("leaveRoom"/,
    "Leave must remain server-authoritative");

  const defaultAcknowledgement = methodSlice(
    client,
    "public override void AcknowledgeResult()",
    "public override void AcknowledgeResult(int matchIndex)");
  assert.match(defaultAcknowledgement,
    /AcknowledgeResult\(lastObservedMatchIndex\)/,
    "The controller-facing result Ack must use the last authoritative index");

  const acknowledgement = methodSlice(
    client,
    "public override void AcknowledgeResult(int matchIndex)",
    "IEnumerator Poll");
  assert.match(acknowledgement, /\\\"matchIndex\\\":/,
    "Result acknowledgements must send matchIndex");

  const poll = methodSlice(
    client,
    "IEnumerator Poll(Action<RoomState> onState)",
    "// ------------------------------------------------ CloudScript state access");
  assert.match(poll, /lastObservedMatchIndex\s*=\s*state\.matchIndex/,
    "Every authoritative poll snapshot must refresh the tracked generation");

  assert.doesNotMatch(client, /catch\s*\{\s*\}/,
    "PlayFab state parsing must never fail silently");
});
