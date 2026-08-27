import test from "node:test";
import assert from "node:assert/strict";
import {
  loadCloudScript,
  startMatch,
  guess,
  midpointSolver,
} from "./cloudscript-harness.mjs";

function playToEnd(cs, roomId, view) {
  const solvers = { host: midpointSolver(), guest: midpointSolver() };

  for (let step = 0; step < 80 && view.phase === "play"; step++) {
    const side = view.turn;
    const value = solvers[side].next();
    const result = guess(cs, roomId, side, value);
    assert.equal(result.ok, true, `submitGuess rejected: ${result.error}`);
    view = cs.view(result);
    solvers[side].tell(value, view.lastHint);
  }

  assert.equal(view.phase, "done", "match failed to finish");
  return view;
}

test("stale match-scoped signal, leave, rematch and acknowledgement commands fail closed", () => {
  const cs = loadCloudScript();
  const started = startMatch(cs, { hostSecret: 42, guestSecret: 77 });
  const roomId = started.roomId;
  const firstResult = playToEnd(cs, roomId, started.state);

  assert.equal(firstResult.matchIndex, 0);
  const hostRematch = cs.call("requestRematch", "HOST", {
    roomId,
    secret: 11,
    matchIndex: firstResult.matchIndex,
  });
  assert.equal(hostRematch.ok, true);

  const secondMatch = cs.view(cs.call("requestRematch", "GUEST", {
    roomId,
    secret: 88,
    matchIndex: firstResult.matchIndex,
  }));
  assert.equal(secondMatch.phase, "play");
  assert.equal(secondMatch.matchIndex, 1);

  for (const args of [
    { roomId, signalId: 0, matchIndex: 0 },
    { roomId, signalId: 0 },
  ]) {
    const staleSignal = cs.call("sendSignal", "HOST", args);
    assert.equal(staleSignal.ok, false);
    assert.equal(staleSignal.error, "stale match");
  }

  for (const args of [
    { roomId, matchIndex: 0 },
    { roomId },
  ]) {
    const staleLeave = cs.call("leaveRoom", "HOST", args);
    assert.equal(staleLeave.ok, false);
    assert.equal(staleLeave.error, "stale match");
  }

  let liveState = cs.view(cs.call("getRoom", "HOST", { roomId }));
  assert.equal(liveState.phase, "play",
    "a stale leave callback must not close the active rematch");
  assert.equal(liveState.matchIndex, 1);

  const secondResult = playToEnd(cs, roomId, liveState);
  assert.equal(secondResult.matchIndex, 1);

  for (const args of [
    { roomId, secret: 22, matchIndex: 0 },
    { roomId, secret: 22 },
  ]) {
    const staleRematch = cs.call("requestRematch", "HOST", args);
    assert.equal(staleRematch.ok, false);
    assert.equal(staleRematch.error, "stale match");
  }

  for (const args of [
    { roomId, matchIndex: 0 },
    { roomId },
  ]) {
    const staleAck = cs.call("ackResult", "HOST", args);
    assert.equal(staleAck.ok, false);
    assert.equal(staleAck.error, "stale match");
  }

  const hostAck = cs.call("ackResult", "HOST", {
    roomId,
    matchIndex: secondResult.matchIndex,
  });
  assert.equal(hostAck.ok, true);
  assert.equal(hostAck.deleted, false);

  const guestAck = cs.call("ackResult", "GUEST", {
    roomId,
    matchIndex: secondResult.matchIndex,
  });
  assert.equal(guestAck.ok, true);
  assert.equal(guestAck.deleted, true);
  assert.equal(cs.store.groups.has(roomId), false);
});
